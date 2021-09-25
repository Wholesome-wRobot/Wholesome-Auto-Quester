using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelQuest
    {
        public QuestStatus Status { get; set; } = QuestStatus.None;
        public List<ModelNpc> QuestGivers { get; set; } = new List<ModelNpc>();
        public List<ModelNpc> QuestTurners { get; set; } = new List<ModelNpc>();
        public List<GatherObjectObjective> GatherObjectsObjectives { get; set; } = new List<GatherObjectObjective>();
        public List<CreaturesToKillObjective> CreaturesToKillObjectives { get; set; } = new List<CreaturesToKillObjective>();
        public List<CreatureToLootObjective> CreaturesToLootObjectives { get; set; } = new List<CreatureToLootObjective>();
        public List<int> NextQuestsIds { get; set; } = new List<int>();
        public List<int> PreviousQuestsIds { get; set; } = new List<int>();
        public int entry { get; set; }
        public int Method { get; set; }
        public int ZoneOrSort { get; set; }
        public int MinLevel { get; set; }
        public int QuestLevel { get; set; }
        public int Type { get; set; }
        public int RequiredClasses { get; set; }
        public int RequiredRaces { get; set; }
        public int RequiredSkill { get; set; }
        public int RequiredSkillValue { get; set; }
        public int RequiredCondition { get; set; }
        public int RepObjectiveFaction { get; set; }
        public int RepObjectiveValue { get; set; }
        public int RequiredMinRepFaction { get; set; }
        public int RequiredMinRepValue { get; set; }
        public int RequiredMaxRepFaction { get; set; }
        public int RequiredMaxRepValue { get; set; }
        public int SuggestedPlayers { get; set; }
        public int LimitTime { get; set; }
        public int QuestFlags { get; set; }
        public int SpecialFlags { get; set; }
        public int CharTitleId { get; set; }
        public int PrevQuestId { get; set; }
        public int NextQuestId { get; set; }
        public int ExclusiveGroup { get; set; }
        public int NextQuestInChain { get; set; }
        public int SrcItemId { get; set; }
        public int SrcItemCount { get; set; }
        public int SrcSpell { get; set; }
        public string Title { get; set; }
        public string Details { get; set; }
        public string Objectives { get; set; }
        public string OfferRewardText { get; set; }
        public string RequestItemsText { get; set; }
        public string EndText { get; set; }
        public string ObjectiveText1 { get; set; }
        public string ObjectiveText2 { get; set; }
        public string ObjectiveText3 { get; set; }
        public string ObjectiveText4 { get; set; }
        public int ReqItemId1 { get; set; }
        public int ReqItemId2 { get; set; }
        public int ReqItemId3 { get; set; }
        public int ReqItemId4 { get; set; }
        public int ReqItemCount1 { get; set; }
        public int ReqItemCount2 { get; set; }
        public int ReqItemCount3 { get; set; }
        public int ReqItemCount4 { get; set; }
        public int ReqSourceId1 { get; set; }
        public int ReqSourceId2 { get; set; }
        public int ReqSourceId3 { get; set; }
        public int ReqSourceId4 { get; set; }
        public int ReqSourceCount1 { get; set; }
        public int ReqSourceCount2 { get; set; }
        public int ReqSourceCount3 { get; set; }
        public int ReqSourceCount4 { get; set; }
        public int ReqCreatureOrGOId1 { get; set; }
        public int ReqCreatureOrGOId2 { get; set; }
        public int ReqCreatureOrGOId3 { get; set; }
        public int ReqCreatureOrGOId4 { get; set; }
        public int ReqCreatureOrGOCount1 { get; set; }
        public int ReqCreatureOrGOCount2 { get; set; }
        public int ReqCreatureOrGOCount3 { get; set; }
        public int ReqCreatureOrGOCount4 { get; set; }
        public int ReqSpellCast1 { get; set; }
        public int ReqSpellCast2 { get; set; }
        public int ReqSpellCast3 { get; set; }
        public int ReqSpellCast4 { get; set; }
        public int RewChoiceItemId1 { get; set; }
        public int RewChoiceItemId2 { get; set; }
        public int RewChoiceItemId3 { get; set; }
        public int RewChoiceItemId4 { get; set; }
        public int RewChoiceItemId5 { get; set; }
        public int RewChoiceItemId6 { get; set; }
        public int RewChoiceItemCount1 { get; set; }
        public int RewChoiceItemCount2 { get; set; }
        public int RewChoiceItemCount3 { get; set; }
        public int RewChoiceItemCount4 { get; set; }
        public int RewChoiceItemCount5 { get; set; }
        public int RewChoiceItemCount6 { get; set; }
        public int RewItemId1 { get; set; }
        public int RewItemId2 { get; set; }
        public int RewItemId3 { get; set; }
        public int RewItemId4 { get; set; }
        public int RewItemCount1 { get; set; }
        public int RewItemCount2 { get; set; }
        public int RewItemCount3 { get; set; }
        public int RewItemCount4 { get; set; }
        public int RewRepFaction1 { get; set; }
        public int RewRepFaction2 { get; set; }
        public int RewRepFaction3 { get; set; }
        public int RewRepFaction4 { get; set; }
        public int RewRepFaction5 { get; set; }
        public int RewRepValue1 { get; set; }
        public int RewRepValue2 { get; set; }
        public int RewRepValue3 { get; set; }
        public int RewRepValue4 { get; set; }
        public int RewRepValue5 { get; set; }
        public int RewMaxRepValue1 { get; set; }
        public int RewMaxRepValue2 { get; set; }
        public int RewMaxRepValue3 { get; set; }
        public int RewMaxRepValue4 { get; set; }
        public int RewMaxRepValue5 { get; set; }
        public int RewHonorableKills { get; set; }
        public int RewOrReqMoney { get; set; }
        public int RewMoneyMaxLevel { get; set; }
        public int RewSpell { get; set; }
        public int RewSpellCast { get; set; }
        public int RewMailTemplateId { get; set; }
        public int RewMailDelaySecs { get; set; }
        public int PointMapId { get; set; }
        public float PointX { get; set; }
        public float PointY { get; set; }
        public int PointOpt { get; set; }
        public int StartScript { get; set; }
        public int CompleteScript { get; set; }
        public int QuestGiver { get; set; }

        public bool IsPickable()
        {
            return (PrevQuestId == 0 || Quest.GetQuestCompleted(PrevQuestId))
                && (PreviousQuestsIds.Count <= 0 || PreviousQuestsIds.Any(qid => Quest.GetQuestCompleted(qid)));
        }

        public string TrackerColor => TrackerColorsDictionary[Status];

        private Dictionary<QuestStatus, string> TrackerColorsDictionary = new Dictionary<QuestStatus, string>
        {
            {  QuestStatus.Completed, "SkyBlue"},
            {  QuestStatus.Failed, "Red"},
            {  QuestStatus.InProgress, "Gold"},
            {  QuestStatus.None, "Gray"},
            {  QuestStatus.ToPickup, "MediumSeaGreen"},
            {  QuestStatus.ToTurnIn, "RoyalBlue"}
        };
    }

    public struct CreaturesToKillObjective
    {
        public int id;
        public int amount;
        public List<ModelNpc> worldCreatures;
        public int objectiveIndex;

        public CreaturesToKillObjective(int amount, int id, List<ModelNpc> worldCreatures, int objectiveIndex)
        {
            this.amount = amount;
            this.id = id;
            this.worldCreatures = worldCreatures;
            this.objectiveIndex = objectiveIndex;
        }

        public string GetName => worldCreatures.Count > 0 ? worldCreatures[0].Name : "N/A";
    }

    public struct CreatureToLootObjective
    {
        public int amount;
        public string itemName;
        public List<ModelNpc> worldCreatures;
        public int objectiveIndex;

        public CreatureToLootObjective(int amount, string itemName, List<ModelNpc> worldCreatures, int objectiveIndex)
        {
            this.amount = amount;
            this.worldCreatures = worldCreatures;
            this.itemName = itemName;
            this.objectiveIndex = objectiveIndex;
        }

        public string GetName => worldCreatures.Count > 0 ? worldCreatures[0].Name : "N/A";
    }

    public struct GatherObjectObjective
    {
        public int id;
        public int amount;
        public List<ModelGatherObject> worldObjects;
        public int objectiveIndex;

        public GatherObjectObjective(int amount, int id, List<ModelGatherObject> worldObjects, int objectiveIndex)
        {
            this.amount = amount;
            this.id = id;
            this.worldObjects = worldObjects;
            this.objectiveIndex = objectiveIndex;
        }

        public string GetName => worldObjects.Count > 0 ? worldObjects[0].Name : "N/A";
    }
}
