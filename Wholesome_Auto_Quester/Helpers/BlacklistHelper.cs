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
