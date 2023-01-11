using System.Collections.Generic;

namespace Db_To_Json.AutoQuester.Models
{
    internal class AQModelCreatureAddon
    {
        public int path_id { get; set; }

        public List<AQModelWayPointData> WayPoints { get; set; }
    }
}
