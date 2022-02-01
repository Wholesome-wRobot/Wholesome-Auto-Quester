using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class GatherObjective : Objective
    {
        public ModelItemTemplate ItemTemplate { get; }
        public ModelGameObjectLootTemplate GameObjectLootTemplate { get; }

        public GatherObjective(int amount, ModelGameObjectLootTemplate gameObjectLootTemplate, ModelItemTemplate itemTemplate, string objectiveName = null)
        {
            Amount = amount;
            ItemTemplate = itemTemplate;
            GameObjectLootTemplate = gameObjectLootTemplate;
            ObjectiveName = string.IsNullOrEmpty(objectiveName) ? ItemTemplate.Name : objectiveName;
        }

        public int GetNbGameObjects()
        {
            int result = 0;
            foreach (ModelGameObjectTemplate template in GameObjectLootTemplate.GameObjectTemplates)
            {
                result += template.GameObjects.Count;
            }
            return result;
        }
    }
}
