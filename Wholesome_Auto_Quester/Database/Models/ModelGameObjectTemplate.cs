using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelGameObjectTemplate
    {
        public List<ModelGameObject> GameObjects { get; set; } = new List<ModelGameObject>();
        public string name { get; }
        public int entry { get; set; }
    }
}
