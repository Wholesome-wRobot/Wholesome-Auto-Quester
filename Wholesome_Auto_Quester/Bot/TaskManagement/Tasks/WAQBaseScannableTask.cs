using robotManager.Helpful;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public abstract class WAQBaseScannableTask : WAQBaseTask
    {
        public int ObjectEntry { get; }
        public int DefaultTimeOutDuration { get; }
        public uint ObjectDBGuid { get; }

        public WAQBaseScannableTask(Vector3 location, int continent, string taskName, int objectEntry, int defaultTimeOutDuration, uint dbGuid)
            : base(location, continent, taskName)
        {
            ObjectDBGuid = dbGuid;
            ObjectEntry = objectEntry;
            DefaultTimeOutDuration = defaultTimeOutDuration;
        }

        protected override bool IsRecordedAsUnreachable => WholesomeAQSettings.CurrentSetting.RecordedUnreachables.Contains(ObjectDBGuid);
        public override void RecordAsUnreachable() => WholesomeAQSettings.RecordGuidAsUnreachable(ObjectDBGuid);
        public override void RegisterEntryToScanner(IWowObjectScanner scanner) => scanner.AddToScannerRegistry(ObjectEntry, this);
        public override void UnregisterEntryToScanner(IWowObjectScanner scanner) => scanner.RemoveFromScannerRegistry(ObjectEntry, this);
        public override abstract bool IsObjectValidForTask(WoWObject wowObject);
    }
}
