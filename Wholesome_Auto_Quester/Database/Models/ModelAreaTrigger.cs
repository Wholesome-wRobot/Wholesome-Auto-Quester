using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelAreaTrigger
    {
        public ModelAreaTrigger(JSONModelAreaTrigger jmat)
        {
            ContinentId = jmat.ContinentId;
            PositionX = jmat.PositionX;
            PositionY = jmat.PositionY;
            PositionZ = jmat.PositionZ;
        }

        public int ContinentId { get; }
        public float PositionX { get; }
        public float PositionY { get; }
        public float PositionZ { get; }
        //public int Radius { get; }

        public Vector3 GetPosition => new Vector3(PositionX, PositionY, PositionZ);
    }
}