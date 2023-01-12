using robotManager.Helpful;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Bot.TravelManagement;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public abstract class WAQBaseTask : IWAQTask
    {
        private Timer _timeOutTimer = new Timer();
        private int _timeoutMultiplicator = 1;
        private string _timeOutReason = "";

        public Vector3 Location { get; }
        public string TaskName { get; }
        public ModelWorldMapArea WorldMapArea { get; }
        public double SpatialWeight { get; protected set; } = 1.0;
        public int PriorityShift { get; protected set; } = 1;
        public int SearchRadius { get; protected set; } = 20;
        private bool IsTimedOut => !_timeOutTimer.IsReady;
        public string InvalidityReason { get; private set; } = " ";
        public bool IsValid
        {
            get
            {
                uint myLevel = ObjectManager.Me.Level;

                if (myLevel < 12 && !IsInMyStartingZone())
                {
                    InvalidityReason = "Sticking to starting zone";
                    return false;
                }

                if (IsTimedOut)
                {
                    InvalidityReason = _timeOutReason;
                    return false;
                }

                if (ReputationMismatch != null)
                {
                    InvalidityReason = ReputationMismatch;
                    return false;
                }

                if (!HasEnoughSkillForTask)
                {
                    InvalidityReason = "Insufficient skill";
                    return false;
                }

                if (IsRecordedAsUnreachable)
                {
                    InvalidityReason = "Unreachable";
                    return false;
                }

                if (WorldMapArea == null)
                {
                    InvalidityReason = "Unable to record world map area";
                    return false;
                }

                if (wManagerSetting.IsBlackListedZone(Location))
                {
                    PutTaskOnTimeout("Zone is blacklisted", 60 * 30, true);
                    InvalidityReason = "Zone is blacklisted";
                    return false;
                }

                // Stick out of Outlands until level 60
                if (myLevel < 60
                    && WorldMapArea.Continent == WAQContinent.Outlands)
                {
                    InvalidityReason = "Sticking to Azeroth";
                    return false;
                }

                // Stick to Outlands between 60 and 70
                if (myLevel < 70
                    && (ToolBox.IsQuestCompleted(9407) || ToolBox.IsQuestCompleted(10119))
                    && WorldMapArea.Continent != WAQContinent.Outlands)
                {
                    InvalidityReason = "Sticking to Outlands";
                    return false;
                }

                // Stick to Outlands between 60 and 70
                if (myLevel >= 70 && myLevel <= 80
                    && WorldMapArea.Continent != WAQContinent.Northrend)
                {
                    InvalidityReason = "Sticking to Northrend";
                    return false;
                }

                InvalidityReason = "";
                return true;
            }
        }

        public WAQBaseTask(Vector3 location, int continent, string taskName, IContinentManager continentManager)
        {
            Location = location;
            TaskName = taskName;
            WorldMapArea = continentManager.GetWorldMapAreaFromPoint(location, continent);
        }

        protected abstract bool IsRecordedAsUnreachable { get; }
        protected abstract bool HasEnoughSkillForTask { get; }
        protected abstract string ReputationMismatch { get; }
        public abstract TaskInteraction InteractionType { get; }
        public abstract string TrackerColor { get; }
        public abstract bool IsObjectValidForTask(WoWObject wowObject);
        public abstract void RegisterEntryToScanner(IWowObjectScanner scanner);
        public abstract void UnregisterEntryToScanner(IWowObjectScanner scanner);
        public abstract void PostInteraction(WoWObject wowObject);
        public abstract void RecordAsUnreachable();

        private bool IsInMyStartingZone()
        {
            WoWRace myRace = ObjectManager.Me.WowRace;
            if (myRace == WoWRace.Human) return WorldMapArea.IsHumanStartingZone;
            if (myRace == WoWRace.Dwarf || myRace == WoWRace.Gnome) return WorldMapArea.IsDwarfStartingZone;
            if (myRace == WoWRace.NightElf) return WorldMapArea.IsElfStartingZone;
            if (myRace == WoWRace.Draenei) return WorldMapArea.IsDraneiStartingZone;
            if (myRace == WoWRace.Orc || myRace == WoWRace.Troll) return WorldMapArea.IsOrcStartingZone;
            if (myRace == WoWRace.Undead) return WorldMapArea.IsUndeadStartingZone;
            if (myRace == WoWRace.Tauren) return WorldMapArea.IsTaurenStartingZone;
            if (myRace == WoWRace.BloodElf) return WorldMapArea.IsBloodElfStartingZone;
            Logger.LogError($"Couldn't detect your race");
            return false;
        }

        public void PutTaskOnTimeout(string reason, int timeInSeconds = 0, bool exponentiallyLonger = false)
        {
            if (!IsTimedOut)
            {
                if (timeInSeconds < 30)
                {
                    timeInSeconds = 60 * 5;
                }
                Logger.Log($"Putting task {TaskName} on time out for {timeInSeconds * _timeoutMultiplicator} seconds. Reason: {reason}");
                _timeOutReason = reason;
                _timeOutTimer = new Timer(timeInSeconds * 1000 * _timeoutMultiplicator);
                if (exponentiallyLonger)
                {
                    _timeoutMultiplicator *= 2;
                }
            }
        }
    }
}
