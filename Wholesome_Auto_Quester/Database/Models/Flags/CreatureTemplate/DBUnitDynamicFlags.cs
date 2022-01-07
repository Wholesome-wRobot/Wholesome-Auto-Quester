namespace Wholesome_Auto_Quester.Database.Models.Flags
{
    public class DBUnitDynamicFlags
    {
        private long Flag { get; }
        public bool UNIT_DYNFLAG_NONE => (Flag & 0) != 0;
        public bool UNIT_DYNFLAG_LOOTABLE => (Flag & 1) != 0;
        public bool UNIT_DYNFLAG_TRACK_UNIT => (Flag & 2) != 0;
        public bool UNIT_DYNFLAG_TAPPED => (Flag & 4) != 0;
        public bool UNIT_DYNFLAG_TAPPED_BY_PLAYER => (Flag & 8) != 0;
        public bool UNIT_DYNFLAG_SPECIALINFO => (Flag & 16) != 0;
        public bool UNIT_DYNFLAG_DEAD => (Flag & 32) != 0;
        public bool UNIT_DYNFLAG_REFER_A_FRIEND => (Flag & 64) != 0;
        public bool UNIT_DYNFLAG_TAPPED_BY_ALL_THREAT_LIST => (Flag & 128) != 0;

        public DBUnitDynamicFlags(long flag)
        {
            Flag = flag;
        }
    }
}
