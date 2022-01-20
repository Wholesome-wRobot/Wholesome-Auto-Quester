using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreatureAddon
    {
        public List<ModelWayPointData> WayPoints { get; set; }
        public int path_id { get; set; }
        public long bytes1 { get; set; }
    }
}
