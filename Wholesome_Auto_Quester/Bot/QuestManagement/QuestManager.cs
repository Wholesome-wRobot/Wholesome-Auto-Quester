using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Bot.JSONManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.Quest.PlayerQuest;
using Timer = robotManager.Helpful.Timer;

namespace Wholesome_Auto_Quester.Bot.QuestManagement
{
    public class QuestManager : IQuestManager
    {
        private readonly IContinentManager _continentManager;
        private readonly IJSONManager _jSONManager;
        private readonly List<int> _itemsGivingQuest = new List<int>();
        private readonly IWowObjectScanner _objectScanner;
        private readonly QuestsTrackerGUI _tracker;
        private readonly List<IWAQQuest> _questList = new List<IWAQQuest>();
        private readonly object _questManagerLock = new object();
        private Timer _itemCheckTimer = new Timer();

        public QuestManager(
            IWowObjectScanner objectScanner, 
            QuestsTrackerGUI questTrackerGUI, 
            IJSONManager jSONManager,
            IContinentManager continentManager)
        {
            questTrackerGUI.Initialize(this);
            _tracker = questTrackerGUI;
            _objectScanner = objectScanner;
            _jSONManager = jSONManager;
            _continentManager = continentManager;
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
                if (WholesomeAQSettings.CurrentSetting.GrindOnly)
                {
                    _questList.Clear();
                    _tracker.UpdateQuestsList(GuiQuestList);
                    return;
                }

                List<ModelQuestTemplate> dbQuestTemplates = _jSONManager.GetAvailableQuestsFromJSON();

                // Remove quests that are not supposed to be here anymore
                List<IWAQQuest> questsToRemove = _questList.FindAll(quest => !dbQuestTemplates.Exists(dbQ => dbQ.Id == quest.QuestTemplate.Id));
                RemoveAllQuests(questsToRemove);

                // Add quests if they don't already exist
                foreach (ModelQuestTemplate qTemplate in dbQuestTemplates)
                {
                    if (!_questList.Exists(quest => quest.QuestTemplate.Id == qTemplate.Id))
                    {
                        _questList.Add(new WAQQuest(qTemplate, _objectScanner, _continentManager));
                    }

                    // Quest started by item
                    if (qTemplate.StartItemTemplate != null 
                        && qTemplate.StartItemTemplate.startquest > 0
                        && qTemplate.Id == qTemplate.StartItemTemplate.startquest
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
                    CheckInventory();
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

        private void CheckInventory()
        {
            if (!_itemCheckTimer.IsReady)
            {
                return;
            }
            _itemCheckTimer = new Timer(1000);

            lock (_questManagerLock)
            {
                List<WoWItem> bagItems = Bag.GetBagItem();

                // Get quest items from DoNotSellList
                List<string> dnsList = wManagerSetting.CurrentSetting.DoNotSellList;
                int WAQlistStartIndex = dnsList.IndexOf("WAQStart");
                int WAQlistEndIndex = dnsList.IndexOf("WAQEnd");
                int WAQListLength = WAQlistEndIndex - WAQlistStartIndex - 1;
                List<string> listQuestItems = dnsList.GetRange(WAQlistStartIndex + 1, WAQListLength);

                // Check for deprecated quest items
                foreach (WoWItem item in bagItems)
                {
                    if (item.GetItemInfo.ItemType == "Quest"
                        && item.GetItemInfo.ItemSubType == "Quest"
                        && !listQuestItems.Contains(item.Name)
                        && !_itemsGivingQuest.Contains(item.Entry))
                    {
                        Logger.Log($"Deleting item {item.Name} because it's a deprecated quest item");
                        WTItem.DeleteItemByName(item.Name);
                        Thread.Sleep(300);
                    }
                }

                // Check items that give quests
                int itemFound = 0;
                foreach (int itemId in _itemsGivingQuest)
                {
                    if (bagItems.Exists(item => item.Entry == itemId))
                    {
                        IWAQQuest questToPickup = _questList
                            .Find(quest =>
                                quest.QuestTemplate.StartItemTemplate != null
                                && quest.QuestTemplate.StartItemTemplate.Entry == itemId
                                && quest.Status == QuestStatus.ToPickup);
                        if (questToPickup != null)
                        {
                            Logger.Log($"Starting {questToPickup.QuestTemplate.LogTitle} from {questToPickup.QuestTemplate.StartItemTemplate.Name}");
                            WTItem.PickupQuestFromBagItem(questToPickup.QuestTemplate.StartItemTemplate.Name);
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
                    if (quest.QuestTemplate.QuestAddon?.ExclusiveGroup > 0 && !logQuests.ContainsKey(quest.QuestTemplate.Id))
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
                    if (logQuests.TryGetValue(quest.QuestTemplate.Id, out Quest.PlayerQuest logQuest))
                    {
                        // Quest to turn in
                        if (logQuest.State == StateFlag.Complete)
                        {
                            itemsToAddToDNSList.AddRange(GetItemsStringsList(quest));
                            quest.ChangeStatusTo(QuestStatus.ToTurnIn);
                            continue;
                        }

                        // Quest failed
                        if (logQuest.State == StateFlag.Failed)
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
                                AbandonQuest(waqQuest.QuestTemplate.Id, "Failed");
                                AddQuestToBlackList(waqQuest.QuestTemplate.Id, "Failed");
                                continue;
                            }
                            if (logQuest.Value.State == StateFlag.None && waqQuest.GetAllObjectives().Count <= 0)
                            {
                                AbandonQuest(waqQuest.QuestTemplate.Id, "In progress with no objectives");
                                AddQuestToBlackList(waqQuest.QuestTemplate.Id, "In progress with no objectives");
                                continue;
                            }
                            if (waqQuest.QuestTemplate.QuestLevel < ObjectManager.Me.Level - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus - 1)
                            {
                                AbandonQuest(waqQuest.QuestTemplate.Id, "Underleveled");
                                AddQuestToBlackList(waqQuest.QuestTemplate.Id, "Underleveled");
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
            WTQuestLog.AbandonQuest(questId);
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

            if (quest.QuestTemplate.QuestAddon?.RequiredSkillID > 0
                && Skill.GetValue((SkillLine)quest.QuestTemplate.QuestAddon.RequiredSkillID) < quest.QuestTemplate.QuestAddon.RequiredSkillPoints)
            {
                return false;
            }

            return true;
        }

        private void InitializeWAQSettings()
        {
            // HORDE
            if (WTPlayer.IsHorde()) AddQuestToBlackList(4740, "Bugged, should only be alliance", false);
            if (WTPlayer.IsHorde()) AddQuestToBlackList(3741, "Hilary's Necklace, should only be alliance", false);
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
            AddQuestToBlackList(962, "Serpentbloom, instance", false);
            AddQuestToBlackList(17, "Uldaman reagent run, too many npcs", false);
            AddQuestToBlackList(1360, "Reclaimed treasures, too many npcs", false);
            AddQuestToBlackList(450, "A recipe for death, too many npcs", false);

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
            AddQuestToBlackList(9936, "Giselda the crone, too many mobs", false);
            AddQuestToBlackList(10412, "Firewing signets, too many mobs", false);
            AddQuestToBlackList(10516, "Trappings of a vindicator, requires item interaction", false);
            AddQuestToBlackList(549, "WANTED: Syndicate personel, too many NPCS", false);
            AddQuestToBlackList(10315, "Neutralizing the Nethermancers, too many NPCS", false);
            AddQuestToBlackList(10678, "The main course!, NPC in poison", false);
            AddQuestToBlackList(11508, "Grezzix Spindlesnap, on boat", false);
            AddQuestToBlackList(11625, "Trident of Naz'Jan, unreachable", false);
            AddQuestToBlackList(14409, "A cautious return, Dalaran", false);
            AddQuestToBlackList(13347, "Reborn from the ashes, NPC absent", false);
            AddQuestToBlackList(12443, "Seeking solvent, too many NPCS", false);
            AddQuestToBlackList(12796, "The magical Kingdom of Dalaran, Dalaran", false);
            AddQuestToBlackList(12462, "Breaking off a piece, too many NPCS", false);
            AddQuestToBlackList(12819, "Just around the corner, mine field", false);
            AddQuestToBlackList(12844, "Equipment recovery, north of northrend", false);
            AddQuestToBlackList(12870, "Ancient relics, north of northrend", false);
            AddQuestToBlackList(12863, "Offering thanks, north of northrend", false);
            AddQuestToBlackList(12854, "On Brann's Tail, north of northrend", false);
            AddQuestToBlackList(12876, "Unwelcome guests, north of northrend", false);
            AddQuestToBlackList(13418, "Preparation of war, Dalaran", false);
            AddQuestToBlackList(312, "Tundra stolen stash, elite mob", false);
            AddQuestToBlackList(303, "Dark iron War, Too many mobs", false);
            AddQuestToBlackList(304, "A grim task, Too many mobs", false);
            AddQuestToBlackList(663, "Land ho!, no objective", false);


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

            if (quest.QuestTemplate.StartItemTemplate != null)
            {
                result.Add(quest.QuestTemplate.StartItemTemplate.Name);
            }

            foreach (KillLootObjective klo in quest.QuestTemplate.KillLootObjectives)
            {
                if (!result.Contains(klo.ItemTemplate.Name))
                {
                    result.Add(klo.ItemTemplate.Name);
                }
            }

            foreach (KillLootObjective klo in quest.QuestTemplate.PrerequisiteLootObjectives)
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

            foreach (GatherObjective go in quest.QuestTemplate.PrerequisiteGatherObjectives)
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
