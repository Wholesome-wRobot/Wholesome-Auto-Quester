using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wholesome_Auto_Quester.Database.Models;

namespace Wholesome_Auto_Quester.Database.Objectives
{
    public class ExplorationObjective
    {
        public int AreaId { get; }
        public ModelArea Area { get; }
        public int ObjectiveIndex { get; }
        public string Name { get; }

        public ExplorationObjective(int id, ModelArea area, int objectiveIndex)
        {
            AreaId = id;
            Area = area;
            ObjectiveIndex = objectiveIndex;
            Name = Area.GetPosition.ToString();
        }
    }
}
