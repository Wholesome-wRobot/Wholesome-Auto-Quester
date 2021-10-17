using robotManager.Helpful;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.Quest.PlayerQuest;

namespace Wholesome_Auto_Quester.Bot {
    public class WAQTasks {
        private static int _tick;
        public static List<WAQTask> TasksPile { get; set; } = new List<WAQTask>();
        public static List<ModelQuestTemplate> Quests { get; set; } = new List<ModelQuestTemplate>();
        public static WAQTask TaskInProgress { get; set; }
        public static WoWObject TaskInProgressWoWObject { get; set; }

        public static void AddQuests(List<ModelQuestTemplate> quests) {
            quests.ForEach(newQuest => {
                if (!Quests.Exists(quest => quest.Id == newQuest.Id))
                    Quests.Add(newQuest);
            });
        }

        public static void UpdateTasks() {
            if (Quests.Count <= 0 
                || !ObjectManager.Me.IsAlive 
                || !ObjectManager.Me.IsValid 
                || Fight.InFight)
                return;

            //Logger.Log("Update tasks");
            var generatedTasks = new List<WAQTask>();
            int myContinent = Usefuls.ContinentId;
            int myLevel = (int)ObjectManager.Me.Level;
            Vector3 myPosition = ObjectManager.Me.Position;

            ToolBox.UpdateObjectiveCompletionDict(Quests.Where(quest => quest.Status == QuestStatus.InProgress)
                .Select(quest => quest.Id).ToArray());

            foreach (ModelQuestTemplate quest in Quests) {
                // Completed
                if (quest.Status == QuestStatus.Completed || quest.Status == QuestStatus.Blacklisted) {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id);
                    continue;
                }

                // Turn in quest
                if (quest.Status == QuestStatus.ToTurnIn) {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id && t.TaskType != TaskType.TurnInQuestToCreature);
                    quest.CreatureQuestTurners.ForEach(creatureTemplate => {
                        creatureTemplate.Creatures.ForEach(creature =>
                        {
                            if (creature.map == myContinent
                                && !TasksPile.Exists(t =>
                                    t.IsSameTask(TaskType.TurnInQuestToCreature, quest.Id, 5, () => (int)creature.guid)))
                                generatedTasks.Add(new WAQTask(TaskType.TurnInQuestToCreature, creatureTemplate, creature, quest, 5));
                        });
                    });
                    quest.GameObjectQuestTurners.ForEach(gameObjectTemplate => {
                        gameObjectTemplate.GameObjects.ForEach(gameObject =>
                        {
                            if (gameObject.map == myContinent
                                && !TasksPile.Exists(t =>
                                    t.IsSameTask(TaskType.TurnInQuestToGameObject, quest.Id, 5, () => (int)gameObject.guid)))
                                generatedTasks.Add(new WAQTask(TaskType.TurnInQuestToGameObject, gameObjectTemplate, gameObject, quest, 5));
                        });
                    });
                    continue;
                }

                // Pick up quest
                if (quest.Status == QuestStatus.ToPickup) {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id && t.TaskType != TaskType.PickupQuestFromCreature);
                    quest.CreatureQuestGivers.ForEach(creatureTemplate => {
                        creatureTemplate.Creatures.ForEach(creature =>
                        {
                            if (creature.map == myContinent
                                && !TasksPile.Exists(t =>
                                    t.IsSameTask(TaskType.PickupQuestFromCreature, quest.Id, 6, () => (int)creature.guid)))
                                generatedTasks.Add(new WAQTask(TaskType.PickupQuestFromCreature, creatureTemplate, creature, quest, 6));
                        });
                    });
                    quest.GameObjectQuestGivers.ForEach(gameObjectTemplate => {
                        gameObjectTemplate.GameObjects.ForEach(gameObject =>
                        {
                            if (gameObject.map == myContinent
                                && !TasksPile.Exists(t =>
                                    t.IsSameTask(TaskType.PickupQuestFromGameObject, quest.Id, 6, () => (int)gameObject.guid)))
                                generatedTasks.Add(new WAQTask(TaskType.PickupQuestFromGameObject, gameObjectTemplate, gameObject, quest, 6));
                        });
                    });
                    continue;
                }

                if (quest.Status == QuestStatus.InProgress) {

                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                             && (t.TaskType == TaskType.PickupQuestFromCreature ||
                                                 t.TaskType == TaskType.TurnInQuestToCreature));

                    // Prerequisite gathers & loots
                    bool needsPrerequisite = false;
                    foreach (KillLootObjective obje in quest.PrerequisiteLootObjectives)
                    {
                        if (ItemsManager.GetItemCountById((uint)obje.ItemToLoot.Entry) <= 0)
                        {
                            needsPrerequisite = true;
                            obje.CreatureTemplate.Creatures.ForEach(creature => {
                                if (creature.map == myContinent
                                    && obje.CreatureTemplate.maxLevel <= myLevel + 2
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.KillAndLoot, quest.Id,
                                            obje.ObjectiveIndex, () => (int)creature.guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.KillAndLoot, obje.CreatureTemplate, creature, quest,
                                        obje.ObjectiveIndex));
                            });
                        }
                        else
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                     && t.ObjectiveIndex == obje.ObjectiveIndex
                                                     && t.TaskType == TaskType.KillAndLoot);
                    }

                    // Gather
                    foreach (GatherObjective obje in quest.PrerequisiteGatherObjectives)
                    {
                        if (ItemsManager.GetItemCountById((uint)obje.GameObjectToGather.entry) <= 0)
                        {
                            needsPrerequisite = true;
                            obje.GameObjectToGather.GameObjects.ForEach(gameObject => {
                                if (gameObject.map == myContinent
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.GatherGameObject, quest.Id,
                                            obje.ObjectiveIndex, () => (int)gameObject.guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.GatherGameObject, obje.GameObjectToGather, gameObject, quest,
                                        obje.ObjectiveIndex));
                            });
                        }
                        else
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                        && t.ObjectiveIndex == obje.ObjectiveIndex
                                                        && t.TaskType == TaskType.GatherGameObject);
                    }

                    if (!needsPrerequisite)
                    {
                        // Explore
                        foreach (ExplorationObjective obje in quest.ExplorationObjectives)
                            if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, quest.Id))
                            {
                                if (obje.Area.ContinentId == myContinent
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.Explore, quest.Id,
                                            obje.ObjectiveIndex)))
                                    generatedTasks.Add(new WAQTask(TaskType.Explore, obje.Area, quest,
                                        obje.ObjectiveIndex));
                            }
                            else
                            {
                                TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.Explore);
                            }

                        // Kill & Loot
                        foreach (KillLootObjective obje in quest.KillLootObjectives)
                            if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, quest.Id))
                                obje.CreatureTemplate.Creatures.ForEach(creature => {
                                    if (creature.map == myContinent
                                        && obje.CreatureTemplate.maxLevel <= myLevel + 2
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.KillAndLoot, quest.Id,
                                                obje.ObjectiveIndex, () => (int)creature.guid)))
                                        generatedTasks.Add(new WAQTask(TaskType.KillAndLoot, obje.CreatureTemplate, creature, quest,
                                            obje.ObjectiveIndex));
                                });
                            else
                                TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.KillAndLoot);

                        // Kill
                        foreach (KillObjective obje in quest.KillObjectives)
                            if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, quest.Id))
                                obje.CreatureTemplate.Creatures.ForEach(creature => {
                                    if (creature.map == myContinent
                                        && obje.CreatureTemplate.maxLevel <= myLevel + 2
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.Kill, quest.Id,
                                                obje.ObjectiveIndex, () => (int)creature.guid)))
                                        generatedTasks.Add(new WAQTask(TaskType.Kill, obje.CreatureTemplate, creature, quest,
                                            obje.ObjectiveIndex));
                                });
                            else
                                TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.Kill);

                        // Gather object
                        foreach (GatherObjective obje in quest.GatherObjectives)
                            if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, quest.Id))
                                obje.GameObjectToGather.GameObjects.ForEach(gameObject => {
                                    if (gameObject.map == myContinent
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.GatherGameObject, quest.Id,
                                                obje.ObjectiveIndex, () => (int)gameObject.guid)))
                                        generatedTasks.Add(new WAQTask(TaskType.GatherGameObject, obje.GameObjectToGather, gameObject, quest,
                                            obje.ObjectiveIndex));
                                });
                            else
                                TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.GatherGameObject);

                        // Interact with object
                        foreach (InteractObjective obje in quest.InteractObjectives)
                            if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, quest.Id))
                                obje.GameObjectTemplate.GameObjects.ForEach(gameObject => {
                                    if (gameObject.map == myContinent
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.InteractWithWorldObject, quest.Id,
                                                obje.ObjectiveIndex, () => (int)gameObject.guid)))
                                        generatedTasks.Add(new WAQTask(TaskType.InteractWithWorldObject, obje.GameObjectTemplate, gameObject, quest,
                                            obje.ObjectiveIndex));
                                });
                            else
                                TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.InteractWithWorldObject);
                    }
                }
            }

            TasksPile.AddRange(generatedTasks);

            if (TasksPile.Count <= 0)
                return;
            
            // Filter far away new quests if we still have quests to turn in
            // if (TasksPile.Any(task => task.Quest.ShouldQuestBeFinished())) {
            //     TasksPile.RemoveAll(task => task.TaskType == TaskType.PickupQuest && !Quests
            //         .Where(quest => quest.ShouldQuestBeFinished())
            //         .Any(questToBeFinished => questToBeFinished.QuestGivers
            //             .Any(questToFinishGiver => task.Quest.QuestGivers.Any(taskQuestGiver =>
            //                 taskQuestGiver.Position().DistanceTo(questToFinishGiver.Position()) < 250))));
            // }
            
            WAQTask.UpdatePriorityData();
            TasksPile = TasksPile.Where(task => !wManagerSetting.IsBlackListedZone(task.Location))
                .OrderBy(t => t.Priority).ToList();

            WAQTask closestTask = TasksPile.Find(t => !t.IsTimedOut);

            // Check if pathing distance of first entries is not too far (big detour)
            var watchTaskLong = Stopwatch.StartNew();
            float closestTaskDistance = ToolBox.CalculatePathTotalDistance(myPosition, closestTask.Location);
            bool isTaskReachable = closestTaskDistance > 0;

            if (!isTaskReachable)
            {
                closestTask.PutTaskOnTimeout(600, "Unreachable");
                closestTask = null;
            }

            if (isTaskReachable && closestTaskDistance > closestTask.GetDistance * 2)
            {
                Logger.LogError($"Detour detected for task {closestTask.TaskName}");
                int nbTasks = TasksPile.Count;
                int closestTaskPriorityScore = closestTask.CalculatePriority(closestTaskDistance);

                for (int i = 0; i <= nbTasks; i++)
                {
                    if (!TasksPile[i].IsTimedOut)
                    {
                        float walkDistanceToTask = ToolBox.CalculatePathTotalDistance(myPosition, TasksPile[i].Location);
                        if (walkDistanceToTask <= 0)
                        {
                            TasksPile[i].PutTaskOnTimeout(600, "Unreachable");
                            continue;
                        }

                        int taskPriority = TasksPile[i].CalculatePriority(walkDistanceToTask);
                        int nextTaskPriority = TasksPile[i + 1].Priority;

                        if (taskPriority < closestTaskPriorityScore)
                        {
                            closestTaskPriorityScore = taskPriority;
                            closestTask = TasksPile[i];
                        }

                        if (closestTaskPriorityScore < nextTaskPriority)
                            break;
                    }
                }
            }

            // Get unique POIs
            var researchedTasks = new List<WAQTask>();
            var wantedUnitEntries = new List<int>();
            var wantedObjectEntries = new List<int>();
            TasksPile.ForEach(pileTask => {
                if (!researchedTasks.Exists(poiTasks => poiTasks.POIEntry == pileTask.POIEntry) &&
                    !pileTask.IsTimedOut) {
                    if (pileTask.CreatureTemplate != null)
                        wantedUnitEntries.Add(pileTask.POIEntry);
                    if (pileTask.GameObjectTemplate != null)
                        wantedObjectEntries.Add(pileTask.POIEntry);

                    researchedTasks.Add(pileTask);
                }
            });

            // Look for surrounding POIs
            List<WoWObject> surroundingWoWObjects = ObjectManager.GetObjectWoW();

            var watchObjectsShort = Stopwatch.StartNew();
            List<WoWObject> filteredSurroundingObjects = surroundingWoWObjects.FindAll(o => {
                int entry = o.Entry;
                WoWObjectType type = o.Type;
                return (type == WoWObjectType.Unit && wantedUnitEntries.Contains(entry)
                        || type == WoWObjectType.GameObject && wantedObjectEntries.Contains(entry))
                        && !wManagerSetting.IsBlackListed(o.Guid)
                       && o.GetRealDistance() < 60
                       && IsObjectValidForTask(o, researchedTasks.Find(task => task.POIEntry == entry));
            }).OrderBy(o => o.GetDistance).ToList();

            // Get objects real distance
            var watchObjectShort = Stopwatch.StartNew();
            if (filteredSurroundingObjects.Count > 0)
            {
                WoWObject closestObject = filteredSurroundingObjects[0];

                float distance = ToolBox.CalculatePathTotalDistance(myPosition, closestObject.Position);
                bool isObjectReachable = distance > 0;

                if (!isObjectReachable)
                {
                    Logger.LogError($"Blacklisting {closestObject.Name} {closestObject.Guid} because it's unreachable");
                    wManagerSetting.AddBlackList(closestObject.Guid, 1000 * 600, true);
                }

                if (isObjectReachable && distance > closestObject.GetDistance * 2)
                {
                    Logger.LogError($"Detour detected for object {closestObject.Name}");
                    int nbObject = filteredSurroundingObjects.Count;
                    for (int i = 0; i < nbObject - 1; i++)
                    {
                        float walkDistanceToObject = ToolBox.CalculatePathTotalDistance(myPosition, filteredSurroundingObjects[i].Position);

                        if (walkDistanceToObject <= 0)
                        {
                            Logger.LogError($"Blacklisting {filteredSurroundingObjects[i].Name} {filteredSurroundingObjects[i].Guid} because it's unreachable");
                            wManagerSetting.AddBlackList(filteredSurroundingObjects[i].Guid, 1000 * 600, true);
                        }

                        float nextFlyDistanceToObject = filteredSurroundingObjects[i + 1].GetDistance;

                        if (walkDistanceToObject > 0 && walkDistanceToObject < distance)
                        {
                            distance = walkDistanceToObject;
                            closestObject = filteredSurroundingObjects[i];
                        }

                        if (distance < nextFlyDistanceToObject)
                            break;
                    }
                }

                if (!isObjectReachable || distance > closestTaskDistance + 20)
                    TaskInProgressWoWObject = null;
                else
                {
                    TaskInProgressWoWObject = closestObject;
                    closestTask = researchedTasks.Find(task => task.POIEntry == TaskInProgressWoWObject.Entry);
                }
            } else {
                TaskInProgressWoWObject = null;
            }

            TaskInProgress = closestTask;

            if (_tick++ % 5 == 0) Main.QuestTrackerGui.UpdateTasksList();
        }

        private static bool IsObjectValidForTask(WoWObject wowObject, WAQTask task) {
            if (task.TaskType == TaskType.KillAndLoot) {
                var unit = (WoWUnit) wowObject;
                if (!unit.IsAlive && !unit.IsLootable)
                    return false;
            }

            if (task.TaskType == TaskType.Kill) {
                var unit = (WoWUnit) wowObject;
                if (!unit.IsAlive)
                    return false;
            }

            return true;
        }

        public static void UpdateStatuses() {
            ToolBox.UpdateCompletedQuests();
            Dictionary<int, Quest.PlayerQuest> currentQuests = Quest.GetLogQuestId().ToDictionary(quest => quest.ID);
            ModelQuestTemplate[] completedQuests =
                Quests.Where(q => q.Status == QuestStatus.Completed && q.PreviousQuestsIds.Count > 0).ToArray();
            // Update quests statuses
            foreach (ModelQuestTemplate quest in Quests)
            {
                // Quest blacklisted
                if (WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Contains(quest.Id)) {
                    quest.RemoveQuestItemsFromDoNotSellList();
                    quest.Status = QuestStatus.Blacklisted;
                    continue;
                }

                // Quest completed
                if (quest.IsCompleted
                    || completedQuests.Any(q => q.PreviousQuestsIds.Contains(quest.Id))) {
                    quest.RemoveQuestItemsFromDoNotSellList();
                    quest.Status = QuestStatus.Completed;
                    continue;
                }

                // Quest to pickup
                if (quest.IsPickable()
                    && !currentQuests.ContainsKey(quest.Id)) {
                    quest.AddQuestItemsToDoNotSellList();
                    quest.Status = QuestStatus.ToPickup;
                    continue;
                }

                // Log quests
                if (currentQuests.TryGetValue(quest.Id, out Quest.PlayerQuest foundQuest)) {
                    // Quest to turn in
                    if (foundQuest.State == StateFlag.Complete) {
                        quest.Status = QuestStatus.ToTurnIn;
                        quest.AddQuestItemsToDoNotSellList();
                        continue;
                    }

                    // Quest failed
                    if (foundQuest.State == StateFlag.Failed) {
                        quest.Status = QuestStatus.Failed;
                        quest.RemoveQuestItemsFromDoNotSellList();
                        continue;
                    }

                    // Quest in progress
                    if (quest.Status != QuestStatus.InProgress)
                    {
                        Logger.Log($"Recording {quest.LogTitle}");
                        quest.RecordObjectiveIndices();
                        quest.Status = QuestStatus.InProgress;
                        quest.AddQuestItemsToDoNotSellList();
                    }
                    continue;
                }

                quest.Status = QuestStatus.None;
            }

            if (_tick++ % 5 == 0) Main.QuestTrackerGui.UpdateQuestsList();
        }
    }
}