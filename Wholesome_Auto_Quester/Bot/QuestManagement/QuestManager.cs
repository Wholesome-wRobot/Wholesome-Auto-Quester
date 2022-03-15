using robotManager.Helpful;
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
            lock (_questManagerLock)
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
        }

        private void GetQuestsFromDB()
        {
            lock (_questManagerLock)
            {
                if (WholesomeAQSettings.CurrentSetting.GoToMobEntry != 0 || WholesomeAQSettings.CurrentSetting.GrindOnly)
                {
                    _questList.Clear();
                    _tracker.UpdateQuestsList(GuiQuestList);
                    return;
                }

                DBQueriesWotlk wotlkQueries = new DBQueriesWotlk();
                List<ModelQuestTemplate> dbQuestTemplates = wotlkQueries.GetAvailableQuests();

                // Remove quests that are not supposed to be here anymore
                List<IWAQQuest> questsToRemove = _questList.FindAll(quest => !dbQuestTemplates.Exists(dbQ => dbQ.Id ==  quest.QuestTemplate.Id));
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
                    UpdateStatuses();
                    break;
                case "QUEST_QUERY_COMPLETE":
                    UpdateCompletedQuests();
                    break;
                case "BAG_UPDATE":
                    CheckInventoryForQuestsGivenByItems();
                    break;
                case "PLAYER_LEVEL_UP":
                    if (ObjectManager.Me.Level < WholesomeAQSettings.CurrentSetting.StopAtLevel)
                    {
                        GetQuestsFromDB();
                    }
                    break;
                case "PLAYER_ENTERING_WORLD":
                    GetQuestsFromDB();
                    break;
            }
        }

        public List<IWAQTask> GetAllValidQuestTasks()
        {
            lock (_questManagerLock)
            {
                List<IWAQTask> allTasks = new List<IWAQTask>();
                foreach (IWAQQuest quest in _questList)
                {
                    allTasks.AddRange(quest.GetAllValidTasks());
                }
                return allTasks;
            }
        }

        public List<IWAQTask> GetAllInvalidQuestTasks()
        {
            lock (_questManagerLock)
            {
                List<IWAQTask> allTasks = new List<IWAQTask>();
                foreach (IWAQQuest quest in _questList)
                {
                    allTasks.AddRange(quest.GetAllInvalidTasks());
                }
                return allTasks;
            }
        }

        private void CheckInventoryForQuestsGivenByItems()
        {
            int itemFound = 0;
            foreach (int itemId in _itemsGivingQuest)
            {
                if (ItemsManager.HasItemById((uint)itemId))
                {
                    lock (_questManagerLock)
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

            lock (_questManagerLock)
            {
                // First loop on newly completed quests to ensure pickup unlocks
                foreach (IWAQQuest quest in _questList)
                {
                    if (quest.Status == QuestStatus.ToTurnIn && !logQuests.ContainsKey(quest.QuestTemplate.Id))
                    {
                        quest.ChangeStatusTo(QuestStatus.Completed);
                        continue;
                    }
                }

                // Update loop
                foreach (IWAQQuest quest in _questList)
                {
                    // DB conditions not met
                    if (!quest.AreDbConditionsMet)
                    {
                        quest.ChangeStatusTo(QuestStatus.DBConditionsNotMet);
                        continue;
                    }

                    // Quest blacklisted
                    if (quest.IsQuestBlackListed)
                    {
                        quest.ChangeStatusTo(QuestStatus.Blacklisted);
                        continue;
                    }

                    // Mark quest as completed if it's part of an exclusive group
                    if (quest.QuestTemplate.QuestAddon.ExclusiveGroup > 0 && !logQuests.ContainsKey(quest.QuestTemplate.Id))
                    {
                        if (quest.QuestTemplate.QuestAddon.ExclusiveQuests.Any(qId =>
                            qId != quest.QuestTemplate.Id
                            && (ToolBox.IsQuestCompleted(qId) || logQuests.ContainsKey(qId))))
                        {
                            quest.ChangeStatusTo(QuestStatus.Completed);
                            continue;
                        }
                    }

                    // Quest completed
                    if (ToolBox.IsQuestCompleted(quest.QuestTemplate.Id))
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

                ToolBox.UpdateObjectiveCompletionDict(_questList
                    .Where(quest => quest.Status == QuestStatus.InProgress)
                    .Select(quest => quest.QuestTemplate.Id).ToArray());

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

                _tracker.UpdateQuestsList(GuiQuestList);
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
                        _tracker.UpdateQuestsList(GuiQuestList);
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
                    MoveHelper.StopAllMove(true);
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
                    MoveHelper.StopAllMove(true);
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
            // Double check here, do we need all completed or just one?
            if (quest.QuestTemplate.PreviousQuestsIds.Count > 0
                && !quest.QuestTemplate.PreviousQuestsIds.Exists(id => ToolBox.IsQuestCompleted(id)))
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
            // HORDE
            if (ToolBox.IsHorde()) AddQuestToBlackList(4740, "Bugged, should only be alliance", false);
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
            AddQuestToBlackList(608, "The bloodsail bucaneers, too many mobs", false);
            AddQuestToBlackList(841, "Another power source?, repeatable", false);
            AddQuestToBlackList(2603, "Blasted Lands gather, bugged", false);
            AddQuestToBlackList(2581, "Blasted Lands gather, bugged", false);
            AddQuestToBlackList(2583, "Blasted Lands gather, bugged", false);
            AddQuestToBlackList(2585, "Blasted Lands gather, bugged", false);
            AddQuestToBlackList(3561, "Delivery to Xylem, Unreachable", false);
            AddQuestToBlackList(4293, "Ooze sample, DB bugged", false);
            AddQuestToBlackList(4494, "March of the Silithid, DB bugged", false);
            AddQuestToBlackList(4496, "Bungle in the Jungle, DB bugged", false);
            AddQuestToBlackList(5021, "Better late than ever, Too many mobs", false);
            AddQuestToBlackList(10103, "Report to Zurai, unreachable (top of tower)", false);
            AddQuestToBlackList(10286, "Arelion's secret, requires bubble talk", false);
            AddQuestToBlackList(9785, "Blessings of the Ancients, requires bubble talk", false);
            AddQuestToBlackList(11039, "Report to spymaster Thalodien, Orange npc", false);
            AddQuestToBlackList(11564, "Succulent orca stew, Underwater", false);
            AddQuestToBlackList(11569, "Keymaster Urmgrgl, Unreachable cave", false);
            AddQuestToBlackList(11900, "Reading the meters, The nexus", false);
            AddQuestToBlackList(11912, "Nuts for Berries, The nexus", false);
            AddQuestToBlackList(11918, "Basic Training, The nexus", false);
            AddQuestToBlackList(11910, "Secrets of the Ancients, The nexus", false);
            AddQuestToBlackList(12218, "Spread the good word, object use", false);
            AddQuestToBlackList(12230, "Stealing from the Siegesmith, unavailable", false);
            AddQuestToBlackList(12234, "Need to know, unavailable", false);
            AddQuestToBlackList(12447, "The Obsidian Dragonshire, top of Wyrmrest Temple", false);
            AddQuestToBlackList(12458, "Seeds of the Lashers, top of Wyrmrest Temple", false);
            AddQuestToBlackList(13242, "Darkness stirs, NPC absent", false);
            AddQuestToBlackList(13986, "An injured colleague, Dalaran", false);
            AddQuestToBlackList(12791, "The magical Kingdom of Dalaran, Dalaran", false);
            AddQuestToBlackList(12790, "Learning to leave and return, portal to dalaran", false);
            AddQuestToBlackList(12521, "Where in the world is Hemet Nesingway, Dalaran", false);
            AddQuestToBlackList(12853, "Luxurious Getaway, Dalaran", false);
            AddQuestToBlackList(13419, "Preparation for war, Dalaran", false);
            AddQuestToBlackList(12695, "Return of the friendly dryskin", false);
            AddQuestToBlackList(12534, "The Sapphire queen", false);
            AddQuestToBlackList(12533, "The wasp's hunter", false);
            AddQuestToBlackList(13135, "It Could Kill Us All, NPC absent", false);
            AddQuestToBlackList(12966, "You Can't Miss Him, NPC absent", false);
            AddQuestToBlackList(12966, "You Can't Miss Him, NPC absent", false);
            AddQuestToBlackList(12895, "The missing bronzebeard, unreachable", false);
            AddQuestToBlackList(12882, "Ancient relics, unreachable", false);
            AddQuestToBlackList(13426, "Xarantaur, unreachable", false);
            AddQuestToBlackList(13054, "The Missing Tracker, unreachable", false);
            AddQuestToBlackList(12992, "Crush dem Vrykuls, unreachable", false);
            AddQuestToBlackList(12806, "To the rise with all haste, unreachable", false);
            AddQuestToBlackList(13106, "Blackwatch, unreachable", false);
            AddQuestToBlackList(13169, "Un undead's best friend, unreachable", false);
            AddQuestToBlackList(13170, "Honor is for the weak, unreachable", false);
            AddQuestToBlackList(13171, "From whence they came, unreachable", false);
            AddQuestToBlackList(13084, "Vandalizing Jotunheim, unreachable", false);
            AddQuestToBlackList(13140, "The runesmisths of Malykriss, unreachable", false);
            // ALLIANCE
            AddQuestToBlackList(168, "Collecting memories, too many NPCS", false);
            AddQuestToBlackList(167, "Oh brother, too many NPCS", false);
            AddQuestToBlackList(128, "Blackrock Bounty, too many NPCS", false);
            AddQuestToBlackList(465, "Nek'Rosh's Gambit, bugged", false);
            AddQuestToBlackList(565, "Bartolo's Yeti Fur CLoak, Requires bought items", false);
            AddQuestToBlackList(664, "Drown Sorrows, underwater", false);
            AddQuestToBlackList(576, "Keep an eye out, too many NPCS", false);
            AddQuestToBlackList(574, "Special forces, bugged", false);
            AddQuestToBlackList(1190, "Keeping pace, NPC absent", false);
            AddQuestToBlackList(734, "This is going to be hard, no gossip", false);
            AddQuestToBlackList(1119, "Zanzil's mixture..., no gossip", false);
            AddQuestToBlackList(4493, "March of the Silithid, no gossip", false);
            AddQuestToBlackList(685, "Wanted! Otto and Falconcrest, too many NPCS", false);
            AddQuestToBlackList(5401, "Argent Dawn Comission, auto C", false);
            AddQuestToBlackList(4103, "Salve via hunting, requires active item", false);
            AddQuestToBlackList(1126, "Hive in the Tower, No objective", false);


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

        private List<IWAQQuest> GuiQuestList
        {
            get
            {
                lock (_questManagerLock)
                {
                    Vector3 myPos = ObjectManager.Me.PositionWithoutType;
                    return _questList
                        .OrderBy(quest => quest.Status)
                        .ThenBy(quest =>
                        {
                            if (quest.QuestTemplate.CreatureQuestGivers.Count <= 0) return float.MaxValue;
                            return quest.GetClosestQuestGiverDistance(myPos);
                        }).ToList();
                }
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
    DBConditionsNotMet,
    Failed,
    None,
    Completed,
    Blacklisted
}
