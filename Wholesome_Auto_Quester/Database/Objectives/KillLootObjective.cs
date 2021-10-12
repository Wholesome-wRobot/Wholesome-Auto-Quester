using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillLootObjective
    {
        public int Amount { get; }
        public ModelCreatureTemplate CreatureTemplate { get; }
        public int ObjectiveIndex { get; }

        public KillLootObjective(int amount, ModelCreatureTemplate creatureTemplate, int objectiveIndex)
        {
            Amount = amount;
            CreatureTemplate = creatureTemplate;
            ObjectiveIndex = objectiveIndex;
        }
    }
}
