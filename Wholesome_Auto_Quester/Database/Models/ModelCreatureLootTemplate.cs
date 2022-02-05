namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreatureLootTemplate
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
        public string Comment { get; }

        public ModelCreatureTemplate CreatureTemplate { get; set; }
    }
}
