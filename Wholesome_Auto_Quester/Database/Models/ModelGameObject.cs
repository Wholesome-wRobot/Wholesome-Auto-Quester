using robotManager.Helpful;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelGameObject
    {
        public ModelGameObject(JSONModelGameObject jmgo)
        {
            guid = jmgo.guid;
            map = jmgo.map;
            position_x = jmgo.position_x;
            position_y = jmgo.position_y;
            position_z = jmgo.position_z;
            spawntimesecs = jmgo.spawntimesecs;
        }

        public uint guid { get; }
        public int map { get; }
        public float position_x { get; }
        public float position_y { get; }
        public float position_z { get; }
        public int spawntimesecs { get; }

        public Vector3 GetSpawnPosition => new Vector3(position_x, position_y, position_z);
    }
}
