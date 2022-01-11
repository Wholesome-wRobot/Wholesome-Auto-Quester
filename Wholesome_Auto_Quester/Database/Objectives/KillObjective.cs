using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillObjective : Objective
    {
        public List<ModelCreature> Creatures { get; }
        public string CreatureName { get; }
        public int CreatureMaxLevel { get; }
        public int CreatureEntry { get; }

        public KillObjective(int amount, ModelCreatureTemplate creatureTemplate, string objectiveName)
        {
            Amount = amount;
            CreatureName = creatureTemplate.name;
            Creatures = creatureTemplate.Creatures;
            CreatureMaxLevel = creatureTemplate.maxLevel;
            CreatureEntry = creatureTemplate.entry;
            ObjectiveName = objectiveName == null ? CreatureName + " slain" : objectiveName;
        }
    }
}
