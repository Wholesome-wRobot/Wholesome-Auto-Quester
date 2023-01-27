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
                    wManagerSetting.GetListZoneBlackListed().RemoveAll(bl => bl.GetPosition().DistanceTo(entry.Position) < 5);
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
