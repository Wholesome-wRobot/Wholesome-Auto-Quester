using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.PathFinder;

namespace Wholesome_Auto_Quester.Bot.TravelManagement
{
    public class TravelManager : ITravelManager
    {
        private readonly List<ModelWorldMapArea> _worldMapAreas = new List<ModelWorldMapArea>();

        public TravelManager()
        {
            Initialize();
        }

        public void Initialize()
        {
            _worldMapAreas.Clear();
            DB _dataBase = new DB();
            _worldMapAreas.AddRange(_dataBase.QueryWorldMapAreas());
            _dataBase.Dispose();
            AddAllOffmeshConnections();
        }

        public void Dispose()
        {
            
        }

        public ModelWorldMapArea GetWorldMapAreaFromPoint(Vector3 position, int mapdId) // return real continent enum later
        {
            List<ModelWorldMapArea> potentialResults = new List<ModelWorldMapArea>();
            foreach (ModelWorldMapArea wma in _worldMapAreas)
            {
                if (wma.IsPointInZone(position, mapdId))
                {
                    potentialResults.Add(wma);
                }
            }
            if (potentialResults.Count <= 0)
            {
                Logger.LogError($"Couldn't find world map area for {position}, {mapdId}");
                return null;
            }
            if (potentialResults.Count <= 1)
            {
                return potentialResults.First();
            }
            potentialResults.RemoveAll(pr => pr.areaID == 0);
            return potentialResults.FirstOrDefault();
        }

        public bool NeedToTravelTo(IWAQTask task, out (ModelWorldMapArea myPosition, ModelWorldMapArea destination) result)
        {
            ModelWorldMapArea myWMArea = GetWorldMapAreaFromPoint(ObjectManager.Me.Position, Usefuls.ContinentId);
            ModelWorldMapArea destWMArea = GetWorldMapAreaFromPoint(task.Location, task.Continent);
            result = (null, null);

            if (myWMArea == null
                || destWMArea == null
                || task == null)
            {
                return false;
            }

            if (myWMArea.Continent != destWMArea.Continent
                || ShouldTakeZeppelinTirisfalToStranglethorn(myWMArea, task)
                || ShouldTakeZeppelinStranglethornToTirisfal(myWMArea, task))
            {
                result = (myWMArea, destWMArea);
                return true;
            }

            return false;
        }

        public bool ShouldTakeZeppelinTirisfalToStranglethorn(ModelWorldMapArea myWMArea, IWAQTask task)
        {
            return myWMArea.Continent == WAQContinent.EasternKingdoms
                && ObjectManager.Me.Position.X > -2384 // above wetlands
                && task.Location.X < -11301.69; // under duskwood
        }

        public bool ShouldTakeZeppelinStranglethornToTirisfal(ModelWorldMapArea myWMArea, IWAQTask task)
        {
            return myWMArea.Continent == WAQContinent.EasternKingdoms
                && ObjectManager.Me.Position.X < -11301.69 // under duskwood
                && task.Location.X > -2384; // above wetlands
        }

        // Zeppelins
        readonly int zeppelinTirisfalToOrgrimmarId = 164871;
        readonly int zeppelinKalidmdorToNorthrendId = 186238;
        readonly int zeppelinTirisfalToStranglethornId = 176495;

        readonly Vector3 tirisfalPlatformZepStranglethorn = new Vector3(2057.805, 242.4949, 99.76875, "None");
        readonly Vector3 insideZeppelinTirisfalToStranglethorn = new Vector3(2056.706, 231.9014, 100.0376, "None");

        readonly Vector3 stranglethornPlatformZepTirisfal = new Vector3(-12415.1, 207.7803, 31.49846, "None");
        readonly Vector3 insideZeppelinStranglethornToTirisfal = new Vector3(-12401.24, 206.9413, 32.03218, "None");

        readonly Vector3 tirisfalPlatformZepOrgrimmar = new Vector3(2066.407, 285.9733, 97.03147);
        readonly Vector3 insideZeppelinTirisfalToOrgrimmar = new Vector3(2068.5, 296.4182, 97.28456);

        readonly Vector3 orgrimmarPlatformZepTirisfal = new Vector3(1324.798, -4652.622, 53.78583);
        readonly Vector3 insideZeppelinKalimdorToTirisfal = new Vector3(1312.742, -4653.958, 54.14745);

        readonly Vector3 tirisfalPlatformZepNorthrend = new Vector3(2061.558, 359.2196, 83.47895);
        readonly Vector3 insideZeppelinTirisfalToNorthrend = new Vector3(2059.505, 374.1549, 82.44972);

        readonly Vector3 northrendPlatformZepTirisfal = new Vector3(1973.989, -6099.464, 67.15666);
        readonly Vector3 insideZeppelinNorthrendToTirisfal = new Vector3(1984.397, -6089.137, 67.68417);

        readonly Vector3 kalimdorlPlatformZepNorthrend = new Vector3(1179.33, -4150.091, 52.13512);
        readonly Vector3 insideZeppelinKalimdorToNorthrend = new Vector3(1192.992, -4142.117, 52.73592);

        readonly Vector3 northrendPlatformZepKalimdor = new Vector3(2829.167, 6178.443, 121.9824f);
        readonly Vector3 insideZeppelinNorthrendToKalimdor = new Vector3(2844.347, 6192.584, 122.2752);

        //Ships
        readonly Vector3 bayRatchetToBootyBay = new Vector3(-993.4946, -3825.295, 5.689055, "None");
        readonly Vector3 insideShipRatchetToBootyBay = new Vector3(-998.897, -3835.783, 6.215654, "None");

        readonly Vector3 bayBootyBayToRatchet = new Vector3(-14285.3, 556.2886, 8.837771, "None");
        readonly Vector3 insideShipBootyBayToRatchet = new Vector3(-14276.39, 575.9086, 6.084363, "None");

        readonly Vector3 bayAzuremystToDarkshore = new Vector3(-4263.875, -11335.03, 5.859486, "None");
        readonly Vector3 insideShipAzuremystToDarkshore = new Vector3(-4262.877, -11324.02, 4.91574, "None");

        readonly Vector3 bayDarkshoreToAzuremyst = new Vector3(6542.727, 922.8457, 5.912864, "None");
        readonly Vector3 insideShipDarkshoreToAzuremyst = new Vector3(6550.912, 945.3401, 4.898618, "None");

        readonly Vector3 bayDarkshoreToDarnassus = new Vector3(6576.859, 769.8532, 5.588161, "None");
        readonly Vector3 insideShipDarkshoreToDarnassus = new Vector3(6587.057, 767.6591, 5.10977, "None");

        readonly Vector3 bayDarnassusToDarkshore = new Vector3(8553.19, 1020.2, 5.543461, "None");
        readonly Vector3 insideShipDarnassusToDarkshore = new Vector3(8541.434, 1021.083, 5.085521, "None");

        readonly Vector3 bayDarkshoreToStormwind = new Vector3(6424.98, 818.6711, 5.490422, "None");
        readonly Vector3 insideShipDarkshoreToStormwind = new Vector3(6414.219, 822.085, 6.1804, "None");

        readonly Vector3 bayStormwindToDarkshore = new Vector3(-8640.113, 1327.584, 5.233279, "None");
        readonly Vector3 insideShipStormwindToDarkshore = new Vector3(-8646.059, 1339.308, 6.215415, "None");

        readonly Vector3 bayStormwindToBoreanTundra = new Vector3(-8295.06, 1408.721, 4.688217, "None");
        readonly Vector3 insideShipStormwindToBoreanTundra = new Vector3(-8291.178, 1427.616, 9.473845, "None");

        readonly Vector3 bayBoreanTundraToStormwind = new Vector3(2231.234, 5135.821, 5.343364, "None");
        readonly Vector3 insideShipBoreanTundraToStormwind = new Vector3(2232.324, 5114.973, 9.400736, "None");
        /*
        readonly Vector3 bayMenethilToHowlingFjord = new Vector3(-3724.361, -583.2341, 4.74352, "None");
        readonly Vector3 insideShipMenethilToHowlingFjord = new Vector3(-3711.364, -573.7974, 9.489109, "None");

        readonly Vector3 bayHowlingFjordToMenethil = new Vector3(591.8311, -5099.395, 5.260396, "None");
        readonly Vector3 insideShipHowlingFjordToMenethil = new Vector3(588.0685, -5120.662, 9.447546, "None");
        
        readonly Vector3 bayDustwallowToMenethil = new Vector3(-4000.404, -4724.158, 4.876398, "None");    
        readonly Vector3 insideShipDustwallowToMenethil = new Vector3(-4010.497, -4741.962, 6.17096, "None");

        readonly Vector3 bayMenethilToDustwallow = new Vector3(-3893.388, -602.8146, 5.425149, "None");
        readonly Vector3 insideShipMenethilToDustwallow = new Vector3(-3904.25, -577.7352, 6.059737, "None");

        readonly int ShipAzuremystToDarkshoreId = 181646;
        readonly int ShipDarkshoreToDarnassusId = 176244;
        readonly int ShipMenethilToDustwallowId = 176231;
        readonly int ShipMenethilToHowlingFjord = 181688;
        readonly int ShipDarkshoreToStormwindId = 176310;*/
        readonly int shipStormwindToBoreanTundraId = 190536;
        readonly int shipRatchetToBootyBayId = 20808;


        // Portals
        readonly int oGPortalToBlastedLandsId = 195142;
        readonly Vector3 oGPortalToBlastedLandsPosition = new Vector3(1472.55f, -4215.7f, 59.221f);

        /*readonly int ExodarPortalToBlastedLandsId = 195141;
        readonly Vector3 ExodarPortalToBlastedLandsPosition = new Vector3(-4037.81, -11555.6, -138.324f);

        readonly int DarnassusPortalToBlastedlandsId = 195141;
        readonly Vector3 DarnassusPortalToBlastedlandsPosition = new Vector3(9661.83, 2509.61, 1331.63f);*/

        readonly int stormwindPortalToBlastedlandsId = 195141;
        readonly Vector3 stormwindPortalToBlastedlandsPosition = new Vector3(-9007.58, 871.87, 129.692f);

        //  Shattrath portals
        readonly int shattrathPortalToKalimdorId = 183323;
        readonly Vector3 shattrathPortalToOrgrimmarPosition = new Vector3(-1934.205f, 5452.766f, -12.42705f);

        /*readonly int shattrathPortalToThunderbluffId = 183326;
        readonly Vector3 shattrathPortalToThunderbluffPosition = new Vector3(-1936.32, 5445.95, -12.4282f);

        readonly int shattrathPortalToUndercityId = 183327;
        readonly Vector3 shattrathPortalToUndercityPosition = new Vector3(-1931.48, 5460.49, -12.4281f);

        readonly int shattrathPortalTQueldanasId = 187056;*/
        readonly Vector3 shattrathPortalToQueldanasPosition = new Vector3(-1839.88, 5500.6, -12.4279f);

        readonly int shattrathPortalToDarnassusId = 183317;
        readonly Vector3 shattrathPortalToDarnassusPosition = new Vector3(-1790.98, 5413.98, -12.4282f);

        /*readonly int ShattrathPortalToStormwindId = 183325;
        readonly Vector3 ShattrathPortalToStormwindPosition = new Vector3(-1792.78, 5406.54, -12.4279f);

        readonly int ShattrathPortalToIronforgeId = 183322;
        readonly Vector3 ShattrathPortalToIronforgePosition = new Vector3(-1795.79, 5399.63, -12.4281f);

        readonly int ShattrathPortalToExodarId = 183321;
        readonly Vector3 ShattrathPortalToExodarPosition = new Vector3(-1880.28, 5357.53, -12.4281f);

        readonly int ShattrathPortalToSilvermoonId = 183324;
        readonly Vector3 ShattrathPortalToSilvermoonPosition = new Vector3(-1894.69, 5362.34, -12.4282f);*/


        // Dalaran portals
        /*readonly int DalaranPortalToStormwindId = 190960;
        readonly Vector3 DalaranPortalToStormwindPosition = new Vector3(5719.19, 719.681, 641.728f);

        readonly int DalaranPortalToIronforgeId = 191008;
        readonly Vector3 DalaranPortalToIronforgePosition = new Vector3(5712.68, 724.845, 641.736f);

        readonly int DalaranPortalToDarnassusId = 191006;
        readonly Vector3 DalaranPortalToDarnassusPosition = new Vector3(5706.16, 730.102, 641.745f);

        readonly int DalaranPortalToExodarId = 191007;
        readonly Vector3 DalaranPortalToExodarPosition = new Vector3(5699.58, 735.469, 641.769f);*/

        readonly int allianceDalaranPortalToShattrathId = 191013;
        readonly Vector3 allianceDalaranPortalToShattrathPosition = new Vector3(5697.49, 744.912, 641.819f);

        readonly int dalaranPortalToUndercityId = 191012;
        readonly Vector3 dalaranPortalToUndercityPosition = new Vector3(5934.66, 590.688, 640.575, "Flying");

        readonly int dalaranPortalToOGId = 191009;
        readonly Vector3 dalaranPortalToOGPosition = new Vector3(5925.85, 593.25, 640.563, "Flying");

        readonly int dalaranPortalToShattrathId = 191014;
        readonly Vector3 dalaranPortalToShattrathPosition = new Vector3(5941.66, 584.887, 640.574, "Flying");


        // ********** FROM EK **********
        public void PortalBlastedLandsToOutlands()
        {
            Logger.Log($"Traversing portal to Outlands");
            GoToTask.ToPosition(new Vector3(-11920.39, -3206.81, -15.35475f));
            Thread.Sleep(5000);
            GoToTask.ToPosition(new Vector3(-182.5485, 1023.459, 54.23014));
            Thread.Sleep(5000);
        }

        public void ZeppelinTirisfalToOrgrimmar()
        {
            Logger.Log($"Taking zeppelin to Orgrimmar");
            GoToTask.ToPosition(tirisfalPlatformZepOrgrimmar);
            if (ObjectManager.Me.Position.DistanceTo(tirisfalPlatformZepOrgrimmar) < 4)
            {
                WaitForTransport(zeppelinTirisfalToOrgrimmarId, 15);
                GoToTask.ToPosition(insideZeppelinTirisfalToOrgrimmar, 1);
                WaitOnTransport(orgrimmarPlatformZepTirisfal, 15);
            }
        }

        public void ZeppelingTirisfalToStrangelthorn()
        {
            Logger.Log("Taking zeppelin to Stranglethorn");
            GoToTask.ToPosition(tirisfalPlatformZepStranglethorn);
            if (ObjectManager.Me.Position.DistanceTo(tirisfalPlatformZepStranglethorn) < 4)
            {
                WaitForTransport(zeppelinTirisfalToStranglethornId, 15);
                ForceMoveTo(insideZeppelinTirisfalToStranglethorn);
                WaitOnTransport(stranglethornPlatformZepTirisfal, 15);
            }
        }

        public void ZeppelingStrangelthornToTirisfal()
        {
            Logger.Log("Taking zeppelin to Tirisfal");
            GoToTask.ToPosition(stranglethornPlatformZepTirisfal);
            if (ObjectManager.Me.Position.DistanceTo(stranglethornPlatformZepTirisfal) < 4)
            {
                WaitForTransport(zeppelinTirisfalToStranglethornId, 15);
                ForceMoveTo(insideZeppelinStranglethornToTirisfal);
                WaitOnTransport(tirisfalPlatformZepStranglethorn, 15);
            }
        }

        public void ShipBootyBayToRatchet()
        {
            Logger.Log("Taking ship to Ratchet");
            GoToTask.ToPosition(bayBootyBayToRatchet);
            if (ObjectManager.Me.Position.DistanceTo(bayBootyBayToRatchet) < 4)
            {
                WaitForTransport(shipRatchetToBootyBayId, 50);
                ForceMoveTo(insideShipBootyBayToRatchet);
                WaitOnTransport(bayRatchetToBootyBay, 30);
            }
        }

        // ********** FROM OUTLANDS **********
        public void PortalShattrathToDarnassus()
        {
            Logger.Log("Taking portal to Darnassus");
            GoToTask.ToPositionAndIntecractWithGameObject(shattrathPortalToDarnassusPosition, shattrathPortalToDarnassusId);
            Thread.Sleep(5000);
        }

        public void PortalShattrathToOrgrimmar()
        {
            Logger.Log("Taking portal to Orgrimmar");
            GoToTask.ToPositionAndIntecractWithGameObject(shattrathPortalToOrgrimmarPosition, shattrathPortalToKalimdorId);
            Thread.Sleep(5000);
        }

        // ********** FROM KALIMDOR **********
        public void PortalStormwindToBlastedLands()
        {
            Logger.Log("Taking portal to Blasted lands");
            GoToTask.ToPositionAndIntecractWithGameObject(stormwindPortalToBlastedlandsPosition, stormwindPortalToBlastedlandsId);
            Thread.Sleep(5000);
        }

        public void ShipRatchetToBootyBay()
        {
            Logger.Log("Taking ship to Booty Bay");
            GoToTask.ToPosition(bayRatchetToBootyBay);
            if (ObjectManager.Me.Position.DistanceTo(bayRatchetToBootyBay) < 4)
            {
                WaitForTransport(shipRatchetToBootyBayId, 30);
                ForceMoveTo(insideShipRatchetToBootyBay);
                WaitOnTransport(bayBootyBayToRatchet, 50);
                ForceMoveTo(new Vector3(-14295.3, 535.4561, 8.808624, "None")); // safety for pathfinder
            }
        }

        public void ZeppelinKalimdorToTirisfal()
        {
            Logger.Log("Taking zeppelin to Tirisfal");
            GoToTask.ToPosition(orgrimmarPlatformZepTirisfal);
            if (ObjectManager.Me.Position.DistanceTo(orgrimmarPlatformZepTirisfal) < 4)
            {
                WaitForTransport(zeppelinTirisfalToOrgrimmarId, 15);
                ForceMoveTo(insideZeppelinKalimdorToTirisfal);
                WaitOnTransport(tirisfalPlatformZepOrgrimmar, 15);
            }
        }

        public void PortalFromOrgrimmarToBlastedLands()
        {
            Logger.Log("Taking portal to Blasted Lands");
            GoToTask.ToPositionAndIntecractWithGameObject(oGPortalToBlastedLandsPosition, oGPortalToBlastedLandsId);
            Thread.Sleep(5000);
        }

        public void ShipStormwindToBoreanTundra()
        {
            Logger.Log("Taking ship to Borean Tundra");
            GoToTask.ToPosition(bayStormwindToBoreanTundra, 0, true);
            if (ObjectManager.Me.Position.DistanceTo(bayStormwindToBoreanTundra) < 4)
            {
                WaitForTransport(shipStormwindToBoreanTundraId, 30);
                GoToTask.ToPosition(insideShipStormwindToBoreanTundra, 1);
                WaitOnTransport(bayBoreanTundraToStormwind, 25);
            }
        }

        public void ZeppelinOrgrimmarToNorthrend()
        {
            Logger.Log("Taking zeppelin to Northrend");
            GoToTask.ToPosition(kalimdorlPlatformZepNorthrend, 0, true);
            if (ObjectManager.Me.Position.DistanceTo(kalimdorlPlatformZepNorthrend) < 4)
            {
                WaitForTransport(zeppelinKalidmdorToNorthrendId, 30);
                GoToTask.ToPosition(insideZeppelinKalimdorToNorthrend, 1);
                WaitOnTransport(northrendPlatformZepKalimdor, 25);
            }
        }

        // ********** FROM NORTHREND **********
        public void ShipBoreanTundraToStormwind()
        {
            Logger.Log("Taking ship to Stormwind");
            GoToTask.ToPosition(bayBoreanTundraToStormwind, 0, true);
            if (ObjectManager.Me.Position.DistanceTo(bayBoreanTundraToStormwind) < 4)
            {
                WaitForTransport(shipStormwindToBoreanTundraId, 30);
                GoToTask.ToPosition(insideShipBoreanTundraToStormwind, 1);
                WaitOnTransport(bayStormwindToBoreanTundra, 25);
            }
        }
        public void PortalDalaranToUndercity()
        {
            Logger.Log("Taking portal to Undercity");
            GoToTask.ToPositionAndIntecractWithGameObject(dalaranPortalToUndercityPosition, dalaranPortalToUndercityId);
            Thread.Sleep(5000);
        }

        public void PortalDalaranToOrgrimmar()
        {
            Logger.Log("Taking portal to Orgrimmar");
            GoToTask.ToPositionAndIntecractWithGameObject(dalaranPortalToOGPosition, dalaranPortalToOGId);
            Thread.Sleep(5000);
        }

        public void AlliacePortalDalaranToShattrath()
        {
            Logger.Log("Taking portal to Shattrath");
            GoToTask.ToPositionAndIntecractWithGameObject(allianceDalaranPortalToShattrathPosition, allianceDalaranPortalToShattrathId);
            Thread.Sleep(5000);
        }

        public void HordePortalDalaranToShattrath()
        {
            Logger.Log("Taking portal to Shattrath");
            GoToTask.ToPositionAndIntecractWithGameObject(dalaranPortalToShattrathPosition, dalaranPortalToShattrathId);
            Thread.Sleep(5000);
        }

        // ************************************

        // Add all offmesh connections
        public void AddAllOffmeshConnections()
        {
            Logger.Log("Adding offmesh connections");
            if (OffMeshConnections.MeshConnection == null || OffMeshConnections.MeshConnection.Count <= 0)
                OffMeshConnections.Load();

            OffMeshConnections.MeshConnection.Clear();

            List<OffMeshConnection> offmeshConnections = new List<OffMeshConnection>
            {
                /*
                //Alliance  Start
                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(bayAzuremystToDarkshore),
                    new Vector3(insideShipAzuremystToDarkshore)
                }, (int) ContinentId.Expansion01, OffMeshConnectionType.Bidirectional, true),

                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(bayDarkshoreToAzuremyst),
                    new Vector3(insideShipDarkshoreToAzuremyst)
                }, (int) ContinentId.Kalimdor, OffMeshConnectionType.Bidirectional, true),

                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(bayDarkshoreToDarnassus),
                    new Vector3(insideShipDarkshoreToDarnassus)
                }, (int) ContinentId.Kalimdor, OffMeshConnectionType.Bidirectional, true),

                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(bayDarnassusToDarkshore),
                    new Vector3(insideShipDarnassusToDarkshore)
                }, (int) ContinentId.Kalimdor, OffMeshConnectionType.Bidirectional, true),

                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(bayDarkshoreToStormwind),
                    new Vector3(insideShipDarkshoreToStormwind)
                }, (int) ContinentId.Kalimdor, OffMeshConnectionType.Bidirectional, true),

                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(bayStormwindToDarkshore),
                    new Vector3(insideShipStormwindToDarkshore)
                }, (int) ContinentId.Azeroth, OffMeshConnectionType.Bidirectional, true),

                //Alliance End

                //Horde  Start
                
                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(tirisfalPlatformZepOrgrimmar),
                    new Vector3(insideZeppelinTirisfalToOrgrimmar)
                }, (int) ContinentId.Azeroth, OffMeshConnectionType.Bidirectional, true),

                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(orgrimmarPlatformZepTirisfal),
                    new Vector3(insideZeppelinKalimdorToTirisfal)
                }, (int) ContinentId.Kalimdor, OffMeshConnectionType.Bidirectional, true),
                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(tirisfalPlatformZepNorthrend),
                    new Vector3(insideZeppelinTirisfalToNorthrend)
                }, (int) ContinentId.Azeroth, OffMeshConnectionType.Bidirectional, true),
                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(northrendPlatformZepTirisfal),
                    new Vector3(insideZeppelinNorthrendToTirisfal)
                }, (int) ContinentId.Northrend, OffMeshConnectionType.Bidirectional, true),
                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(kalimdorlPlatformZepNorthrend),
                    new Vector3(insideZeppelinKalimdorToNorthrend)
                }, (int) ContinentId.Kalimdor, OffMeshConnectionType.Bidirectional, true),
                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(northrendPlatformZepKalimdor),
                    new Vector3(insideZeppelinNorthrendToKalimdor)
                }, (int) ContinentId.Northrend, OffMeshConnectionType.Bidirectional, true),
                
                // Kalimdor Tower to Zep Northrend
                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(1176.045, -4176.415, 21.40396),
                    new Vector3(1168.245, -4165.519, 22.72018),
                    new Vector3(1162.044, -4162.295, 23.09159),
                    new Vector3(1158.513, -4156.68, 25.38495),
                    new Vector3(1163.283, -4152.811, 28.29832),
                    new Vector3(1165.906, -4158.321, 30.88497),
                    new Vector3(1160.758, -4159.898, 33.17702),
                    new Vector3(1159.89, -4153.644, 36.38201),
                    new Vector3(1165.508, -4154.742, 39.0405),
                    new Vector3(1163.239, -4160.285, 41.67056),
                    new Vector3(1157.424, -4157.666, 43.97564),
                    new Vector3(1160.468, -4152.043, 46.343),
                    new Vector3(1165.743, -4155.318, 48.80746),
                    new Vector3(1162.361, -4160.978, 51.57532),
                    new Vector3(1159.651, -4166.412, 51.64627),
                    new Vector3(1166.205, -4167.621, 51.64627),
                    new Vector3(1171.159, -4162.846, 51.64627),
                    new Vector3(1173.271, -4156.186, 51.64627),
                    kalimdorlPlatformZepNorthrend
                }, (int) ContinentId.Kalimdor, OffMeshConnectionType.Bidirectional, true),

                // Kalimdor Tower to Zep EK
                new OffMeshConnection(new List<Vector3>
                {
                    new Vector3(1336.811, -4627.349, 23.71372),
                    new Vector3(1338.415, -4635.818, 24.5164),
                    new Vector3(1342.444, -4641.667, 24.60308),
                    new Vector3(1347.086, -4646.91, 25.73846),
                    new Vector3(1346.112, -4653.215, 28.24372),
                    new Vector3(1340.509, -4651.799, 30.71605),
                    new Vector3(1344.352, -4645.396, 34.15182),
                    new Vector3(1347.778, -4650.873, 36.73572),
                    new Vector3(1339.766, -4649.848, 41.08609),
                    new Vector3(1343.882, -4645.242, 43.53802),
                    new Vector3(1348.05, -4651.791, 46.52235),
                    new Vector3(1342.296, -4654.397, 48.70516),
                    new Vector3(1339.82, -4648.606, 51.09646),
                    new Vector3(1349.617, -4644.258, 53.52875),
                    new Vector3(1346.508, -4638.681, 53.52875),
                    new Vector3(1339.75, -4639.255, 53.52875),
                    new Vector3(1334.665, -4644.297, 53.52875),
                    new Vector3(1326.729, -4649.013, 53.99952)
                }, (int) ContinentId.Kalimdor, OffMeshConnectionType.Bidirectional, true)*/

                //Horde  End
            };

            OffMeshConnections.MeshConnection.AddRange(offmeshConnections);
            OffMeshConnections.Save();
        }

        // Wait for GameObject (Zep, elevator etc..)
        public void WaitForTransport(int transportId, int distance)
        {
            // If the zep is already here, we skip
            if (ObjectManager.GetWoWGameObjectByEntry(transportId).Count > 0 &&
                ObjectManager.GetWoWGameObjectByEntry(transportId).OrderBy(o => o.GetDistance).FirstOrDefault().GetDistance2D <= distance)
                return;

            Logger.Log("Waiting for transport...");
            // Wait for zep
            while (ObjectManager.GetWoWGameObjectByEntry(transportId).Count <= 0 ||
                ObjectManager.GetWoWGameObjectByEntry(transportId).Count > 0 &&
                ObjectManager.GetWoWGameObjectByEntry(transportId).OrderBy(o => o.GetDistance).FirstOrDefault().GetDistance2D > distance)
            {
                if (ObjectManager.GetWoWGameObjectByEntry(transportId).Count <= 0)
                    Logger.Log("The transport is not on in sight yet.");
                else
                    Logger.Log(ObjectManager.GetWoWGameObjectByEntry(transportId).OrderBy(o => o.GetDistance).FirstOrDefault().GetDistance2D.ToString());
                Thread.Sleep(5000);
            }

            // Wait 5 sec before hopping in
            if (ObjectManager.GetWoWGameObjectByEntry(transportId).OrderBy(o => o.GetDistance).FirstOrDefault().GetDistance2D <= distance)
                Thread.Sleep(5000);
        }

        // Wait on transport (Zepp, elevator etc..)
        private void WaitOnTransport(Vector3 arrivalPoint, int distance)
        {
            if (ObjectManager.Me.InTransport)
            {
                Logger.Log("Waiting inside transport...");
                while (ObjectManager.Me.Position.DistanceTo(arrivalPoint) > distance)
                {
                    Thread.Sleep(5000);
                    Logger.Log($"We are {ObjectManager.Me.Position.DistanceTo(arrivalPoint)} yards away from arrival.");
                }
                Thread.Sleep(3000);
                ForceMoveTo(arrivalPoint);
            }
            else
                Logger.Log("ERROR : Not on transport");
        }

        private void ForceMoveTo(Vector3 destination)
        {
            MovementManager.MoveTo(destination);
            while (MovementManager.InMoveTo)
                Thread.Sleep(20);
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
    None
}