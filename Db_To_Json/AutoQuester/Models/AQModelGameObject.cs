namespace Db_To_Json.AutoQuester.Models
{
    internal class AQModelGameObject
    {
        public int id { get; set; }
        public uint guid { get; set; }
        public int map { get; set; }
        public float position_x { get; set; }
        public float position_y { get; set; }
        public float position_z { get; set; }
        public int spawntimesecs { get; set; }
    }
}
