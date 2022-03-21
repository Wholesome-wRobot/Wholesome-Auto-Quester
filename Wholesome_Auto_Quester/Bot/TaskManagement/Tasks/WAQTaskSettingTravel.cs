using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskSettingTravel : WAQBaseTask
    {
        public WAQTaskSettingTravel(ModelCreatureTemplate creatureTemplate)
            : base(creatureTemplate.Creatures[0].GetSpawnPosition, creatureTemplate.Creatures[0].map, $"Going to {creatureTemplate.name} (force travel)")
        {
            SearchRadius = 2;
        }

        protected override bool IsRecordedAsUnreachable => false;
        public override bool IsObjectValidForTask(WoWObject wowObject) => throw new System.Exception($"Tried to scan for {TaskName}");
        public override void RegisterEntryToScanner(IWowObjectScanner scanner) { }
        public override void UnregisterEntryToScanner(IWowObjectScanner scanner) { }
        public override void RecordAsUnreachable() => throw new System.Exception($"Tried to record unreachable for {TaskName}");
        public override void PostInteraction(WoWObject wowObject) => throw new System.Exception($"Tried to run PostInteraction for {TaskName}");

        public override string TrackerColor => "White";
        public override TaskInteraction InteractionType => TaskInteraction.None;
        protected override bool HasEnoughSkillForTask => true;
        protected override string ReputationMismatch => null;
    }
}
