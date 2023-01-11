namespace Db_To_Json.AutoQuester.Models
{
    internal class AQModelCreature
    {
        public AQModelCreature(float positionX, float positionY, float positionZ, uint cGuid, int cMap, int spawnTime)
        {
            position_x = positionX;
            position_y = positionY;
            position_z = positionZ;
            guid = cGuid;
            map = cMap;
            spawnTimeSecs = spawnTime;
        }

        public AQModelCreature() { }

        public uint guid { get; set; }
        public int map { get; set; }
        public int spawnTimeSecs { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }
        public float position_z { get; set; }

        public AQModelCreatureAddon CreatureAddon { get; set; }
    }
}
