using System.Collections.Generic;

namespace Db_To_Json.AutoQuester.Models
{
    internal class AQModelItemTemplate
    {
        public int Entry { get; set; }
        public string Name { get; set; }
        public int Class { get; set; }
        public long Flags { get; set; }
        public int spellid_1 { get; set; }
        public int spellid_2 { get; set; }
        public int spellid_3 { get; set; }
        public int spellid_4 { get; set; }
        public int startquest { get; set; }

        public List<AQModelCreatureLootTemplate> CreatureLootTemplates { get; set; } = new List<AQModelCreatureLootTemplate>();
        public List<AQModelGameObjectLootTemplate> GameObjectLootTemplates { get; set; } = new List<AQModelGameObjectLootTemplate>();
        public List<AQModelItemLootTemplate> ItemLootTemplates { get; set; } = new List<AQModelItemLootTemplate>();
    }
}
