using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillLootObjective
    {
        public int Amount { get; }
        public string ItemName { get; }
        public List<ModelNpc> WorldCreatures { get; }
        public int ObjectiveIndex { get; }
        public string CreatureName { get; }

        public KillLootObjective(int amount, List<ModelNpc> worldCreatures, int objectiveIndex)
        {
            Amount = amount;
            WorldCreatures = worldCreatures;
            ObjectiveIndex = objectiveIndex;
            ItemName = worldCreatures.Count > 0 ? worldCreatures[0].ItemName : "N/A";
            CreatureName = worldCreatures.Count > 0 ? worldCreatures[0].Name : "N/A";
        }
    }
}
