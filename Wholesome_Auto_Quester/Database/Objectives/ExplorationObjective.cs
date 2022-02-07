using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class ExplorationObjective : Objective
    {
        public ModelAreaTrigger Area { get; }

        public ExplorationObjective(ModelAreaTrigger area, string areaDescription)
        {
            Area = area;
            ObjectiveName = areaDescription;
        }
    }
}
