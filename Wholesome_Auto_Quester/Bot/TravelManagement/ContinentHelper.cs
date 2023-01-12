/*using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Database.Models;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TravelManagement
{
    class ContinentHelper
    {
        private static List<ModelWorldMapArea> _listWorldMapAreas;
        private static List<ModelWorldMapArea> ListWorldMapAreas
        {
            get
            {
                if (_listWorldMapAreas == null)
                {
                    DB _dataBase = new DB();
                    _listWorldMapAreas = _dataBase.QueryWorldMapAreas();
                    _listWorldMapAreas.Add(new ModelWorldMapArea(369, 0, "DeeprunTram"));
                    _dataBase.Dispose();
                }

                return _listWorldMapAreas;
            }
        }

        public static ModelWorldMapArea GetWorldMapAreaFromPoint(Vector3 position, int mapdId) // CAN BE NULL!
        {
            List<ModelWorldMapArea> potentialResults = new List<ModelWorldMapArea>();
            foreach (ModelWorldMapArea wma in ListWorldMapAreas)
            {
                if (wma.IsPointInMapArea(position, mapdId))
                {
                    potentialResults.Add(wma);
                }
            }
            // resort to returning continent
            if (potentialResults.Count <= 0)
            {
                return ListWorldMapAreas.Find(wma => wma.mapID == mapdId && wma.areaID == 0);
            }
            if (potentialResults.Count <= 1)
            {
                return potentialResults.First();
            }
            potentialResults.RemoveAll(pr => pr.areaID == 0);
            return potentialResults.FirstOrDefault();
        }

        public static ModelWorldMapArea MyMapArea => GetWorldMapAreaFromPoint(ObjectManager.Me.Position, Usefuls.ContinentId);
        public static bool PointIsOnMyContinent(Vector3 position, int continentId)
        {
            ModelWorldMapArea pointWma = GetWorldMapAreaFromPoint(position, continentId);
            return pointWma?.Continent == MyMapArea.Continent;
        }
    }
}
*/