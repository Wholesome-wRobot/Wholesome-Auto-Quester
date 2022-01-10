using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillLootObjective : Objective
    {
        public ModelCreatureLootTemplate CreatureLootTemplate { get; }
        public ModelItemTemplate ItemToLoot { get; }

        public KillLootObjective(int amount, ModelCreatureLootTemplate creatureLootTemplate, ModelItemTemplate itemToLoot, string objectiveName = null)
        {
            ItemToLoot = itemToLoot;
            Amount = amount;
            CreatureLootTemplate = creatureLootTemplate;
            ObjectiveName = objectiveName == null ? ItemToLoot.Name : objectiveName;
        }
    }
}
