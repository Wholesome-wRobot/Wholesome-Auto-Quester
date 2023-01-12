using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Bot.JSONManagement;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.ContinentManagement
{
    public class ContinentManager : IContinentManager
    {
        private readonly IJSONManager _JSONManager;
        private List<ModelWorldMapArea> _worldMapAreas;

        public ContinentManager(IJSONManager jSONManager)
        {
            _JSONManager = jSONManager;
            Initialize();
        }

        public void Initialize()
        {
            _worldMapAreas = _JSONManager.GetWorldMapAreasFromJSON();
            _worldMapAreas.Add(new ModelWorldMapArea(369, 0, "DeeprunTram"));
        }

        public void Dispose()
        {
        }

        public ModelWorldMapArea GetWorldMapAreaFromPoint(Vector3 position, int mapdId) // CAN BE NULL!
        {
            List<ModelWorldMapArea> potentialResults = new List<ModelWorldMapArea>();
            foreach (ModelWorldMapArea wma in _worldMapAreas)
            {
                if (wma.IsPointInMapArea(position, mapdId))
                {
                    potentialResults.Add(wma);
                }
            }
            // resort to returning continent
            if (potentialResults.Count <= 0)
            {
                return _worldMapAreas.Find(wma => wma.mapID == mapdId && wma.areaID == 0);
            }
            if (potentialResults.Count <= 1)
            {
                return potentialResults.First();
            }
            potentialResults.RemoveAll(pr => pr.areaID == 0);
            return potentialResults.FirstOrDefault();
        }

        public ModelWorldMapArea MyMapArea => GetWorldMapAreaFromPoint(ObjectManager.Me.Position, Usefuls.ContinentId);

        public bool PointIsOnMyContinent(Vector3 position, int continentId)
        {
            ModelWorldMapArea pointWma = GetWorldMapAreaFromPoint(position, continentId);
            return pointWma != null && pointWma.Continent == MyMapArea.Continent;
        }
    }
}
