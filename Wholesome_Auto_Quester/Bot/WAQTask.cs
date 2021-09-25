using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot
{
    public class WAQTask
    {
        public TaskType TaskType { get; set; }
        public ModelQuest Quest { get; set; }
        public ModelNpc Npc { get; set; }
        public ModelGatherObject GatherObject { get; set; }
        public string TaskName { get; set; }
        public Vector3 Location { get; set; }
        public int ObjectiveIndex { get; set; }
        public int POIEntry { get; set; }

        private Timer _timeOutTimer = new Timer();

        public WAQTask(TaskType taskType, ModelNpc npc, ModelQuest quest, int objectiveIndex)
        {
            TaskType = taskType;
            Npc = npc;
            Location = npc.GetSpawnPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            POIEntry = npc.Id;

            if (taskType == TaskType.PickupQuest)
                TaskName = $"[{ToolBox.GetTaskId(this)}] Pick up {quest.Title} from {npc.Name}";
            if (taskType == TaskType.TurnInQuest)
                TaskName = $"[{ToolBox.GetTaskId(this)}] Turn in {quest.Title} at {npc.Name}";
            if (taskType == TaskType.Kill)
                TaskName = $"[{ToolBox.GetTaskId(this)}] Kill {npc.Name} for {quest.Title}";
            if (taskType == TaskType.KillAndLoot)
                TaskName = $"[{ToolBox.GetTaskId(this)}] Kill and Loot {npc.Name} for {quest.Title}";
        }

        public WAQTask(TaskType taskType, ModelGatherObject modelGatherObject, ModelQuest quest, int objectiveIndex)
        {
            TaskType = taskType;
            GatherObject = modelGatherObject;
            Location = modelGatherObject.GetSpawnPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            POIEntry = modelGatherObject.GameObjectEntry;

            if (taskType == TaskType.PickupObject)
                TaskName = $"[{ToolBox.GetTaskId(this)}] Gather {modelGatherObject.Name} for {quest.Title}";
        }

        public void PutTaskOnTimeout(int timeInSeconds)
        {
            _timeOutTimer = new Timer(timeInSeconds * 1000);
            WAQTasks.UpdateTasks();
        }

        public bool IsTimedOut => !_timeOutTimer.IsReady;
        public string TrackerColor => GetTrackerColor();
        public float GetDistance => ObjectManager.Me.Position.DistanceTo(Location);

        private string GetTrackerColor()
        {
            if (IsTimedOut)
                return "Gray";

            if (WAQTasks.TaskInProgress == this)
                return "Gold";

            return "Beige";
        }
    }
}
