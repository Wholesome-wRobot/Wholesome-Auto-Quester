using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class InteractObjective : Objective
    {
        public ModelGameObjectTemplate GameObjectTemplate { get; }

        public InteractObjective(int amount, ModelGameObjectTemplate gameObjectTemplate, string objectiveName)
        {
            Amount = amount;
            GameObjectTemplate = gameObjectTemplate;
            ObjectiveName = objectiveName;
        }
    }
}
