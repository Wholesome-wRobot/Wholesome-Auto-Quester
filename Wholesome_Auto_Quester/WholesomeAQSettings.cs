using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.IO;
using Wholesome_Auto_Quester.Helpers;
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
        public bool RecordUnreachables { get; set; }
        public List<uint> RecordedUnreachables { get; set; }
        public bool SmoothMove { get; set; }
        public double LastUpdateDate { get; set; }
        public bool GrindOnly { get; set; }
        public double QuestTrackerPositionLeft { get; set; }
        public double QuestTrackerPositionTop { get; set; }
        public bool ContinentTravel { get; set; }
        public List<BlackListedQuest> BlackListedQuests { get; set; }
        public bool AbandonUnfitQuests { get; set; }
        public int StopAtLevel { get; set; }
        public bool BlacklistDangerousZones { get; set; }
        public bool AllowStopWatch { get; set; }

        public WholesomeAQSettings()
        {
            LogDebug = false;
            ActivateQuestsGUI = true;
            DevMode = false;
            ListCompletedQuests = new List<int>();
            RecordedUnreachables = new List<uint>();
            LevelDeltaPlus = 0;
            LevelDeltaMinus = 5;
            SmoothMove = false;
            LastUpdateDate = 0;
            GrindOnly = false;
            ContinentTravel = true;
            BlackListedQuests = new List<BlackListedQuest>();
            AbandonUnfitQuests = true;
            RecordUnreachables = false;
            StopAtLevel = 80;
            BlacklistDangerousZones = true;

            AllowStopWatch = false;
        }

        public static void RecordGuidAsUnreachable(uint guid)
        {
            if (CurrentSetting.RecordUnreachables && !CurrentSetting.RecordedUnreachables.Contains(guid))
            {
                CurrentSetting.RecordedUnreachables.Add(guid);
                CurrentSetting.Save();
                Logger.Log($"Recorded {guid} as unreachable");
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