using System;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Objectives;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelQuestTemplate
    {
        public int Id { get; }
        public int AllowableRaces { get; }
        public string AreaDescription { get; }
        public long Flags { get; }
        public int ItemDrop1 { get; }
        public int ItemDrop2 { get; }
        public int ItemDrop3 { get; }
        public int ItemDrop4 { get; }
        public int ItemDropQuantity1 { get; }
        public int ItemDropQuantity2 { get; }
        public int ItemDropQuantity3 { get; }
        public int ItemDropQuantity4 { get; }
        public string LogTitle { get; }
        public int MinLevel { get; }
        public int NextQuestInChain { get; } // TBC DB Only
        public string ObjectiveText1 { get; }
        public string ObjectiveText2 { get; }
        public string ObjectiveText3 { get; }
        public string ObjectiveText4 { get; }
        public int QuestLevel { get; set; }
        public int QuestSortID { get; }
        public int QuestInfoID { get; }
        public int QuestType { get; }
        public int RequiredFactionId1 { get; }
        public int RequiredFactionId2 { get; }
        public int RequiredFactionValue1 { get; }
        public int RequiredFactionValue2 { get; }
        public int RequiredItemCount1 { get; }
        public int RequiredItemCount2 { get; }
        public int RequiredItemCount3 { get; }
        public int RequiredItemCount4 { get; }
        public int RequiredItemCount5 { get; }
        public int RequiredItemCount6 { get; }
        public int RequiredItemId1 { get; }
        public int RequiredItemId2 { get; }
        public int RequiredItemId3 { get; }
        public int RequiredItemId4 { get; }
        public int RequiredItemId5 { get; }
        public int RequiredItemId6 { get; }
        public int RequiredNpcOrGo1 { get; }
        public int RequiredNpcOrGo2 { get; }
        public int RequiredNpcOrGo3 { get; }
        public int RequiredNpcOrGo4 { get; }
        public int RequiredNpcOrGoCount1 { get; }
        public int RequiredNpcOrGoCount2 { get; }
        public int RequiredNpcOrGoCount3 { get; }
        public int RequiredNpcOrGoCount4 { get; }
        public int StartItem { get; }
        public int TimeAllowed { get; }
        public int Unknown0 { get; }

        public ModelQuestTemplateAddon QuestAddon { get; set; }

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


        public void AddObjective(Objective objective)
        {
            if (objective is ExplorationObjective) ExplorationObjectives.Add((ExplorationObjective)objective);
            if (objective is GatherObjective) GatherObjectives.Add((GatherObjective)objective);
            if (objective is InteractObjective) InteractObjectives.Add((InteractObjective)objective);
            if (objective is KillLootObjective) KillLootObjectives.Add((KillLootObjective)objective);
            if (objective is KillObjective) KillObjectives.Add((KillObjective)objective);
        }

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
