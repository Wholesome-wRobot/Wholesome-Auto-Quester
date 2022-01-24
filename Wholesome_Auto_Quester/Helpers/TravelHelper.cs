using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.PathFinder;

namespace Wholesome_Auto_Quester.Helpers
{
    public class TravelHelper
    {

        // Zeppelins
        static readonly Vector3 tirisfalPlatformZepOrgrimmar = new Vector3(2066.407, 285.9733, 97.03147);
        static readonly Vector3 insideZeppelinTirisfalToOrgrimmar = new Vector3(2068.5, 296.4182, 97.28456);

        static readonly Vector3 kalimdorlPlatformZepTirisfal = new Vector3(1324.798, -4652.622, 53.78583);
        static readonly Vector3 insideZeppelinKalimdorToTirisfal = new Vector3(1313.312, -4652.777, 54.19608);

        static readonly Vector3 tirisfalPlatformZepNorthrend = new Vector3(2061.558, 359.2196, 83.47895);
        static readonly Vector3 insideZeppelinTirisfalToNorthrend = new Vector3(2059.505, 374.1549, 82.44972);

        static readonly Vector3 northrendPlatformZepTirisfal = new Vector3(1973.989, -6099.464, 67.15666);
        static readonly Vector3 insideZeppelinNorthrendToTirisfal = new Vector3(1984.397, -6089.137, 67.68417);

        static readonly Vector3 kalimdorlPlatformZepNorthrend = new Vector3(1179.33, -4150.091, 52.13512);
        static readonly Vector3 insideZeppelinKalimdorToNorthrend = new Vector3(1192.992, -4142.117, 52.73592);

        static readonly Vector3 northrendPlatformZepKalimdor = new Vector3(2829.167, 6178.443, 121.9824f);
        static readonly Vector3 insideZeppelinNorthrendToKalimdor = new Vector3(2844.347, 6192.584, 122.2752);


        static readonly int ZeppelinTirisfalToOrgrimmarId = 164871;
        static readonly int ZeppelinKalidmdorToNorthrendId = 186238;

        //Ships
        static readonly Vector3 bayAzuremystToDarkshore = new Vector3(-4263.875, -11335.03, 5.859486, "None");
        static readonly Vector3 insideShipAzuremystToDarkshore = new Vector3(-4262.877, -11324.02, 4.91574, "None");

        static readonly Vector3 bayDarkshoreToAzuremyst = new Vector3(6542.727, 922.8457, 5.912864, "None");
        static readonly Vector3 insideShipDarkshoreToAzuremyst = new Vector3(6550.912, 945.3401, 4.898618, "None");

        static readonly Vector3 bayDarkshoreToDarnassus = new Vector3(6576.859, 769.8532, 5.588161, "None");
        static readonly Vector3 insideShipDarkshoreToDarnassus = new Vector3(6587.057, 767.6591, 5.10977, "None");

        static readonly Vector3 bayDarnassusToDarkshore = new Vector3(8553.19, 1020.2, 5.543461, "None");
        static readonly Vector3 insideShipDarnassusToDarkshore = new Vector3(8541.434, 1021.083, 5.085521, "None");

        static readonly Vector3 bayDarkshoreToStormwind = new Vector3(6424.98, 818.6711, 5.490422, "None");
        static readonly Vector3 insideShipDarkshoreToStormwind = new Vector3(6414.219, 822.085, 6.1804, "None");

        static readonly Vector3 bayStormwindToDarkshore = new Vector3(-8640.113, 1327.584, 5.233279, "None");
        static readonly Vector3 insideShipStormwindToDarkshore = new Vector3(-8646.059, 1339.308, 6.215415, "None");

        static readonly Vector3 bayStormwindToBoreanTundra = new Vector3(-8295.06, 1408.721, 4.688217, "None");
        static readonly Vector3 insideShipStormwindToBoreanTundra = new Vector3(-8291.178, 1427.616, 9.473845, "None");

        static readonly Vector3 bayBoreanTundraToStormwind = new Vector3(2231.234, 5135.821, 5.343364, "None");
        static readonly Vector3 insideShipBoreanTundraToStormwind = new Vector3(2232.324, 5114.973, 9.400736, "None");

        /*static readonly int ShipAzuremystToDarkshoreId = 181646;
        static readonly int ShipDarkshoreToDarnassusId = 176244;
        static readonly int ShipDarkshoreToStormwindId = 176310;*/
        static readonly int ShipStormwindToBoreanTundraId = 190536;


        // Portals
        static readonly int OGPortalToBlastedLandsId = 195142;
        static readonly Vector3 OGPortalToBlastedLandsPosition = new Vector3(1472.55f, -4215.7f, 59.221f);

        /*static readonly int ExodarPortalToBlastedLandsId = 195141;
        static readonly Vector3 ExodarPortalToBlastedLandsPosition = new Vector3(-4037.81, -11555.6, -138.324f);

        static readonly int DarnassusPortalToBlastedlandsId = 195141;
        static readonly Vector3 DarnassusPortalToBlastedlandsPosition = new Vector3(9661.83, 2509.61, 1331.63f);*/

        static readonly int StormwindPortalToBlastedlandsId = 195141;
        static readonly Vector3 StormwindPortalToBlastedlandsPosition = new Vector3(-9007.58, 871.87, 129.692f);

        //  Shattrath portals
        static readonly int shattrathPortalToKalimdorId = 183323;
        static readonly Vector3 shattrathPortalToKalimdorPosition = new Vector3(-1934.205f, 5452.766f, -12.42705f);

        /*static readonly int shattrathPortalToThunderbluffId = 183326;
        static readonly Vector3 shattrathPortalToThunderbluffPosition = new Vector3(-1936.32, 5445.95, -12.4282f);

        static readonly int shattrathPortalToUndercityId = 183327;
        static readonly Vector3 shattrathPortalToUndercityPosition = new Vector3(-1931.48, 5460.49, -12.4281f);

        static readonly int shattrathPortalTQueldanasId = 187056;*/
        static readonly Vector3 shattrathPortalToQueldanasPosition = new Vector3(-1839.88, 5500.6, -12.4279f);

        static readonly int ShattrathPortalToDarnassusId = 183317;
        static readonly Vector3 ShattrathPortalToDarnassusPosition = new Vector3(-1790.98, 5413.98, -12.4282f);

        /*static readonly int ShattrathPortalToStormwindId = 183325;
        static readonly Vector3 ShattrathPortalToStormwindPosition = new Vector3(-1792.78, 5406.54, -12.4279f);

        static readonly int ShattrathPortalToIronforgeId = 183322;
        static readonly Vector3 ShattrathPortalToIronforgePosition = new Vector3(-1795.79, 5399.63, -12.4281f);

        static readonly int ShattrathPortalToExodarId = 183321;
        static readonly Vector3 ShattrathPortalToExodarPosition = new Vector3(-1880.28, 5357.53, -12.4281f);

        static readonly int ShattrathPortalToSilvermoonId = 183324;
        static readonly Vector3 ShattrathPortalToSilvermoonPosition = new Vector3(-1894.69, 5362.34, -12.4282f);*/


        // Dalaran portals
        /*static readonly int DalaranPortalToStormwindId = 190960;
        static readonly Vector3 DalaranPortalToStormwindPosition = new Vector3(5719.19, 719.681, 641.728f);

        static readonly int DalaranPortalToIronforgeId = 191008;
        static readonly Vector3 DalaranPortalToIronforgePosition = new Vector3(5712.68, 724.845, 641.736f);

        static readonly int DalaranPortalToDarnassusId = 191006;
        static readonly Vector3 DalaranPortalToDarnassusPosition = new Vector3(5706.16, 730.102, 641.745f);

        static readonly int DalaranPortalToExodarId = 191007;
        static readonly Vector3 DalaranPortalToExodarPosition = new Vector3(5699.58, 735.469, 641.769f);*/

        static readonly int AllianceDalaranPortalToShattrathId = 191013;
        static readonly Vector3 AllianceDalaranPortalToShattrathPosition = new Vector3(5697.49, 744.912, 641.819f);

        static readonly int DalaranPortalToUndercityId = 191012;
        static readonly Vector3 DalaranPortalToUndercityPosition = new Vector3(5934.66, 590.688, 640.575, "Flying");

        static readonly int DalaranPortalToOGId = 191009;
        static readonly Vector3 DalaranPortalToOGPosition = new Vector3(5925.85, 593.25, 640.563, "Flying");

        static readonly int HordeDalaranPortalToShattrathId = 191014;
        static readonly Vector3 HordeDalaranPortalToShattrathPosition = new Vector3(5941.66, 584.887, 640.574, "Flying");



        // Add all offmesh connections
        public static void AddAllOffmeshConnections()
        {
            Logger.LogDebug("Adding offmesh connections");
            if (OffMeshConnections.MeshConnection == null || OffMeshConnections.MeshConnection.Count <= 0)
                OffMeshConnections.Load();

            var me = new List<OffMeshConnection>
        {

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
                new Vector3(kalimdorlPlatformZepTirisfal),
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
            }, (int) ContinentId.Kalimdor, OffMeshConnectionType.Bidirectional, true)

            //Horde  End
        };

            OffMeshConnections.MeshConnection.AddRange(me);
        }

        // Return the continent id of the zone
        public static int GetContinentFromZoneName(string zoneName)
        {
            string escapedZoneName = zoneName.Replace("'", "\\'");
            Logger.LogDebug($"Searching for zone {escapedZoneName}");
            string lua = @"local result
                        local function LoadZones(...)
                            local array = {}
	                        for i=1,select('#', ...),1 do
		                        array[i] = select(i,...)
                                if array[i] == '" + escapedZoneName + @"' then
                                    return true
                                end
	                        end
                            return false
                        end
                        if LoadZones(GetMapZones(1))
                            then result = 1
                        elseif LoadZones(GetMapZones(2))
                            then result = 0
                        elseif LoadZones(GetMapZones(3))
                            then result = 530
                        elseif LoadZones(GetMapZones(4))
                            then result = 571
                        else
                            result = -1
                        end
                        return result";

            return Lua.LuaDoString<int>(lua);
        }

        // Wait for GameObject (Zep, elevator etc..)
        public static void WaitForTransport(int zepID, int distance)
        {
            // If the zep is already here, we skip
            if (ObjectManager.GetWoWGameObjectByEntry(zepID).Count > 0 &&
                ObjectManager.GetWoWGameObjectByEntry(zepID).OrderBy(o => o.GetDistance).FirstOrDefault().GetDistance2D <= distance)
                return;

            Logger.LogDebug("Waiting for transport...");
            // Wait for zep
            while (ObjectManager.GetWoWGameObjectByEntry(zepID).Count <= 0 ||
                (ObjectManager.GetWoWGameObjectByEntry(zepID).Count > 0 &&
                ObjectManager.GetWoWGameObjectByEntry(zepID).OrderBy(o => o.GetDistance).FirstOrDefault().GetDistance2D > distance))
            {
                if (ObjectManager.GetWoWGameObjectByEntry(zepID).Count <= 0)
                    Logger.LogDebug("The transport is not on this continent yet.");
                else
                    Logger.LogDebug(ObjectManager.GetWoWGameObjectByEntry(zepID).OrderBy(o => o.GetDistance).FirstOrDefault().GetDistance2D.ToString());
                Thread.Sleep(5000);
            }

            // Wait 5 sec before hopping in
            if (ObjectManager.GetWoWGameObjectByEntry(zepID).OrderBy(o => o.GetDistance).FirstOrDefault().GetDistance2D <= distance)
                Thread.Sleep(5000);
        }

        // Wait on transport (Zepp, elevator etc..)
        private static void WaitOnTransport(Vector3 arrivalPoint, int distance, ContinentId destinationContinent)
        {
            if (ObjectManager.Me.InTransport)
            {
                Logger.LogDebug("Waiting inside transport...");
                while (ObjectManager.Me.Position.DistanceTo(arrivalPoint) > distance)
                {
                    Thread.Sleep(5000);
                    Logger.LogDebug($"We are {ObjectManager.Me.Position.DistanceTo(arrivalPoint)} yards away from arrival.");
                }
            }
            else
                Logger.Log("ERROR : Not on transport");
        }

        // ******************************** HORDE ********************************

        // ********** FROM EK **********
        // To Outland
        public static void AllianceEKToOutland()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Azeroth)
            {
                GoToTask.ToPosition(new Vector3(-11920.39, -3206.81, -15.35475f));
                Thread.Sleep(5000);
                GoToTask.ToPosition(new Vector3(-182.5485, 1023.459, 54.23014));
                Thread.Sleep(5000);
            }
        }
        public static void HordeEKToOutland()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Azeroth)
            {
                GoToTask.ToPosition(new Vector3(-11920.39, -3206.81, -15.35475f));
                Thread.Sleep(5000);
                GoToTask.ToPosition(new Vector3(-182.5485, 1023.459, 54.23014));
                Thread.Sleep(5000);
            }
        }

        // To Kalimdor
        public static void AllianceEKToKalimdor()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Azeroth)
            {

            }
        }
        public static void HordeEKToKalimdor()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Azeroth)
            {
                GoToTask.ToPosition(tirisfalPlatformZepOrgrimmar);
                WaitForTransport(ZeppelinTirisfalToOrgrimmarId, 15);
                GoToTask.ToPosition(insideZeppelinTirisfalToOrgrimmar, 1);
                WaitOnTransport(kalimdorlPlatformZepTirisfal, 15, ContinentId.Kalimdor);
            }
        }

        // ********** FROM OUTLAND **********
        // To Kalimdor
        public static void AllianceOutlandToKalimdor()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Expansion01)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(ShattrathPortalToDarnassusPosition, ShattrathPortalToDarnassusId);
                Thread.Sleep(5000);
            }
        }

        public static void HordeOutlandToKalimdor()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Expansion01)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(shattrathPortalToKalimdorPosition, shattrathPortalToKalimdorId);
                Thread.Sleep(5000);
            }
        }

        // ********** FROM KALIMDOR **********
        // To EK
        public static void AllianceEKToBlastedLands()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Kalimdor)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(StormwindPortalToBlastedlandsPosition, StormwindPortalToBlastedlandsId);
                Thread.Sleep(5000);
            }
        }

        public static void HordeKalimdorToEK()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Kalimdor)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(OGPortalToBlastedLandsPosition, OGPortalToBlastedLandsId);
                Thread.Sleep(5000);
            }
        }

        // To Northrend 
        public static void AllianceAzerothToNorthrend()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Kalimdor)
            {
                GoToTask.ToPosition(bayStormwindToBoreanTundra, 0, true);
                WaitForTransport(ShipStormwindToBoreanTundraId, 30);
                GoToTask.ToPosition(insideShipStormwindToBoreanTundra, 1);
                WaitOnTransport(bayBoreanTundraToStormwind, 25, ContinentId.Northrend);
            }
        }

        public static void HordeKalimdorToNorthrend()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Kalimdor)
            {
                // first go down the tower
                GoToTask.ToPosition(new Vector3(1168.775, -4164.387, 22.71999));
                // then almost all the way up the tower
                GoToTask.ToPosition(new Vector3(1166.608, -4157.879, 49.74893));
                GoToTask.ToPosition(kalimdorlPlatformZepNorthrend, 0, true);
                WaitForTransport(ZeppelinKalidmdorToNorthrendId, 30);
                GoToTask.ToPosition(insideZeppelinKalimdorToNorthrend, 1);
                WaitOnTransport(northrendPlatformZepKalimdor, 25, ContinentId.Northrend);
            }
        }

        // ********** FROM NORTHREND **********
        //To EK
        public static void AllianceNorthrendToEK()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Northrend)
            {
                GoToTask.ToPosition(bayBoreanTundraToStormwind, 0, true);
                WaitForTransport(ShipStormwindToBoreanTundraId, 30);
                GoToTask.ToPosition(insideShipBoreanTundraToStormwind, 1);
                WaitOnTransport(bayStormwindToBoreanTundra, 25, ContinentId.Northrend);
            }
        }
        public static void HordeNorthrendToEK()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Northrend)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(DalaranPortalToUndercityPosition, DalaranPortalToUndercityId);
                Thread.Sleep(5000);
            }
        }

        //To Kalimdor
        public static void AllianceNorthrendToKalimdor()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Northrend)
            {

            }
        }

        public static void HordeNorthrendToKalimdor()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Northrend)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(DalaranPortalToOGPosition, DalaranPortalToOGId);
                Thread.Sleep(5000);
            }
        }

        //To Outland
        public static void AllianceNorthrendToOutland()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Northrend)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(AllianceDalaranPortalToShattrathPosition, AllianceDalaranPortalToShattrathId);
                Thread.Sleep(5000);
            }
        }

        public static void HordeNorthrendToOutland()
        {
            if (Usefuls.ContinentId == (int)ContinentId.Northrend)
            {
                GoToTask.ToPositionAndIntecractWithGameObject(HordeDalaranPortalToShattrathPosition, HordeDalaranPortalToShattrathId);
                Thread.Sleep(5000);
            }
        }
    }
}
