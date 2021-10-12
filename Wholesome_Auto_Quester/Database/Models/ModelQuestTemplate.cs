using robotManager.Helpful;
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
        public ModelQuestAddon QuestAddon { get; set; }
        public QuestStatus Status { get; set; } = QuestStatus.None;

        public List<ModelCreatureTemplate> CreatureQuestGivers { get; set; } = new List<ModelCreatureTemplate>();
        public List<ModelCreatureTemplate> CreatureQuestTurners { get; set; } = new List<ModelCreatureTemplate>();
        public List<ModelGameObjectTemplate> GameObjectQuestGivers { get; set; } = new List<ModelGameObjectTemplate>();
        public List<ModelGameObjectTemplate> GameObjectQuestTurners { get; set; } = new List<ModelGameObjectTemplate>();

        public List<ExplorationObjective> ExplorationObjectives { get; set; } = new List<ExplorationObjective>();
        public List<GatherObjective> GatherObjectives { get; set; } = new List<GatherObjective>();
        public List<KillObjective> KillObjectives { get; set; } = new List<KillObjective>();
        public List<KillLootObjective> KillLootObjectives { get; set; } = new List<KillLootObjective>();
        public List<InteractObjective> InteractObjectives { get; set; } = new List<InteractObjective>();
        public List<GatherObjective> PrerequisiteGatherItems { get; set; } = new List<GatherObjective>();
        public List<KillLootObjective> PrerequisiteLootItems { get; set; } = new List<KillLootObjective>();

        public List<int> NextQuestsIds { get; set; } = new List<int>();
        public List<int> PreviousQuestsIds { get; set; } = new List<int>();

        public int Id { get; set; }
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
        public int NextQuestInChain { get; set; } // TBC DB Only
        public string ObjectiveText1 { get; set; }
        public string ObjectiveText2 { get; set; }
        public string ObjectiveText3 { get; set; }
        public string ObjectiveText4 { get; set; }
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
        public int StartItem { get; set; }
        public int TimeAllowed { get; set; }

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
                closestsQg.Add(cqg.Creatures.Min(c => c.GetSpawnPosition.DistanceTo(myPosition)))
            );
            GameObjectQuestGivers.ForEach(goqg =>
                closestsQg.Add(goqg.GameObjects.Min(c => c.GetSpawnPosition.DistanceTo(myPosition)))
            );
            return closestsQg.Min();
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
            KillLootObjectives.ForEach(o => ToolBox.AddItemToDoNotSellList(o.CreatureTemplate.Loot.Name));
            GatherObjectives.ForEach(o => ToolBox.AddItemToDoNotSellList(o.GameObjectTemplate.name));
        }

        public void RemoveQuestItemsFromDoNotSellList()
        {
            KillLootObjectives.ForEach(o => ToolBox.RemoveItemFromDoNotSellList(o.CreatureTemplate.Loot.Name));
            GatherObjectives.ForEach(o => ToolBox.RemoveItemFromDoNotSellList(o.GameObjectTemplate.name));
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
}
