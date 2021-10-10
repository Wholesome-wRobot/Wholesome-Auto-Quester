using System;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot {
    public class WAQTask
    {
        public ModelArea Area { get; }
        public ModelItem Item { get; }
        public ModelWorldObject WorldObject { get; }
        public Vector3 Location { get; }
        public int Map { get; }
        public ModelNpc Npc { get; }
        public int ObjectGuid { get; }
        public int ObjectiveIndex { get; }
        public int POIEntry { get; }
        public ModelQuest Quest { get; }
        public string TaskName { get; }
        public TaskType TaskType { get; }

        private Timer _timeOutTimer = new Timer();

        public WAQTask(TaskType taskType, ModelNpc npc, ModelWorldObject worldObject, ModelQuest quest, int objectiveIndex) {
            TaskType = taskType;
            Npc = npc;
            WorldObject = worldObject;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;

            Map = npc?.Map ?? worldObject.Map;
            Location = npc?.GetSpawnPosition ?? worldObject.GetSpawnPosition;
            POIEntry = npc?.Id ?? worldObject.Entry;
            ObjectGuid = npc?.Guid ?? worldObject.Guid;

            if (taskType == TaskType.PickupQuestFromNpc)
                TaskName = $"Pick up {quest.LogTitle} from {npc?.Name}";
            if (taskType == TaskType.PickupQuestFromGameObject)
                TaskName = $"Pick up {quest.LogTitle} from {worldObject?.Name}";
            if (taskType == TaskType.TurnInQuestToNpc)
                TaskName = $"Turn in {quest.LogTitle} at {npc?.Name}";
            if (taskType == TaskType.TurnInQuestToGameObject)
                TaskName = $"Turn in {quest.LogTitle} at {worldObject?.Name}";
            if (taskType == TaskType.Kill)
                TaskName = $"Kill {npc.Name} for {quest.LogTitle}";
            if (taskType == TaskType.KillAndLoot)
                TaskName = $"Kill and Loot {npc.Name} for {quest.LogTitle}";
        }

        // Gather world item
        public WAQTask(TaskType taskType, ModelItem modelGatherObject, ModelQuest quest, int objectiveIndex) {
            TaskType = taskType;
            Item = modelGatherObject;
            Location = modelGatherObject.GetSpawnPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            POIEntry = modelGatherObject.WorldObjectEntry;
            Map = modelGatherObject.Map;
            ObjectGuid = Npc?.Guid ?? Item?.Guid ?? 0;

            TaskName = $"Gather item {modelGatherObject.Name} for {quest.LogTitle}";
        }

        // Interact with world object
        public WAQTask(TaskType taskType, ModelWorldObject modelWorldObject, ModelQuest quest, int objectiveIndex)
        {
            TaskType = taskType;
            WorldObject = modelWorldObject;
            Location = modelWorldObject.GetSpawnPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            POIEntry = modelWorldObject.Entry;
            Map = modelWorldObject.Map;
            ObjectGuid = WorldObject.Guid;

            TaskName = $"Interact with object {modelWorldObject.Name} for {quest.LogTitle}";
        }

        // Explore
        public WAQTask(TaskType taskType, ModelArea modelArea, ModelQuest quest, int objectiveIndex) {
            TaskType = taskType;
            Area = modelArea;
            Location = modelArea.GetPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            Map = modelArea.ContinentId;
            ObjectGuid = Npc?.Guid ?? Item?.Guid ?? 0;

            TaskName = $"Explore {modelArea.GetPosition} for {quest.LogTitle}";
        }

        public void PutTaskOnTimeout() {
            int timeInSecs = Npc?.SpawnTimeSecs ?? Item?.SpawnTimeSecs ?? WorldObject.SpawnTimeSecs;
            if (timeInSecs < 30) timeInSecs = 120;
            _timeOutTimer = new Timer(timeInSecs * 1000);
            WAQTasks.UpdateTasks();
        }

        public bool IsSameTask(TaskType taskType, int questEntry, int objIndex, Func<int> getUniqueId = null) {
            return TaskType == taskType && Quest.Id == questEntry && ObjectiveIndex == objIndex
                   && ObjectGuid == (getUniqueId?.Invoke() ?? 0);
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
                if (TaskType == TaskType.PickupQuestFromNpc) result *= 2;
                if (TaskType == TaskType.TurnInQuestToNpc) result *= 2;
                if (Quest.AllowableClasses > 0) result /= 4;
                if (Quest.TimeAllowed > 0 && TaskType != TaskType.PickupQuestFromNpc) result = 0;

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