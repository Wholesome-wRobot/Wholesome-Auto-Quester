namespace Wholesome_Auto_Quester.Database.Models.Flags
{
    public class DBQuestSpecialFlags
    {
        private long Flag { get; }
        public bool QUEST_REPEATABLE => (Flag & 1) != 0;
        public bool QUEST_EXTERNAL_EVENTS => (Flag & 2) != 0;
        public bool QUEST_AUTO_ACCEPT => (Flag & 4) != 0;
        public bool QUEST_DUNGEON_FINDER => (Flag & 8) != 0;
        public bool QUEST_MONTHLY => (Flag & 16) != 0;
        public bool QUEST_KILL_BUNNY_NPC => (Flag & 32) != 0;

        public DBQuestSpecialFlags(long flag)
        {
            Flag = flag;
        }
    }
}
