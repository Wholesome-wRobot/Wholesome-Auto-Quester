using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillLootObjective : Objective
    {
        public string CreatureName { get; }
        public List<ModelCreature> Creatures { get; }
        public int CreatureMaxLevel { get; }
        public int CreatureEntry { get; }
        public string ItemName { get; }
        public ModelSpell ItemSpell1 { get; }
        public ModelSpell ItemSpell2 { get; }
        public ModelSpell ItemSpell3 { get; }
        public ModelSpell ItemSpell4 { get; }
        public int ItemEntry { get; }

        public KillLootObjective(int amount, ModelCreatureLootTemplate creatureLootTemplate, ModelItemTemplate itemToLoot, string objectiveName = null)
        {
            CreatureName = creatureLootTemplate.CreatureTemplate.name;
            ItemName = itemToLoot.Name;
            Amount = amount;
            Creatures = creatureLootTemplate.CreatureTemplate.Creatures;
            ItemSpell1 = itemToLoot.Spell1;
            ItemSpell2 = itemToLoot.Spell2;
            ItemSpell3 = itemToLoot.Spell3;
            ItemSpell4 = itemToLoot.Spell4;
            ItemEntry = itemToLoot.Entry;
            CreatureMaxLevel = creatureLootTemplate.CreatureTemplate.maxLevel;
            CreatureEntry = creatureLootTemplate.CreatureTemplate.entry;

            ObjectiveName = objectiveName == null ? ItemName : objectiveName;
        }
    }
}
