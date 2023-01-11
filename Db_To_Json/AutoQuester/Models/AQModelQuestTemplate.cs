using System.Collections.Generic;

namespace Db_To_Json.AutoQuester.Models
{
    internal class AQModelQuestTemplate
    {
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
        public string ObjectiveText1 { get; set; }
        public string ObjectiveText2 { get; set; }
        public string ObjectiveText3 { get; set; }
        public string ObjectiveText4 { get; set; }
        public int QuestLevel { get; set; }
        public int QuestSortID { get; set; }
        public int QuestInfoID { get; set; }
        public int RequiredFactionId1 { get; set; }
        public int RequiredFactionId2 { get; set; }
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

        public AQModelQuestTemplateAddon QuestAddon { get; set; }

        public AQModelItemTemplate StartItemTemplate { get; set; }
        public List<int> CreatureQuestGiversEntries { get; set; } = new List<int>();
        public List<int> GameObjectQuestGiversEntries { get; set; } = new List<int>();
        public List<int> CreatureQuestEndersEntries { get; set; } = new List<int>();
        public List<int> GameObjectQuestEndersEntries { get; set; } = new List<int>();
        public List<int> NextQuestsIds { get; set; } = new List<int>();
        public List<int> PreviousQuestsIds { get; set; } = new List<int>();
        public List<AQModelConditions> Conditions { get; set; }
        public List<int> CreatureQuestGivers { get; set; }
        public List<int> GameObjectQuestGivers { get; set; }
        public List<AQModelAreaTrigger> ModelAreasTriggers { get; set; } = new List<AQModelAreaTrigger>();
    }
}
