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

        public ModelWorldMapArea(JSONModelWorldMapArea jmwma)
        {
            mapID = jmwma.mapID;
            areaID = jmwma.areaID;
            areaName = jmwma.areaName;
            locLeft = jmwma.locLeft;
            locRight = jmwma.locRight;
            locTop = jmwma.locTop;
            locBottom = jmwma.locBottom;
        }

        public ModelWorldMapArea() { }

        private bool _isContinentSet = false;
        //public int ID { get; }
        public int mapID { get; set; }
        public int areaID { get; set; }
        public string areaName { get; set; }
        public double locLeft { get; set; }
        public double locRight { get; set; }
        public double locTop { get; set; }
        public double locBottom { get; set; }

        public bool IsPointInMapArea(Vector3 point, int mapId)
        {
            if (mapId == 369 && mapID == 369) // Deeprun tram
            {
                return true;
            }
            if (mapId != mapID)
            {
                return false;
            }
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
                    else if (IsElfStartingZone)
                    {
                        _continent = WAQContinent.Teldrassil;
                    }
                    // EversongWoods - Ghostlands - SilvermoonCity
                    else if (IsBloodElfStartingZone)
                    {
                        _continent = WAQContinent.BloodElfStartingZone;
                    }
                    // AzuremystIsle - TheExodar - BloodmystIsle
                    else if (IsDraneiStartingZone)
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

        public bool IsBloodElfStartingZone => areaID == 3430 || areaID == 3433 || areaID == 3487;
        public bool IsDraneiStartingZone => areaID == 3524 || areaID == 3557 || areaID == 3525;
        public bool IsElfStartingZone => areaID == 141 || areaID == 1657;
        public bool IsOrcStartingZone => areaID == 14 || areaID == 1637;
        public bool IsTaurenStartingZone => areaID == 215 || areaID == 1638;
        public bool IsUndeadStartingZone => areaID == 85 || areaID == 1497;
        public bool IsDwarfStartingZone => areaID == 1 || areaID == 1537;
        public bool IsHumanStartingZone => areaID == 12 || areaID == 1519;
        public bool IsInAStartingZone => 
            IsBloodElfStartingZone
            || IsDraneiStartingZone
            || IsElfStartingZone
            || IsOrcStartingZone
            || IsTaurenStartingZone
            || IsUndeadStartingZone
            || IsDwarfStartingZone
            || IsHumanStartingZone;
    }
}
