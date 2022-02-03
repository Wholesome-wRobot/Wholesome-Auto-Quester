using robotManager.Helpful;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public abstract class WAQBaseScannableTask : WAQBaseTask
    {
        public int ObjectEntry { get; }
        public int DefaultTimeOutDuration { get; }

        public WAQBaseScannableTask(Vector3 location, int continent, string taskName, int objectEntry, int defaultTimeOutDuration) 
            : base(location, continent, taskName) 
        {
            ObjectEntry = objectEntry;
            DefaultTimeOutDuration = defaultTimeOutDuration;
        }

        public override void RegisterEntryToScanner(IWowObjectScanner scanner) => scanner.AddToScannerRegistry(ObjectEntry, this);
        public override void UnregisterEntryToScanner(IWowObjectScanner scanner) => scanner.RemoveFromScannerRegistry(ObjectEntry, this);
        public override abstract bool IsObjectValidForTask(WoWObject wowObject);
    }
}
