using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelNpc
    {
        public string Name { get; set; }
        public string ItemName { get; set; } // for loot lookup
        public int Id { get; set; }
        public int Guid { get; set; }
        public int Map { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int SpawnTimeMin { get; set; }
        public int SpawnTimeMax { get; set; }

        public Vector3 GetSpawnPosition => new Vector3(PositionX, PositionY, PositionZ);
    }
}
