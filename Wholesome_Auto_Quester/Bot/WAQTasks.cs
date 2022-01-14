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

            foreach (ModelQuestTemplate quest in Quests)
            {
                // Completed
                if (quest.Status == QuestStatus.Completed || quest.Status == QuestStatus.Blacklisted) {
                    TasksPile.RemoveAll(t => t.QuestId == quest.Id);
                    continue;
                }

                // Turn in quest
                if (quest.Status == QuestStatus.ToTurnIn)
                {
                    TasksPile.RemoveAll(t => t.QuestId == quest.Id 
                        && t.TaskType != TaskType.TurnInQuestToCreature 
                        && t.TaskType != TaskType.TurnInQuestToGameObject);
                    if (quest.CreatureQuestTurners.Count > 0) // NPC
                    {
                        quest.CreatureQuestTurners.ForEach(creatureTemplate => {
                            creatureTemplate.Creatures.ForEach(creature =>
                            {
                                if (creature.map == myContinent
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.TurnInQuestToCreature, quest.Id, 5, () => (int)creature.guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.TurnInQuestToCreature, creatureTemplate.name, creatureTemplate.entry,
                                        quest.LogTitle, quest.Id, creature, 5));
                            });
                        });
                    }
                    if (quest.GameObjectQuestTurners.Count > 0) // Game Object
                    {
                        quest.GameObjectQuestTurners.ForEach(gameObjectTemplate => {
                            gameObjectTemplate.GameObjects.ForEach(gameObject =>
                            {
                                if (gameObject.map == myContinent
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.TurnInQuestToGameObject, quest.Id, 5, () => (int)gameObject.guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.TurnInQuestToGameObject, gameObjectTemplate.name, gameObjectTemplate.entry,
                                        quest.LogTitle, quest.Id, gameObject, 5));
                            });
                        });
                    }
                    continue;
                }

                // Pick up quest
                if (quest.Status == QuestStatus.ToPickup)
                {
                    TasksPile.RemoveAll(t => t.QuestId == quest.Id 
                        && t.TaskType != TaskType.PickupQuestFromCreature
                        && t.TaskType != TaskType.PickupQuestFromGameObject);
                    if (quest.CreatureQuestGivers.Count > 0) // NPC
                    {
                        quest.CreatureQuestGivers.ForEach(creatureTemplate => {
                            creatureTemplate.Creatures.ForEach(creature =>
                            {
                                if (creature.map == myContinent
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.PickupQuestFromCreature, quest.Id, 6, () => (int)creature.guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.PickupQuestFromCreature, creatureTemplate.name, creatureTemplate.entry,
                                        quest.LogTitle, quest.Id, creature, 6));
                            });
                        });
                    }
                    if (quest.GameObjectQuestGivers.Count > 0) // Game Object
                    {
                        quest.GameObjectQuestGivers.ForEach(gameObjectTemplate => {
                            gameObjectTemplate.GameObjects.ForEach(gameObject =>
                            {
                                if (gameObject.map == myContinent
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.PickupQuestFromGameObject, quest.Id, 6, () => (int)gameObject.guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.PickupQuestFromGameObject, gameObjectTemplate.name, gameObjectTemplate.entry,
                                        quest.LogTitle, quest.Id, gameObject, 6));
                            });
                        });
                    }
                    continue;
                }

                // Prerequisite gathers & loots
                if (quest.Status == QuestStatus.InProgress) 
                {
                    TasksPile.RemoveAll(t => t.QuestId == quest.Id
                                             && (t.TaskType == TaskType.PickupQuestFromCreature 
                                                || t.TaskType == TaskType.TurnInQuestToCreature
                                                || t.TaskType == TaskType.TurnInQuestToGameObject
                                                || t.TaskType == TaskType.PickupQuestFromGameObject));

                    bool needsPrerequisite = false;
                    foreach (KillLootObjective obje in quest.PrerequisiteLootObjectives)
                    {
                        if (ItemsManager.GetItemCountById((uint)obje.ItemEntry) <= 0)
                        {
                            needsPrerequisite = true;
                            obje.Creatures.ForEach(creature => {
                                if (creature.map == myContinent
                                    && obje.CreatureMaxLevel <= myLevel + 2
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.KillAndLoot, quest.Id,
                                            obje.ObjectiveIndex, () => (int)creature.guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.KillAndLoot, obje.CreatureName, obje.CreatureEntry,
                                        quest.LogTitle, quest.Id, creature, obje.ObjectiveIndex));
                            });
                        }
                        else
                            TasksPile.RemoveAll(t => t.QuestId == quest.Id
                                                     && t.ObjectiveIndex == obje.ObjectiveIndex
                                                     && t.TaskType == TaskType.KillAndLoot);
                    }

                    // Gather Game Object
                    foreach (GatherObjective obje in quest.PrerequisiteGatherObjectives)
                    {
                        foreach (ObjGOTemplate got in obje.ObjGOTemplates)
                        {
                            if (ItemsManager.GetItemCountById((uint)got.GameObjectEntry) <= 0)
                            {
                                needsPrerequisite = true;
                                got.GameObjects.ForEach(gameObject => {
                                    if (gameObject.map == myContinent
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.GatherGameObject, quest.Id,
                                                obje.ObjectiveIndex, () => (int)gameObject.guid)))
                                        generatedTasks.Add(new WAQTask(TaskType.GatherGameObject, got.GameObjectName, got.GameObjectEntry,
                                            quest.LogTitle, quest.Id, gameObject, obje.ObjectiveIndex));
                                });
                            }
                            else
                                TasksPile.RemoveAll(t => t.QuestId == quest.Id
                                                            && t.ObjectiveIndex == obje.ObjectiveIndex
                                                            && t.TaskType == TaskType.GatherGameObject);
                        }
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
                                    generatedTasks.Add(new WAQTask(TaskType.Explore, quest.LogTitle, quest.Id, obje.Area, obje.ObjectiveIndex));
                            }
                            else
                            {
                                TasksPile.RemoveAll(t => t.QuestId == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.Explore);
                            }

                        // Kill & Loot
                        foreach (KillLootObjective obje in quest.KillLootObjectives)
                        {
                            if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, quest.Id))
                                obje.Creatures.ForEach(creature => {
                                    if (creature.map == myContinent
                                        && obje.CreatureMaxLevel <= myLevel + 2
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.KillAndLoot, quest.Id,
                                                obje.ObjectiveIndex, () => (int)creature.guid)))
                                        generatedTasks.Add(new WAQTask(TaskType.KillAndLoot, obje.CreatureName, obje.CreatureEntry, 
                                            quest.LogTitle, quest.Id, creature, obje.ObjectiveIndex));
                                });
                            else
                                TasksPile.RemoveAll(t => t.QuestId == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.KillAndLoot);
                        }

                        // Kill
                        foreach (KillObjective obje in quest.KillObjectives)
                            if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, quest.Id))
                                obje.Creatures.ForEach(creature => {
                                    if (creature.map == myContinent
                                        && obje.CreatureMaxLevel <= myLevel + 2
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.Kill, quest.Id,
                                                obje.ObjectiveIndex, () => (int)creature.guid)))
                                        generatedTasks.Add(new WAQTask(TaskType.Kill, obje.CreatureName, obje.CreatureEntry, quest.LogTitle, 
                                            quest.Id, creature, obje.ObjectiveIndex));
                                });
                            else
                                TasksPile.RemoveAll(t => t.QuestId == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.Kill);

                        // Gather object
                        foreach (GatherObjective obje in quest.GatherObjectives)
                        {
                            foreach (ObjGOTemplate got in obje.ObjGOTemplates)
                            {
                                if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, quest.Id))
                                    got.GameObjects.ForEach(gameObject => {
                                        if (gameObject.map == myContinent
                                            && !TasksPile.Exists(t =>
                                                t.IsSameTask(TaskType.GatherGameObject, quest.Id,
                                                    obje.ObjectiveIndex, () => (int)gameObject.guid)))
                                            generatedTasks.Add(new WAQTask(TaskType.GatherGameObject, got.GameObjectName, got.GameObjectEntry,
                                            quest.LogTitle, quest.Id, gameObject, obje.ObjectiveIndex));
                                    });
                                else
                                    TasksPile.RemoveAll(t => t.QuestId == quest.Id
                                                             && t.ObjectiveIndex == obje.ObjectiveIndex
                                                             && t.TaskType == TaskType.GatherGameObject);
                            }
                        }

                        // Interact with object
                        foreach (InteractObjective obje in quest.InteractObjectives)
                            if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, quest.Id))
                                obje.GameObjects.ForEach(gameObject => {
                                    if (gameObject.map == myContinent
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.InteractWithWorldObject, quest.Id,
                                                obje.ObjectiveIndex, () => (int)gameObject.guid)))
                                        generatedTasks.Add(new WAQTask(TaskType.InteractWithWorldObject, obje.GameObjectName, obje.GameObjectEntry,
                                        quest.LogTitle, quest.Id, gameObject, obje.ObjectiveIndex));
                                });
                            else
                                TasksPile.RemoveAll(t => t.QuestId == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.InteractWithWorldObject);
                    }
                }
            }

            TasksPile.AddRange(generatedTasks);

            if (TasksPile.Count <= 0 || !TasksPile.Exists(t => !t.IsTimedOut))
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
                closestTask.PutTaskOnTimeout(600, "Unreachable (1)");
                closestTask = null;
                return;
            }

            if (isTaskReachable && closestTaskDistance > closestTask.GetDistance * 2)
            {
                Logger.LogError($"Detour detected for task {closestTask.TaskName}");
                int nbTasks = TasksPile.Count;
                int closestTaskPriorityScore = closestTask.CalculatePriority(closestTaskDistance);

                for (int i = 0; i < nbTasks - 1; i++)
                {
                    if (i > 3) break;
                    if (!TasksPile[i].IsTimedOut)
                    {
                        float walkDistanceToTask = ToolBox.CalculatePathTotalDistance(myPosition, TasksPile[i].Location);
                        if (walkDistanceToTask <= 0)
                        {
                            TasksPile[i].PutTaskOnTimeout(600, "Unreachable (2)");
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
                if (!researchedTasks.Exists(poiTasks => poiTasks.ObjectGuid == pileTask.ObjectGuid) &&
                    !pileTask.IsTimedOut) {
                    if (pileTask.Creature != null)
                        wantedUnitEntries.Add(pileTask.TargetEntry);
                    if (pileTask.GameObject != null)
                        wantedObjectEntries.Add(pileTask.TargetEntry);

                    researchedTasks.Add(pileTask);
                }
            });

            // Look for surrounding POIs
            List<WoWObject> surroundingWoWObjects = ObjectManager.GetObjectWoW();

            var watchObjectsShort = Stopwatch.StartNew();
            List<WoWObject> filteredSurroundingObjects = surroundingWoWObjects.FindAll(o => {
                int objectEntry = o.Entry;
                WoWObjectType type = o.Type;
                return (type == WoWObjectType.Unit && wantedUnitEntries.Contains(objectEntry)
                        || type == WoWObjectType.GameObject && wantedObjectEntries.Contains(objectEntry))
                        && !wManagerSetting.IsBlackListed(o.Guid)
                        && o.GetRealDistance() < 60
                        && IsObjectValidForTask(o, researchedTasks.Find(task => task.TargetEntry == objectEntry));
            }).OrderBy(o => o.GetDistance).ToList();

            // Get objects real distance
            var watchObjectShort = Stopwatch.StartNew();
            if (filteredSurroundingObjects.Count > 0)
            {
                WoWObject closestObject = filteredSurroundingObjects[0];

                float distanceToClosestObject = ToolBox.CalculatePathTotalDistance(myPosition, closestObject.Position);
                bool isObjectReachable = distanceToClosestObject > 0;

                if (!isObjectReachable)
                {
                    Logger.LogError($"Blacklisting {closestObject.Name} {closestObject.Guid} because it's unreachable");
                    wManagerSetting.AddBlackList(closestObject.Guid, 1000 * 600, true);
                    return;
                }

                if (isObjectReachable && distanceToClosestObject > closestObject.GetDistance * 2)
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

                        if (walkDistanceToObject > 0 && walkDistanceToObject < distanceToClosestObject)
                        {
                            distanceToClosestObject = walkDistanceToObject;
                            closestObject = filteredSurroundingObjects[i];
                        }

                        if (distanceToClosestObject < nextFlyDistanceToObject)
                            break;
                    }
                }

                if (!isObjectReachable || distanceToClosestObject > closestTaskDistance + 20)
                {
                    TaskInProgressWoWObject = null;
                }
                else
                {
                    TaskInProgressWoWObject = closestObject;
                    closestTask = researchedTasks.Find(task => task.TargetEntry == TaskInProgressWoWObject.Entry);
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
            Dictionary<int, Quest.PlayerQuest> logQuests = Quest.GetLogQuestId().ToDictionary(quest => quest.ID);
            List<string> itemsToAddToDNSList = new List<string>();
            if (ToolBox.GetServerNbCompletedQuests() <= 0 && WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Count > 0)
            {
                ToolBox.UpdateCompletedQuests();
                Logger.Log("Waiting for server-side list of completed quests");
                return;
            }

            // Update quests statuses
            foreach (ModelQuestTemplate quest in Quests)
            {
                // Quest blacklisted
                if (WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Contains(quest.Id)) {
                    quest.Status = QuestStatus.Blacklisted;
                    continue;
                }

                // Mark quest as completed if it's part of an exclusive group
                if (quest.QuestAddon.ExclusiveGroup > 0)
                {
                    if (quest.QuestAddon.ExclusiveQuests.Any(qId => qId != quest.Id 
                        && (ToolBox.IsQuestCompleted(qId) || logQuests.ContainsKey(qId))))
                    {
                        quest.Status = QuestStatus.Completed;
                        continue;
                    }
                }

                // Quest completed
                if (quest.IsCompleted)
                {
                    quest.Status = QuestStatus.Completed;
                    continue;
                }

                // Quest to pickup
                if (quest.IsPickable() && !logQuests.ContainsKey(quest.Id))
                {
                    itemsToAddToDNSList.AddRange(quest.GetItemsStringsList());
                    quest.Status = QuestStatus.ToPickup;
                    continue;
                }

                // Log quests
                if (logQuests.TryGetValue(quest.Id, out Quest.PlayerQuest foundQuest)) 
                {
                    // Quest to turn in
                    if (foundQuest.State == StateFlag.Complete)
                    {
                        itemsToAddToDNSList.AddRange(quest.GetItemsStringsList());
                        quest.Status = QuestStatus.ToTurnIn;
                        continue;
                    }

                    // Quest failed
                    if (foundQuest.State == StateFlag.Failed)
                    {
                        quest.Status = QuestStatus.Failed;
                        continue;
                    }

                    // Quest in progress
                    quest.Status = QuestStatus.InProgress;
                    itemsToAddToDNSList.AddRange(quest.GetItemsStringsList());
                    if (!quest.AreObjectivesRecorded && quest.GetAllObjectives().Count > 0)
                        quest.RecordObjectiveIndices();
                    continue;
                }

                quest.Status = QuestStatus.None;
            }

            // WAQ DNS List
            int WAQlistStartIndex = wManagerSetting.CurrentSetting.DoNotSellList.IndexOf("WAQStart");
            int WAQlistEndIndex = wManagerSetting.CurrentSetting.DoNotSellList.IndexOf("WAQEnd");
            int WAQListLength = WAQlistEndIndex - WAQlistStartIndex - 1;
            List<string> initialWAQList = wManagerSetting.CurrentSetting.DoNotSellList.GetRange(WAQlistStartIndex + 1, WAQListLength);
            if (!initialWAQList.SequenceEqual(itemsToAddToDNSList))
            {
                initialWAQList.ForEach(item => {
                    if (!itemsToAddToDNSList.Contains(item))
                        Logger.Log($"Removed {item} from Do Not Sell List"); });
                itemsToAddToDNSList.ForEach(item => {
                    if (!initialWAQList.Contains(item))
                        Logger.Log($"Added {item} to Do Not Sell List"); });
                wManagerSetting.CurrentSetting.DoNotSellList.RemoveRange(WAQlistStartIndex + 1, WAQListLength);
                wManagerSetting.CurrentSetting.DoNotSellList.InsertRange(WAQlistStartIndex + 1, itemsToAddToDNSList);
                wManagerSetting.CurrentSetting.Save();
            }
            
            if (_tick++ % 5 == 0) Main.QuestTrackerGui.UpdateQuestsList();
        }

        public static void MarQuestAsCompleted(int questId)
        {
            if (!WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Contains(questId))
            {
                WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Add(questId);
                WholesomeAQSettings.CurrentSetting.Save();
            }
        }
    }
}