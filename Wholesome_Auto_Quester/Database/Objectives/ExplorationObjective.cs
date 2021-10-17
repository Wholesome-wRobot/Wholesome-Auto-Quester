using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class ExplorationObjective : Objective
    {
        public int AreaId { get; }
        public ModelAreaTrigger Area { get; }

        public ExplorationObjective(int id, ModelAreaTrigger area, string areaDescription)
        {
            AreaId = id;
            Area = area;
            ObjectiveName = areaDescription;
        }
    }
}
