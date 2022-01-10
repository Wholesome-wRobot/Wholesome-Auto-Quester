using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class GatherObjective : Objective
    {
        public ModelGameObjectLootTemplate GameObjectLootTemplate { get; }
        public ModelItemTemplate ItemToObtain { get; }

        public GatherObjective(int amount, ModelGameObjectLootTemplate gameObjectLootTemplate, ModelItemTemplate itemToObtain, string objectiveName = null)
        {
            GameObjectLootTemplate = gameObjectLootTemplate;
            ItemToObtain = itemToObtain;
            Amount = amount;
            ObjectiveName = objectiveName == null ? ItemToObtain.Name : objectiveName;
        }
    }
}
