namespace Wholesome_Auto_Quester.Database.Models.Flags
{
    public class DBUnitFlagsExtra
    {
        private long Flag { get; }
        public bool CREATURE_FLAG_EXTRA_INSTANCE_BIND => (Flag & 1) != 0;
        public bool CREATURE_FLAG_EXTRA_CIVILIAN => (Flag & 2) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_PARRY => (Flag & 4) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_PARRY_HASTEN => (Flag & 8) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_BLOCK => (Flag & 16) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_CRUSHING_BLOWS => (Flag & 32) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_XP => (Flag & 64) != 0;
        public bool CREATURE_FLAG_EXTRA_TRIGGER => (Flag & 128) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_TAUNT => (Flag & 256) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_MOVE_FLAGS_UPDATE => (Flag & 512) != 0;
        public bool CREATURE_FLAG_EXTRA_GHOST_VISIBILITY => (Flag & 1024) != 0;
        public bool CREATURE_FLAG_EXTRA_USE_OFFHAND_ATTACK => (Flag & 2048) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_SELL_VENDOR => (Flag & 4096) != 0;
        public bool CREATURE_FLAG_EXTRA_IGNORE_COMBAT => (Flag & 8192) != 0;
        public bool CREATURE_FLAG_EXTRA_WORLDEVENT => (Flag & 16384) != 0;
        public bool CREATURE_FLAG_EXTRA_GUARD => (Flag & 32768) != 0;
        public bool CREATURE_FLAG_EXTRA_IGNORE_FEIGN_DEATH => (Flag & 65536) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_CRIT => (Flag & 131072) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_SKILL_GAINS => (Flag & 262144) != 0;
        public bool CREATURE_FLAG_EXTRA_OBEYS_TAUNT_DIMINISHING_RETURNS => (Flag & 524288) != 0;
        public bool CREATURE_FLAG_EXTRA_ALL_DIMINISH => (Flag & 1048576) != 0;
        public bool CREATURE_FLAG_EXTRA_NO_PLAYER_DAMAGE_REQ => (Flag & 2097152) != 0;
        public bool CREATURE_FLAG_EXTRA_DUNGEON_BOSS => (Flag & 268435456) != 0;
        public bool CREATURE_FLAG_EXTRA_IGNORE_PATHFINDING => (Flag & 536870912) != 0;
        public bool CREATURE_FLAG_EXTRA_IMMUNITY_KNOCKBACK => (Flag & 1073741824) != 0;

        public DBUnitFlagsExtra(long flag)
        {
            Flag = flag;
        }
    }
}
