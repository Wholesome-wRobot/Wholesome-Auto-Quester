using System.Collections.Generic;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreatureAddon
    {
        public ModelCreatureAddon(JSONModelCreatureAddon jmca)
        {
            path_id = jmca.path_id;

            foreach (JSONModelWayPointData jmwpd in jmca.WayPoints)
            {
                WayPoints.Add(new ModelWayPointData(jmwpd));
            }
        }

        public int path_id { get; }
        //public long bytes1 { get; }

        public List<ModelWayPointData> WayPoints { get; set; } = new List<ModelWayPointData>();
    }
}
