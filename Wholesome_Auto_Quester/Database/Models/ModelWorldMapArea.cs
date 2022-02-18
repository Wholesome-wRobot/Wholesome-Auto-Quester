using robotManager.Helpful;
using System.Windows;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelWorldMapArea
    {
        private bool _isContinentSet = false;
        public int ID { get; }
        public int mapID { get; }
        public int areaID { get; }
        public string areaName { get; }
        public double locLeft { get; }
        public double locRight { get; }
        public double locTop { get; }
        public double locBottom { get; }

        public bool IsPointInMapArea(Vector3 point, int mapId)
        {
            if (mapId != mapID) return false;
            Rect zone = new Rect(new Point(locBottom, locLeft), new Point(locTop, locRight));
            return zone.Contains(new Point(point.X, point.Y));
        }

        private WAQContinent _continent = WAQContinent.None;
        public WAQContinent Continent
        {
            get
            {
                if (_continent == WAQContinent.None && !_isContinentSet)
                {
                    // Teldrassil - Darnassus
                    if (areaID == 141 || areaID == 1657)
                    {
                        _continent = WAQContinent.Teldrassil;
                    }
                    // EversongWoods - Ghostlands - SilvermoonCity
                    if (areaID == 3430 || areaID == 3433 || areaID == 3487)
                    {
                        _continent = WAQContinent.BloodElfStartingZone;
                    }
                    // AzuremystIsle - TheExodar - BloodmystIsle
                    if (areaID == 3524 || areaID == 3557 || areaID == 3525)
                    {
                        _continent = WAQContinent.DraeneiStartingZone;
                    }
                    if (mapID == 0)
                    {
                        _continent = WAQContinent.EasternKingdoms;
                    }
                    if (mapID == 1)
                    {
                        _continent = WAQContinent.Kalimdor;
                    }
                    if (mapID == 571)
                    {
                        _continent = WAQContinent.Northrend;
                    }
                    if (mapID == 530)
                    {
                        _continent = WAQContinent.Outlands;
                    }
                    _isContinentSet = true;
                }

                return _continent;
            }
        }
    }
}
