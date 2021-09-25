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
        private static List<ModelQuest> Quests { get; set; } = new List<ModelQuest>();
        public static WAQTask TaskInProgress { get; set; } = null;
        public static WoWObject TaskInProgressWoWObject { get; set; } = null;

        public static void AddQuests(List<ModelQuest> quests)
        {
            quests.ForEach(newQuest =>
            {
                Logger.Log($"{newQuest.RequiredClasses}");
                if (!Quests.Exists(quest => quest.entry == newQuest.entry))
                    Quests.Add(newQuest);
            });
        }

        public static void UpdateTasks()
        {
            if (Quests.Count <= 0)
                return;

            List<WAQTask> generatedTasks = new List<WAQTask>();
            foreach(ModelQuest quest in Quests)
            {
                // Completed
                if (quest.Status == QuestStatus.Completed)
                {
                    TasksPile.RemoveAll(t => t.Quest.entry == quest.entry);
                    continue;
                }

                // Turn in
                if (quest.Status == QuestStatus.ToTurnIn)
                {
                    TasksPile.RemoveAll(t => t.Quest.entry == quest.entry && t.TaskType != TaskType.TurnInQuest);
                    quest.QuestTurners.ForEach(qt => {
                        if (!TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.TurnInQuest, quest.entry, 5, qt.Guid)))
                            generatedTasks.Add(new WAQTask(TaskType.TurnInQuest, qt, quest, 5));
                    });
                    continue;
                }

                // Pick up
                if (quest.Status == QuestStatus.ToPickup)
                {
                    TasksPile.RemoveAll(t => t.Quest.entry == quest.entry && t.TaskType != TaskType.PickupQuest);
                    quest.QuestGivers.ForEach(qg => {
                        if (!TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.PickupQuest, quest.entry, 6, qg.Guid)))
                            generatedTasks.Add(new WAQTask(TaskType.PickupQuest, qg, quest, 6));
                    });
                    continue;
                }

                if (quest.Status == QuestStatus.InProgress)
                {
                    TasksPile.RemoveAll(t => t.Quest.entry == quest.entry 
                        && (t.TaskType == TaskType.PickupQuest || t.TaskType == TaskType.TurnInQuest));
                    // Kill & Loot
                    foreach (CreatureToLootObjective lootObjective in quest.CreaturesToLootObjectives)
                    {
                        if (!Quest.IsObjectiveComplete(lootObjective.objectiveIndex, quest.entry))
                        {
                            lootObjective.worldCreatures.ForEach(wc => {
                                if (!TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.KillAndLoot, quest.entry, lootObjective.objectiveIndex, wc.Guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.KillAndLoot, wc, quest, lootObjective.objectiveIndex));
                            });
                        }
                        else
                        {
                            TasksPile.RemoveAll(t => t.Quest.entry == quest.entry
                                && t.ObjectiveIndex == lootObjective.objectiveIndex
                                && t.TaskType == TaskType.PickupObject);
                        }
                    }

                    // Kill & Loot
                    foreach (CreaturesToKillObjective killObjective in quest.CreaturesToKillObjectives)
                    {
                        if (!Quest.IsObjectiveComplete(killObjective.objectiveIndex, quest.entry))
                        {
                            killObjective.worldCreatures.ForEach(wc => {
                                if (!TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.Kill, quest.entry, killObjective.objectiveIndex, wc.Guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.Kill, wc, quest, killObjective.objectiveIndex));
                            });
                        }
                        else
                        {
                            TasksPile.RemoveAll(t => t.Quest.entry == quest.entry
                                && t.ObjectiveIndex == killObjective.objectiveIndex
                                && t.TaskType == TaskType.PickupObject);
                        }
                    }

                    // Gather object
                    foreach (GatherObjectObjective gatherObjective in quest.GatherObjectsObjectives)
                    {
                        if (!Quest.IsObjectiveComplete(gatherObjective.objectiveIndex, quest.entry))
                        {
                            gatherObjective.worldObjects.ForEach(wo => {
                                if (!TasksPile.Exists(t => ToolBox.GetTaskId(t) == ToolBox.GetTaskId(TaskType.PickupObject, quest.entry, gatherObjective.objectiveIndex, wo.Guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.PickupObject, wo, quest, gatherObjective.objectiveIndex));
                            });
                        }
                        else
                        {
                            TasksPile.RemoveAll(t => t.Quest.entry == quest.entry 
                                && t.ObjectiveIndex == gatherObjective.objectiveIndex
                                && t.TaskType == TaskType.PickupObject);
                        }
                    }
                }
            }

            TasksPile.AddRange(generatedTasks);
            TasksPile = TasksPile.OrderBy(t => ObjectManager.Me.Position.DistanceTo2D(t.Location)).ToList();

            WAQTask closestTask = TasksPile.Find(t => !t.IsTimedOut);

            // Get unique POIs
            List<WAQTask> researchedTasks = new List<WAQTask>();
            List<int> researchedPOIEntries = new List<int>();
            TasksPile.ForEach(pileTask =>
            {
                if (!researchedTasks.Exists(poiTasks => poiTasks.POIEntry == pileTask.POIEntry) && !pileTask.IsTimedOut)
                {
                    researchedPOIEntries.Add(pileTask.POIEntry);
                    researchedTasks.Add(pileTask);
                }
            });

            // Look for surrounding POIs
            List<WoWObject> surroundingWoWObjects = ObjectManager.GetObjectWoW();
            surroundingWoWObjects.RemoveAll(o => 
                !researchedPOIEntries.Contains(o.Entry) 
                || o.GetDistance > 40
                || !IsObjectValidForTask(o, researchedTasks.Find(task => task.POIEntry == o.Entry))
            );

            if (surroundingWoWObjects.Count > 0)
            {
                TaskInProgressWoWObject = surroundingWoWObjects.OrderBy(o => o.GetDistance).First();
                //Logger.Log($"Closest POI is {TaskInProgressWoWObject.Name} ({TaskInProgressWoWObject.Position.DistanceTo(ObjectManager.Me.Position)})");
                closestTask = researchedTasks.Find(task => task.POIEntry == TaskInProgressWoWObject.Entry);
            }
            else
            {
                //Logger.Log($"Closest POI is NULL");
                TaskInProgressWoWObject = null;
            }

            TaskInProgress = closestTask;
            //Logger.Log($"Active task is {TaskInProgress?.TaskName} - {TaskInProgress?.IsTimedOut}");

            Main.questTrackerGUI.UpdateTasksList(TasksPile);
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
                // Quest completed
                if (Quest.GetQuestCompleted(quest.entry) 
                    || Quests.Any(q => q.Status == QuestStatus.Completed && q.PreviousQuestsIds.Contains(quest.entry)))
                {
                    quest.Status = QuestStatus.Completed;
                    continue;
                }

                // Quest to pickup
                if (quest.IsPickable()
                    && !Quest.HasQuest(quest.entry))
                {
                    quest.Status = QuestStatus.ToPickup;
                    continue;
                }

                // Log quests
                if (Quest.GetLogQuestId().Exists(q => q.ID == quest.entry))
                {
                    // Quest to turn in
                    if (Quest.GetLogQuestId().Find(q => q.ID == quest.entry).State == StateFlag.Complete)
                    {
                        quest.Status = QuestStatus.ToTurnIn;
                        continue;
                    }

                    // Quest failed
                    if (Quest.GetLogQuestId().Find(q => q.ID == quest.entry).State == StateFlag.Failed)
                    {
                        quest.Status = QuestStatus.Failed;
                        continue;
                    }

                    // Quest in progress
                    if (Quest.HasQuest(quest.entry))
                    {
                        quest.Status = QuestStatus.InProgress;
                        continue;
                    }
                }

                quest.Status = QuestStatus.None;
            }

            Main.questTrackerGUI.UpdateQuestsList(Quests);
        }
    }
}
