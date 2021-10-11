using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillLootObjective
    {
        public int Amount { get; }
        public string ItemName { get; }
        public List<ModelNpc> WorldCreatures { get; }
        public int ObjectiveIndex { get; }
        public int ItemId { get; }

        public KillLootObjective(int amount, List<ModelNpc> worldCreatures, int objectiveIndex)
        {
            Amount = amount;
            WorldCreatures = worldCreatures;
            ObjectiveIndex = objectiveIndex;
            ItemName = worldCreatures.Count > 0 ? worldCreatures[0].ItemName : "N/A";
            ItemId = worldCreatures.Count > 0 ? worldCreatures[0].ItemId : -1;
        }
    }
}
