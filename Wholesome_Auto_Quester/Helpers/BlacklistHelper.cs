using robotManager.Helpful;
using System.Collections.Generic;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;

namespace Wholesome_Auto_Quester.Helpers
{
    public class BlacklistHelper
    {
        public static List<WAQBlacklistEntry> ListEntries = new List<WAQBlacklistEntry>();

        public static string GetQuestBlacklistReason(int questId)
        {
            return WholesomeAQSettings.CurrentSetting.BlackListedQuests.Find(blq => blq.Id == questId).Reason;
        }

        public static void AddQuestToBlackList(int questId, string reason)
        {
            if (!WholesomeAQSettings.CurrentSetting.BlackListedQuests.Exists(blq => blq.Id == questId))
            {
                WholesomeAQSettings.CurrentSetting.BlackListedQuests.Add(new BlackListedQuest(questId, reason));
                WholesomeAQSettings.CurrentSetting.Save();
                Logger.Log($"The quest {questId} has been blacklisted ({reason})");
            }
        }

        public static void RemoveQuestFromBlackList(int questId, string reason)
        {
            BlackListedQuest questToRemove = WholesomeAQSettings.CurrentSetting.BlackListedQuests.Find(blq => blq.Id == questId);
            if (questToRemove.Id != 0)
            {
                WholesomeAQSettings.CurrentSetting.BlackListedQuests.Remove(questToRemove);
                WholesomeAQSettings.CurrentSetting.Save();
                Logger.Log($"The quest {questId} has been removed from the blacklist ({reason})");

            }
        }

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
