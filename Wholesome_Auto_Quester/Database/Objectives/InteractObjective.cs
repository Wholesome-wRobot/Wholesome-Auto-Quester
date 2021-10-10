using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class InteractObjective
    {
        public int ObjectId { get; }
        public int Amount { get; }
        public List<ModelWorldObject> WorldObjects { get; }
        public int ObjectiveIndex { get; }
        public string ItemName { get; }

        public InteractObjective(int amount, List<ModelWorldObject> worldObjects, int objectiveIndex)
        {
            Amount = amount;
            WorldObjects = worldObjects;
            ObjectiveIndex = objectiveIndex;
            ObjectId = worldObjects.Count > 0 ? worldObjects[0].Entry : -1;
            ItemName = worldObjects.Count > 0 ? worldObjects[0].Name : "N/A";
        }
    }
}
