using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelWayPointData
    {
        //public int point { get; }
        public float position_x { get; }
        public float position_y { get; }
        public float position_z { get; }

        public Vector3 GetPosition => new Vector3(position_x, position_y, position_z);
    }
}
