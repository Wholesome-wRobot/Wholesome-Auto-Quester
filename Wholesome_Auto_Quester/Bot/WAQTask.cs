using System;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot {
    public class WAQTask {
        public ModelGatherObject GatherObject { get; }
        public Vector3 Location { get; }
        public int Map { get; }
        public ModelNpc Npc { get; }
        public int TargetGuid { get; }
        public int ObjectiveIndex { get; }
        public int POIEntry { get; }
        public ModelQuest Quest { get; }
        public ModelArea Area { get; }
        public string TaskName { get; }
        public TaskType TaskType { get; }

        private Timer _timeOutTimer = new Timer();

        public WAQTask(TaskType taskType, ModelNpc npc, ModelQuest quest, int objectiveIndex) {
            TaskType = taskType;
            Npc = npc;
            Location = npc.GetSpawnPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            POIEntry = npc.Id;
            Map = npc.Map;
            TargetGuid = Npc?.Guid ?? GatherObject?.Guid ?? 0;

            if (taskType == TaskType.PickupQuest)
                TaskName = $"Pick up {quest.LogTitle} from {npc.Name}";
            if (taskType == TaskType.TurnInQuest)
                TaskName = $"Turn in {quest.LogTitle} at {npc.Name}";
            if (taskType == TaskType.Kill)
                TaskName = $"Kill {npc.Name} for {quest.LogTitle}";
            if (taskType == TaskType.KillAndLoot)
                TaskName = $"Kill and Loot {npc.Name} for {quest.LogTitle}";
        }

        public WAQTask(TaskType taskType, ModelGatherObject modelGatherObject, ModelQuest quest, int objectiveIndex) {
            TaskType = taskType;
            GatherObject = modelGatherObject;
            Location = modelGatherObject.GetSpawnPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            POIEntry = modelGatherObject.GameObjectEntry;
            Map = modelGatherObject.Map;
            TargetGuid = Npc?.Guid ?? GatherObject?.Guid ?? 0;

            TaskName = $"Gather {modelGatherObject.Name} for {quest.LogTitle}";
        }

        public WAQTask(TaskType taskType, ModelArea modelArea, ModelQuest quest, int objectiveIndex) {
            TaskType = taskType;
            Area = modelArea;
            Location = modelArea.GetPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            Map = modelArea.ContinentId;
            TargetGuid = Npc?.Guid ?? GatherObject?.Guid ?? 0;

            TaskName = $"Explore {modelArea.GetPosition} for {quest.LogTitle}";
        }

        public void PutTaskOnTimeout() {
            int timeInSecs = Npc?.SpawnTimeSecs ?? GatherObject.SpawnTimeSecs;
            _timeOutTimer = new Timer(timeInSecs * 1000);
            WAQTasks.UpdateTasks();
        }

        public bool IsSameTask(TaskType taskType, int questEntry, int objIndex, Func<int> getUniqueId = null) {
            return TaskType == taskType && Quest.Id == questEntry && ObjectiveIndex == objIndex
                   && TargetGuid == (getUniqueId?.Invoke() ?? 0);
        }

        public bool IsTimedOut => !_timeOutTimer.IsReady;
        public string TrackerColor => GetTrackerColor();
        public float GetDistance => ObjectManager.Me.PositionWithoutType.DistanceTo(Location);

        private static Vector3 _myPos = ObjectManager.Me.PositionWithoutType;
        private static uint _myLevel = ObjectManager.Me.Level;

        public static void UpdatePriorityData() {
            _myPos = ObjectManager.Me.PositionWithoutType;
            _myLevel = ObjectManager.Me.Level;
        }

        public int Priority {
            get {
                // Lowest priority == do first
                float result = _myPos.DistanceTo(Location);

                // var levelDiff = (int) (Quest.QuestLevel - _myLevel);
                // result *= System.Math.Max(0.2f, (10f + levelDiff*2) / 10);
                if (TaskType == TaskType.PickupQuest) result *= 2;
                if (TaskType == TaskType.TurnInQuest) result *= 2;
                if (Quest.AllowableClasses > 0) result /= 4;
                if (Quest.TimeAllowed > 0 && TaskType != TaskType.PickupQuest) result = 0;

                return (int) result;
            }
        }

        private string GetTrackerColor() {
            if (IsTimedOut)
                return "Gray";

            if (WAQTasks.TaskInProgress == this)
                return "Gold";

            return "Beige";
        }
    }
}