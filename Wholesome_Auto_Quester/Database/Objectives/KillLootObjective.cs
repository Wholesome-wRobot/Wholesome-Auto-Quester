using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillLootObjective : Objective
    {
        public ModelCreatureTemplate CreatureTemplate { get; }
        public ModelItemTemplate ItemToLoot { get; }

        public KillLootObjective(int amount, ModelCreatureTemplate creatureTemplate, ModelItemTemplate itemToLoot, string objectiveName = null)
        {
            ItemToLoot = itemToLoot;
            Amount = amount;
            CreatureTemplate = creatureTemplate;
            ObjectiveName = objectiveName == null ? ItemToLoot.Name : objectiveName;
        }
    }
}
