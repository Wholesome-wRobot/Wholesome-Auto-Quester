using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelWayPointData
    {
        public ModelWayPointData(JSONModelWayPointData jmwpd)
        {
            position_x = jmwpd.position_x;
            position_y = jmwpd.position_y;
            position_z = jmwpd.position_z;
        }

        //public int point { get; }
        public float position_x { get; }
        public float position_y { get; }
        public float position_z { get; }

        public Vector3 GetPosition => new Vector3(position_x, position_y, position_z);
    }
}
