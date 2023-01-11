using System.Collections.Generic;

namespace Db_To_Json.AutoQuester.Models
{
    internal class AQModelGameObjectTemplate
    {
        public string name { get; set; }
        public int entry { get; set; }
        public int type { get; set; }
        public int Data0 { get; set; }

        public List<AQModelGameObject> GameObjects { get; set; }
    }
}
