using robotManager.Helpful;
using System.Collections.Generic;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using static robotManager.Helpful.Timer;

namespace Wholesome_Auto_Quester.Helpers
{
    public class BlacklistHelper
    {
        public static List<WAQBlacklistEntry> ListEntries = new List<WAQBlacklistEntry>();

        public static void AddQuestToBlackList(int questId)
        {
            if (!WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Contains(questId))
            {
                WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Add(questId);
                WholesomeAQSettings.CurrentSetting.Save();
                Logger.Log($"The quest {questId} has been blacklisted");
            }
        }

        public static void RemoveQuestFromBlackList(int questId)
        {
            if (WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Contains(questId))
            {
                WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Remove(questId);
                WholesomeAQSettings.CurrentSetting.Save();
                Logger.Log($"The quest {questId} has been removed from the blacklist");
            }
        }

        public static void AddZone(Vector3 position, int radius, int timeInMs = 1000 * 60 * 15)
        {
            ListEntries.Add(new WAQBlacklistEntry(position, radius, timeInMs));
        }

        public static void AddNPC(ulong guid, int timeInMs = 1000 * 60 * 15)
        {
            ListEntries.Add(new WAQBlacklistEntry(guid, timeInMs));
        }

        public static void CleanupBlacklist()
        {
            ListEntries.ForEach(le =>
            {
                if (le.Guid > 0 && le.ShouldBeRemoved)
                    wManagerSetting.RemoveBlackList(le.Guid);
                if (le.Position != null && le.ShouldBeRemoved)
                    wManagerSetting.GetListZoneBlackListed().RemoveAll(bl => bl.GetPosition() == le.Position);
            });
            ListEntries.RemoveAll(le => le.ShouldBeRemoved);
        }
    }

    public class WAQBlacklistEntry
    {
        public Vector3 Position;
        public ulong Guid;
        public int Radius;
        Timer _timer;

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
