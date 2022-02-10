using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public class WAQTaskExploreLocation : WAQBaseTask
    {
        public WAQTaskExploreLocation(ModelQuestTemplate questTemplate, Vector3 location, int continent)
            : base(location, continent, $"Explore {location} for {questTemplate.LogTitle}")
        {
            SearchRadius = 2;
            if (questTemplate.QuestAddon?.AllowableClasses > 0)
            {
                PriorityShift = 3;
            }
            if (questTemplate.TimeAllowed > 0)
            {
                PriorityShift = 7;
            }
        }

        public override bool IsRecordedAsUnreachable => false;
        public override string TrackerColor => IsTimedOut ? "Gray" : "Linen";
        public override bool IsObjectValidForTask(WoWObject wowObject) => throw new System.Exception($"Tried to scan for {TaskName}");
        public override void RegisterEntryToScanner(IWowObjectScanner scanner) { }
        public override void UnregisterEntryToScanner(IWowObjectScanner scanner) { }
        public override void PostInteraction(WoWObject wowObject) { }
        public override void RecordAsUnreachable() => throw new System.Exception($"Tried to record unreachable for {TaskName}");

        public override TaskInteraction InteractionType => TaskInteraction.None;
    }
}
