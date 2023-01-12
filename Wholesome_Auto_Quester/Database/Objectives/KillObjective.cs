using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class KillObjective : Objective
    {
        public ModelCreatureTemplate CreatureTemplate { get; }

        public KillObjective(int amount, ModelCreatureTemplate creatureTemplate, string objectiveName)
        {
            Amount = amount;
            CreatureTemplate = creatureTemplate;
            ObjectiveName = string.IsNullOrEmpty(objectiveName) ? CreatureTemplate.Name + " slain" : objectiveName;
        }
    }
}
