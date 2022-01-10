namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreatureLootTemplate
    {
        public ModelCreatureTemplate CreatureTemplate { get; set; }

        public int Entry { get; set; }
        public int Item { get; set; }
        public int Reference { get; set; }
        public int Chance { get; set; }
        public int QuestRequired { get; set; }
        public int LootMode { get; set; }
        public int GroupId { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
        public string Comment { get; set; }
    }
}
