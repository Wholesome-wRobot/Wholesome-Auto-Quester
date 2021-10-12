using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class ExplorationObjective
    {
        public int AreaId { get; }
        public ModelAreaTrigger Area { get; }
        public int ObjectiveIndex { get; }
        public string Name { get; }

        public ExplorationObjective(int id, ModelAreaTrigger area, int objectiveIndex)
        {
            AreaId = id;
            Area = area;
            ObjectiveIndex = objectiveIndex;
            Name = Area.GetPosition.ToString();
        }
    }
}
