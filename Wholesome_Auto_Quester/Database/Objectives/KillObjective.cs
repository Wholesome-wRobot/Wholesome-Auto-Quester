using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillObjective
    {
        public int CreatureId { get; }
        public int Amount { get; }
        public List<ModelNpc> WorldCreatures { get; }
        public int ObjectiveIndex { get; }
        public string CreatureName { get; }

        public KillObjective(int amount, List<ModelNpc> worldCreatures, int objectiveIndex)
        {
            Amount = amount;
            WorldCreatures = worldCreatures;
            ObjectiveIndex = objectiveIndex;
            CreatureId = worldCreatures.Count > 0 ? worldCreatures[0].Id : -1;
            CreatureName = worldCreatures.Count > 0 ? worldCreatures[0].Name : "N/A";
        }
    }
}
