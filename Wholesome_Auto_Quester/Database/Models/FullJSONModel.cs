using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class FullJSONModel
    {
        public List<JSONModelQuestTemplate> QuestTemplates { get; set; }
        public List<JSONModelCreatureTemplate> CreatureTemplates { get; set; }
        public List<JSONModelGameObjectTemplate> GameObjectTemplates { get; set; }
        public List<JSONModelItemTemplate> ItemTemplates { get; set; }
        public List<JSONModelSpell> Spells { get; set; }
        public List<JSONModelCreatureTemplate> CreaturesToGrind { get; set; }
        public List<JSONModelWorldMapArea> WorldMapAreas { get; set; }
    }
    public class JSONModelWorldMapArea
    {
        public int mapID { get; set; }
        public int areaID { get; set; }
        public string areaName { get; set; }
        public double locLeft { get; set; }
        public double locRight { get; set; }
        public double locTop { get; set; }
        public double locBottom { get; set; }
    }

    public class JSONModelSpell
    {
        public int Id { get; set; }
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
        public List<JSONModelCreatureLootTemplate> CreatureLootTemplates { get; set; } = new List<JSONModelCreatureLootTemplate>();
        public List<JSONModelGameObjectLootTemplate> GameObjectLootTemplates { get; set; } = new List<JSONModelGameObjectLootTemplate>();
        public List<JSONModelItemLootTemplate> ItemLootTemplates { get; set; } = new List<JSONModelItemLootTemplate>();
    }

    public class JSONModelItemLootTemplate
    {
        public int Entry { get; set; }
    }

    public class JSONModelGameObjectLootTemplate
    {
        public int Entry { get; set; }
        public List<int> GameObjectTemplates { get; set; } = new List<int>();
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

        public List<JSONModelGameObject> GameObjects { get; set; } = new List<JSONModelGameObject>();
    }

    public class JSONModelCreatureTemplate
    {
        public int entry { get; set; }
        public string name { get; set; }
        public uint faction { get; set; }
        public int KillCredit1 { get; set; }
        public int KillCredit2 { get; set; }
        public int maxLevel { get; set; }
        public int minLevel { get; set; }
        public long unit_flags { get; set; }
        public long unit_flags2 { get; set; }
        public long type_flags { get; set; }
        public long dynamicflags { get; set; }
        public long flags_extra { get; set; }
        public int rank { get; set; }

        public List<int> KillCredits { get; set; } = new List<int>();
        public List<JSONModelCreature> Creatures { get; set; } = new List<JSONModelCreature>();
    }

    public class JSONModelCreature
    {
        public uint guid { get; set; }
        public int map { get; set; }
        public int spawnTimeSecs { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }
        public float position_z { get; set; }
        public JSONModelCreatureAddon CreatureAddon { get; set; }
    }

    public class JSONModelCreatureAddon
    {
        public int path_id { get; set; }

        public List<JSONModelWayPointData> WayPoints { get; set; } = new List<JSONModelWayPointData>();
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
        public int MinLevel { get; set; }
        public JSONModelQuestTemplateAddon QuestAddon { get; set; }
        public JSONModelItemTemplate StartItemTemplate { get; set; }
        public List<JSONModelConditions> Conditions { get; set; } = new List<JSONModelConditions>();
        public List<JSONModelAreaTrigger> ModelAreasTriggers { get; set; } = new List<JSONModelAreaTrigger>();
        public List<int> CreatureQuestGiversEntries { get; set; } = new List<int>();
        public List<int> GameObjectQuestGiversEntries { get; set; } = new List<int>();
        public List<int> CreatureQuestEndersEntries { get; set; } = new List<int>();
        public List<int> GameObjectQuestEndersEntries { get; set; } = new List<int>();
        public List<int> NextQuestsIds { get; set; } = new List<int>();
        public List<int> PreviousQuestsIds { get; set; } = new List<int>();
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
        public List<int> ExclusiveQuests { get; set; } = new List<int>();
    }
}
