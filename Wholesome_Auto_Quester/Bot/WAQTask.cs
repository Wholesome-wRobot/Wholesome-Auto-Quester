using System;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot {
    public class WAQTask
    {
        public ModelAreaTrigger Area { get; }
        public ModelGameObject GameObject { get; }
        public ModelCreature Creature { get; }

        public Vector3 Location { get; }
        public int Map { get; }
        public uint ObjectGuid { get; }
        public int ObjectiveIndex { get; }
        public int TargetEntry { get; }
        public string TargetName { get; }
        public string TaskName { get; }
        public TaskType TaskType { get; }

        public string QuestTitle { get; }
        public int QuestId { get; }

        private Timer _timeOutTimer = new Timer();

        // Creatures
        public WAQTask(TaskType taskType, string creatureName, int creatureEntry, string questTitle, int questId, 
            ModelCreature creature, int objectiveIndex)
        {
            TaskType = taskType;
            Creature = creature;
            Map = creature.map;
            Location = creature.GetSpawnPosition;
            ObjectGuid = creature.guid;
            QuestId = questId;
            TargetEntry = creatureEntry;
            TargetName = creatureName;
            ObjectiveIndex = objectiveIndex;
            QuestTitle = questTitle;

            if (taskType == TaskType.PickupQuestFromCreature)
                TaskName = $"Pick up {QuestTitle} from {TargetName}";
            if (taskType == TaskType.TurnInQuestToCreature)
                TaskName = $"Turn in {QuestTitle} at {TargetName}";
            if (taskType == TaskType.Kill)
                TaskName = $"Kill {TargetName} for {QuestTitle}";
            if (taskType == TaskType.KillAndLoot)
                TaskName = $"Kill and Loot {TargetName} for {QuestTitle}";
        }
        
        // Game Objects
        public WAQTask(TaskType taskType, string gameObjectName, int gameObjectEntry, string questTitle, int questId,
            ModelGameObject gameObject, int objectiveIndex) 
        {
            TaskType = taskType;
            GameObject = gameObject;
            Map = gameObject.map;
            Location = gameObject.GetSpawnPosition;
            ObjectGuid = gameObject.guid;
            QuestId = questId;
            TargetEntry = gameObjectEntry;
            TargetName = gameObjectName;
            ObjectiveIndex = objectiveIndex;
            QuestTitle = questTitle;

            if (taskType == TaskType.PickupQuestFromGameObject)
                TaskName = $"Pick up {QuestTitle} from {TargetName}";
            if (taskType == TaskType.TurnInQuestToGameObject)
                TaskName = $"Turn in {QuestTitle} at {TargetName}";
            if (taskType == TaskType.GatherGameObject)
                TaskName = $"Gather item {TargetName} for {QuestTitle}";
            if (taskType == TaskType.InteractWithWorldObject)
                TaskName = $"Interact with object {TargetName} for {QuestTitle}";
        }

        // Explore
        public WAQTask(TaskType taskType, string questTitle, int questId, 
            ModelAreaTrigger modelArea, int objectiveIndex) 
        {
            TaskType = taskType;
            Area = modelArea;
            Location = modelArea.GetPosition;
            ObjectiveIndex = objectiveIndex;
            Map = modelArea.ContinentId;
            QuestTitle = questTitle;
            QuestId = questId;

            TaskName = $"Explore {Location} for {QuestTitle}";
        }

        public void PutTaskOnTimeout(string reason) 
        {
            if (!IsTimedOut)
            {
                int timeInSeconds = Creature?.spawnTimeSecs ?? GameObject.spawntimesecs;
                if (timeInSeconds < 30) timeInSeconds = 120;
                Logger.Log($"Putting task {TaskName} on time out for {timeInSeconds} seconds. Reason: {reason}");
                _timeOutTimer = new Timer(timeInSeconds * 1000);
                WAQTasks.UpdateTasks();
            }
        }

        public void PutTaskOnTimeout(int timeInSeconds, string reason)
        {
            Logger.Log($"Putting task {TaskName} on time out for {timeInSeconds} seconds. Reason: {reason}");
            _timeOutTimer = new Timer(timeInSeconds * 1000);
            WAQTasks.UpdateTasks();
        }

        public bool IsSameTask(TaskType taskType, int questEntry, int objIndex, Func<int> getUniqueId = null) 
        {
            return TaskType == taskType && QuestId == questEntry && ObjectiveIndex == objIndex
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
                return CalculatePriority(_myPos.DistanceTo(Location));
            }
        }

        public int CalculatePriority(float taskDistance)
        {
            ModelQuestTemplate quest = WAQTasks.Quests.Find(q => q.Id == QuestId);
            if (taskDistance > 0) // path found
            {
                if (TaskType == TaskType.PickupQuestFromCreature) taskDistance *= 2.5f;
                if (TaskType == TaskType.TurnInQuestToCreature) taskDistance *= 1.5f;
                if (quest.QuestAddon.AllowableClasses > 0) taskDistance /= 5;
                if (quest.TimeAllowed > 0 && TaskType != TaskType.PickupQuestFromCreature) taskDistance /= 100;
            }

            return (int)taskDistance;
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