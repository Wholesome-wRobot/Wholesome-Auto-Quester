using robotManager.Helpful;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public abstract class WAQBaseTask : IWAQTask
    {
        private Timer _timeOutTimer = new Timer();
        private int _timeoutMultiplicator = 1;

        public Vector3 Location { get; }
        public string TaskName { get; }
        public int Continent { get; }
        public double SpatialWeight { get; protected set; } = 1.0;
        public int PriorityShift { get; protected set; } = 1;
        public int SearchRadius { get; protected set; } = 20;
        public abstract string TrackerColor { get; }
        public abstract TaskInteraction InteractionType { get; }
        public bool IsTimedOut => !_timeOutTimer.IsReady;

        public WAQBaseTask(Vector3 location, int continent, string taskName)
        {
            Location = location;
            TaskName = taskName;
            Continent = continent;
        }

        public abstract bool IsObjectValidForTask(WoWObject wowObject);
        public abstract void RegisterEntryToScanner(IWowObjectScanner scanner);
        public abstract void UnregisterEntryToScanner(IWowObjectScanner scanner);
        public abstract void PostInteraction(WoWObject wowObject);

        public void PutTaskOnTimeout(string reason, int timeInSeconds = 0, bool exponentiallyLonger = false)
        {
            if (!IsTimedOut)
            {
                if (timeInSeconds < 30)
                {
                    timeInSeconds = 60 * 5;
                }
                Logger.Log($"Putting task {TaskName} on time out for {timeInSeconds * _timeoutMultiplicator} seconds. Reason: {reason}");
                _timeOutTimer = new Timer(timeInSeconds * 1000 * _timeoutMultiplicator);
                if (exponentiallyLonger)
                {
                    _timeoutMultiplicator *= 2;
                }
            }
        }
    }
}
