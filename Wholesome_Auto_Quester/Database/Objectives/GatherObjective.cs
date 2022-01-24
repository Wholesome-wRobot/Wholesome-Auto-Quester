using System.Collections.Generic;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class GatherObjective : Objective
    {
        public string ItemName { get; }
        public ModelSpell ItemSpell1 { get; }
        public ModelSpell ItemSpell2 { get; }
        public ModelSpell ItemSpell3 { get; }
        public ModelSpell ItemSpell4 { get; }
        public int ItemEntry { get; }
        public List<ObjGOTemplate> ObjGOTemplates { get; } = new List<ObjGOTemplate>();

        public GatherObjective(int amount, ModelGameObjectLootTemplate gameObjectLootTemplate, ModelItemTemplate itemToObtain, string objectiveName = null)
        {
            gameObjectLootTemplate.GameObjectTemplates.ForEach(got => ObjGOTemplates.Add(new ObjGOTemplate(got)));
            ItemName = itemToObtain.Name;
            ItemSpell1 = itemToObtain.Spell1;
            ItemSpell2 = itemToObtain.Spell2;
            ItemSpell3 = itemToObtain.Spell3;
            ItemSpell4 = itemToObtain.Spell4;
            ItemEntry = itemToObtain.Entry;
            Amount = amount;
            ObjectiveName = string.IsNullOrEmpty(objectiveName) ? ItemName : objectiveName;
        }

        public List<ModelGameObject> GetAllGameObjects()
        {
            List<ModelGameObject> result = new List<ModelGameObject>();
            ObjGOTemplates.ForEach(got => result.AddRange(got.GameObjects));
            return result;
        }
    }

    public class ObjGOTemplate
    {
        public int GameObjectEntry { get; }
        public float GameObjectSize { get; }
        public string GameObjectName { get; }
        public List<ModelGameObject> GameObjects { get; }

        public ObjGOTemplate(ModelGameObjectTemplate template)
        {
            GameObjectEntry = template.entry;
            GameObjectName = template.name;
            GameObjects = template.GameObjects;
        }
    }
}
