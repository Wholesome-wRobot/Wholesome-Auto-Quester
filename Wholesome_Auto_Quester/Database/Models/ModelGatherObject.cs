using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelGatherObject
    {
        public int Entry { get; set; }
        public int Class { get; set; }
        public int SubClass { get; set; }
        public string Name { get; set; }
        public int DisplayId { get; set; }
        public int Quality { get; set; }
        public int Flags { get; set; }
        public int GOLootEntry { get; set; }
        public int GameObjectEntry { get; set; }
        public int Guid { get; set; }
        public int Map { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int SpawnTimeSecs { get; set; }

        public Vector3 GetSpawnPosition => new Vector3(PositionX, PositionY, PositionZ);
    }
}
