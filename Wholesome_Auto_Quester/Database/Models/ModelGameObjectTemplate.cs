using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelGameObjectTemplate
    {
        public string name { get; }
        public int entry { get; }
        //public float size { get; }
        public int type { get; }
        public int Data0 { get; }
        //public int Data1 { get; }

        public List<ModelGameObject> GameObjects { get; set; } = new List<ModelGameObject>();
    }
}
