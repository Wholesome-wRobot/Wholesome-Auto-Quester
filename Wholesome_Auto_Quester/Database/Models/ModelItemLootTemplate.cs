namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelItemLootTemplate
    {
        public int Entry { get; }
        public int Item { get; }
        public int Reference { get; }
        public int Chance { get; }
        public int QuestRequired { get; }
        public int LootMode { get; }
        public int GroupId { get; }
        public int MinCount { get; }
        public int MaxCount { get; }
    }
}
