using robotManager.Helpful;
using System.Windows;

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

        public bool IsPointInZone(Vector3 point, int mapId)
        {
            if (mapId != mapID) return false;
            Rect zone = new Rect(new Point(locBottom, locLeft), new Point(locTop, locRight));
            return zone.Contains(new Point(point.X, point.Y));
        }

        public WAQContinent Continent
        {
            get
            {
                // Teldrassil - Darnassus
                if (areaID == 141 || areaID == 1657) return WAQContinent.Teldrassil;
                // EversongWoods - Ghostlands - SilvermoonCity
                if (areaID == 3430 || areaID == 3433 || areaID == 3487) return WAQContinent.BloodElfStartingZone;
                // AzuremystIsle - TheExodar - BloodmystIsle
                if (areaID == 3524 || areaID == 3557 || areaID == 3525) return WAQContinent.DraeneiStartingZone;
                if (mapID == 0) return WAQContinent.EasternKingdoms;
                if (mapID == 1) return WAQContinent.Kalimdor;
                if (mapID == 571) return WAQContinent.Northrend;
                if (mapID == 530) return WAQContinent.Outlands;
                return WAQContinent.None;
            }
        }
    }
}
