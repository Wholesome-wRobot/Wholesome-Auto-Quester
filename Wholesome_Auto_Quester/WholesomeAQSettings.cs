using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.IO;
using System.Collections.Generic;

namespace Wholesome_Auto_Quester
{
    [Serializable]
    public class WholesomeAQSettings : Settings
    {
        public static WholesomeAQSettings CurrentSetting { get; set; }

        public int LevelDeltaPlus { get; set; }
        public int LevelDeltaMinus { get; set; }
        public bool LogDebug { get; set; }
        public bool DevMode { get; set; }
        public bool ActivateQuestsGUI { get; set; }
        public List<int> ListCompletedQuests { get; set; }
        public List<int> BlacklistesQuests { get; set; }
        public bool SmoothMove { get; set; }
        public double LastUpdateDate { get; set; }
        public bool GrindOnly { get; set; }
        public double QuestTrackerPositionLeft { get; set; }
        public double QuestTrackerPositionTop { get; set; }

        public WholesomeAQSettings()
        {
            LogDebug = false;
            ActivateQuestsGUI = false;
            DevMode = false;
            ListCompletedQuests = new List<int>();
            LevelDeltaPlus = 2;
            LevelDeltaMinus = 5;
            SmoothMove = false;
            LastUpdateDate = 0;
            BlacklistesQuests = new List<int>();
            GrindOnly = false;
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
