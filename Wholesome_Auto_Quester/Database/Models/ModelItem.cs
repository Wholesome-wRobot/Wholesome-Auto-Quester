using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelItem
    {
        public int AreaId { get; set; }
        public int Class { get; set; }
        public int DisplayId { get; set; }
        public int Entry { get; set; }
        public int Flags { get; set; }
        public int GameObjectEntry { get; set; }
        public int WorldObjectEntry { get; set; }
        public int Guid { get; set; }
        public int Map { get; set; }
        public string Name { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float PositionZ { get; set; }
        public int Quality { get; set; }
        public int SpawnTimeSecs { get; set; }
        public int SpellId1 { get; set; }
        public int SpellId2 { get; set; }
        public int SpellId3 { get; set; }
        public int SpellId4 { get; set; }
        public int SpellId5 { get; set; }
        public int SubClass { get; set; }
        public int ZoneId { get; set; }

        public Vector3 GetSpawnPosition => new Vector3(PositionX, PositionY, PositionZ);
    }
}
