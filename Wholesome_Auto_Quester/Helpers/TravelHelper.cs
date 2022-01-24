using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.PathFinder;

public static class TravelHelper
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

    // Portals
    static readonly int shattrathPortalToKalimdorId = 183323;
    static readonly Vector3 shattrathPortalToKalimdorPosition = new Vector3(-1934.205f, 5452.766f, -12.42705f);

    static readonly int OGPortalToBlastedLandsId = 195142;
    static readonly Vector3 OGPortalToBlastedLandsPosition = new Vector3(1472.55f, -4215.7f, 59.221f);

    // Dalaran portals
    static readonly int DalaranPortalToUndercityId = 191012;
    static readonly Vector3 DalaranPortalToUndercityPosition = new Vector3(5934.66, 590.688, 640.575, "Flying");

    static readonly int DalaranPortalToOGId = 191009;
    static readonly Vector3 DalaranPortalToOGPosition = new Vector3(5925.85, 593.25, 640.563, "Flying");

    static readonly int DalaranPortalToShattrathId = 191014;
    static readonly Vector3 DalaranPortalToShattrathPosition = new Vector3(5941.66, 584.887, 640.574, "Flying");



    // Add all offmesh connections
    public static void AddAllOffmeshConnections()
    {
        Logger.LogDebug("Adding offmesh connections");
        if (OffMeshConnections.MeshConnection == null || OffMeshConnections.MeshConnection.Count <= 0)
            OffMeshConnections.Load();

        var me = new List<OffMeshConnection>
        {
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
            Logger.LogError("ERROR : Not on transport");
    }

    // ******************************** HORDE ********************************

    // ********** FROM EK **********
    // To Outland
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
    public static void HordeKalimdorToEK()
    {
        if (Usefuls.ContinentId == (int)ContinentId.Kalimdor)
        {
            GoToTask.ToPositionAndIntecractWithGameObject(OGPortalToBlastedLandsPosition, OGPortalToBlastedLandsId);
            Thread.Sleep(5000);
        }
    }

    // To Northrend 
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
    public static void HordeNorthrendToEK()
    {
        if (Usefuls.ContinentId == (int)ContinentId.Northrend)
        {
            GoToTask.ToPositionAndIntecractWithGameObject(DalaranPortalToUndercityPosition, DalaranPortalToUndercityId);
            Thread.Sleep(5000);
        }
    }

    //To Kalimdor
    public static void HordeNorthrendToKalimdor()
    {
        if (Usefuls.ContinentId == (int)ContinentId.Northrend)
        {
            GoToTask.ToPositionAndIntecractWithGameObject(DalaranPortalToOGPosition, DalaranPortalToOGId);
            Thread.Sleep(5000);
        }
    }

    //To Outland
    public static void HordeNorthrendToOutland()
    {
        if (Usefuls.ContinentId == (int)ContinentId.Northrend)
        {
            GoToTask.ToPositionAndIntecractWithGameObject(DalaranPortalToShattrathPosition, DalaranPortalToShattrathId);
            Thread.Sleep(5000);
        }
    }
}
