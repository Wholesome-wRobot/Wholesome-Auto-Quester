using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement.Tasks
{
    public interface IWAQTask
    {
        Vector3 Location { get; }
        string TaskName { get; }
        ModelWorldMapArea WorldMapArea { get; }
        double SpatialWeight { get; }
        int PriorityShift { get; }
        string TrackerColor { get; }
        int SearchRadius { get; } // limit for MoveToHotspot state
        TaskInteraction InteractionType { get; }
        bool IsValid { get; }
        string InvalidityReason { get; }

        void PostInteraction(WoWObject wowObject);
        void RegisterEntryToScanner(IWowObjectScanner scanner);
        void UnregisterEntryToScanner(IWowObjectScanner scanner);
        void PutTaskOnTimeout(string reason, int timeInSeconds = 0, bool exponentiallyLonger = false);
        bool IsObjectValidForTask(WoWObject wowObject);
        void RecordAsUnreachable();
    }
}