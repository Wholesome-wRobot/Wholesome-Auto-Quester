using System;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelItemTemplate
    {
        public ModelItemTemplate(
            JSONModelItemTemplate jmit,
            Dictionary<int, JSONModelSpell> spellsDic,
            Dictionary<int, JSONModelCreatureTemplate> creatureTemplatesDic,
            Dictionary<int, JSONModelGameObjectTemplate> gameObjectTemplatesDic)
        {
            Entry = jmit.Entry;
            Name = jmit.Name;
            Class = jmit.Class;
            Flags = jmit.Flags;
            startquest = jmit.startquest;

            if (jmit.spellid_1 > 0)
            {
                if (spellsDic.TryGetValue(jmit.spellid_1, out JSONModelSpell jms))
                    Spell1 = new ModelSpell(jms);
                else
                    Logger.LogDevDebug($"WARNING: spellid_1 with entry {jmit.spellid_1} couldn't be found in dictionary");
            }

            if (jmit.spellid_2 > 0)
            {
                if (spellsDic.TryGetValue(jmit.spellid_2, out JSONModelSpell jms))
                    Spell2 = new ModelSpell(jms);
                else
                    Logger.LogDevDebug($"WARNING: spellid_2 with entry {jmit.spellid_2} couldn't be found in dictionary");
            }

            if (jmit.spellid_3 > 0)
            {
                if (spellsDic.TryGetValue(jmit.spellid_3, out JSONModelSpell jms))
                    Spell3 = new ModelSpell(jms);
                else
                    Logger.LogDevDebug($"WARNING: spellid_3 with entry {jmit.spellid_3} couldn't be found in dictionary");
            }

            if (jmit.spellid_4 > 0)
            {
                if (spellsDic.TryGetValue(jmit.spellid_4, out JSONModelSpell jms))
                    Spell4 = new ModelSpell(jms);
                else
                    Logger.LogDevDebug($"WARNING: spellid_4 with entry {jmit.spellid_4} couldn't be found in dictionary");
            }

            foreach (JSONModelCreatureLootTemplate jmclt in jmit.CreatureLootTemplates)
            {
                CreatureLootTemplates.Add(new ModelCreatureLootTemplate(jmclt, creatureTemplatesDic));
            }

            foreach (JSONModelGameObjectLootTemplate jgolt in jmit.GameObjectLootTemplates)
            {
                GameObjectLootTemplates.Add(new ModelGameObjectLootTemplate(jgolt, gameObjectTemplatesDic));
            }

            foreach (JSONModelItemLootTemplate jmilt in jmit.ItemLootTemplates)
            {
                ItemLootTemplates.Add(new ModelItemLootTemplate(jmilt));
            }
        }

        public int Entry { get; }
        public string Name { get; }
        //public int AllowableClass { get; }
        //public int AllowableRace { get; }
        //public int BuyCount { get; }
        //public int BuyPrice { get; }
        public int Class { get; }
        //public int DisplayId { get; }
        public long Flags { get; }
        /*public int FlagsExtra { get; }
        public int InventoryType { get; }
        public int ItemLevel { get; }
        public int Quality { get; }
        public int RequiredLevel { get; }
        public int RequiredSkill { get; }
        public int RequiredSkillRank { get; }
        public int RequiredSpell { get; }
        public int RequiredHonorRank { get; }
        public int RequiredCityRank { get; }
        public int RequiredReputationFaction { get; }
        public int RequiredReputationRank { get; }
        public int maxcount { get; }
        public int stackable { get; }
        public int ContainerSlots { get; }
        public int delay { get; }
        public int ammo_type { get; }
        public int RangedModRange { get; }*/
        //public int spellid_1 { get; }
        /*public int spelltrigger_1 { get; }
        public int spellcharges_1 { get; }
        public int spellppmRate_1 { get; }
        public int spellcooldown_1 { get; }
        public int spellcategory_1 { get; }
        public int spellcategorycooldown_1 { get; }*/
        //public int spellid_2 { get; }
        /*
        public int spelltrigger_2 { get; }
        public int spellcharges_2 { get; }
        public int spellppmRate_2 { get; }
        public int spellcooldown_2 { get; }
        public int spellcategory_2 { get; }
        public int spellcategorycooldown_2 { get; }*/
        //public int spellid_3 { get; }
        /*public int spelltrigger_3 { get; }
        public int spellcharges_3 { get; }
        public int spellppmRate_3 { get; }
        public int spellcooldown_3 { get; }
        public int spellcategory_3 { get; }
        public int spellcategorycooldown_3 { get; }*/
        //public int spellid_4 { get; }
        /*public int spelltrigger_4 { get; }
        public int spellcharges_4 { get; }
        public int spellppmRate_4 { get; }
        public int spellcooldown_4 { get; }
        public int spellcategory_4 { get; }
        public int spellcategorycooldown_4 { get; }
        public int spellid_5 { get; }
        public int spelltrigger_5 { get; }
        public int spellcharges_5 { get; }
        public int spellppmRate_5 { get; }
        public int spellcooldown_5 { get; }
        public int spellcategory_5 { get; }
        public int spellcategorycooldown_5 { get; }
        public int bonding { get; }
        public string Description { get; }
        public int PageText { get; }
        public int LanguageID { get; }
        public int PageMaterial { get; }*/
        public int startquest { get; }
        /*public int lockid { get; }
        public int Material { get; }
        public int sheath { get; }
        public int RandomProperty { get; }
        public int RandomSuffix { get; }
        public int block { get; }
        public int itemset { get; }
        public int MaxDurability { get; }
        public int area { get; }
        public int Map { get; }
        public int BagFamily { get; }
        public int duration { get; }
        public int ItemLimitCategory { get; }
        public int HolidayId { get; }
        public string ScriptName { get; }
        public int DisenchantID { get; }
        public int FoodType { get; }
        public int minMoneyLoot { get; }
        public int maxMoneyLoot { get; }
        public int flagsCustom { get; }
        public int SellPrice { get; }
        public int Subclass { get; }*/

        public ModelSpell Spell1 { get; set; }
        public ModelSpell Spell2 { get; set; }
        public ModelSpell Spell3 { get; set; }
        public ModelSpell Spell4 { get; set; }
        public List<ModelCreatureLootTemplate> CreatureLootTemplates { get; set; } = new List<ModelCreatureLootTemplate>();
        public List<ModelGameObjectLootTemplate> GameObjectLootTemplates { get; set; } = new List<ModelGameObjectLootTemplate>();
        public List<ModelItemLootTemplate> ItemLootTemplates { get; set; } = new List<ModelItemLootTemplate>();


        private List<string> _item_flags;
        public List<string> Item_Flags
        {
            get
            {
                if (_item_flags == null) _item_flags = GetMatchingItemFLags(Flags);
                return _item_flags;
            }
        }

        public List<string> GetMatchingItemFLags(long flag)
        {
            List<string> result = new List<string>();
            foreach (long i in Enum.GetValues(typeof(ITEM_FLSG)))
            {
                if ((flag & i) != 0)
                    result.Add(Enum.GetName(typeof(ITEM_FLSG), i));
            }
            return result;
        }

        public bool HasASpellAttached => Spell1 != null
            || Spell2 != null
            || Spell3 != null
            || Spell4 != null
            || (Flags & 64) != 0; // PLAYER_CAST
    }
}

public enum ITEM_FLSG : long
{
    ITEM_FLAG_NO_PICKUP = 1,
    CONJURED_ITEM = 2,
    OPENABLE = 4,
    GREEN_HEROIC_TEXT = 8,
    DEPRECATED = 16,
    CANT_BE_DESTROYED_EXCEPT_BY_SPELL = 32,
    ITEM_FLAG_PLAYERCAST = 64,
    NO_DEFAULT_30_SEC_CD_WHEN_EQUIPPED = 128,
    ITEM_FLAG_MULTI_LOOT_QUEST = 256,
    WRAPPER = 512,
    USES_RESOURCE = 1024,
    PARTY_LOOT = 2048,
    REFUNDABLE = 4096,
    ITEM_FLAG_PETITION = 8192,
    ITEM_FLAG_HAS_TEXT = 16384,
    ITEM_FLAG_NO_DISENCHANT = 32768,
    ITEM_FLAG_REAL_DURATION = 65536,
    NO_CREATOR = 131072,
    CAN_BE_PROSPECTED = 262144,
    UNIQUE_EQUIPPED = 524288,
    ITEM_FLAG_IGNORE_FOR_AURAS = 1048576,
    CAN_BE_USED_DURING_ARENA = 2097152,
    NO_DURABILITY_LOSS = 4194304,
    USABLE_IN_SHAPESHIFT = 8388608,
    QUEST_GLOW = 16777216,
    PROFESSION_RECIPE = 33554432,
    UNUSABLE_IN_ARENA = 67108864,
    ACCOUNT_BOUND = 134217728,
    SPELL_IS_CAST_IGNORING_REAGENT = 268435456,
    CAN_BE_MILLED = 536870912,
    REPORT_TO_GUILD_CHAT = 1073741824,
    BIND_ON_PICKUP_TRADABLE = 2147483648
}