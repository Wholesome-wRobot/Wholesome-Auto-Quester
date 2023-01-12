using robotManager.Helpful;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public abstract class WAQBaseScannableTask : WAQBaseTask
    {
        public int ObjectEntry { get; }
        public int DefaultTimeOutDuration { get; }
        public uint ObjectDBGuid { get; }

        public WAQBaseScannableTask(Vector3 location, int continent, string taskName, int objectEntry, int defaultTimeOutDuration, uint dbGuid, IContinentManager continentManager)
            : base(location, continent, taskName, continentManager)
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
