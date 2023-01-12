using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelGameObjectLootTemplate
    {
        public ModelGameObjectLootTemplate(
            JSONModelGameObjectLootTemplate jgolt, 
            Dictionary<int, JSONModelGameObjectTemplate> gameObjectTemplatesDic)
        {
            Entry = jgolt.Entry;

            foreach (int gotId in jgolt.GameObjectTemplates)
            {
                if (gameObjectTemplatesDic.TryGetValue(gotId, out JSONModelGameObjectTemplate gameObjectTemplate))
                {
                    GameObjectTemplates.Add(new ModelGameObjectTemplate(gameObjectTemplate));
                }
            }
        }

        public int Entry { get; }
        //public int Item { get; }
        //public int Reference { get; }
        //public int Chance { get; }
        //public int QuestRequired { get; }
        //public int LootMode { get; }
        //public int GroupId { get; }
        //public int MinCount { get; }
        //public int MaxCount { get; }
        //public string Comment { get; }

        public List<ModelGameObjectTemplate> GameObjectTemplates { get; set; } = new List<ModelGameObjectTemplate>();
    }
}
