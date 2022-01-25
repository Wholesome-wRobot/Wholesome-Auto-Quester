using robotManager.Helpful;
using System.Windows;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelWorldMapArea
    {
        public int ID { get; }
        public int mapID { get; }
        public int areaID { get; }
        public string areaName { get; }
        public double locLeft { get; }
        public double locRight { get; }
        public double locTop { get; }
        public double locBottom { get; }

        public void PointInZone()
        {
            Rect zone = new Rect();
            zone.Location = new Point(locBottom, locRight);
            zone.Size = new Size(locTop - locBottom, locLeft - locRight);
            Vector3 myPos = ObjectManager.Me.Position;
            if (zone.Contains(myPos.Y, myPos.X))
                Logger.LogError($"You are in {mapID}, {areaID}, {areaName}");
        }
    }
}
