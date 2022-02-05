using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.IO;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

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
        public bool SmoothMove { get; set; }
        public double LastUpdateDate { get; set; }
        public bool GrindOnly { get; set; }
        public double QuestTrackerPositionLeft { get; set; }
        public double QuestTrackerPositionTop { get; set; }
        public bool ContinentTravel { get; set; }
        public List<BlackListedQuest> BlackListedQuests { get; set; }
        public bool AbandonUnfitQuests { get; set; }
        public int GoToMobEntry { get; set; }

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
            GrindOnly = false;
            ContinentTravel = false;
            BlackListedQuests = new List<BlackListedQuest>();
            AbandonUnfitQuests = false;
            GoToMobEntry = 0;
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

public struct BlackListedQuest
{
    public int Id;
    public string Reason;

    public BlackListedQuest(int id, string reason)
    {
        Id = id;
        Reason = reason;
    }
}