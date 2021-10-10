using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelWorldObject
    {
        public int AreaId { get; set; }
        public int DisplayId { get; set; }
        public int Entry { get; set; }
        public int Guid { get; set; }
        public int Map { get; set; }
        public string Name { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int SpawnTimeSecs { get; set; }
        public float Type { get; set; }
        public int ZoneId { get; set; }

        public Vector3 GetSpawnPosition => new Vector3(PositionX, PositionY, PositionZ);
    }
}
