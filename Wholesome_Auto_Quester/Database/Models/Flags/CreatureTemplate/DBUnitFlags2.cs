namespace Wholesome_Auto_Quester.Database.Models.Flags
{
    public class DBUnitFlags2
    {
        private long Flag { get; }
        public bool UNIT_FLAG2_FEIGN_DEATH => (Flag & 1) != 0;
        public bool UNIT_FLAG2_HIDE_BODY => (Flag & 2) != 0;
        public bool UNIT_FLAG2_IGNORE_REPUTATION => (Flag & 4) != 0;
        public bool UNIT_FLAG2_COMPREHEND_LANG => (Flag & 8) != 0;
        public bool UNIT_FLAG2_MIRROR_IMAGE => (Flag & 16) != 0;
        public bool UNIT_FLAG2_DO_NOT_FADE_IN => (Flag & 32) != 0;
        public bool UNIT_FLAG2_FORCE_MOVEMENT => (Flag & 64) != 0;
        public bool UNIT_FLAG2_DISARM_OFFHAND => (Flag & 128) != 0;
        public bool UNIT_FLAG2_DISABLE_PRED_STATS => (Flag & 256) != 0;
        public bool UNIT_FLAG2_DISARM_RANGED => (Flag & 1024) != 0;
        public bool UNIT_FLAG2_REGENERATE_POWER => (Flag & 2048) != 0;
        public bool UNIT_FLAG2_RESTRICT_PARTY_INTERACTION => (Flag & 4096) != 0;
        public bool UNIT_FLAG2_PREVENT_SPELL_CLICK => (Flag & 8192) != 0;
        public bool UNIT_FLAG2_ALLOW_ENEMY_INTERACT => (Flag & 16384) != 0;
        public bool UNIT_FLAG2_CANNOT_TURN => (Flag & 32768) != 0;
        public bool UNIT_FLAG2_UNK2 => (Flag & 65536) != 0;
        public bool UNIT_FLAG2_PLAY_DEATH_ANIM => (Flag & 131072) != 0;
        public bool UNIT_FLAG2_ALLOW_CHEAT_SPELLS => (Flag & 262144) != 0;

        public DBUnitFlags2(long flag)
        {
            Flag = flag;
        }
    }
}
