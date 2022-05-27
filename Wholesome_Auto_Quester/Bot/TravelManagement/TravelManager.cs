using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.PathFinder;

namespace Wholesome_Auto_Quester.Bot.TravelManagement
{
    public class TravelManager : ITravelManager
    {
        private bool _shouldTravel;

        public TravelManager()
        {
            Initialize();
        }

        public void Initialize()
        {
            AddAllOffmeshConnections();
        }

        public void Dispose()
        {

        }

        public void ResetTravel()
        {
            _shouldTravel = false;
        }

        public bool TravelInProgress => _shouldTravel;

        public bool IsTravelRequired(IWAQTask task)
        {
            ModelWorldMapArea myArea = ContinentHelper.MyMapArea;
            ModelWorldMapArea destinationArea = task.WorldMapArea;

            if (myArea.Continent != destinationArea.Continent
                || ShouldTravelFromNorthEKToSouthEk(task)
                || ShouldTravelFromSouthEKToNorthEK(task)
                || ShouldTakePortalDarnassusToRutTheran(task)
                || ShouldTakePortalRutTheranToDarnassus(task))
            {
                _shouldTravel = true;
                return true;
            }

            ResetTravel();
            return false;
        }

        public bool ShouldTravelFromNorthEKToSouthEk(IWAQTask task)
        {
            return ObjectManager.Me.Level <= 40
                && ContinentHelper.MyMapArea.Continent == WAQContinent.EasternKingdoms
                && (ObjectManager.Me.Position.X > -8118 || ContinentHelper.MyMapArea.areaID == 1537) // above burning steppes
                && task.Location.X <= -8118;
        }

        public bool ShouldTravelFromSouthEKToNorthEK(IWAQTask task)
        {
            return ObjectManager.Me.Level <= 40
                && ContinentHelper.MyMapArea.Continent == WAQContinent.EasternKingdoms
                && (ObjectManager.Me.Position.X < -8118 || ContinentHelper.MyMapArea.areaID == 1519) // under burning steppes
                && task.Location.X >= -8118;
        }

        public bool ShouldTakePortalDarnassusToRutTheran(IWAQTask task)
        {
            return ContinentHelper.MyMapArea.Continent == WAQContinent.Teldrassil
                && ObjectManager.Me.Position.Z >= 600
                && (task.Location.Z < 600 || task.WorldMapArea.Continent != WAQContinent.Teldrassil); // Under teldrassil tree
        }

        public bool ShouldTakePortalRutTheranToDarnassus(IWAQTask task)
        {
            return ContinentHelper.MyMapArea.Continent == WAQContinent.Teldrassil
                && ObjectManager.Me.Position.Z < 600
                && task.WorldMapArea.Continent == WAQContinent.Teldrassil
                && task.Location.Z > 600; // Over teldrassil tree
        }


        // Add all offmesh connections
        public void AddAllOffmeshConnections()
        {
            Logger.Log("Adding offmesh connections");
            OffMeshConnections.MeshConnection.Clear(); // must do first to clear faulty connections
            WTSettings.AddRecommendedBlacklistZones();
            WTSettings.AddRecommendedOffmeshConnections();
            WTTransport.AddRecommendedTransportsOffmeshes();
        }
    }
}

public enum WAQContinent
{
    Kalimdor,
    EasternKingdoms,
    BloodElfStartingZone,
    DraeneiStartingZone,
    Outlands,
    Northrend,
    Teldrassil,
    DeeprunTram,
    None
}