namespace Wholesome_Auto_Quester.Database.Models.Flags
{
    public class DBUnitTypeFlags
    {
        private long Flag { get; }
        public bool CREATURE_TYPE_FLAG_TAMEABLE => (Flag & 1) != 0;
        public bool CREATURE_TYPE_FLAG_VISIBLE_TO_GHOSTS => (Flag & 2) != 0;
        public bool CREATURE_TYPE_FLAG_BOSS_MOB => (Flag & 4) != 0;
        public bool CREATURE_TYPE_FLAG_DO_NOT_PLAY_WOUND_ANIM => (Flag & 8) != 0;
        public bool CREATURE_TYPE_FLAG_NO_FACTION_TOOLTIP => (Flag & 16) != 0;
        public bool CREATURE_TYPE_FLAG_MORE_AUDIBLE => (Flag & 32) != 0;
        public bool CREATURE_TYPE_FLAG_SPELL_ATTACKABLE => (Flag & 64) != 0;
        public bool CREATURE_TYPE_FLAG_INTERACT_WHILE_DEAD => (Flag & 128) != 0;
        public bool CREATURE_TYPE_FLAG_SKIN_WITH_HERBALISM => (Flag & 256) != 0;
        public bool CREATURE_TYPE_FLAG_SKIN_WITH_MINING => (Flag & 512) != 0;
        public bool CREATURE_TYPE_FLAG_NO_DEATH_MESSAGE => (Flag & 1024) != 0;
        public bool CREATURE_TYPE_FLAG_ALLOW_MOUNTED_COMBAT => (Flag & 2048) != 0;
        public bool CREATURE_TYPE_FLAG_CAN_ASSIST => (Flag & 4096) != 0;
        public bool CREATURE_TYPE_FLAG_NO_PET_BAR => (Flag & 8192) != 0;
        public bool CREATURE_TYPE_FLAG_MASK_UID => (Flag & 16384) != 0;
        public bool CREATURE_TYPE_FLAG_SKIN_WITH_ENGINEERING => (Flag & 32768) != 0;
        public bool CREATURE_TYPE_FLAG_EXOTIC_PET => (Flag & 65536) != 0;
        public bool CREATURE_TYPE_FLAG_USE_MODEL_COLLISION_SIZE => (Flag & 131072) != 0;
        public bool CREATURE_TYPE_FLAG_ALLOW_INTERACTION_WHILE_IN_COMBAT => (Flag & 262144) != 0;
        public bool CREATURE_TYPE_FLAG_COLLIDE_WITH_MISSILES => (Flag & 524288) != 0;
        public bool CREATURE_TYPE_FLAG_NO_NAME_PLATE => (Flag & 1048576) != 0;
        public bool CREATURE_TYPE_FLAG_DO_NOT_PLAY_MOUNTED_ANIMATIONS => (Flag & 2097152) != 0;
        public bool CREATURE_TYPE_FLAG_LINK_ALL => (Flag & 4194304) != 0;
        public bool CREATURE_TYPE_FLAG_INTERACT_ONLY_WITH_CREATOR => (Flag & 8388608) != 0;
        public bool CREATURE_TYPE_FLAG_DO_NOT_PLAY_UNIT_EVENT_SOUNDS => (Flag & 16777216) != 0;
        public bool CREATURE_TYPE_FLAG_HAS_NO_SHADOW_BLOB => (Flag & 33554432) != 0;
        public bool CREATURE_TYPE_FLAG_TREAT_AS_RAID_UNIT => (Flag & 67108864) != 0;
        public bool CREATURE_TYPE_FLAG_FORCE_GOSSIP => (Flag & 134217728) != 0;
        public bool CREATURE_TYPE_FLAG_DO_NOT_SHEATHE => (Flag & 268435456) != 0;
        public bool CREATURE_TYPE_FLAG_DO_NOT_TARGET_ON_INTERACTION => (Flag & 536870912) != 0;
        public bool CREATURE_TYPE_FLAG_DO_NOT_RENDER_OBJECT_NAME => (Flag & 1073741824) != 0;
        public bool CREATURE_TYPE_FLAG_QUEST_BOSS => (Flag & 2147483648) != 0;

        public DBUnitTypeFlags(long flag)
        {
            Flag = flag;
        }
    }
}
