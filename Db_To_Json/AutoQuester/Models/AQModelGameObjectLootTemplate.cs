using System.Collections.Generic;

namespace Db_To_Json.AutoQuester.Models
{
    internal class AQModelGameObjectLootTemplate
    {
        public int Entry { get; set; }

        public List<int> GameObjectTemplates { get; set; }
    }
}
