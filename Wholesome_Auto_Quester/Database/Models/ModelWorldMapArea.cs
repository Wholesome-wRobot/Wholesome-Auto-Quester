using robotManager.Helpful;
using System.Windows;

namespace Wholesome_Auto_Quester.Database.Models
{
    public class ModelWorldMapArea
    {
        public ModelWorldMapArea(int _mapId, int _areaId, string _areaName) 
        {
            mapID = _mapId;
            areaID = _areaId;
            areaName = _areaName;
        }
        public ModelWorldMapArea() { }

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
            if (mapId == 369 && mapID == 369) // Deeprun tram
            {
                return true;
            }
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
                    // Deeprun tram
                    if (mapID == 369)
                    {
                        _continent = WAQContinent.DeeprunTram;
                    }
                    // Teldrassil - Darnassus
                    else if (areaID == 141 || areaID == 1657)
                    {
                        _continent = WAQContinent.Teldrassil;
                    }
                    // EversongWoods - Ghostlands - SilvermoonCity
                    else if (areaID == 3430 || areaID == 3433 || areaID == 3487)
                    {
                        _continent = WAQContinent.BloodElfStartingZone;
                    }
                    // AzuremystIsle - TheExodar - BloodmystIsle
                    else if (areaID == 3524 || areaID == 3557 || areaID == 3525)
                    {
                        _continent = WAQContinent.DraeneiStartingZone;
                    }
                    else if (mapID == 0)
                    {
                        _continent = WAQContinent.EasternKingdoms;
                    }
                    else if (mapID == 1)
                    {
                        _continent = WAQContinent.Kalimdor;
                    }
                    else if (mapID == 571)
                    {
                        _continent = WAQContinent.Northrend;
                    }
                    else if (mapID == 530)
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
