using robotManager.Helpful;
using System.Collections.Generic;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;

namespace Wholesome_Auto_Quester.Helpers
{
    public class BlacklistHelper
    {
        private readonly static List<WAQBlacklistEntry> ListEntries = new List<WAQBlacklistEntry>();

        public static void AddZone(Vector3 position, int radius, string reason, int timeInMs = 1000 * 60 * 15)
        {
            if (!ListEntries.Exists(wbl => wbl.Position == position))
            {
                Logger.Log($"Adding {position} to zone blacklist ({reason})");
                ListEntries.Add(new WAQBlacklistEntry(position, radius, timeInMs));
            }
        }

        public static void AddNPC(ulong guid, string reason, int timeInMs = 1000 * 60 * 15)
        {
            if (!ListEntries.Exists(wbl => wbl.Guid == guid))
            {
                Logger.Log($"Adding {guid} to NPC blacklist ({reason})");
                ListEntries.Add(new WAQBlacklistEntry(guid, timeInMs));
            }
        }

        public static void AddDefaultBLZones()
        {
            // Faction specific
            if (ToolBox.IsHorde())
            {
                // Astranaar
                wManagerSetting.AddBlackListZone(new Vector3(2735.73, -373.2593, 107.1535), 160, ContinentId.Kalimdor, isSessionBlacklist: true);
            }
            else
            {
                // Crossroads
                wManagerSetting.AddBlackListZone(new Vector3(-452.84, -2650.76, 95.5209), 160, ContinentId.Kalimdor, isSessionBlacklist: true);
                // Ratchet
                wManagerSetting.AddBlackListZone(new Vector3(-956.664, -3754.71, 5.33239), 160, ContinentId.Kalimdor, isSessionBlacklist: true);
                // BrackenWall Village
                wManagerSetting.AddBlackListZone(new Vector3(-3124.758, -2882.661, 34.73463), 130, ContinentId.Kalimdor, isSessionBlacklist: true);
            }

            // Drak'Tharon Keep
            wManagerSetting.AddBlackListZone(new Vector3(4643.429, -2043.915, 184.1842), 200, ContinentId.Northrend, isSessionBlacklist: true);

            // Blue sky logging camp water
            wManagerSetting.AddBlackListZone(new Vector3(4321.85, -3021.175, 305.8569), 50, ContinentId.Northrend, isSessionBlacklist: true);

            // Avoid Orgrimmar Braseros
            wManagerSetting.AddBlackListZone(new Vector3(1731.702, -4423.403, 36.86293), 5, ContinentId.Kalimdor, isSessionBlacklist: true);
            wManagerSetting.AddBlackListZone(new Vector3(1669.99, -4359.609, 29.23425), 5, ContinentId.Kalimdor, isSessionBlacklist: true);

            // Warsong hold top elevator
            wManagerSetting.AddBlackListZone(new Vector3(2892.18, 6236.34, 208.908), 15, ContinentId.Northrend, isSessionBlacklist: true);

            // Portal Rut'Theran UP/DOWN
            wManagerSetting.AddBlackListZone(new Vector3(9946.391, 2630.067, 1316.194), 15, ContinentId.Kalimdor, isSessionBlacklist: true);
            wManagerSetting.AddBlackListZone(new Vector3(8798.752, 969.5687, 30.38474), 15, ContinentId.Kalimdor, isSessionBlacklist: true);

            // Staghein Point
            wManagerSetting.AddBlackListZone(new Vector3(-6427.419, 219.1993, 4.853653), 70, ContinentId.Kalimdor, isSessionBlacklist: true);

            // Hellfire giants passage
            wManagerSetting.AddBlackListZone(new Vector3(41.35702, 4443.034, 81.65746), 70, ContinentId.Expansion01, isSessionBlacklist: true);

            // Telredor base
            wManagerSetting.AddBlackListZone(new Vector3(283.2617, 6052.715, 23.4), 60, ContinentId.Expansion01, isSessionBlacklist: true);

            // Shadowmoon pool
            wManagerSetting.AddBlackListZone(new Vector3(-4204.122, 1712.808, 88.00595), 60, ContinentId.Expansion01, isSessionBlacklist: true);
            wManagerSetting.AddBlackListZone(new Vector3(-4189.208, 2012.61, 57.39383), 50, ContinentId.Expansion01, isSessionBlacklist: true);
        }

        public static void CleanupBlacklist()
        {
            foreach (WAQBlacklistEntry entry in ListEntries)
            {
                if (entry.Guid > 0 && entry.ShouldBeRemoved)
                {
                    wManagerSetting.RemoveBlackList(entry.Guid);
                }
                if (entry.Position != null && entry.ShouldBeRemoved)
                {
                    wManagerSetting.GetListZoneBlackListed().RemoveAll(bl => bl.GetPosition() == entry.Position);
                }
            }
            ListEntries.RemoveAll(le => le.ShouldBeRemoved);
        }
    }

    public class WAQBlacklistEntry
    {
        private Timer _timer;
        public Vector3 Position;
        public ulong Guid;
        public int Radius;

        public WAQBlacklistEntry(Vector3 position, int radius, int timeInMs)
        {
            Position = position;
            Radius = radius;
            _timer = new Timer(timeInMs);
            wManagerSetting.AddBlackListZone(position, radius, (ContinentId)Usefuls.ContinentId, isSessionBlacklist: true);
        }

        public WAQBlacklistEntry(ulong guid, int timeInMs)
        {
            Guid = guid;
            _timer = new Timer(timeInMs);
            wManagerSetting.AddBlackList(guid, timeInMs, true);
        }

        public bool ShouldBeRemoved => _timer.IsReady;
    }
}
