using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreatureAddon
    {
        public int path_id { get; }
        public long bytes1 { get; }

        public List<ModelWayPointData> WayPoints { get; set; }
    }
}
