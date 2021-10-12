using System;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot {
    public class WAQTask
    {
        public ModelAreaTrigger Area { get; }
        //public ModelItem Item { get; }
        //public ModelWorldObject WorldObject { get; }
        public ModelGameObjectTemplate GameObjectTemplate { get; }
        public ModelGameObject GameObject { get; }
        public ModelCreatureTemplate CreatureTemplate { get; }
        public ModelCreature Creature { get; }
        public ModelQuestTemplate Quest { get; }

        public Vector3 Location { get; }
        public int Map { get; }
        public uint ObjectGuid { get; }
        public int ObjectiveIndex { get; }
        public int POIEntry { get; }
        public string TaskName { get; }
        public TaskType TaskType { get; }

        private Timer _timeOutTimer = new Timer();

        // Creatures
        public WAQTask(TaskType taskType, ModelCreatureTemplate creatureTemplate, ModelCreature creature, ModelQuestTemplate quest, int objectiveIndex)
        {
            TaskType = taskType;
            CreatureTemplate = creatureTemplate;
            Creature = creature;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;

            Map = creature.map;
            Location = creature.GetSpawnPosition;
            POIEntry = creatureTemplate.entry;
            ObjectGuid = creature.guid;

            if (taskType == TaskType.PickupQuestFromCreature)
                TaskName = $"Pick up {quest.LogTitle} from {creatureTemplate.name}";
            if (taskType == TaskType.TurnInQuestToCreature)
                TaskName = $"Turn in {quest.LogTitle} at {creatureTemplate.name}";
            if (taskType == TaskType.Kill)
                TaskName = $"Kill {creatureTemplate.name} for {quest.LogTitle}";
            if (taskType == TaskType.KillAndLoot)
                TaskName = $"Kill and Loot {creatureTemplate.name} for {quest.LogTitle}";
        }
        
        // Game Objects
        public WAQTask(TaskType taskType, ModelGameObjectTemplate gameObjectTemplate, ModelGameObject gameObject, ModelQuestTemplate quest, int objectiveIndex) {
            TaskType = taskType;
            GameObjectTemplate = gameObjectTemplate;
            GameObject = gameObject;

            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            Map = gameObject.map;
            Location = gameObject.GetSpawnPosition;
            POIEntry = gameObjectTemplate.entry;
            ObjectGuid = gameObject.guid;

            if (taskType == TaskType.PickupQuestFromGameObject)
                TaskName = $"Pick up {quest.LogTitle} from {gameObjectTemplate.name}";
            if (taskType == TaskType.TurnInQuestToGameObject)
                TaskName = $"Turn in {quest.LogTitle} at {gameObjectTemplate.name}";
            if (taskType == TaskType.GatherGameObject)
                TaskName = $"Gather item {gameObjectTemplate.name} for {quest.LogTitle}";
            if (taskType == TaskType.InteractWithWorldObject)
                TaskName = $"Interact with object {gameObjectTemplate.name} for {quest.LogTitle}";
        }

        // Explore
        public WAQTask(TaskType taskType, ModelAreaTrigger modelArea, ModelQuestTemplate quest, int objectiveIndex) {
            TaskType = taskType;
            Area = modelArea;
            Location = modelArea.GetPosition;
            Quest = quest;
            ObjectiveIndex = objectiveIndex;
            Map = modelArea.ContinentId;

            TaskName = $"Explore {modelArea.GetPosition} for {quest.LogTitle}";
        }

        public void PutTaskOnTimeout() {
            int timeInSecs = Creature?.spawnTimeSecs ?? GameObject.spawntimesecs;
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
                if (TaskType == TaskType.PickupQuestFromCreature) result *= 2;
                if (TaskType == TaskType.TurnInQuestToCreature) result *= 2;
                if (Quest.QuestAddon.AllowableClasses > 0) result /= 4;
                if (Quest.TimeAllowed > 0 && TaskType != TaskType.PickupQuestFromCreature) result = 0;

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