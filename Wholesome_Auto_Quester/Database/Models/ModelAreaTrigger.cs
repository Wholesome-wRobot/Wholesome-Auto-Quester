using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelAreaTrigger
    {
        public int ContinentId { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int Radius { get; set; }

        public Vector3 GetPosition => new Vector3(PositionX, PositionY, PositionZ);
    }
}