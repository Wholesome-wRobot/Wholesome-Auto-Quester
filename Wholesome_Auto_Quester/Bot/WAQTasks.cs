using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.Quest.PlayerQuest;

namespace Wholesome_Auto_Quester.Bot
{
    public class WAQTasks
    {
        private static int _tick;
        public static List<WAQTask> TasksPile { get; set; } = new List<WAQTask>();
        public static List<ModelQuestTemplate> Quests { get; set; } = new List<ModelQuestTemplate>();
        public static WAQTask TaskInProgress { get; set; }
        public static WoWObject WoWObjectInProgress { get; set; }
        //public static WAQPath PathToCurrentTask { get; set; }
        public static List<int> EntriesToLoot { get; set; }
        public static List<WoWObject> WAQObjectManager { get; set; } = new List<WoWObject>();
        public static int NbQuestsToTurnIn { get; set; }
        public static int NbQuestsInProgress { get; set; }

        public static void AddQuests(List<ModelQuestTemplate> quests)
        {
            quests.ForEach(newQuest =>
            {
                if (!Quests.Exists(quest => quest.Id == newQuest.Id))
                    Quests.Add(newQuest);
            });
        }

        public static void UpdateTasks()
        {
            _tick++;
            WoWLocalPlayer me = ObjectManager.Me;

            if (me.IsOnTaxi
                || me.IsDead
                || !me.IsValid)
            {
                TaskInProgress = null;
                WoWObjectInProgress = null;
                return;
            }

            if (!Main.RequestImmediateTaskReset && !Main.RequestImmediateTaskUpdate)
            {
                if (Fight.InFight
                    || me.HaveBuff("Drink")
                    || me.HaveBuff("Food")
                    || MovementManager.InMovement && (_tick % 5) != 0)
                    return;
            }

            //Logger.Log("Update tasks");
            var generatedQuestTasks = new List<WAQTask>();
            int myContinent = Usefuls.ContinentId;
            int myLevel = (int)ObjectManager.Me.Level;
            Vector3 myPosition = ObjectManager.Me.Position;

            ToolBox.UpdateObjectiveCompletionDict(Quests.Where(quest => quest.Status == QuestStatus.InProgress)
                .Select(quest => quest.Id).ToArray());

            // TASK GENERATION
            foreach (ModelQuestTemplate quest in Quests)
            {
                // Completed
                if (quest.Status == QuestStatus.Completed || quest.Status == QuestStatus.Blacklisted)
                {
                    TasksPile.RemoveAll(t => t.QuestId == quest.Id);
                    continue;
                }

                // quest is in progress but we don't have the starting item
                if (quest.Status == QuestStatus.InProgress
                    && quest.StartItem > 0
                    && !ItemsManager.HasItemById((uint)quest.StartItem))
                    continue;

                // Pickup quest from bag item
                if (quest.Status != QuestStatus.InProgress
                    && quest.Status != QuestStatus.ToTurnIn
                    && quest.StartItemTemplate?.startquest > 0
                    && quest.Id == quest.StartItemTemplate?.startquest)
                {
                    if (ItemsManager.HasItemById((uint)quest.StartItem))
                    {
                        Logger.Log($"Starting {quest.LogTitle} from {quest.StartItemTemplate.Name}");
                        ToolBox.PickupQuestFromBagItem(quest.StartItemTemplate.Name);
                    }
                }

                // Turn in quest
                if (quest.Status == QuestStatus.ToTurnIn)
                {
                    TasksPile.RemoveAll(t => t.QuestId == quest.Id
                        && t.TaskType != TaskType.TurnInQuestToCreature
                        && t.TaskType != TaskType.TurnInQuestToGameObject);
                    if (quest.CreatureQuestTurners.Count > 0) // NPC
                    {
                        quest.CreatureQuestTurners.ForEach(creatureTemplate =>
                        {
                            creatureTemplate.Creatures.ForEach(creature =>
                            {
                                if (/*creature.map == myContinent
                                    &&*/ !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.TurnInQuestToCreature, quest.Id, 5, () => (int)creature.guid)))
                                    generatedQuestTasks.Add(new WAQTask(TaskType.TurnInQuestToCreature, creatureTemplate.name, creatureTemplate.entry,
                                        quest.LogTitle, quest.Id, creature, 5));
                            });
                        });
                    }
                    if (quest.GameObjectQuestTurners.Count > 0) // Game Object
                    {
                        quest.GameObjectQuestTurners.ForEach(gameObjectTemplate =>
                        {
                            gameObjectTemplate.GameObjects.ForEach(gameObject =>
                            {
                                if (/*gameObject.map == myContinent
                                    && */!TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.TurnInQuestToGameObject, quest.Id, 5, () => (int)gameObject.guid)))
                                    generatedQuestTasks.Add(new WAQTask(TaskType.TurnInQuestToGameObject, gameObjectTemplate.name, gameObjectTemplate.entry,
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
                        quest.CreatureQuestGivers.ForEach(creatureTemplate =>
                        {
                            creatureTemplate.Creatures.ForEach(creature =>
                            {
                                if (/*creature.map == myContinent
                                    && */!TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.PickupQuestFromCreature, quest.Id, 6, () => (int)creature.guid)))
                                    generatedQuestTasks.Add(new WAQTask(TaskType.PickupQuestFromCreature, creatureTemplate.name, creatureTemplate.entry,
                                        quest.LogTitle, quest.Id, creature, 6));
                            });
                        });
                    }
                    if (quest.GameObjectQuestGivers.Count > 0) // Game Object
                    {
                        quest.GameObjectQuestGivers.ForEach(gameObjectTemplate =>
                        {
                            gameObjectTemplate.GameObjects.ForEach(gameObject =>
                            {
                                if (/*gameObject.map == myContinent
                                    && */!TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.PickupQuestFromGameObject, quest.Id, 6, () => (int)gameObject.guid)))
                                    generatedQuestTasks.Add(new WAQTask(TaskType.PickupQuestFromGameObject, gameObjectTemplate.name, gameObjectTemplate.entry,
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
                            obje.Creatures.ForEach(creature =>
                            {
                                if (/*creature.map == myContinent
                                    && */obje.CreatureMaxLevel <= myLevel + 2
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.KillAndLoot, quest.Id,
                                            obje.ObjectiveIndex, () => (int)creature.guid)))
                                    generatedQuestTasks.Add(new WAQTask(TaskType.KillAndLoot, obje.CreatureName, obje.CreatureEntry,
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
                                got.GameObjects.ForEach(gameObject =>
                                {
                                    if (/*gameObject.map == myContinent
                                        && */!TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.GatherGameObject, quest.Id,
                                                obje.ObjectiveIndex, () => (int)gameObject.guid)))
                                        generatedQuestTasks.Add(new WAQTask(TaskType.GatherGameObject, got.GameObjectName, got.GameObjectEntry,
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
                                if (/*obje.Area.ContinentId == myContinent
                                    && */!TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.Explore, quest.Id,
                                            obje.ObjectiveIndex)))
                                    generatedQuestTasks.Add(new WAQTask(TaskType.Explore, quest.LogTitle, quest.Id, obje.Area, obje.ObjectiveIndex));
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
                                obje.Creatures.ForEach(creature =>
                                {
                                    if (/*creature.map == myContinent
                                        && */obje.CreatureMaxLevel <= myLevel + 2
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.KillAndLoot, quest.Id,
                                                obje.ObjectiveIndex, () => (int)creature.guid)))
                                        generatedQuestTasks.Add(new WAQTask(TaskType.KillAndLoot, obje.CreatureName, obje.CreatureEntry,
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
                                obje.Creatures.ForEach(creature =>
                                {
                                    if (/*creature.map == myContinent
                                        && */obje.CreatureMaxLevel <= myLevel + 2
                                        && !TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.Kill, quest.Id,
                                                obje.ObjectiveIndex, () => (int)creature.guid)))
                                        generatedQuestTasks.Add(new WAQTask(TaskType.Kill, obje.CreatureName, obje.CreatureEntry, quest.LogTitle,
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
                                    got.GameObjects.ForEach(gameObject =>
                                    {
                                        if (/*gameObject.map == myContinent
                                            && */!TasksPile.Exists(t =>
                                                t.IsSameTask(TaskType.GatherGameObject, quest.Id,
                                                    obje.ObjectiveIndex, () => (int)gameObject.guid)))
                                            generatedQuestTasks.Add(new WAQTask(TaskType.GatherGameObject, got.GameObjectName, got.GameObjectEntry,
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
                                obje.GameObjects.ForEach(gameObject =>
                                {
                                    if (/*gameObject.map == myContinent
                                        && */!TasksPile.Exists(t =>
                                            t.IsSameTask(TaskType.InteractWithWorldObject, quest.Id,
                                                obje.ObjectiveIndex, () => (int)gameObject.guid)))
                                        generatedQuestTasks.Add(new WAQTask(TaskType.InteractWithWorldObject, obje.GameObjectName, obje.GameObjectEntry,
                                        quest.LogTitle, quest.Id, gameObject, obje.ObjectiveIndex));
                                });
                            else
                                TasksPile.RemoveAll(t => t.QuestId == quest.Id
                                                         && t.ObjectiveIndex == obje.ObjectiveIndex
                                                         && t.TaskType == TaskType.InteractWithWorldObject);
                    }
                }
            }

            TasksPile.AddRange(generatedQuestTasks);

            if (!WholesomeAQSettings.CurrentSetting.ContinentTravel)
                TasksPile.RemoveAll(t => !t.IsOnMyContinent);

            WAQTask.UpdatePriorityData();
            TasksPile = TasksPile.Where(task => !wManagerSetting.IsBlackListedZone(task.Location))
                .OrderBy(t => t.Priority).ToList();

            if (TasksPile.Any(t => t.TaskType != TaskType.Grind && !t.IsTimedOut))
                TasksPile.RemoveAll(t => t.TaskType == TaskType.Grind);

            WAQTask closestTask = TasksPile.Find(t => !t.IsTimedOut && !wManagerSetting.IsBlackListedZone(t.Location));

            if (closestTask == null)
            {
                Logger.Log("No task found");
                // Generate Kill tasks for grinding
                DB _database = new DB();
                List<ModelCreatureTemplate> ctToGrind = _database.QueryCreatureTemplatesToGrind();
                _database.Dispose();

                ctToGrind.RemoveAll(ct => ct.Creatures.Any(c => c.map != Usefuls.ContinentId) || ct.IsFriendly);
                Logger.Log($"Found {ctToGrind.Count} templates to grind");
                ctToGrind.ForEach(ct =>
                {
                    ct.Creatures.ForEach(c =>
                        TasksPile.Add(new WAQTask(TaskType.Grind, ct.name, ct.entry, c)));
                });
                return;
            }

            if (!closestTask.IsOnMyContinent)
            {
                TaskInProgress = closestTask;
                return;
            }

            // Check if pathing distance of first entries is not too far (big detour)
            var watchTaskLong = Stopwatch.StartNew();
            WAQPath pathToClosestTask = ToolBox.GetWAQPath(myPosition, closestTask.Location);
            /*
            if (!pathToClosestTask.IsReachable)
                pathToClosestTask = ToolBox.AdjustPathToTask(closestTask);
            */
            if (!pathToClosestTask.IsReachable)
            {
                closestTask.PutTaskOnTimeout(600, "Unreachable (1)", true);
                BlacklistHelper.AddZone(closestTask.Location, 5, "Unreachable (1)");
                Main.RequestImmediateTaskReset = true;
                return;
            }

            if (pathToClosestTask.Distance > closestTask.GetDistance * 2)
            {
                //Logger.Log($"Detour detected for task {closestTask.TaskName}");
                int closestTaskPriorityScore = closestTask.CalculatePriority(pathToClosestTask.Distance);

                for (int i = 0; i < TasksPile.Count - 1; i++)
                {
                    if (i > 3) break;
                    if (!TasksPile[i].IsTimedOut)
                    {
                        WAQPath pathToNewTask = ToolBox.GetWAQPath(myPosition, TasksPile[i].Location);
                        /*
                        if (!pathToNewTask.IsReachable)
                            pathToNewTask = ToolBox.AdjustPathToTask(TasksPile[i]);
                        */
                        if (!pathToNewTask.IsReachable)
                        {
                            TasksPile[i].PutTaskOnTimeout(600, "Unreachable (2)", true);
                            BlacklistHelper.AddZone(closestTask.Location, 5, "Unreachable (2)");
                            continue;
                        }

                        int newTaskPriority = TasksPile[i].CalculatePriority(pathToNewTask.Distance);

                        if (newTaskPriority < closestTaskPriorityScore)
                        {
                            closestTaskPriorityScore = newTaskPriority;
                            closestTask = TasksPile[i];
                            pathToClosestTask = pathToNewTask;
                        }

                        if (closestTaskPriorityScore < TasksPile[i + 1].Priority)
                            break;
                    }
                }
            }

            // Get unique POIs
            var researchedTasks = new List<WAQTask>();
            var wantedUnitEntries = new List<int>();
            var wantedObjectEntries = new List<int>();
            var wantedLootEntries = new List<int>();
            TasksPile.ForEach(pileTask =>
            {
                if (!researchedTasks.Exists(poiTasks => poiTasks.ObjectDBGuid == pileTask.ObjectDBGuid) &&
                    !pileTask.IsTimedOut)
                {
                    if (pileTask.Creature != null)
                    {
                        wantedUnitEntries.Add(pileTask.TargetEntry);
                        if (pileTask.TaskType == TaskType.KillAndLoot)
                            wantedLootEntries.Add(pileTask.TargetEntry);
                    }
                    if (pileTask.GameObject != null)
                        wantedObjectEntries.Add(pileTask.TargetEntry);

                    researchedTasks.Add(pileTask);
                }
            });

            EntriesToLoot = wantedLootEntries;

            // Look for surrounding POIs
            WAQObjectManager = ObjectManager.GetObjectWoW()
                //.Where(o => o.GetRealDistance() < 60)
                .OrderBy(o => o.GetDistance)
                .ToList();

            var watchObjectsShort = Stopwatch.StartNew();
            List<WoWObject> filteredSurroundingObjects = WAQObjectManager.FindAll(o =>
            {
                int objectEntry = o.Entry;
                WoWObjectType type = o.Type;
                return (type == WoWObjectType.Unit && wantedUnitEntries.Contains(objectEntry)
                    || type == WoWObjectType.GameObject && wantedObjectEntries.Contains(objectEntry))
                    && !wManagerSetting.IsBlackListed(o.Guid)
                    && !wManagerSetting.IsBlackListedZone(o.Position)
                    && IsObjectValidForTask(o, researchedTasks.Find(task => task.TargetEntry == objectEntry));
            }).ToList();

            // Get objects real distance
            var watchObjectShort = Stopwatch.StartNew();
            if (filteredSurroundingObjects.Count > 0)
            {
                WoWObject closestObject = filteredSurroundingObjects[0];
                WAQPath pathToClosestObject = ToolBox.GetWAQPath(myPosition, closestObject.Position);
                //Logger.Log(closestObject.Name + " " + closestObject.Guid + " " + pathToClosestObject.IsReachable.ToString() + " " + pathToClosestObject.Distance);
                /*
                if (!pathToClosestObject.IsReachable)
                    pathToClosestObject = ToolBox.AdjustPathToObject(closestObject);
                */
                if (!pathToClosestObject.IsReachable)
                {
                    BlacklistHelper.AddNPC(closestObject.Guid, "Unreachable (3)");
                    Main.RequestImmediateTaskReset = true;
                    return;
                }

                if (pathToClosestObject.Distance > closestObject.GetDistance * 2)
                {
                    int nbObject = filteredSurroundingObjects.Count;
                    for (int i = 1; i < nbObject - 1; i++)
                    {
                        WAQPath pathToNewObject = ToolBox.GetWAQPath(myPosition, filteredSurroundingObjects[i].Position);
                        /*
                        if (!pathToNewObject.IsReachable)
                            pathToNewObject = ToolBox.AdjustPathToObject(filteredSurroundingObjects[i]);
                        */
                        if (!pathToNewObject.IsReachable)
                        {
                            Logger.Log($"Blacklisting {filteredSurroundingObjects[i].Name} {filteredSurroundingObjects[i].Guid} because it's unreachable");
                            BlacklistHelper.AddNPC(filteredSurroundingObjects[i].Guid, "Unreachable (4)");
                            break;
                        }

                        if (pathToNewObject.Distance < pathToClosestObject.Distance)
                        {
                            pathToClosestObject = pathToNewObject;
                            closestObject = filteredSurroundingObjects[i];
                        }

                        float flyDistanceToNextObject = filteredSurroundingObjects[i + 1].GetDistance;
                        if (pathToClosestObject.Distance < flyDistanceToNextObject)
                            break;
                    }
                }

                if (!pathToClosestObject.IsReachable || pathToClosestObject.Distance > pathToClosestTask.Distance + 20)
                {
                    WoWObjectInProgress = null;
                }
                else
                {
                    WoWObjectInProgress = closestObject;
                    closestTask = researchedTasks
                        .OrderBy(t => t.Location.DistanceTo(WoWObjectInProgress.Position))
                        .FirstOrDefault(task => task.TargetEntry == WoWObjectInProgress.Entry);
                }
            }
            else
                WoWObjectInProgress = null;

            TaskInProgress = closestTask;
            //PathToCurrentTask = pathToClosestTask;
            if (_tick % 2 == 0) Main.QuestTrackerGui.UpdateTasksList();
        }

        private static bool IsObjectValidForTask(WoWObject wowObject, WAQTask task)
        {
            if (task.TaskType == TaskType.KillAndLoot)
            {
                WoWUnit unit = (WoWUnit)wowObject;
                if (!unit.IsAlive && !unit.IsLootable || unit.IsTaggedByOther)
                    return false;
            }

            if (task.TaskType == TaskType.Kill || task.TaskType == TaskType.Grind)
            {
                WoWUnit unit = (WoWUnit)wowObject;
                if (!unit.IsAlive || unit.IsTaggedByOther)
                    return false;
            }

            return true;
        }

        public static void UpdateStatuses()
        {
            Dictionary<int, Quest.PlayerQuest> logQuests = Quest.GetLogQuestId().ToDictionary(quest => quest.ID);
            List<string> itemsToAddToDNSList = new List<string>();
            ToolBox.UpdateCompletedQuests();
            /*
            if (ToolBox.GetServerNbCompletedQuests() <= 0 && WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Count > 0)
            {
                Logger.Log($"Waiting for server-side list of completed quests " +
                    $"({ToolBox.GetServerNbCompletedQuests()}/{WholesomeAQSettings.CurrentSetting.ListCompletedQuests.Count})");
                return;
            }
            */

            // Update quests statuses
            foreach (ModelQuestTemplate quest in Quests)
            {
                // Quest blacklisted
                if (WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Contains(quest.Id))
                {
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

            NbQuestsToTurnIn = Quests
                .FindAll(q => q.Status == QuestStatus.ToTurnIn)
                .Count;

            NbQuestsInProgress = Quests
                .FindAll(q => q.Status == QuestStatus.InProgress)
                .Count;

            // WAQ DNS List
            int WAQlistStartIndex = wManagerSetting.CurrentSetting.DoNotSellList.IndexOf("WAQStart");
            int WAQlistEndIndex = wManagerSetting.CurrentSetting.DoNotSellList.IndexOf("WAQEnd");
            int WAQListLength = WAQlistEndIndex - WAQlistStartIndex - 1;
            List<string> initialWAQList = wManagerSetting.CurrentSetting.DoNotSellList.GetRange(WAQlistStartIndex + 1, WAQListLength);
            if (!initialWAQList.SequenceEqual(itemsToAddToDNSList))
            {
                initialWAQList.ForEach(item =>
                {
                    if (!itemsToAddToDNSList.Contains(item))
                        Logger.Log($"Removed {item} from Do Not Sell List");
                });
                itemsToAddToDNSList.ForEach(item =>
                {
                    if (!initialWAQList.Contains(item))
                        Logger.Log($"Added {item} to Do Not Sell List");
                });
                wManagerSetting.CurrentSetting.DoNotSellList.RemoveRange(WAQlistStartIndex + 1, WAQListLength);
                wManagerSetting.CurrentSetting.DoNotSellList.InsertRange(WAQlistStartIndex + 1, itemsToAddToDNSList);
                wManagerSetting.CurrentSetting.Save();
            }

            if ((_tick % 2) == 0) Main.QuestTrackerGui.UpdateQuestsList();
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