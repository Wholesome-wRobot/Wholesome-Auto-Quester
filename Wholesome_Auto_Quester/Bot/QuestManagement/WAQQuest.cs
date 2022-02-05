using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;

namespace Wholesome_Auto_Quester.Bot.QuestManagement
{
    public class WAQQuest : IWAQQuest
    {
        private readonly IWowObjectScanner _objectScanner;
        private readonly Dictionary<int, List<IWAQTask>> _questTasks = new Dictionary<int, List<IWAQTask>>(); // objective index => task list
        private bool _objectivesRecorded;
        private bool _objectivesRecordFailed;

        public ModelQuestTemplate QuestTemplate { get; }
        public QuestStatus Status { get; private set; } = QuestStatus.Unchecked;

        public WAQQuest(ModelQuestTemplate questTemplate, IWowObjectScanner objectScanner)
        {
            _objectScanner = objectScanner;
            QuestTemplate = questTemplate;
        }

        public List<IWAQTask> GetAllTasks()
        {
            List<IWAQTask> allTasks = new List<IWAQTask>();
            foreach (KeyValuePair<int, List<IWAQTask>> entry in _questTasks)
            {
                allTasks.AddRange(entry.Value);
            }
            return allTasks;
        }

        private void AddTaskToDictionary(int objectiveIndex, IWAQTask task)
        {
            if (!WholesomeAQSettings.CurrentSetting.ContinentTravel && task.Continent != Usefuls.ContinentId)
            {
                return;
            }

            // create the empty entry if it doesn't exist
            if (!_questTasks.ContainsKey(objectiveIndex))
            {
                _questTasks[objectiveIndex] = new List<IWAQTask>();
            }

            if (!_questTasks[objectiveIndex].Contains(task))
            {
                _questTasks[objectiveIndex].Add(task);
                task.RegisterEntryToScanner(_objectScanner);
            }
            else
            {
                throw new Exception($"Tried to add {task.TaskName} to objective {objectiveIndex} but it already existed");
            }
        }

        private void ClearTasksDictionary()
        {
            foreach (KeyValuePair<int, List<IWAQTask>> entry in _questTasks)
            {
                foreach (IWAQTask task in entry.Value)
                {
                    task.UnregisterEntryToScanner(_objectScanner);
                }
            }
            _questTasks.Clear();
        }

        private void ClearDictionaryObjective(int objectiveId)
        {
            _questTasks.Remove(objectiveId);
        }

        public void CheckForFinishedObjectives()
        {
            if (Status == QuestStatus.InProgress)
            {
                List<int> keysToRemove = new List<int>();
                foreach (KeyValuePair<int, List<IWAQTask>> objective in _questTasks.Reverse())
                {
                    if (ToolBox.IsObjectiveCompleted(objective.Key, QuestTemplate.Id))
                    {
                        keysToRemove.Add(objective.Key);
                        foreach (IWAQTask task in objective.Value)
                        {
                            task.UnregisterEntryToScanner(_objectScanner);
                        }
                    }
                }

                foreach (int key in keysToRemove)
                {
                    _questTasks.Remove(key);
                }
            }
        }

        // Triggers on LOG_UPDATE from the quest manager's UpdateStatuses
        public void ChangeStatusTo(QuestStatus newStatus)
        {
            if (Status == newStatus)
            {
                return;
            }
            Logger.LogDebug($"{QuestTemplate.LogTitle} changed status from {Status} to {newStatus}");
            ClearTasksDictionary();

            Status = newStatus;

            // TASK GENERATION

            // Skip failed indices
            if (Status == QuestStatus.InProgress && !_objectivesRecorded && !_objectivesRecordFailed)
            {
                RecordObjectiveIndices();
                if (_objectivesRecordFailed)
                {
                    return;
                }
            }

            // Completed
            if (Status == QuestStatus.Completed)
            {
                if (ToolBox.SaveQuestAsCompleted(QuestTemplate.Id))
                {
                    WholesomeAQSettings.CurrentSetting.Save();
                }
                ClearTasksDictionary();
                return;
            }

            // Blacklisted
            if (Status == QuestStatus.Blacklisted)
            {
                ClearTasksDictionary();
                return;
            }

            // quest is in progress but we don't have the starting item
            if (Status == QuestStatus.InProgress
                && QuestTemplate.StartItem > 0
                && !ItemsManager.HasItemById((uint)QuestTemplate.StartItem))
            {
                return;
            }

            // Turn in quest
            if (Status == QuestStatus.ToTurnIn)
            {
                ClearTasksDictionary();

                // Turn in quest to an NPC
                foreach (ModelCreatureTemplate creatureTemplate in QuestTemplate.CreatureQuestTurners)
                {
                    foreach (ModelCreature creature in creatureTemplate.Creatures)
                    {
                        AddTaskToDictionary(0, new WAQTaskTurninQuestToCreature(QuestTemplate, creatureTemplate, creature));
                    }
                }

                // Turn in quest to a game object
                foreach (ModelGameObjectTemplate gameObjectTemplate in QuestTemplate.GameObjectQuestTurners)
                {
                    foreach (ModelGameObject gameObject in gameObjectTemplate.GameObjects)
                    {
                        AddTaskToDictionary(0, new WAQTaskTurninQuestToGameObject(QuestTemplate, gameObjectTemplate, gameObject));
                    }
                }

                return;
            }

            // Pick up quest
            if (Status == QuestStatus.ToPickup)
            {
                ClearTasksDictionary();

                // Pick up quest from an NPC
                foreach (ModelCreatureTemplate creatureTemplate in QuestTemplate.CreatureQuestGivers)
                {
                    foreach (ModelCreature creature in creatureTemplate.Creatures)
                    {
                        AddTaskToDictionary(0, new WAQTaskPickupQuestFromCreature(QuestTemplate, creatureTemplate, creature));
                    }
                }

                // Pick up quest from a game object
                foreach (ModelGameObjectTemplate gameObjectTemplate in QuestTemplate.GameObjectQuestGivers)
                {
                    foreach (ModelGameObject gameObject in gameObjectTemplate.GameObjects)
                    {
                        AddTaskToDictionary(0, new WAQTaskPickupQuestFromGameObject(QuestTemplate, gameObjectTemplate, gameObject));
                    }
                }

                return;
            }

            // Prerequisites
            if (Status == QuestStatus.InProgress)
            {
                bool needsPrerequisite = false;

                // Prerequisite Kill & Loot
                foreach (KillLootObjective obje in QuestTemplate.PrerequisiteLootObjectives)
                {
                    if (ItemsManager.GetItemCountById((uint)obje.ItemTemplate.Entry) <= 0)
                    {
                        needsPrerequisite = true;
                        foreach (ModelCreature creature in obje.CreatureLootTemplate.CreatureTemplate.Creatures)
                        {
                            AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskKillAndLoot(QuestTemplate, obje.CreatureLootTemplate.CreatureTemplate, creature));
                        }
                    }
                    else
                    {
                        ClearDictionaryObjective(obje.ObjectiveIndex);
                    }
                }

                // Prerequisite Gather Game Object
                foreach (GatherObjective obje in QuestTemplate.PrerequisiteGatherObjectives)
                {
                    foreach (ModelGameObjectTemplate gameObjectTemplate in obje.GameObjectLootTemplate.GameObjectTemplates)
                    {
                        if (ItemsManager.GetItemCountById((uint)gameObjectTemplate.entry) <= 0)
                        {
                            needsPrerequisite = true;
                            foreach (ModelGameObject gameObject in gameObjectTemplate.GameObjects)
                            {
                                AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskGatherGameObject(QuestTemplate, gameObjectTemplate, gameObject));
                            }
                        }
                        else
                        {
                            ClearDictionaryObjective(obje.ObjectiveIndex);
                        }
                    }
                }

                if (!needsPrerequisite)
                {
                    // Explore
                    foreach (ExplorationObjective obje in QuestTemplate.ExplorationObjectives)
                    {
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, QuestTemplate.Id))
                        {
                            AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskExploreLocation(QuestTemplate, obje.Area.GetPosition, obje.Area.ContinentId));
                        }
                        else
                        {
                            ClearDictionaryObjective(obje.ObjectiveIndex);
                        }
                    }

                    // Kill & Loot
                    foreach (KillLootObjective obje in QuestTemplate.KillLootObjectives)
                    {
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, QuestTemplate.Id))
                        {
                            foreach (ModelCreature creature in obje.CreatureLootTemplate.CreatureTemplate.Creatures)
                            {
                                AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskKillAndLoot(QuestTemplate, obje.CreatureLootTemplate.CreatureTemplate, creature));
                            }
                        }
                        else
                        {
                            ClearDictionaryObjective(obje.ObjectiveIndex);
                        }
                    }

                    // Kill
                    foreach (KillObjective obje in QuestTemplate.KillObjectives)
                    {
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, QuestTemplate.Id))
                        {
                            foreach (ModelCreature creature in obje.CreatureTemplate.Creatures)
                            {
                                AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskKill(QuestTemplate, obje.CreatureTemplate, creature));
                            }
                        }
                        else
                        {
                            ClearDictionaryObjective(obje.ObjectiveIndex);
                        }
                    }

                    // Gather object
                    foreach (GatherObjective obje in QuestTemplate.GatherObjectives)
                    {
                        foreach (ModelGameObjectTemplate gameObjectTemplate in obje.GameObjectLootTemplate.GameObjectTemplates)
                        {
                            if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, QuestTemplate.Id))
                            {
                                foreach (ModelGameObject gameObject in gameObjectTemplate.GameObjects)
                                {
                                    AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskGatherGameObject(QuestTemplate, gameObjectTemplate, gameObject));
                                }
                            }
                            else
                            {
                                ClearDictionaryObjective(obje.ObjectiveIndex);
                            }
                        }
                    }

                    // Interact with object
                    foreach (InteractObjective obje in QuestTemplate.InteractObjectives)
                    {
                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, QuestTemplate.Id))
                        {
                            foreach (ModelGameObject gameObject in obje.GameObjectTemplate.GameObjects)
                            {
                                AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskInteractWithGameObject(QuestTemplate, obje.GameObjectTemplate, gameObject));
                            }
                        }
                        else
                        {
                            ClearDictionaryObjective(obje.ObjectiveIndex);
                        }
                    }
                }
            }
        }

        private void RecordObjectiveIndices()
        {
            int nbAtempts = 0;
            int nbMaxAttempts = 5;
            while (nbAtempts <= nbMaxAttempts)
            {
                nbAtempts++;
                Logger.Log($"Recording objective indices for {QuestTemplate.LogTitle} ({nbAtempts})");
                string[] objectives = Lua.LuaDoString<string[]>(@$"local numEntries, numQuests = GetNumQuestLogEntries()
                            local objectivesTable = {{}}
                            for i=1, numEntries do
                                local questLogTitleText, level, questTag, suggestedGroup, isHeader, isCollapsed, isComplete, isDaily, questID = GetQuestLogTitle(i)
                                if questID == {QuestTemplate.Id} then
                                    local numObjectives = GetNumQuestLeaderBoards(i)
                                    for j=1, numObjectives do
                                        local text, objetype, finished = GetQuestLogLeaderBoard(j, i)
                                        table.insert(objectivesTable, text)
                                    end
                                end
                            end
                            return unpack(objectivesTable)");

                foreach (Objective ob in GetAllObjectives())
                {
                    string objectiveToRecord = objectives.FirstOrDefault(o => ob.ObjectiveName != "" && o.StartsWith(ob.ObjectiveName));
                    if (objectiveToRecord != null)
                    {
                        ob.ObjectiveIndex = Array.IndexOf(objectives, objectiveToRecord) + 1;
                    }
                    else
                    {
                        Logger.LogError($"Couldn't find matching objective {ob.ObjectiveName} for {QuestTemplate.LogTitle}");
                        Thread.Sleep(1000);
                        continue;
                    }
                }
                break;
            }

            if (nbAtempts >= nbMaxAttempts)
            {
                Logger.LogError($"Failed to record objectives for {QuestTemplate.LogTitle} after {nbMaxAttempts} attempts");
                _objectivesRecordFailed = true;
                return;
            }

            Logger.Log($"Objectives for {QuestTemplate.LogTitle} succesfully recorded");
            _objectivesRecorded = true;
        }

        public float GetClosestQuestGiverDistance(Vector3 myPosition)
        {
            List<float> closestsQg = new List<float>();
            foreach (ModelCreatureTemplate cqg in QuestTemplate.CreatureQuestGivers)
            {
                if (cqg.Creatures.Count > 0)
                {
                    closestsQg.Add(cqg.Creatures.Min(c => c.GetSpawnPosition.DistanceTo(myPosition)));
                }
            }

            foreach (ModelGameObjectTemplate goqg in QuestTemplate.GameObjectQuestGivers)
            {
                if (goqg.GameObjects.Count > 0)
                {
                    closestsQg.Add(goqg.GameObjects.Min(c => c.GetSpawnPosition.DistanceTo(myPosition)));
                }
            }

            return closestsQg.Count > 0 ? closestsQg.Min() : float.MaxValue;
        }

        public List<Objective> GetAllObjectives()
        {
            List<Objective> result = new List<Objective>();
            result.AddRange(QuestTemplate.ExplorationObjectives);
            result.AddRange(QuestTemplate.GatherObjectives);
            result.AddRange(QuestTemplate.InteractObjectives);
            result.AddRange(QuestTemplate.KillLootObjectives);
            result.AddRange(QuestTemplate.KillObjectives);
            return result;
        }

        public string TrackerColor => /*WAQTasks.TaskInProgress?.QuestId == QuestTemplate.Id ? "White" : */_trackerColorsDictionary[Status];
        public bool IsQuestBlackListed => WholesomeAQSettings.CurrentSetting.BlackListedQuests.Exists(blq => blq.Id == QuestTemplate.Id);

        private readonly Dictionary<QuestStatus, string> _trackerColorsDictionary = new Dictionary<QuestStatus, string>
        {
            {  QuestStatus.Completed, "SkyBlue"},
            {  QuestStatus.Failed, "Red"},
            {  QuestStatus.InProgress, "Gold"},
            {  QuestStatus.None, "Gray"},
            {  QuestStatus.ToPickup, "MediumSeaGreen"},
            {  QuestStatus.ToTurnIn, "RoyalBlue"},
            {  QuestStatus.Blacklisted, "Red"}
        };
    }
}
