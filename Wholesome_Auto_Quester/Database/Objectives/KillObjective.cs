using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillObjective
    {
        public int Amount { get; }
        //public List<ModelNpc> WorldCreatures { get; }
        public ModelCreatureTemplate CreatureTemplate { get; }
        public int ObjectiveIndex { get; }

        public KillObjective(int amount, ModelCreatureTemplate creatureTemplate, int objectiveIndex)
        {
            Amount = amount;
            CreatureTemplate = creatureTemplate;
            ObjectiveIndex = objectiveIndex;
        }
    }
}
