using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Bot;
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
        public List<ExplorationObjective> ExplorationObjectives { get; set; } = new List<ExplorationObjective>();
        public List<GatherObjectObjective> GatherObjectsObjectives { get; set; } = new List<GatherObjectObjective>();
        public List<CreaturesToKillObjective> CreaturesToKillObjectives { get; set; } = new List<CreaturesToKillObjective>();
        public List<CreatureToLootObjective> CreaturesToLootObjectives { get; set; } = new List<CreatureToLootObjective>();
        public List<int> NextQuestsIds { get; set; } = new List<int>();
        public List<int> PreviousQuestsIds { get; set; } = new List<int>();

        public int Id { get; set; }
        public int AllowableClasses { get; set; }
        public int AllowableRaces { get; set; }
        public string AreaDescription { get; set; }
        public int Flags { get; set; }
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
        public int NextQuestID { get; set; }
        public int NextQuestInChain { get; set; } // TBC DB Only
        public string ObjectiveText1 { get; set; }
        public string ObjectiveText2 { get; set; }
        public string ObjectiveText3 { get; set; }
        public string ObjectiveText4 { get; set; }
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
        public int SpecialFlags { get; set; }
        public int StartItem { get; set; }
        public int TimeAllowed { get; set; }

        public bool IsPickable()
        {
            if (PreviousQuestsIds.Count > 0 && !PreviousQuestsIds.Any(ToolBox.IsQuestCompleted))
                return false;

            if (RequiredSkillID > 0 && Skill.GetValue((SkillLine)RequiredSkillID) < RequiredSkillPoints)
                return false;

            // Add reputation req

            return true;
        }

        public void MarkAsCompleted()
        {
            if(!WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Contains(Id)) {
                WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Add(Id);
                WholesomeAQSettings.CurrentSetting.Save();
            }
        }

        public void AddQuestItemsToDoNotSellList()
        {
            CreaturesToLootObjectives.ForEach(o => ToolBox.AddItemToDoNotSellList(o.itemName));
            GatherObjectsObjectives.ForEach(o => ToolBox.AddItemToDoNotSellList(o.itemName));
        }

        public void RemoveQuestItemsFromDoNotSellList()
        {
            CreaturesToLootObjectives.ForEach(o => ToolBox.RemoveItemFromDoNotSellList(o.itemName));
            GatherObjectsObjectives.ForEach(o => ToolBox.RemoveItemFromDoNotSellList(o.itemName));
        }

        public bool IsCompleted => ToolBox.IsQuestCompleted(Id);
        public string TrackerColor => WAQTasks.TaskInProgress?.Quest.Id == Id ? "White" : _trackerColorsDictionary[Status];

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
    }

    public struct CreaturesToKillObjective
    {
        public int id;
        public int amount;
        public List<ModelNpc> worldCreatures;
        public int objectiveIndex;

        public CreaturesToKillObjective(int amount, List<ModelNpc> worldCreatures, int objectiveIndex)
        {
            this.amount = amount;
            this.id = worldCreatures.Count > 0 ? worldCreatures[0].Id : -1;
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

        public CreatureToLootObjective(int amount, List<ModelNpc> worldCreatures, int objectiveIndex)
        {
            this.amount = amount;
            this.worldCreatures = worldCreatures;
            this.itemName = worldCreatures.Count > 0 ? worldCreatures[0].ItemName : "N/A";
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
        public string itemName;

        public GatherObjectObjective(int amount, List<ModelGatherObject> worldObjects, int objectiveIndex)
        {
            this.amount = amount;
            this.id = worldObjects[0].Entry;
            this.worldObjects = worldObjects;
            this.objectiveIndex = objectiveIndex;
            this.itemName = worldObjects.Count > 0 ? worldObjects[0].Name : "N/A";
        }

        public string GetName => worldObjects?.Count > 0 ? worldObjects[0].Name : "N/A";
    }

    public struct ExplorationObjective
    {
        public int id;
        public ModelArea area;
        public int objectiveIndex;

        public ExplorationObjective(int id, ModelArea area, int objectiveIndex)
        {
            this.id = id;
            this.area = area;
            this.objectiveIndex = objectiveIndex;
        }

        public string GetName => area.GetPosition.ToString();
    }
}
