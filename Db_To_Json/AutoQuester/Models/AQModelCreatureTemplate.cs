using System.Collections.Generic;

namespace Db_To_Json.AutoQuester.Models
{
    internal class AQModelCreatureTemplate
    {
        public int entry { get; set; }
        public string name { get; set; }
        public uint faction { get; set; }
        public int KillCredit1 { get; set; }
        public int KillCredit2 { get; set; }
        public int maxLevel { get; set; }
        public long unit_flags { get; set; }
        public long unit_flags2 { get; set; }
        public long type_flags { get; set; }
        public long dynamicflags { get; set; }
        public long flags_extra { get; set; }
        public int rank { get; set; }

        public List<int> KillCredits { get; set; } = new List<int>();
        public List<AQModelCreature> Creatures { get; set; }
    }
}
