namespace Wholesome_Auto_Quester.Database.Models.Flags
{
    public class DBUnitFlags
    {
        private long Flag { get; }
        public bool UNIT_FLAG_SERVER_CONTROLLED => (Flag & 1) != 0;
        public bool UNIT_FLAG_NON_ATTACKABLE => (Flag & 2) != 0;
        public bool UNIT_FLAG_REMOVE_CLIENT_CONTROL => (Flag & 4) != 0;
        public bool UNIT_FLAG_PLAYER_CONTROLLED => (Flag & 8) != 0;
        public bool UNIT_FLAG_RENAME => (Flag & 16) != 0;
        public bool UNIT_FLAG_PREPARATION => (Flag & 32) != 0;
        public bool UNIT_FLAG_UNK_6 => (Flag & 64) != 0;
        public bool UNIT_FLAG_NOT_ATTACKABLE_1 => (Flag & 128) != 0;
        public bool UNIT_FLAG_IMMUNE_TO_PC => (Flag & 256) != 0;
        public bool UNIT_FLAG_IMMUNE_TO_NPC => (Flag & 512) != 0;
        public bool UNIT_FLAG_LOOTING => (Flag & 1024) != 0;
        public bool UNIT_FLAG_PET_IN_COMBAT => (Flag & 2048) != 0;
        public bool UNIT_FLAG_PVP => (Flag & 4096) != 0;
        public bool UNIT_FLAG_SILENCED => (Flag & 8192) != 0;
        public bool UNIT_FLAG_CANNOT_SWIM => (Flag & 16384) != 0;
        public bool NIT_FLAG_SWIMMING => (Flag & 32768) != 0;
        public bool UNIT_FLAG_NON_ATTACKABLE_2 => (Flag & 65536) != 0;
        public bool UNIT_FLAG_PACIFIED => (Flag & 131072) != 0;
        public bool UNIT_FLAG_STUNNED => (Flag & 262144) != 0;
        public bool UNIT_FLAG_IN_COMBAT => (Flag & 524288) != 0;
        public bool UNIT_FLAG_TAXI_FLIGHT => (Flag & 1048576) != 0;
        public bool UNIT_FLAG_DISARMED => (Flag & 2097152) != 0;
        public bool UNIT_FLAG_CONFUSED => (Flag & 4194304) != 0;
        public bool UNIT_FLAG_FLEEING => (Flag & 8388608) != 0;
        public bool UNIT_FLAG_POSSESSED => (Flag & 16777216) != 0;
        public bool UNIT_FLAG_NOT_SELECTABLE => (Flag & 33554432) != 0;
        public bool UNIT_FLAG_SKINNABLE => (Flag & 67108864) != 0;
        public bool UNIT_FLAG_MOUNT => (Flag & 134217728) != 0;
        public bool UNIT_FLAG_UNK_28 => (Flag & 268435456) != 0;
        public bool UNIT_FLAG_UNK_29 => (Flag & 536870912) != 0;
        public bool UNIT_FLAG_SHEATHE => (Flag & 1073741824) != 0;
        public bool UNIT_FLAG_IMMUNE => (Flag & 2147483648) != 0;

        public DBUnitFlags(long flag)
        {
            Flag = flag;
        }
    }
}
