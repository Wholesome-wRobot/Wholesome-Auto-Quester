using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class InteractObjective : Objective
    {
        public List<ModelGameObject> GameObjects { get; }
        public int GameObjectEntry { get; }
        public string GameObjectName { get; }

        public InteractObjective(int amount, ModelGameObjectTemplate gameObjectTemplate, string objectiveName)
        {
            GameObjects = gameObjectTemplate.GameObjects;
            GameObjectEntry = gameObjectTemplate.entry;
            GameObjectName = gameObjectTemplate.name;
            Amount = amount;
            ObjectiveName = objectiveName;
        }
    }
}
