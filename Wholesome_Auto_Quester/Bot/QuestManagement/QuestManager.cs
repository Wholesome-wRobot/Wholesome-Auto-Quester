using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.Quest.PlayerQuest;

namespace Wholesome_Auto_Quester.Bot.QuestManagement
{
    public class QuestManager : IQuestManager
    {
        private readonly List<int> _itemsGivingQuest = new List<int>();
        private readonly IWowObjectScanner _objectScanner;
        private readonly QuestsTrackerGUI _tracker;
        private readonly List<IWAQQuest> _questList = new List<IWAQQuest>();
        private readonly object _questManagerLock = new object();

        public QuestManager(IWowObjectScanner objectScanner, QuestsTrackerGUI questTrackerGUI)
        {
            questTrackerGUI.Initialize(this);
            _tracker = questTrackerGUI;
            _objectScanner = objectScanner;
            Initialize();
        }

        private void RemoveAllQuests(List<IWAQQuest> questsToRemove)
        {
            foreach (IWAQQuest questToRemove in questsToRemove)
            {
                foreach (IWAQTask taskToUnRegister in questToRemove.GetAllTasks())
                {
                    taskToUnRegister.UnregisterEntryToScanner(_objectScanner);
                }
                _questList.Remove(questToRemove);
            }
        }

        private void GetQuestsFromDB()
        {
            lock (_questManagerLock)
            {
                if (WholesomeAQSettings.CurrentSetting.GoToMobEntry != 0 || WholesomeAQSettings.CurrentSetting.GrindOnly)
                {
                    _questList.Clear();
                    _tracker.UpdateQuestsList(_questList);
                    return;
                }

                DBQueriesWotlk wotlkQueries = new DBQueriesWotlk();
                List<ModelQuestTemplate> dbQuestTemplates = wotlkQueries.GetAvailableQuests();

                // Remove quests that are not supposed to be here anymore
                List<IWAQQuest> questsToRemove = _questList.FindAll(quest => !dbQuestTemplates.Contains(quest.QuestTemplate));
                RemoveAllQuests(questsToRemove);

                // Add quests if they don't already exist
                foreach (ModelQuestTemplate qTemplate in dbQuestTemplates)
                {
                    if (!_questList.Exists(quest => quest.QuestTemplate.Id == qTemplate.Id))
                    {
                        _questList.Add(new WAQQuest(qTemplate, _objectScanner));
                    }

                    // Quest started by item
                    if (qTemplate.StartItemTemplate?.startquest > 0
                        && qTemplate.Id == qTemplate.StartItemTemplate?.startquest
                        && !Quest.HasQuest(qTemplate.Id)
                        && !_itemsGivingQuest.Contains(qTemplate.StartItemTemplate.Entry))
                    {
                        _itemsGivingQuest.Add(qTemplate.StartItemTemplate.Entry);
                    }
                }

                UpdateStatuses();
            }
        }

        public void Initialize()
        {
            InitializeWAQSettings();
            GetQuestsFromDB();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += LuaEventHandler;
        }

        public void Dispose()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= LuaEventHandler;
            lock (_questManagerLock)
            {
                _questList.Clear();
            }
        }

        private void LuaEventHandler(string eventid, List<string> args)
        {
            switch (eventid)
            {
                case "QUEST_LOG_UPDATE":
                    Logger.LogDebug("QUEST_LOG_UPDATE");
                    lock (_questManagerLock)
                    {
                        UpdateStatuses();
                    }
                    break;
                case "QUEST_QUERY_COMPLETE":
                    UpdateCompletedQuests();
                    break;
                case "BAG_UPDATE":
                    CheckInventoryForQuestsGivenByItems();
                    break;
                case "PLAYER_LEVEL_UP":
                    Logger.LogDebug("PLAYER_LEVEL_UP");
                    GetQuestsFromDB();
                    break;
            }
        }

        public List<IWAQTask> GetAllQuestTasks()
        {
            List<IWAQTask> allTasks = new List<IWAQTask>();
            foreach (IWAQQuest quest in _questList)
            {
                allTasks.AddRange(quest.GetAllTasks());
            }

            allTasks.RemoveAll(task => task.WorldMapArea == null);

            return allTasks;
        }

        private void CheckInventoryForQuestsGivenByItems()
        {
            int itemFound = 0;
            foreach (int itemId in _itemsGivingQuest)
            {
                if (ItemsManager.HasItemById((uint)itemId))
                {
                    IWAQQuest questToPickup = _questList.Find(quest => quest.QuestTemplate.StartItem == itemId && quest.Status == QuestStatus.ToPickup);
                    if (questToPickup != null)
                    {
                        Logger.Log($"Starting {questToPickup.QuestTemplate.LogTitle} from {questToPickup.QuestTemplate.StartItemTemplate.Name}");
                        ToolBox.PickupQuestFromBagItem(questToPickup.QuestTemplate.StartItemTemplate.Name);
                        itemFound = itemId;
                        break;
                    }
                    else
                    {
                        throw new System.Exception($"Couldn't find quest associated with item {itemId}");
                    }
                }
            }
            if (itemFound > 0)
            {
                _itemsGivingQuest.Remove(itemFound);
            }
        }

        private void UpdateStatuses()
        {
            Dictionary<int, Quest.PlayerQuest> logQuests = Quest.GetLogQuestId().ToDictionary(quest => quest.ID);
            List<string> itemsToAddToDNSList = new List<string>();
            ToolBox.UpdateObjectiveCompletionDict(_questList.Select(quest => quest.QuestTemplate.Id).ToArray());

            foreach (IWAQQuest quest in _questList)
            {
                // Quest blacklisted
                if (quest.IsQuestBlackListed)
                {
                    quest.ChangeStatusTo(QuestStatus.Blacklisted);
                    continue;
                }

                // Mark quest as completed if it's part of an exclusive group
                if (quest.QuestTemplate.QuestAddon.ExclusiveGroup > 0)
                {
                    if (quest.QuestTemplate.QuestAddon.ExclusiveQuests.Any(qId => qId != quest.QuestTemplate.Id
                        && (ToolBox.IsQuestCompleted(qId) || logQuests.ContainsKey(qId))))
                    {
                        quest.ChangeStatusTo(QuestStatus.Completed);
                        continue;
                    }
                }

                // Quest completed
                if (ToolBox.IsQuestCompleted(quest.QuestTemplate.Id)
                    || quest.Status == QuestStatus.ToTurnIn && !logQuests.ContainsKey(quest.QuestTemplate.Id))
                {
                    quest.ChangeStatusTo(QuestStatus.Completed);
                    continue;
                }

                // Quest to pickup
                if (IsQuestPickable(quest) && !logQuests.ContainsKey(quest.QuestTemplate.Id))
                {
                    itemsToAddToDNSList.AddRange(GetItemsStringsList(quest));
                    quest.ChangeStatusTo(QuestStatus.ToPickup);
                    continue;
                }

                // Log quests
                if (logQuests.TryGetValue(quest.QuestTemplate.Id, out Quest.PlayerQuest foundQuest))
                {
                    // Quest to turn in
                    if (foundQuest.State == StateFlag.Complete)
                    {
                        itemsToAddToDNSList.AddRange(GetItemsStringsList(quest));
                        quest.ChangeStatusTo(QuestStatus.ToTurnIn);
                        continue;
                    }

                    // Quest failed
                    if (foundQuest.State == StateFlag.Failed)
                    {
                        quest.ChangeStatusTo(QuestStatus.Failed);
                        continue;
                    }

                    // Quest in progress
                    quest.ChangeStatusTo(QuestStatus.InProgress);

                    itemsToAddToDNSList.AddRange(GetItemsStringsList(quest));
                    continue;
                }

                quest.ChangeStatusTo(QuestStatus.None);
            }

            // loop for clearing up finished objectives
            foreach (IWAQQuest quest in _questList)
            {
                quest.CheckForFinishedObjectives();
            }

            // Second loop for unfit quests in the log
            if (WholesomeAQSettings.CurrentSetting.AbandonUnfitQuests)
            {
                foreach (KeyValuePair<int, Quest.PlayerQuest> logQuest in logQuests)
                {
                    IWAQQuest waqQuest = _questList.Find(q => q.QuestTemplate.Id == logQuest.Key);
                    if (waqQuest == null)
                    {
                        AbandonQuest(logQuest.Key, "Quest not in our DB list");
                    }
                    else
                    {
                        if (logQuest.Value.State == StateFlag.Failed)
                        {
                            AddQuestToBlackList(waqQuest.QuestTemplate.Id, "Failed");
                            AbandonQuest(waqQuest.QuestTemplate.Id, "Failed");
                            continue;
                        }
                        if (logQuest.Value.State == StateFlag.None && waqQuest.GetAllObjectives().Count <= 0)
                        {
                            AddQuestToBlackList(waqQuest.QuestTemplate.Id, "In progress with no objectives");
                            AbandonQuest(waqQuest.QuestTemplate.Id, "In progress with no objectives");
                            continue;
                        }
                        if (waqQuest.QuestTemplate.QuestLevel < ObjectManager.Me.Level - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus - 1)
                        {
                            AddQuestToBlackList(waqQuest.QuestTemplate.Id, "Underleveled");
                            AbandonQuest(waqQuest.QuestTemplate.Id, "Underleveled");
                            continue;
                        }
                    }
                }
            }

            // WAQ Do Not Sell List
            int WAQlistStartIndex = wManagerSetting.CurrentSetting.DoNotSellList.IndexOf("WAQStart");
            int WAQlistEndIndex = wManagerSetting.CurrentSetting.DoNotSellList.IndexOf("WAQEnd");
            int WAQListLength = WAQlistEndIndex - WAQlistStartIndex - 1;
            List<string> initialWAQList = wManagerSetting.CurrentSetting.DoNotSellList.GetRange(WAQlistStartIndex + 1, WAQListLength);
            if (!initialWAQList.SequenceEqual(itemsToAddToDNSList))
            {
                foreach (string item in initialWAQList)
                {
                    if (!itemsToAddToDNSList.Contains(item))
                        Logger.LogDebug($"Removed {item} from Do Not Sell List");
                }
                foreach (string item in itemsToAddToDNSList)
                {
                    if (!initialWAQList.Contains(item))
                        Logger.LogDebug($"Added {item} to Do Not Sell List");
                }
                wManagerSetting.CurrentSetting.DoNotSellList.RemoveRange(WAQlistStartIndex + 1, WAQListLength);
                wManagerSetting.CurrentSetting.DoNotSellList.InsertRange(WAQlistStartIndex + 1, itemsToAddToDNSList);
                wManagerSetting.CurrentSetting.Save();
            }

            _tracker.UpdateQuestsList(_questList);
        }

        private void AbandonQuest(int questId, string reason)
        {
            Logger.Log($"Abandonning quest {questId} ({reason})");
            int logIndex = Lua.LuaDoString<int>(@$"
                local nbLogQuests  = GetNumQuestLogEntries()
                for i=1, nbLogQuests do
                    local _, _, _, _, _, _, _, _, questID = GetQuestLogTitle(i);
                    if questID == {questId} then
                        return i;
                    end
                end
            ");
            Lua.LuaDoString($"SelectQuestLogEntry({logIndex}); SetAbandonQuest(); AbandonQuest();");
            Thread.Sleep(500);
        }

        private void UpdateCompletedQuests()
        {
            lock (_questManagerLock)
            {
                if (Quest.FinishedQuestSet.Count > 0)
                {
                    List<int> questsSavedFromServer = new List<int>();
                    foreach (int questId in Quest.FinishedQuestSet)
                    {
                        if (ToolBox.SaveQuestAsCompleted(questId))
                        {
                            questsSavedFromServer.Add(questId);
                        }
                    }

                    if (questsSavedFromServer.Count > 0)
                    {
                        List<IWAQQuest> questsToRemove = _questList.FindAll(quest => questsSavedFromServer.Contains(quest.QuestTemplate.Id));
                        RemoveAllQuests(questsToRemove);
                        _tracker.UpdateQuestsList(_questList);
                        UpdateStatuses();
                        WholesomeAQSettings.CurrentSetting.Save();
                    }
                    return;
                }
                Logger.LogError($"Server has not sent our quests yet");
            }
        }

        public void AddQuestToBlackList(int questId, string reason, bool triggerStatusUpdate = true)
        {
            lock (_questManagerLock)
            {
                if (!WholesomeAQSettings.CurrentSetting.BlackListedQuests.Exists(blq => blq.Id == questId))
                {
                    WholesomeAQSettings.CurrentSetting.BlackListedQuests.Add(new BlackListedQuest(questId, reason));
                    WholesomeAQSettings.CurrentSetting.Save();
                    Logger.Log($"The quest {questId} has been blacklisted ({reason})");
                    if (triggerStatusUpdate)
                    {
                        UpdateStatuses();
                    }
                }
            }
        }

        public void RemoveQuestFromBlackList(int questId, string reason, bool triggerStatusUpdate = true)
        {
            lock (_questManagerLock)
            {
                BlackListedQuest questToRemove = WholesomeAQSettings.CurrentSetting.BlackListedQuests.Find(blq => blq.Id == questId);
                if (questToRemove.Id != 0)
                {
                    WholesomeAQSettings.CurrentSetting.BlackListedQuests.Remove(questToRemove);
                    WholesomeAQSettings.CurrentSetting.Save();
                    Logger.Log($"The quest {questId} has been removed from the blacklist ({reason})");
                    if (triggerStatusUpdate)
                    {
                        UpdateStatuses();
                    }
                }
            }
        }

        private bool IsQuestPickable(IWAQQuest quest)
        {
            if (quest.QuestTemplate.PreviousQuestsIds.Count > 0
                && quest.QuestTemplate.PreviousQuestsIds.Any(id => !ToolBox.IsQuestCompleted(id)))
            {
                return false;
            }

            if (quest.QuestTemplate.QuestAddon.RequiredSkillID > 0
                && Skill.GetValue((SkillLine)quest.QuestTemplate.QuestAddon.RequiredSkillID) < quest.QuestTemplate.QuestAddon.RequiredSkillPoints)
            {
                return false;
            }

            return true;
        }

        private void InitializeWAQSettings()
        {
            AddQuestToBlackList(1202, "Theramore docks, runs through ally city", false);
            AddQuestToBlackList(863, "Ignition, bugged platform", false);
            AddQuestToBlackList(6383, "Ashenvale hunt, bugged", false);
            AddQuestToBlackList(891, "The Guns of NorthWatch, too many mobs", false);
            AddQuestToBlackList(9612, "A hearty thanks, requires heal on mob", false);
            AddQuestToBlackList(857, "The tear of the moons, way too many mobs", false);
            AddQuestToBlackList(520, "The Crown of Will, too many mobs", false);
            AddQuestToBlackList(1177, "Hungry!, way too many mobs", false);
            AddQuestToBlackList(8483, "A dwarven spy, gossip required", false);
            AddQuestToBlackList(629, "Vile Reef, underwater", false);
            AddQuestToBlackList(1107, "Encrusted tail fins, underwater", false);
            AddQuestToBlackList(662, "Deep sea salvage, underwater", false);
            AddQuestToBlackList(709, "Solution to doom, too many mobs", false);
            AddQuestToBlackList(2342, "Reclaimed treasures, too many mobs", false);
            if (ToolBox.IsHorde()) AddQuestToBlackList(4740, "Bugged, should only be alliance", false);

            if (!wManagerSetting.CurrentSetting.DoNotSellList.Contains("WAQStart") || !wManagerSetting.CurrentSetting.DoNotSellList.Contains("WAQEnd"))
            {
                wManagerSetting.CurrentSetting.DoNotSellList.Remove("WAQStart");
                wManagerSetting.CurrentSetting.DoNotSellList.Remove("WAQEnd");
                wManagerSetting.CurrentSetting.DoNotSellList.Add("WAQStart");
                wManagerSetting.CurrentSetting.DoNotSellList.Add("WAQEnd");
                wManagerSetting.CurrentSetting.Save();
            }
        }

        private List<string> GetItemsStringsList(IWAQQuest quest)
        {
            List<string> result = new List<string>();

            foreach (KillLootObjective klo in quest.QuestTemplate.KillLootObjectives)
            {
                if (!result.Contains(klo.ItemTemplate.Name))
                {
                    result.Add(klo.ItemTemplate.Name);
                }
            }

            foreach (GatherObjective go in quest.QuestTemplate.GatherObjectives)
            {
                if (!result.Contains(go.ItemTemplate.Name))
                {
                    result.Add(go.ItemTemplate.Name);
                }
            }

            return result;
        }
    }
}

public enum QuestStatus
{
    Unchecked,
    ToTurnIn,
    InProgress,
    ToPickup,
    Failed,
    None,
    Completed,
    Blacklisted
}
