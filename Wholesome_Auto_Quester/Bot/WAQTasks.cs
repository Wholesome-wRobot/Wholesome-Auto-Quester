using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Database.Models;
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
        public static List<ModelQuest> Quests { get; set; } = new List<ModelQuest>();
        public static WAQTask TaskInProgress { get; set; }
        public static WoWObject TaskInProgressWoWObject { get; set; }

        public static void AddQuests(List<ModelQuest> quests) {
            quests.ForEach(newQuest => {
                if (!Quests.Exists(quest => quest.Id == newQuest.Id))
                    Quests.Add(newQuest);
            });
        }

        public static void UpdateTasks() {
            //Logger.Log($"Finished quests : {Quest.FinishedQuestSet.Count}");

            if (Quests.Count <= 0)
                return;

            //Logger.Log("Update tasks");
            var generatedTasks = new List<WAQTask>();
            int myContinent = Usefuls.ContinentId;
            ToolBox.UpdateObjectiveCompletionDict(Quests.Where(quest => quest.Status == QuestStatus.InProgress)
                .Select(quest => quest.Id).ToArray());
            foreach (ModelQuest quest in Quests) {
                // Completed
                if (quest.Status == QuestStatus.Completed || quest.Status == QuestStatus.Blacklisted) {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id);
                    continue;
                }

                // Turn in
                if (quest.Status == QuestStatus.ToTurnIn) {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id && t.TaskType != TaskType.TurnInQuest);
                    quest.QuestTurners.ForEach(qt => {
                        if (qt.Map == myContinent
                            && !TasksPile.Exists(t =>
                                t.IsSameTask(TaskType.TurnInQuest, quest.Id, 5, () => qt.Guid)))
                            generatedTasks.Add(new WAQTask(TaskType.TurnInQuest, qt, quest, 5));
                    });
                    continue;
                }

                // Pick up
                if (quest.Status == QuestStatus.ToPickup) {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id && t.TaskType != TaskType.PickupQuest);
                    quest.QuestGivers.ForEach(qg => {
                        if (qg.Map == myContinent
                            && !TasksPile.Exists(t =>
                                t.IsSameTask(TaskType.PickupQuest, quest.Id, 6, () => qg.Guid)))
                            generatedTasks.Add(new WAQTask(TaskType.PickupQuest, qg, quest, 6));
                    });
                    continue;
                }

                if (quest.Status == QuestStatus.InProgress) {
                    TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                             && (t.TaskType == TaskType.PickupQuest ||
                                                 t.TaskType == TaskType.TurnInQuest));

                    // Explore
                    foreach (ExplorationObjective areaObjective in quest.ExplorationObjectives)
                        if (!ToolBox.GetObjectiveCompletion(areaObjective.objectiveIndex, quest.Id)) {
                            if (areaObjective.area.ContinentId == myContinent
                                && !TasksPile.Exists(t =>
                                    t.IsSameTask(TaskType.Explore, quest.Id,
                                        areaObjective.objectiveIndex)))
                                generatedTasks.Add(new WAQTask(TaskType.Explore, areaObjective.area, quest,
                                    areaObjective.objectiveIndex));
                        } else {
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                     && t.ObjectiveIndex == areaObjective.objectiveIndex
                                                     && t.TaskType == TaskType.Explore);
                        }

                    // Kill & Loot
                    foreach (CreatureToLootObjective lootObjective in quest.CreaturesToLootObjectives)
                        if (!ToolBox.GetObjectiveCompletion(lootObjective.objectiveIndex, quest.Id))
                            lootObjective.worldCreatures.ForEach(wc => {
                                if (wc.Map == myContinent
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.KillAndLoot, quest.Id,
                                            lootObjective.objectiveIndex, () => wc.Guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.KillAndLoot, wc, quest,
                                        lootObjective.objectiveIndex));
                            });
                        else
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                     && t.ObjectiveIndex == lootObjective.objectiveIndex
                                                     && t.TaskType == TaskType.KillAndLoot);

                    // Kill
                    foreach (CreaturesToKillObjective killObjective in quest.CreaturesToKillObjectives)
                        if (!ToolBox.GetObjectiveCompletion(killObjective.objectiveIndex, quest.Id))
                            killObjective.worldCreatures.ForEach(wc => {
                                if (wc.Map == myContinent
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.Kill, quest.Id,
                                            killObjective.objectiveIndex, () => wc.Guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.Kill, wc, quest,
                                        killObjective.objectiveIndex));
                            });
                        else
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                     && t.ObjectiveIndex == killObjective.objectiveIndex
                                                     && t.TaskType == TaskType.Kill);

                    // Gather object
                    foreach (GatherObjectObjective gatherObjective in quest.GatherObjectsObjectives)
                        if (!ToolBox.GetObjectiveCompletion(gatherObjective.objectiveIndex, quest.Id))
                            gatherObjective.worldObjects.ForEach(wo => {
                                if (wo.Map == myContinent
                                    && !TasksPile.Exists(t =>
                                        t.IsSameTask(TaskType.GatherObject, quest.Id,
                                            gatherObjective.objectiveIndex, () => wo.Guid)))
                                    generatedTasks.Add(new WAQTask(TaskType.GatherObject, wo, quest,
                                        gatherObjective.objectiveIndex));
                            });
                        else
                            TasksPile.RemoveAll(t => t.Quest.Id == quest.Id
                                                     && t.ObjectiveIndex == gatherObjective.objectiveIndex
                                                     && t.TaskType == TaskType.GatherObject);
                }
            }

            TasksPile.AddRange(generatedTasks);
            WAQTask.UpdatePriorityData();
            TasksPile = TasksPile.Where(task => !wManagerSetting.IsBlackListedZone(task.Location))
                .OrderBy(t => t.Priority).ToList();

            // Filter far away new quests if we still have quests to turn in
            // if (TasksPile.Any(task => task.Quest.ShouldQuestBeFinished())) {
            //     TasksPile.RemoveAll(task => task.TaskType == TaskType.PickupQuest && !Quests
            //         .Where(quest => quest.ShouldQuestBeFinished())
            //         .Any(questToBeFinished => questToBeFinished.QuestGivers
            //             .Any(questToFinishGiver => task.Quest.QuestGivers.Any(taskQuestGiver =>
            //                 taskQuestGiver.Position().DistanceTo(questToFinishGiver.Position()) < 250))));
            // }

            WAQTask closestTask = TasksPile.Find(t => !t.IsTimedOut);

            // Get unique POIs
            var researchedTasks = new List<WAQTask>();
            var wantedUnitEntries = new List<int>();
            var wantedObjectEntries = new List<int>();
            TasksPile.ForEach(pileTask => {
                if (!researchedTasks.Exists(poiTasks => poiTasks.POIEntry == pileTask.POIEntry) &&
                    !pileTask.IsTimedOut) {
                    if (pileTask.Npc != null)
                        wantedUnitEntries.Add(pileTask.POIEntry);
                    if (pileTask.GatherObject != null)
                        wantedObjectEntries.Add(pileTask.POIEntry);

                    researchedTasks.Add(pileTask);
                }
            });

            // Look for surrounding POIs
            List<WoWObject> surroundingWoWObjects = ObjectManager.GetObjectWoW();

            var myLevel = (int) ObjectManager.Me.Level;

            List<WoWObject> filteredSurroundingObjects = surroundingWoWObjects.FindAll(o => {
                int entry = o.Entry;
                WoWObjectType type = o.Type;
                return (type == WoWObjectType.Unit && wantedUnitEntries.Contains(entry) // &&
                        // (!(closestTask.TaskType == TaskType.Kill ||
                        //    closestTask.TaskType == TaskType.KillAndLoot) || ((WoWUnit) o).Level - myLevel <= 2)
                        || type == WoWObjectType.GameObject && wantedObjectEntries.Contains(entry))
                       && o.GetRealDistance() < 40
                       && IsObjectValidForTask(o, researchedTasks.Find(task => task.POIEntry == entry));
            });

            if (filteredSurroundingObjects.Count > 0) {
                TaskInProgressWoWObject = filteredSurroundingObjects.TakeHighest(
                    o => (int) -o.GetRealDistance());
                // filteredSurroundingObjects.OrderBy(o => o.GetRealDistance()).First();
                //Logger.Log($"Closest POI is {TaskInProgressWoWObject.Name} ({TaskInProgressWoWObject.GetDistance})");
                closestTask = researchedTasks.Find(task => task.POIEntry == TaskInProgressWoWObject.Entry);
            } else {
                //Logger.Log($"Closest POI is NULL");
                TaskInProgressWoWObject = null;
            }

            TaskInProgress = closestTask;
            // Logger.Log($"Active task is {TaskInProgress?.TaskName} - {TaskInProgress?.GetDistance} - {TaskInProgress?.Location.ToStringNewVector()}");

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
            ModelQuest[] completedQuests =
                Quests.Where(q => q.Status == QuestStatus.Completed && q.PreviousQuestsIds.Count > 0).ToArray();
            // Update quests statuses
            foreach (ModelQuest quest in Quests) {
                // Quest blacklisted
                if (WholesomeAQSettings.CurrentSetting.BlacklistesQuests.Contains(quest.Id)) {
                    quest.Status = QuestStatus.Blacklisted;
                    continue;
                }

                // Quest completed
                if (quest.IsCompleted
                    || completedQuests.Any(q => q.PreviousQuestsIds.Contains(quest.Id))) {
                    quest.Status = QuestStatus.Completed;
                    continue;
                }

                // Quest to pickup
                if (quest.IsPickable()
                    && !currentQuests.ContainsKey(quest.Id)) {
                    quest.Status = QuestStatus.ToPickup;
                    continue;
                }

                // Log quests
                if (currentQuests.TryGetValue(quest.Id, out Quest.PlayerQuest foundQuest)) {
                    // Quest to turn in
                    if (foundQuest.State == StateFlag.Complete) {
                        quest.Status = QuestStatus.ToTurnIn;
                        continue;
                    }

                    // Quest failed
                    if (foundQuest.State == StateFlag.Failed) {
                        quest.Status = QuestStatus.Failed;
                        continue;
                    }

                    // Quest in progress
                    // if (Quest.HasQuest(quest.Id)) {
                    quest.Status = QuestStatus.InProgress;
                    continue;
                    // }
                }

                quest.Status = QuestStatus.None;
            }

            if (_tick++ % 5 == 0) Main.QuestTrackerGui.UpdateQuestsList();
        }
    }
}