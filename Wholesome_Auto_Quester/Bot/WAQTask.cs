using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot
{
    public class WAQTask
    {
        public ModelGatherObject GatherObject { get; set; }
        public Vector3 Location { get; set; }
        public int Map { get; set; }
        public ModelNpc Npc { get; set; }
        public int ObjectiveIndex { get; set; }
        public int POIEntry { get; set; }
        public ModelQuest Quest { get; set; }
        public ModelArea Area { get; set; }
        public string TaskName { get; set; }
        public TaskType TaskType { get; set; }

        private Timer _timeOutTimer = new Timer();

        public WAQTask(TaskType taskType, ModelNpc npc, ModelQuest quest, int objectiveIndex)
        {
            TaskType = taskType;
            Npc = npc;
            Location = npc.GetSpawnPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            POIEntry = npc.Id;
            Map = npc.Map;

            if (taskType == TaskType.PickupQuest)
                TaskName = $"Pick up {quest.LogTitle} from {npc.Name}";
            if (taskType == TaskType.TurnInQuest)
                TaskName = $"Turn in {quest.LogTitle} at {npc.Name}";
            if (taskType == TaskType.Kill)
                TaskName = $"Kill {npc.Name} for {quest.LogTitle}";
            if (taskType == TaskType.KillAndLoot)
                TaskName = $"Kill and Loot {npc.Name} for {quest.LogTitle}";
        }

        public WAQTask(TaskType taskType, ModelGatherObject modelGatherObject, ModelQuest quest, int objectiveIndex)
        {
            TaskType = taskType;
            GatherObject = modelGatherObject;
            Location = modelGatherObject.GetSpawnPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            POIEntry = modelGatherObject.GameObjectEntry;
            Map = modelGatherObject.Map;

            TaskName = $"Gather {modelGatherObject.Name} for {quest.LogTitle}";
        }

        public WAQTask(TaskType taskType, ModelArea modelArea, ModelQuest quest, int objectiveIndex)
        {
            TaskType = taskType;
            Area = modelArea;
            Location = modelArea.GetPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            Map = modelArea.ContinentId;

            TaskName = $"Explore {modelArea.GetPosition} for {quest.LogTitle}";
        }

        public void PutTaskOnTimeout()
        {
            int timeInSecs = Npc != null ? Npc.SpawnTimeSecs : GatherObject.SpawnTimeSecs;
            _timeOutTimer = new Timer(timeInSecs * 1000);
            WAQTasks.UpdateTasks();
        }

        public bool IsTimedOut => !_timeOutTimer.IsReady;
        public string TrackerColor => GetTrackerColor();
        public float GetDistance => ObjectManager.Me.Position.DistanceTo(Location);
        public int Priority => GetPriority();

        public int GetPriority()
        {
            // Lowest priority == do first
            float result = GetDistance;

            if (TaskType == TaskType.PickupQuest) result = result * 2;
            if (TaskType == TaskType.TurnInQuest) result = result * 2;
            if (Quest.AllowableClasses > 0) result -= 1000;
            if (Quest.TimeAllowed > 0 && TaskType != TaskType.PickupQuest) result -= 10000;

            return (int)result;
        }

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
