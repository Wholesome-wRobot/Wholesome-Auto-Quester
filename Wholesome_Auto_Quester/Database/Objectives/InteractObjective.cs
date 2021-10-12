using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class InteractObjective
    {
        public int Amount { get; }
        public ModelGameObjectTemplate GameObjectTemplate { get; }
        public int ObjectiveIndex { get; }

        public InteractObjective(int amount, ModelGameObjectTemplate gameObjectTemplate, int objectiveIndex)
        {
            Amount = amount;
            GameObjectTemplate = gameObjectTemplate;
            ObjectiveIndex = objectiveIndex;
        }
    }
}
