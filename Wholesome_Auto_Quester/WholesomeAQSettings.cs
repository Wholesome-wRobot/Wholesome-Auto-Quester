using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.IO;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester
{
    [Serializable]
    public class WholesomeAQSettings : Settings
    {
        public static WholesomeAQSettings CurrentSetting { get; set; }

        public WholesomeAQSettings()
        {
            LogDebug = false;
            ActivateQuestsGUI = false;
            DevMode = false;
            ListCompletedQuests = new List<int>();
            LevelDeltaPlus = 2;
            LevelDeltaMinus = 5;
            SmoothMove = false;

            BlacklistesQuests = new List<int>()
            {
                354, // Roaming mobs, hard to find in a hostile zone
                1202, // Theramore docks
            };
        }

        public int LevelDeltaPlus { get; set; }
        public int LevelDeltaMinus { get; set; }
        public bool LogDebug { get; set; }
        public bool DevMode { get; set; }
        public bool ActivateQuestsGUI { get; set; }
        public List<int> ListCompletedQuests { get; set; }
        public List<int> BlacklistesQuests { get; set; }
        public bool SmoothMove { get; set; }

        public static void AddQuestToBlackList(int questId)
        {
            if (!CurrentSetting.BlacklistesQuests.Contains(questId))
            {
                CurrentSetting.BlacklistesQuests.Add(questId);
                CurrentSetting.Save();
                Logger.Log($"The quest {questId} has been blacklisted");
            }
        }

        public static void RemoveQuestFromBlackList(int questId)
        {
            if (CurrentSetting.BlacklistesQuests.Contains(questId))
            {
                CurrentSetting.BlacklistesQuests.Remove(questId);
                CurrentSetting.Save();
                Logger.Log($"The quest {questId} has been removed from the blacklist");
            }
        }

        public bool Save()
        {
            try
            {
                return Save(AdviserFilePathAndName("WholesomeAQSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
            }
            catch (Exception e)
            {
                Logging.WriteError("WholesomeAQSettings > Save(): " + e);
                return false;
            }
        }

        public static bool Load()
        {
            try
            {
                if (File.Exists(AdviserFilePathAndName("WholesomeAQSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName)))
                {
                    CurrentSetting = Load<WholesomeAQSettings>(
                        AdviserFilePathAndName("WholesomeAQSettings",
                        ObjectManager.Me.Name + "." + Usefuls.RealmName));
                    return true;
                }
                CurrentSetting = new WholesomeAQSettings();
            }
            catch (Exception e)
            {
                Logging.WriteError("WholesomeAQSettings > Load(): " + e);
            }
            return false;
        }
    }
}
