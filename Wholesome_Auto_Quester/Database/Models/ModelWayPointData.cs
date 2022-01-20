using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelWayPointData
    {
        public int point { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }
        public float position_z { get; set; }
        public Vector3 GetPosition => new Vector3(position_x, position_y, position_z);
    }
}
