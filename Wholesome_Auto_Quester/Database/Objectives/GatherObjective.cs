using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class GatherObjective : Objective
    {
        public ModelGameObjectTemplate GameObjectToGather { get; }
        public ModelItemTemplate ItemToObtain { get; }

        public GatherObjective(int amount, ModelGameObjectTemplate gameObjectToGather, ModelItemTemplate itemToObtain, string objectiveName = null)
        {
            GameObjectToGather = gameObjectToGather;
            ItemToObtain = itemToObtain;
            Amount = amount;
            ObjectiveName = objectiveName == null ? ItemToObtain.Name : objectiveName;
        }
    }
}
