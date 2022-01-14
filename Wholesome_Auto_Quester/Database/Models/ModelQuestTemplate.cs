using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelQuestTemplate
    {
        public ModelQuestTemplateAddon QuestAddon { get; set; }
        public QuestStatus Status { get; set; } = QuestStatus.None;
        public bool AreObjectivesRecorded { get; set; }

        private List<string> _questFlags;
        public List<string> QuestFlags
        {
            get
            {
                if (_questFlags == null) _questFlags = GetMatchingQuestFlags(Flags);
                return _questFlags;
            }
        }

        private List<string> _questSpecialFlags;
        public List<string> QuestSpecialFlags
        {
            get
            {
                if (_questSpecialFlags == null) _questSpecialFlags = GetMatchingQuestSpecialFlags(QuestAddon.SpecialFlags);
                return _questSpecialFlags;
            }
        }
        public List<ModelCreatureTemplate> CreatureQuestGivers { get; set; } = new List<ModelCreatureTemplate>();
        public List<ModelCreatureTemplate> CreatureQuestTurners { get; set; } = new List<ModelCreatureTemplate>();
        public List<ModelGameObjectTemplate> GameObjectQuestGivers { get; set; } = new List<ModelGameObjectTemplate>();
        public List<ModelGameObjectTemplate> GameObjectQuestTurners { get; set; } = new List<ModelGameObjectTemplate>();
        public List<ModelAreaTrigger> ModelAreasTriggers { get; set; } = new List<ModelAreaTrigger>();

        public ModelItemTemplate StartItemTemplate { get; set; }
        public ModelItemTemplate ItemDrop1Template { get; set; }
        public ModelItemTemplate ItemDrop2Template { get; set; }
        public ModelItemTemplate ItemDrop3Template { get; set; }
        public ModelItemTemplate ItemDrop4Template { get; set; }
        public ModelItemTemplate RequiredItem1Template { get; set; }
        public ModelItemTemplate RequiredItem2Template { get; set; }
        public ModelItemTemplate RequiredItem3Template { get; set; }
        public ModelItemTemplate RequiredItem4Template { get; set; }
        public ModelItemTemplate RequiredItem5Template { get; set; }
        public ModelItemTemplate RequiredItem6Template { get; set; }
        public ModelCreatureTemplate RequiredNPC1Template { get; set; }
        public ModelCreatureTemplate RequiredNPC2Template { get; set; }
        public ModelCreatureTemplate RequiredNPC3Template { get; set; }
        public ModelCreatureTemplate RequiredNPC4Template { get; set; }
        public ModelGameObjectTemplate RequiredGO1Template { get; set; }
        public ModelGameObjectTemplate RequiredGO2Template { get; set; }
        public ModelGameObjectTemplate RequiredGO3Template { get; set; }
        public ModelGameObjectTemplate RequiredGO4Template { get; set; }

        // Objectives
        public List<ExplorationObjective> ExplorationObjectives { get; set; } = new List<ExplorationObjective>();
        public List<GatherObjective> GatherObjectives { get; set; } = new List<GatherObjective>();
        public List<KillObjective> KillObjectives { get; set; } = new List<KillObjective>();
        public List<KillLootObjective> KillLootObjectives { get; set; } = new List<KillLootObjective>();
        public List<InteractObjective> InteractObjectives { get; set; } = new List<InteractObjective>();
        public List<GatherObjective> PrerequisiteGatherObjectives { get; set; } = new List<GatherObjective>();
        public List<KillLootObjective> PrerequisiteLootObjectives { get; set; } = new List<KillLootObjective>();

        public List<int> NextQuestsIds { get; set; } = new List<int>();
        public List<int> PreviousQuestsIds { get; set; } = new List<int>();

        public int Id { get; set; }
        public int AllowableRaces { get; set; }
        public string AreaDescription { get; set; }
        public long Flags { get; set; }
        public int ItemDrop1 { get; set; }
        public int ItemDrop2 { get; set; }
        public int ItemDrop3 { get; set; }
        public int ItemDrop4 { get; set; }
        public int ItemDropQuantity1 { get; set; }
        public int ItemDropQuantity2 { get; set; }
        public int ItemDropQuantity3 { get; set; }
        public int ItemDropQuantity4 { get; set; }
        public string LogTitle { get; set; }
        public int MinLevel { get; set; }
        public int NextQuestInChain { get; set; } // TBC DB Only
        public string ObjectiveText1 { get; set; }
        public string ObjectiveText2 { get; set; }
        public string ObjectiveText3 { get; set; }
        public string ObjectiveText4 { get; set; }
        public int QuestLevel { get; set; }
        public int QuestSortID { get; set; }
        public int QuestInfoID { get; set; }
        public int QuestType { get; set; }
        public int RequiredFactionId1 { get; set; }
        public int RequiredFactionId2 { get; set; }
        public int RequiredFactionValue1 { get; set; }
        public int RequiredFactionValue2 { get; set; }
        public int RequiredItemCount1 { get; set; }
        public int RequiredItemCount2 { get; set; }
        public int RequiredItemCount3 { get; set; }
        public int RequiredItemCount4 { get; set; }
        public int RequiredItemCount5 { get; set; }
        public int RequiredItemCount6 { get; set; }
        public int RequiredItemId1 { get; set; }
        public int RequiredItemId2 { get; set; }
        public int RequiredItemId3 { get; set; }
        public int RequiredItemId4 { get; set; }
        public int RequiredItemId5 { get; set; }
        public int RequiredItemId6 { get; set; }
        public int RequiredNpcOrGo1 { get; set; }
        public int RequiredNpcOrGo2 { get; set; }
        public int RequiredNpcOrGo3 { get; set; }
        public int RequiredNpcOrGo4 { get; set; }
        public int RequiredNpcOrGoCount1 { get; set; }
        public int RequiredNpcOrGoCount2 { get; set; }
        public int RequiredNpcOrGoCount3 { get; set; }
        public int RequiredNpcOrGoCount4 { get; set; }
        public int StartItem { get; set; }
        public int TimeAllowed { get; set; }
        public int Unknown0 { get; set; }

        public bool IsPickable()
        {
            if (PreviousQuestsIds.Count > 0 && !PreviousQuestsIds.Any(ToolBox.IsQuestCompleted))
                return false;

            if (QuestAddon.RequiredSkillID > 0 && Skill.GetValue((SkillLine)QuestAddon.RequiredSkillID) < QuestAddon.RequiredSkillPoints)
                return false;

            // Add reputation req

            return true;
        }

        public float GetClosestQuestGiverDistance(Vector3 myPosition)
        {
            List<float> closestsQg = new List<float>();            
            CreatureQuestGivers.ForEach(cqg =>
            {
                if (cqg.Creatures.Count > 0)
                    closestsQg.Add(cqg.Creatures.Min(c => c.GetSpawnPosition.DistanceTo(myPosition)));
            });
            GameObjectQuestGivers.ForEach(goqg =>
            {
                if (goqg.GameObjects.Count > 0)
                    closestsQg.Add(goqg.GameObjects.Min(c => c.GetSpawnPosition.DistanceTo(myPosition)));
            });
            return closestsQg.Count > 0 ? closestsQg.Min() : float.MaxValue;
        }

        public List<string> GetItemsStringsList()
        {
            List<string> result = new List<string>();
            KillLootObjectives.ForEach(o =>
            {
                if (!result.Contains(o.ItemName))  result.Add(o.ItemName);
            });
            GatherObjectives.ForEach(o =>
            {
                if (!result.Contains(o.ItemName)) result.Add(o.ItemName);
            });
            return result;
        }

        public bool IsCompleted => ToolBox.IsQuestCompleted(Id);
        public string TrackerColor => WAQTasks.TaskInProgress?.QuestId == Id ? "White" : _trackerColorsDictionary[Status];

        public string GetObjectiveText(int objectiveIndex)
        {
            if (objectiveIndex == 1) return ObjectiveText1;
            if (objectiveIndex == 2) return ObjectiveText2;
            if (objectiveIndex == 3) return ObjectiveText3;
            if (objectiveIndex == 4) return ObjectiveText4;
            return "N/A";
        }

        private readonly Dictionary<QuestStatus, string> _trackerColorsDictionary = new Dictionary<QuestStatus, string>
        {
            {  QuestStatus.Completed, "SkyBlue"},
            {  QuestStatus.Failed, "Red"},
            {  QuestStatus.InProgress, "Gold"},
            {  QuestStatus.None, "Gray"},
            {  QuestStatus.ToPickup, "MediumSeaGreen"},
            {  QuestStatus.ToTurnIn, "RoyalBlue"},
            {  QuestStatus.Blacklisted, "Red"}
        };

        public void RecordObjectiveIndices()
        {            
            Logger.Log($"Recording objective indices for {LogTitle}");
            string[] objectives = Lua.LuaDoString<string[]>(@$"local numEntries, numQuests = GetNumQuestLogEntries()
                            local objectivesTable = {{}}
                            for i=1, numEntries do
                                local questLogTitleText, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(i)
                                if questID == {Id} then
                                    local numObjectives = GetNumQuestLeaderBoards(i)
                                    for j=1, numObjectives do
                                        local text, objetype, finished = GetQuestLogLeaderBoard(j, i)
                                        table.insert(objectivesTable, text)
                                    end
                                end
                            end
                            return unpack(objectivesTable)");

            GetAllObjectives().ForEach(ob =>
            {
                for (int i = 0; i < objectives.Length; i++)
                {
                    if (objectives[i].StartsWith(ob.ObjectiveName))
                    {
                        ob.ObjectiveIndex = i + 1;
                        AreObjectivesRecorded = true;
                    }
                }
            });
        }

        public List<Objective> GetAllObjectives()
        {
            List<Objective> result = new List<Objective>();
            result.AddRange(ExplorationObjectives);
            result.AddRange(GatherObjectives);
            result.AddRange(InteractObjectives);
            result.AddRange(KillLootObjectives);
            result.AddRange(KillObjectives);
            return result;
        }

        public void AddObjective(Objective objective)
        {
            if (objective is ExplorationObjective) ExplorationObjectives.Add((ExplorationObjective)objective);
            if (objective is GatherObjective) GatherObjectives.Add((GatherObjective)objective);
            if (objective is InteractObjective) InteractObjectives.Add((InteractObjective)objective);
            if (objective is KillLootObjective) KillLootObjectives.Add((KillLootObjective)objective);
            if (objective is KillObjective) KillObjectives.Add((KillObjective)objective);
        }

        public List<string> GetMatchingQuestFlags(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(QUEST_FLAGS)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(QUEST_FLAGS), i));
            }
            return result;
        }

        public List<string> GetMatchingQuestSpecialFlags(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(QUEST_SPECIAL_FLAGS)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(QUEST_SPECIAL_FLAGS), i));
            }
            return result;
        }
    }
}

public enum QUEST_SPECIAL_FLAGS : long
{
    QUEST_REPEATABLE = 1,
    QUEST_EXTERNAL_EVENTS = 2,
    QUEST_AUTO_ACCEPT = 4,
    QUEST_DUNGEON_FINDER = 8,
    QUEST_MONTHLY = 16,
    QUEST_KILL_BUNNY_NPC = 32,
}

public enum QUEST_FLAGS : long
{
    QUEST_FLAGS_NONE = 0,
    QUEST_FLAGS_STAY_ALIVE = 1,
    QUEST_FLAGS_PARTY_ACCEPT = 2,
    QUEST_FLAGS_EXPLORATION = 4,
    QUEST_FLAGS_SHARABLE = 8,
    QUEST_FLAGS_HAS_CONDITION = 16,
    QUEST_FLAGS_HIDE_REWARD_POI = 32,
    QUEST_FLAGS_RAID = 64,
    QUEST_FLAGS_TBC = 128,
    QUEST_FLAGS_NO_MONEY_FROM_XP = 256,
    QUEST_FLAGS_HIDDEN_REWARDS = 512,
    QUEST_FLAGS_TRACKING = 1024,
    QUEST_FLAGS_DEPRECATE_REPUTATION = 2048,
    QUEST_FLAGS_DAILY = 4096,
    QUEST_FLAGS_FLAGS_PVP = 8192,
    QUEST_FLAGS_UNAVAILABLE = 16384,
    QUEST_FLAGS_WEEKLY = 32768,
    QUEST_FLAGS_AUTOCOMPLETE = 65536,
    QUEST_FLAGS_DISPLAY_ITEM_IN_TRACKER = 131072,
    QUEST_FLAGS_OBJ_TEXT = 262144,
    QUEST_FLAGS_AUTO_ACCEPT = 524288,
    QUEST_FLAGS_PLAYER_CAST_ON_ACCEPT = 1048576,
    QUEST_FLAGS_PLAYER_CAST_ON_COMPLETE = 2097152,
    QUEST_FLAGS_UPDATE_PHASE_SHIFT = 4194304,
    QUEST_FLAGS_SOR_WHITELIST = 8388608,
    QUEST_FLAGS_LAUNCH_GOSSIP_COMPLETE = 16777216,
    QUEST_FLAGS_REMOVE_EXTRA_GET_ITEMS = 33554432,
    QUEST_FLAGS_HIDE_UNTIL_DISCOVERED = 67108864,
    QUEST_FLAGS_PORTRAIT_IN_QUEST_LOG = 134217728,
    QUEST_FLAGS_SHOW_ITEM_WHEN_COMPLETED = 268435456,
    QUEST_FLAGS_LAUNCH_GOSSIP_ACCEPT = 536870912,
    QUEST_FLAGS_ITEMS_GLOW_WHEN_DONE = 1073741824,
    QUEST_FLAGS_FAIL_ON_LOGOUT = 2147483648,
}
