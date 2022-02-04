using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.Quest.PlayerQuest;

namespace Wholesome_Auto_Quester.Bot.QuestManagement
{
    public class QuestManager : IQuestManager
    {
        private List<int> _itemsGivingQuest = new List<int>();
        private readonly IWowObjectScanner _objectScanner;
        private readonly QuestsTrackerGUI _tracker;
        private List<IWAQQuest> _questList = new List<IWAQQuest>();

        public QuestManager(IWowObjectScanner objectScanner, QuestsTrackerGUI questTrackerGUI)
        {
            questTrackerGUI.Initialize(this);
            _tracker = questTrackerGUI;
            _objectScanner = objectScanner;
            Initialize();
        }

        private void GetQuestsFromDB()
        {
            if (WholesomeAQSettings.CurrentSetting.GoToMobEntry != 0 || WholesomeAQSettings.CurrentSetting.GrindOnly)
            {
                _questList.Clear();
                _tracker.UpdateQuestsList(_questList);
                return;
            }

            DBQueriesWotlk wotlkQueries = new DBQueriesWotlk();
            List<ModelQuestTemplate> dbQuestTemplates = wotlkQueries.GetAvailableQuests();

            _questList.RemoveAll(quest => !dbQuestTemplates.Exists(q => q.Id == quest.QuestTemplate.Id));

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

        public void Initialize()
        {
            InitializeWAQSettings();
            GetQuestsFromDB();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += LuaEventHandler;
        }

        public void Dispose()
        {
            _questList.Clear();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= LuaEventHandler;
        }

        private void LuaEventHandler(string eventid, List<string> args)
        {
            switch (eventid)
            {
                case "QUEST_LOG_UPDATE":
                    Logger.Log("QUEST_LOG_UPDATE");
                    UpdateStatuses();
                    break;
                // This event fires whenever the player accepts a quest.
                /*case "QUEST_ACCEPTED":
                    Logger.Log("QUEST_ACCEPTED");
                    args.ForEach((arg) => Logger.Log($"QUEST_ACCEPTED arg : {arg}"));
                    UpdateStatuses();
                    break;
                // Fired upon completion of a world quest, or turning in a quest with the "Complete Quest" button
                case "QUEST_TURNED_IN":
                    Logger.Log("QUEST_TURNED_IN");
                    args.ForEach((arg) => Logger.Log($"QUEST_TURNED_IN arg : {arg}"));
                    UpdateStatuses();
                    break;
                // Fired when the quest items are updated
                /*case "QUEST_ITEM_UPDATE": 
                    UpdateStatuses(); 
                    Logger.Log("QUEST_ITEM_UPDATE"); 
                    break;
                // Fired whenever the quest log changes. (Frequently, but not as frequently as QUEST_LOG_UPDATE)
                case "UNIT_QUEST_LOG_CHANGED":
                    UpdateStatuses(); 
                    Logger.Log("UNIT_QUEST_LOG_CHANGED"); 
                    break;
                // Fired after the player hits the "Continue" button in the quest-information page, before the "Complete Quest" button.
                case "QUEST_COMPLETE": 
                    UpdateStatuses(); 
                    Logger.Log("QUEST_COMPLETE"); 
                    break;
                // Fired whenever the quest frame changes (Detail to Progress to Reward, etc.) or is closed.
                case "QUEST_FINISHED": 
                    UpdateStatuses(); 
                    Logger.Log("QUEST_FINISHED"); 
                    break;*/

                case "QUEST_QUERY_COMPLETE":
                    UpdateCompletedQuests();
                    break;
                case "BAG_UPDATE":
                    CheckInventoryForQuestsGivenByItems();
                    break;
                case "PLAYER_LEVEL_UP":
                    Logger.Log("PLAYER_LEVEL_UP");
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

            return allTasks;
        }

        private void CheckInventoryForQuestsGivenByItems()
        {
            foreach (int itemId in _itemsGivingQuest)
            {
                if (ItemsManager.HasItemById((uint)itemId))
                {
                    IWAQQuest questToPickup = _questList.Find(quest => quest.QuestTemplate.StartItem == itemId && quest.Status == QuestStatus.Unchecked);
                    if (questToPickup != null)
                    {
                        Logger.Log($"Starting {questToPickup.QuestTemplate.LogTitle} from {questToPickup.QuestTemplate.StartItemTemplate.Name}");
                        ToolBox.PickupQuestFromBagItem(questToPickup.QuestTemplate.StartItemTemplate.Name);
                        _itemsGivingQuest.Remove(itemId);
                    }
                    else
                    {
                        throw new System.Exception($"Couldn't find quest associated with item {itemId}");
                    }
                }
            }
        }

        private void UpdateStatuses()
        {
            Dictionary<int, Quest.PlayerQuest> logQuests = Quest.GetLogQuestId().ToDictionary(quest => quest.ID);
            List<string> itemsToAddToDNSList = new List<string>();

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
                if (quest.IsCompleted || quest.Status == QuestStatus.ToTurnIn && !logQuests.ContainsKey(quest.QuestTemplate.Id))
                {
                    quest.ChangeStatusTo(QuestStatus.Completed);
                    continue;
                }

                // Quest to pickup
                if (quest.IsPickable && !logQuests.ContainsKey(quest.QuestTemplate.Id))
                {
                    itemsToAddToDNSList.AddRange(quest.GetItemsStringsList());
                    quest.ChangeStatusTo(QuestStatus.ToPickup);
                    continue;
                }

                // Log quests
                if (logQuests.TryGetValue(quest.QuestTemplate.Id, out Quest.PlayerQuest foundQuest))
                {
                    // Quest to turn in
                    if (foundQuest.State == StateFlag.Complete)
                    {
                        itemsToAddToDNSList.AddRange(quest.GetItemsStringsList());
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

                    itemsToAddToDNSList.AddRange(quest.GetItemsStringsList());
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
                        Logger.Log($"Removed {item} from Do Not Sell List");
                }
                foreach (string item in itemsToAddToDNSList)
                {
                    if (!initialWAQList.Contains(item))
                        Logger.Log($"Added {item} to Do Not Sell List");
                }
                wManagerSetting.CurrentSetting.DoNotSellList.RemoveRange(WAQlistStartIndex + 1, WAQListLength);
                wManagerSetting.CurrentSetting.DoNotSellList.InsertRange(WAQlistStartIndex + 1, itemsToAddToDNSList);
                wManagerSetting.CurrentSetting.Save();
            }

            _tracker.UpdateQuestsList(_questList);
        }

        private void UpdateCompletedQuests()
        {
            if (Quest.FinishedQuestSet.Count > 0)
            {
                bool shouldSave = false;
                foreach (int questId in Quest.FinishedQuestSet)
                {
                    if (ToolBox.SaveQuestAsCompleted(questId))
                    {
                        shouldSave = true;
                    }
                }
                if (shouldSave)
                {
                    WholesomeAQSettings.CurrentSetting.Save();
                }
                return;
            }
            Logger.LogError($"Server has not sent our quests yet");
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

        public void AddQuestToBlackList(int questId, string reason, bool triggerStatusUpdate = true)
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

        public void RemoveQuestFromBlackList(int questId, string reason, bool triggerStatusUpdate = true)
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

        private void InitializeWAQSettings()
        {
            AddQuestToBlackList(1202, "Theramore docks, runs through ally city", false);
            AddQuestToBlackList(863, "Ignition, bugged platform", false);
            AddQuestToBlackList(6383, "Ashenvale hunt, bugged", false);
            AddQuestToBlackList(891, "The Guns of NorthWatch, too many mobs", false);
            AddQuestToBlackList(9612, "A hearty thanks, requires heal on mob", false);
            AddQuestToBlackList(857, "The tear of the moons, way too many mobs", false);
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
