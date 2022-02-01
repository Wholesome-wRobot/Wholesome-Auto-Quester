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
        private HashSet<int> _completeQuests;
        private List<int> _itemsGivingQuest = new List<int>();
        private readonly IWowObjectScanner _objectScanner;
        private readonly QuestsTrackerGUI _tracker;

        public List<IWAQQuest> Quests { get; } = new List<IWAQQuest>();

        public QuestManager(IWowObjectScanner objectScanner, QuestsTrackerGUI questTrackerGUI)
        {
            _tracker = questTrackerGUI;
            _objectScanner = objectScanner;
            Initialize();
        }

        private void RecordQuestsFromDB()
        {
            Quests.Clear();
            DBQueriesWotlk wotlkQueries = new DBQueriesWotlk();
            List<ModelQuestTemplate> dbQuestTemplates = wotlkQueries.GetAvailableQuests();

            foreach (ModelQuestTemplate qTemplate in dbQuestTemplates)
            {
                Quests.Add(new WAQQuest(qTemplate, _objectScanner));

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
            RecordQuestsFromDB();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += LuaEventHandler;
        }

        public void Dispose()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= LuaEventHandler;
        }

        private void LuaEventHandler(string eventid, List<string> args)
        {
            switch (eventid)
            {
                case "QUEST_LOG_UPDATE": UpdateStatuses(); break;
                case "BAG_UPDATE": CheckInventoryForQuestsGivenByItems(); break;
                case "PLAYER_LEVEL_UP": RecordQuestsFromDB(); break;
            }
        }

        public List<IWAQTask> GetAllQuestTasks()
        {
            List<IWAQTask> allTasks = new List<IWAQTask>();
            foreach (IWAQQuest quest in Quests)
            {
                allTasks.AddRange(quest.GetAllTasks());
            }

            return allTasks;
        }

        /*public IWAQQuest GetQuestByTask(IWAQTask taskToSearch)
        {
            foreach (IWAQQuest quest in _questList)
            {
                foreach (IWAQTask task in quest.GetAllTasks())
                {
                    if (task == taskToSearch)
                    {
                        return quest;
                    }
                }
            }

            throw new System.Exception($"Tries to find the quest associated with the task {taskToSearch.TaskName} but it didn't exist");
        }*/

        private void CheckInventoryForQuestsGivenByItems()
        {
            foreach (int itemId in _itemsGivingQuest)
            {
                if (ItemsManager.HasItemById((uint)itemId))
                {
                    IWAQQuest questToPickup = Quests.Find(quest => quest.QuestTemplate.StartItem == itemId && quest.Status == QuestStatus.Unchecked);
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
            UpdateCompletedQuests();

            foreach (IWAQQuest quest in Quests)
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
                if (quest.IsCompleted)
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
                    if (!quest.AreObjectivesRecorded && quest.GetAllObjectives().Count > 0)
                    {
                        quest.RecordObjectiveIndices();
                    }
                    continue;
                }

                quest.ChangeStatusTo(QuestStatus.None);
            }

            // Second loop for unfit quests in the log
            if (WholesomeAQSettings.CurrentSetting.AbandonUnfitQuests)
            {
                foreach (KeyValuePair<int, Quest.PlayerQuest> logQuest in logQuests)
                {
                    IWAQQuest waqQuest = Quests.Find(q => q.QuestTemplate.Id == logQuest.Key);
                    if (waqQuest == null)
                    {
                        AbandonQuest(logQuest.Key, "Quest not in our DB list");
                    }
                    else
                    {
                        if (logQuest.Value.State == StateFlag.None && waqQuest.GetAllObjectives().Count <= 0)
                        {
                            BlacklistHelper.AddQuestToBlackList(waqQuest.QuestTemplate.Id, "In progress with no objectives");
                            AbandonQuest(waqQuest.QuestTemplate.Id, "In progress with no objectives");
                            continue;
                        }
                        if (waqQuest.QuestTemplate.QuestLevel < ObjectManager.Me.Level - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus - 1)
                        {
                            BlacklistHelper.AddQuestToBlackList(waqQuest.QuestTemplate.Id, "Underleveled");
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

            _tracker.UpdateQuestsList(Quests);
        }
        
        private void UpdateCompletedQuests()
        {
            List<int> completedQuests = new List<int>();
            completedQuests.AddRange(Quest.FinishedQuestSet);
            completedQuests.AddRange(WholesomeAQSettings.CurrentSetting.ListCompletedQuests);
            _completeQuests = completedQuests.Distinct().ToHashSet();
            bool shouldSave = false;
            foreach (int questId in _completeQuests)
            {
                if (!WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Contains(questId))
                {
                    WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Add(questId);
                    Logger.Log($"Saved quest {questId} as completed");
                    shouldSave = true;
                }
            }
            if (shouldSave)
            {
                WholesomeAQSettings.CurrentSetting.Save();
            }
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
