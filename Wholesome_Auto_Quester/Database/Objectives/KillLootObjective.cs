using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillLootObjective : Objective
    {
        public ModelCreatureLootTemplate CreatureLootTemplate { get; }
        public ModelItemTemplate ItemTemplate { get; }

        public KillLootObjective(int amount, ModelCreatureLootTemplate creatureLootTemplate, ModelItemTemplate itemToLoot, string objectiveName = null)
        {
            Amount = amount;
            CreatureLootTemplate = creatureLootTemplate;
            ItemTemplate = itemToLoot;
            ObjectiveName = string.IsNullOrEmpty(objectiveName) ? ItemTemplate.Name : objectiveName;
        }
    }
}
