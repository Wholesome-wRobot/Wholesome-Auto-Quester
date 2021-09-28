using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
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

        public int Id { get; set; }
        public int AllowableClasses { get; set; }
        public int AllowableRaces { get; set; }
        public string LogTitle { get; set; }
        public int MinLevel { get; set; }
        public int NextQuestID { get; set; }
        public int NextQuestInChain { get; set; } // TBC DB Only
        public int PrevQuestID { get; set; }
        public int QuestLevel { get; set; }
        public int QuestSortID { get; set; }
        public int QuestInfoID { get; set; }
        public int QuestType { get; set; }
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
        public int RequiredSkillID { get; set; }
        public int RequiredSkillPoints { get; set; }
        public int StartItem { get; set; }

        public bool IsPickable()
        {
            if (PreviousQuestsIds.Count > 0 && !PreviousQuestsIds.Any(qid => ToolBox.IsQuestCompleted(qid)))
                return false;

            if (RequiredSkillID > 0 && Skill.GetValue((SkillLine)RequiredSkillID) < RequiredSkillPoints)
                return false;

            return true;
        }

        public void MarkAsCompleted()
        {
            WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Add(Id);
            WholesomeAQSettings.CurrentSetting.Save();
        }

        public bool IsCompleted => ToolBox.IsQuestCompleted(Id);
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
