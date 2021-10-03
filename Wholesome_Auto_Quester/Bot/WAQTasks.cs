using FlXProfiles;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.Quest.PlayerQuest;

namespace Wholesome_Auto_Quester.Bot
{
    public class WAQTasks
    {
        public static List<WAQTask> TasksPile { get; set; } = new List<WAQTask>();
        public static List<ModelQuest> Quests { get; set; } = new List<ModelQuest>();
        public static WAQTask TaskInProgress { get; set; } = null;
        public static WoWObject TaskInProgressWoWObject { get; set; } = null;

        public static void AddQuests(List<ModelQuest> quests)
        {
            quests.ForEach(newQuest =>
            {
                if (!Quests.Exists(quest => quest.Id == newQuest.Id))
                    Quests.Add(newQuest);
            });
        }

        public static void UpdateTasks()
        {
            //Logger.Log($"Finished quests : {Quest.FinishedQuestSet.Count}");

            if (Quests.Count <= 0)
                return;

            //Logger.Log("Update tasks");
            List<WAQTask> generatedTasks = new List<WAQTask>();
            foreach(ModelQuest quest in Quests)
            {
                // Completed
                if (quest.Status == QuestStatus.Completed || quest.Status == QuestStatus.Blacklisted)
                {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id);
                    continue;
                }

                // Turn in
                if (quest.Status == QuestStatus.ToTurnIn)
                {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id && t.TaskType != TaskType.TurnInQuest);
                    quest.QuestTurners.ForEach(qt => {
                        if (qt.Map == Usefuls.ContinentId
                            && !TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.TurnInQuest, quest.Id, 5, qt.Guid)))
                            generatedTasks.Add(new WAQTask(TaskType.TurnInQuest, qt, quest, 5));
                    });
                    continue;
                }

                // Pick up
                if (quest.Status == QuestStatus.ToPickup)
                {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id && t.TaskType != TaskType.PickupQuest);
                    quest.QuestGivers.ForEach(qg => {
                        if (qg.Map == Usefuls.ContinentId
                            && !TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.PickupQuest, quest.Id, 6, qg.Guid)))
                            generatedTasks.Add(new WAQTask(TaskType.PickupQuest, qg, quest, 6));
                    });
                    continue;
                }

                if (quest.Status == QuestStatus.InProgress)
                {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                        && (t.TaskType == TaskType.PickupQuest || t.TaskType == TaskType.TurnInQuest));
                    
                    // Explore
                    foreach (ExplorationObjective areaObjective in quest.ExplorationObjectives)
                    {
                        if (!Quest.IsObjectiveComplete(areaObjective.objectiveIndex, quest.Id))
                        {
                            if (areaObjective.area.ContinentId == Usefuls.ContinentId
                                && !TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.Explore, quest.Id, areaObjective.objectiveIndex)))
                                generatedTasks.Add(new WAQTask(TaskType.Explore, areaObjective.area, quest, areaObjective.objectiveIndex));
                        }
                        else
                        {
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                && t.ObjectiveIndex == areaObjective.objectiveIndex
                                && t.TaskType == TaskType.Explore);
                        }
                    }

                    // Kill & Loot
                    foreach (CreatureToLootObjective lootObjective in quest.CreaturesToLootObjectives)
                    {
                        if (!Quest.IsObjectiveComplete(lootObjective.objectiveIndex, quest.Id))
                        {
                            lootObjective.worldCreatures.ForEach(wc => {
                                if (wc.Map == Usefuls.ContinentId
                                    && !TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.KillAndLoot, quest.Id, lootObjective.objectiveIndex, wc.Guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.KillAndLoot, wc, quest, lootObjective.objectiveIndex));
                            });
                        }
                        else
                        {
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                && t.ObjectiveIndex == lootObjective.objectiveIndex
                                && t.TaskType == TaskType.KillAndLoot);
                        }
                    }

                    // Kill
                    foreach (CreaturesToKillObjective killObjective in quest.CreaturesToKillObjectives)
                    {
                        if (!Quest.IsObjectiveComplete(killObjective.objectiveIndex, quest.Id))
                        {
                            killObjective.worldCreatures.ForEach(wc => {
                                if (wc.Map == Usefuls.ContinentId
                                    && !TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.Kill, quest.Id, killObjective.objectiveIndex, wc.Guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.Kill, wc, quest, killObjective.objectiveIndex));
                            });
                        }
                        else
                        {
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                && t.ObjectiveIndex == killObjective.objectiveIndex
                                && t.TaskType == TaskType.Kill);
                        }
                    }

                    // Gather object
                    foreach (GatherObjectObjective gatherObjective in quest.GatherObjectsObjectives)
                    {
                        if (!Quest.IsObjectiveComplete(gatherObjective.objectiveIndex, quest.Id))
                        {
                            gatherObjective.worldObjects.ForEach(wo => {
                                if (wo.Map == Usefuls.ContinentId
                                    && !TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.GatherObject, quest.Id, gatherObjective.objectiveIndex, wo.Guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.GatherObject, wo, quest, gatherObjective.objectiveIndex));
                            });
                        }
                        else
                        {
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                && t.ObjectiveIndex == gatherObjective.objectiveIndex
                                && t.TaskType == TaskType.GatherObject);
                        }
                    }
                }
            }

            TasksPile.AddRange(generatedTasks);
            TasksPile = TasksPile.OrderBy(t => t.Priority).ToList();

            WAQTask closestTask = TasksPile.Find(t => !t.IsTimedOut);

            // Get unique POIs
            List<WAQTask> researchedTasks = new List<WAQTask>();
            List<int> wantedUnitEntries = new List<int>();
            List<int> wantedObjectEntries = new List<int>();
            TasksPile.ForEach(pileTask =>
            {
                if (!researchedTasks.Exists(poiTasks => poiTasks.POIEntry == pileTask.POIEntry) && !pileTask.IsTimedOut)
                {
                    if (pileTask.Npc != null)
                        wantedUnitEntries.Add(pileTask.POIEntry);
                    if (pileTask.GatherObject != null)
                        wantedObjectEntries.Add(pileTask.POIEntry);

                    researchedTasks.Add(pileTask);
                }
            });

            // Look for surrounding POIs
            List<WoWObject> surroundingWoWObjects = ObjectManager.GetObjectWoW();

            List<WoWObject> filteredSurroundingObjects = surroundingWoWObjects.FindAll(o =>
                (o is WoWUnit || o is WoWObject)
                && (wantedUnitEntries.Contains(o.Entry) && o is WoWUnit || wantedObjectEntries.Contains(o.Entry) && o is WoWObject)
                && o.GetDistance < 40
                && IsObjectValidForTask(o, researchedTasks.Find(task => task.POIEntry == o.Entry)
            ));

            if (filteredSurroundingObjects.Count > 0)
            {
                TaskInProgressWoWObject = filteredSurroundingObjects.OrderBy(o => o.GetDistance).First();
                //Logger.Log($"Closest POI is {TaskInProgressWoWObject.Name} ({TaskInProgressWoWObject.GetDistance})");
                closestTask = researchedTasks.Find(task => task.POIEntry == TaskInProgressWoWObject.Entry);
            }
            else
            {
                //Logger.Log($"Closest POI is NULL");
                TaskInProgressWoWObject = null;
            }

            // If we switch task in progress
            if (TaskInProgress != null && closestTask != null && ToolBox.GetTaskId(TaskInProgress) != ToolBox.GetTaskId(closestTask))
            {
                TaskInProgress = closestTask;
                MoveHelper.StopCurrentMovementThread();
            }
            else
            {
                TaskInProgress = closestTask;
            }

            //Logger.Log($"Active task is {TaskInProgress?.TaskName} - {TaskInProgress?.GetDistance}- {TaskInProgress?.Location.ToStringNewVector()}");

            Main.questTrackerGUI.UpdateTasksList();
        }

        private static bool IsObjectValidForTask(WoWObject wowObject, WAQTask task)
        {
            if (task.TaskType == TaskType.KillAndLoot)
            {
                WoWUnit unit = (WoWUnit)wowObject;
                if (!unit.IsAlive && !unit.IsLootable)
                    return false;
            }

            if (task.TaskType == TaskType.Kill)
            {
                WoWUnit unit = (WoWUnit)wowObject;
                if (!unit.IsAlive)
                    return false;
            }

            return true;
        }

        public static void UpdateStatuses()
        {
            // Update quests statuses
            foreach (ModelQuest quest in Quests)
            {
                // Quest blacklisted
                if (WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Contains(quest.Id))
                {
                    quest.Status = QuestStatus.Blacklisted;
                    continue;
                }

                // Quest completed
                if (quest.IsCompleted
                    || Quests.Any(q => q.Status == QuestStatus.Completed && q.PreviousQuestsIds.Contains(quest.Id)))
                {
                    quest.Status = QuestStatus.Completed;
                    continue;
                }
                
                // Quest to pickup
                if (quest.IsPickable()
                    && !Quest.HasQuest(quest.Id))
                {
                    quest.Status = QuestStatus.ToPickup;
                    continue;
                }

                // Log quests
                if (Quest.GetLogQuestId().Exists(q => q.ID == quest.Id))
                {
                    // Quest to turn in
                    if (Quest.GetLogQuestId().Find(q => q.ID == quest.Id).State == StateFlag.Complete)
                    {
                        quest.Status = QuestStatus.ToTurnIn;
                        continue;
                    }

                    // Quest failed
                    if (Quest.GetLogQuestId().Find(q => q.ID == quest.Id).State == StateFlag.Failed)
                    {
                        quest.Status = QuestStatus.Failed;
                        continue;
                    }

                    // Quest in progress
                    if (Quest.HasQuest(quest.Id))
                    {
                        quest.Status = QuestStatus.InProgress;
                        continue;
                    }
                }

                quest.Status = QuestStatus.None;
            }

            Main.questTrackerGUI.UpdateQuestsList();
        }
    }
}
