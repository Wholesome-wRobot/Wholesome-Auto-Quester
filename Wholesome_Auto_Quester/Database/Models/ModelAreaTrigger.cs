using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelAreaTrigger
    {
        public int ContinentId { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float PositionZ { get; }
        //public int Radius { get; }

        public Vector3 GetPosition => new Vector3(PositionX, PositionY, PositionZ);
        public bool ShouldSerializeGetPosition() => false;
    }
}