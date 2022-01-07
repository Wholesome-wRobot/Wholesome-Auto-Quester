namespace Wholesome_Auto_Quester.Database.Models.Flags
{
    public class DBQuestFlags
    {
        private long Flag { get; }
        public bool QUEST_FLAGS_NONE => (Flag & 0) != 0;
        public bool QUEST_FLAGS_STAY_ALIVE => (Flag & 1) != 0;
        public bool QUEST_FLAGS_PARTY_ACCEPT => (Flag & 2) != 0;
        public bool QUEST_FLAGS_EXPLORATION => (Flag & 4) != 0;
        public bool QUEST_FLAGS_SHARABLE => (Flag & 8) != 0;
        public bool QUEST_FLAGS_HAS_CONDITION => (Flag & 16) != 0;
        public bool QUEST_FLAGS_HIDE_REWARD_POI => (Flag & 32) != 0;
        public bool QUEST_FLAGS_RAID => (Flag & 64) != 0;
        public bool QUEST_FLAGS_TBC => (Flag & 128) != 0;
        public bool QUEST_FLAGS_NO_MONEY_FROM_XP => (Flag & 256) != 0;
        public bool QUEST_FLAGS_HIDDEN_REWARDS => (Flag & 512) != 0;
        public bool QUEST_FLAGS_TRACKING => (Flag & 1024) != 0;
        public bool QUEST_FLAGS_DEPRECATE_REPUTATION => (Flag & 2048) != 0;
        public bool QUEST_FLAGS_DAILY => (Flag & 4096) != 0;
        public bool QUEST_FLAGS_FLAGS_PVP => (Flag & 8192) != 0;
        public bool QUEST_FLAGS_UNAVAILABLE => (Flag & 16384) != 0;
        public bool QUEST_FLAGS_WEEKLY => (Flag & 32768) != 0;
        public bool QUEST_FLAGS_AUTOCOMPLETE => (Flag & 65536) != 0;
        public bool QUEST_FLAGS_DISPLAY_ITEM_IN_TRACKER => (Flag & 131072) != 0;
        public bool QUEST_FLAGS_OBJ_TEXT => (Flag & 262144) != 0;
        public bool QUEST_FLAGS_AUTO_ACCEPT => (Flag & 524288) != 0;
        public bool QUEST_FLAGS_PLAYER_CAST_ON_ACCEPT => (Flag & 1048576) != 0;
        public bool QUEST_FLAGS_PLAYER_CAST_ON_COMPLETE => (Flag & 2097152) != 0;
        public bool QUEST_FLAGS_UPDATE_PHASE_SHIFT => (Flag & 4194304) != 0;
        public bool QUEST_FLAGS_SOR_WHITELIST => (Flag & 8388608) != 0;
        public bool QUEST_FLAGS_LAUNCH_GOSSIP_COMPLETE => (Flag & 16777216) != 0;
        public bool QUEST_FLAGS_REMOVE_EXTRA_GET_ITEMS => (Flag & 33554432) != 0;
        public bool QUEST_FLAGS_HIDE_UNTIL_DISCOVERED => (Flag & 67108864) != 0;
        public bool QUEST_FLAGS_PORTRAIT_IN_QUEST_LOG => (Flag & 134217728) != 0;
        public bool QUEST_FLAGS_SHOW_ITEM_WHEN_COMPLETED => (Flag & 268435456) != 0;
        public bool QUEST_FLAGS_LAUNCH_GOSSIP_ACCEPT => (Flag & 536870912) != 0;
        public bool QUEST_FLAGS_ITEMS_GLOW_WHEN_DONE => (Flag & 1073741824) != 0;
        public bool QUEST_FLAGS_FAIL_ON_LOGOUT => (Flag & 2147483648) != 0;

        public DBQuestFlags(long flag)
        {
            Flag = flag;
        }
    }
}
