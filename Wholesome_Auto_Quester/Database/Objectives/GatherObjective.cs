using System;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class GatherObjective
    {
        public int ObjectId { get; }
        public int Amount { get; }
        public List<ModelItem> WorldObjects { get; }
        public int ObjectiveIndex { get; }
        public string ItemName { get; }

        public GatherObjective(int amount, List<ModelItem> worldObjects, int objectiveIndex)
        {
            Amount = amount;
            WorldObjects = worldObjects;
            ObjectiveIndex = objectiveIndex;
            ObjectId = worldObjects.Count > 0 ? worldObjects[0].Entry : -1;
            ItemName = worldObjects.Count > 0 ? worldObjects[0].Name : "N/A";
        }
    }
}
