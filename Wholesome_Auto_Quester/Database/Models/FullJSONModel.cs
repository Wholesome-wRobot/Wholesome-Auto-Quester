using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class FullJSONModel
    {
        public List<JSONModelQuestTemplate> QuestTemplates { get; set; }
        public List<JSONModelCreatureTemplate> CreatureTemplates { get; set; }
        public List<JSONModelGameObjectTemplate> GameObjectTemplates { get; set; }
        public List<JSONModelItemTemplate> ItemTemplates { get; set; }
    }

    public class JSONModelItemTemplate
    {
        public int Entry { get; set; }
        public string Name { get; set; }
        public int Class { get; set; }
        public int Flags { get; set; }
        public int spellid_1 { get; set; }
        public int spellid_2 { get; set; }
        public int spellid_3 { get; set; }
        public int spellid_4 { get; set; }
        public int startquest { get; set; }
        public List<JSONModelCreatureLootTemplate> CreatureLootTemplates { get; set; }
        public List<JSONModelGameObjectLootTemplate> GameObjectLootTemplates { get; set; }
        public List<JSONModelItemLootTemplate> ItemLootTemplates { get; set; }
    }

    public class JSONModelItemLootTemplate
    {
        public int Entry { get; set; }
    }

    public class JSONModelGameObjectLootTemplate
    {
        public int Entry { get; set; }
        public List<int> GameObjectTemplates { get; set; }
    }

    public class JSONModelCreatureLootTemplate
    {
        public int Entry { get; set; }
        public float Chance { get; set; }
        public int CreatureTemplate { get; set; }
    }

    public class JSONModelGameObject
    {
        public int id { get; set; }
        public uint guid { get; set; }
        public int map { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }
        public float position_z { get; set; }
        public int spawntimesecs { get; set; }
    }

    public class JSONModelGameObjectTemplate
    {
        public string name { get; set; }
        public int entry { get; set; }
        public int type { get; set; }
        public int Data0 { get; set; }

        public List<JSONModelGameObject> GameObjects { get; set; }
    }

    public class JSONModelCreatureTemplate
    {
        public int entry { get; set; }
        public string name { get; set; }
        public uint faction { get; set; }
        public int KillCredit1 { get; set; }
        public int KillCredit2 { get; set; }
        public int maxLevel { get; set; }
        public long unit_flags { get; set; }
        public long unit_flags2 { get; set; }
        public long type_flags { get; set; }
        public long dynamicflags { get; set; }
        public long flags_extra { get; set; }
        public int rank { get; set; }

        public List<int> KillCredits { get; set; }
        public List<JSONModelCreature> Creatures { get; set; }
    }

    public class JSONModelCreature
    {
        public uint guid { get; set; }
        public int map { get; set; }
        public int spawnTimeSecs { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }
        public float position_z { get; set; }
        public ModelCreatureAddon CreatureAddon { get; set; }
    }

    public class JSONModelCreatureAddon
    {
        public int path_id { get; set; }

        public List<JSONModelWayPointData> WayPoints { get; set; }
    }

    public class JSONModelWayPointData
    {
        public int id { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }
        public float position_z { get; set; }
    }

    public class JSONModelQuestTemplate
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
        public JSONModelQuestTemplateAddon QuestAddon { get; set; }
        public JSONModelItemTemplate StartItemTemplate { get; set; }
        public List<JSONModelConditions> Conditions { get; set; }
        public List<JSONModelAreaTrigger> ModelAreasTriggers { get; set; }
        public List<int> CreatureQuestGiversEntries { get; set; }
        public List<int> GameObjectQuestGiversEntries { get; set; }
        public List<int> CreatureQuestEndersEntries { get; set; }
        public List<int> GameObjectQuestEndersEntries { get; set; }
        public List<int> NextQuestsIds { get; set; }
        public List<int> PreviousQuestsIds { get; set; }
        public List<int> CreatureQuestGivers { get; set; }
        public List<int> GameObjectQuestGivers { get; set; }
    }

    public class JSONModelAreaTrigger
    {
        public int ContinentId { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int ID { get; set; }
    }

    public class JSONModelConditions
    {
        public int SourceTypeOrReferenceId { get; set; }
        public int SourceGroup { get; set; }
        public int SourceEntry { get; set; }
        public int ElseGroup { get; set; }
        public int ConditionTypeOrReference { get; set; }
        public int ConditionValue1 { get; set; }
        public int ConditionValue2 { get; set; }
        public int NegativeCondition { get; set; }
    }

    public class JSONModelQuestTemplateAddon
    {
        public int ID { get; set; }
        public int AllowableClasses { get; set; }
        public int PrevQuestID { get; set; }
        public int NextQuestID { get; set; }
        public int ExclusiveGroup { get; set; }
        public int RequiredSkillID { get; set; }
        public int RequiredSkillPoints { get; set; }
        public int SpecialFlags { get; set; }
        public int RequiredMaxRepFaction { get; set; }
        public int RequiredMaxRepValue { get; set; }
        public int RequiredMinRepFaction { get; set; }
        public int RequiredMinRepValue { get; set; }
        public List<int> ExclusiveQuests { get; set; }
    }
}
