using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class GatherObjective : Objective
    {
        public List<ModelGameObject> GameObjects { get; }
        public string ItemName { get; }
        public ModelSpell ItemSpell1 { get; }
        public ModelSpell ItemSpell2 { get; }
        public ModelSpell ItemSpell3 { get; }
        public ModelSpell ItemSpell4 { get; }
        public int ItemEntry { get; }
        public int GameObjectEntry { get; }
        public string GameObjectName { get; }

        public GatherObjective(int amount, ModelGameObjectLootTemplate gameObjectLootTemplate, ModelItemTemplate itemToObtain, string objectiveName = null)
        {
            GameObjects = gameObjectLootTemplate.GameObjectTemplate.GameObjects;
            GameObjectEntry = gameObjectLootTemplate.GameObjectTemplate.entry;
            GameObjectName = gameObjectLootTemplate.GameObjectTemplate.name;
            ItemName = itemToObtain.Name;
            ItemSpell1 = itemToObtain.Spell1;
            ItemSpell2 = itemToObtain.Spell2;
            ItemSpell3 = itemToObtain.Spell3;
            ItemSpell4 = itemToObtain.Spell4;
            ItemEntry = itemToObtain.Entry;
            Amount = amount;
            ObjectiveName = objectiveName == null ? ItemName : objectiveName;
        }
    }
}
