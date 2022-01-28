using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelCreature
    {
        public ModelCreature(float positionX, float positionY, float positionZ, uint cGuid, int cMap, int spawnTime)
        {
            position_x = positionX;
            position_y = positionY;
            position_z = positionZ;
            guid = cGuid;
            map = cMap;
            spawnTimeSecs = spawnTime;
        }

        public ModelCreature() { }

        public ModelCreatureAddon CreatureAddon { get; set; }
        public uint guid { get; }
        public int map { get; }
        public int spawnTimeSecs { get; }
        public float position_x { get; }
        public float position_y { get; }
        public float position_z { get; }

        public Vector3 GetSpawnPosition => new Vector3(position_x, position_y, position_z);
    }
}
