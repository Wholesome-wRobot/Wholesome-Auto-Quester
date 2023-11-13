﻿using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Database.Conditions;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Database.Objectives;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.QuestManagement
{
    public class WAQQuest : IWAQQuest
    {
        private readonly IWowObjectScanner _objectScanner;
        private readonly IContinentManager _continentManager;
        private readonly Dictionary<int, List<IWAQTask>> _questTasks = new Dictionary<int, List<IWAQTask>>(); // objective index => task list
        private bool _objectivesRecorded;
        private bool _objectivesRecordFailed;
        private object _questLock = new object();

        public ModelQuestTemplate QuestTemplate { get; }
        public QuestStatus Status { get; private set; } = QuestStatus.Unchecked;

        public WAQQuest(
            ModelQuestTemplate questTemplate, 
            IWowObjectScanner objectScanner,
            IContinentManager continentManager)
        {
            _objectScanner = objectScanner;
            _continentManager = continentManager;
            QuestTemplate = questTemplate;

            //先赋值
            _objectivesRecorded = true;
            _objectivesRecordFailed = false;
        }

        public string GetConditionsText
        {
            get
            {
                string result = "";
                foreach (IDBConditionGroup condGroup in QuestTemplate.DBConditionGroups)
                {
                    result += $"{condGroup.GetGroupConditionsText} \n";
                }
                return result;
            }
        }

        public List<IWAQTask> GetAllTasks()
        {
            lock (_questLock)
            {
                List<IWAQTask> allTasks = new List<IWAQTask>();
                foreach (KeyValuePair<int, List<IWAQTask>> entry in _questTasks)
                {
                    allTasks.AddRange(entry.Value);
                }
                return allTasks;
            }
        }

        public List<IWAQTask> GetAllValidTasks()
        {
            return GetAllTasks().FindAll(task => task.IsValid).ToList();
        }

        public List<IWAQTask> GetAllInvalidTasks()
        {
            return GetAllTasks().FindAll(task => !task.IsValid).ToList();
        }

        private void AddTaskToDictionary(int objectiveIndex, IWAQTask task)
        {
            if (task.WorldMapArea == null)
            {
                return;
            }

            if (ObjectManager.Me.Level < 58
                && !WholesomeAQSettings.CurrentSetting.ContinentTravel
                && task.WorldMapArea.Continent != _continentManager.MyMapArea.Continent)
            {
                return;
            }

            lock (_questLock)
            {
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
        }

        private void ClearTasksDictionary()
        {
            lock (_questLock)
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
        }

        private void ClearDictionaryObjective(int objectiveId)
        {
            lock (_questLock)
            {
                Logger.LogError("是这里删除的么？");
                _questTasks.Remove(objectiveId);
            }
        }

        public void CheckForFinishedObjectives()
        {
            if (Status == QuestStatus.InProgress)
            {
                lock (_questLock)
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
        }

        // Triggers on LOG_UPDATE from the quest manager's UpdateStatuses
        public void ChangeStatusTo(QuestStatus newStatus)
        {
            if (Status == newStatus)
            {
                return;
            }
            Logger.LogDebug($"{QuestTemplate.LogTitle} changed status from {Status} to {newStatus}");

            Status = newStatus;
            ClearTasksDictionary();

            // TASK GENERATION

            // Skip failed indices
            if (Status == QuestStatus.InProgress && !_objectivesRecorded && !_objectivesRecordFailed)
            {
                //RecordObjectiveIndices();
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
                    ClearTasksDictionary();
                }
                return;
            }

            // Blacklisted
            if (Status == QuestStatus.Blacklisted)
            {
                //ClearTasksDictionary();
                return;
            }

            // quest is in progress but we don't have the starting item
            if (Status == QuestStatus.InProgress
                && QuestTemplate.StartItemTemplate?.Entry > 0)
            {
                if (!Bag.GetBagItem().Any(item => item.Entry == QuestTemplate.StartItemTemplate.Entry))
                {
                    return;
                }
            }

            // Turn in quest
            if (Status == QuestStatus.ToTurnIn)
            {
                ClearTasksDictionary();

                // Turn in quest to an NPC
                foreach (ModelCreatureTemplate creatureTemplate in QuestTemplate.CreatureQuestEnders)
                {
                    foreach (ModelCreature creature in creatureTemplate.Creatures)
                    {
                        AddTaskToDictionary(0, new WAQTaskTurninQuestToCreature(QuestTemplate, creatureTemplate, creature, _continentManager));
                    }
                }

                // Turn in quest to a game object
                foreach (ModelGameObjectTemplate gameObjectTemplate in QuestTemplate.GameObjectQuestEnders)
                {
                    foreach (ModelGameObject gameObject in gameObjectTemplate.GameObjects)
                    {
                        AddTaskToDictionary(0, new WAQTaskTurninQuestToGameObject(QuestTemplate, gameObjectTemplate, gameObject, _continentManager));
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
                        AddTaskToDictionary(0, new WAQTaskPickupQuestFromCreature(QuestTemplate, creatureTemplate, creature, _continentManager));
                    }
                }

                // Pick up quest from a game object
                foreach (ModelGameObjectTemplate gameObjectTemplate in QuestTemplate.GameObjectQuestGivers)
                {
                    foreach (ModelGameObject gameObject in gameObjectTemplate.GameObjects)
                    {
                        AddTaskToDictionary(0, new WAQTaskPickupQuestFromGameObject(QuestTemplate, gameObjectTemplate, gameObject, _continentManager));
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
                    if (obje.CreatureLootTemplate.CreatureTemplate.MaxLevel > ObjectManager.Me.Level + 3)
                    {
                        continue;
                    }

                    if (ItemsManager.GetItemCountById((uint)obje.ItemTemplate.Entry) <= 0)
                    {
                        needsPrerequisite = true;
                        foreach (ModelCreature creature in obje.CreatureLootTemplate.CreatureTemplate.Creatures)
                        {
                            AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskKillAndLoot(QuestTemplate, obje.CreatureLootTemplate.CreatureTemplate, creature, _continentManager));
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
                                AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskGatherGameObject(QuestTemplate, gameObjectTemplate, gameObject, _continentManager));
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
                            AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskExploreLocation(QuestTemplate, obje.Area.GetPosition, _continentManager, obje.Area.ContinentId));
                        }
                        else
                        {
                            ClearDictionaryObjective(obje.ObjectiveIndex);
                        }
                    }

                    // Kill & Loot
                    foreach (KillLootObjective obje in QuestTemplate.KillLootObjectives)
                    {
                        if (obje.CreatureLootTemplate.CreatureTemplate.MaxLevel > ObjectManager.Me.Level + 3)
                        {
                            continue;
                        }

                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, QuestTemplate.Id))
                        {
                            foreach (ModelCreature creature in obje.CreatureLootTemplate.CreatureTemplate.Creatures)
                            {
                                AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskKillAndLoot(QuestTemplate, obje.CreatureLootTemplate.CreatureTemplate, creature, _continentManager));
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
                        if (obje.CreatureTemplate.MaxLevel > ObjectManager.Me.Level + 3)
                        {
                            continue;
                        }

                        if (!ToolBox.IsObjectiveCompleted(obje.ObjectiveIndex, QuestTemplate.Id))
                        {
                            foreach (ModelCreature creature in obje.CreatureTemplate.Creatures)
                            {
                                if (QuestTemplate.Id == 11243
                                    && creature.GetSpawnPosition.DistanceTo(new Vector3(746.2075, -4927.192, 16.62478)) > 50) // If Valgarde falls, important northrend starter quest
                                    continue;
                                AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskKill(QuestTemplate, obje.CreatureTemplate, creature, _continentManager));
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
                                    AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskGatherGameObject(QuestTemplate, gameObjectTemplate, gameObject, _continentManager));
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
                                AddTaskToDictionary(obje.ObjectiveIndex, new WAQTaskInteractWithGameObject(QuestTemplate, obje.GameObjectTemplate, gameObject, _continentManager));
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
            WTQuestLog.ExpandQuestHeader(); // 先把所有任务不折叠 否则numEntries就变得很小
            while (nbAtempts < nbMaxAttempts)
            {
                bool recordFailed = false;
                nbAtempts++;
                Logger.Log($"Recording objective indices for {QuestTemplate.LogTitle} ({nbAtempts})");

                // local numEntries, numQuests = GetNumQuestLogEntries() 
                /* numEntries 如果任务折叠了 就算一个
                 * numQuests 不算标题 只算里面的真实任务个数
                 * local numObjectives = GetNumQuestLeaderBoards(i) 
                 * numObjectives 是一个任务里面有几个要求要完成
                 * local text, objetype, finished = GetQuestLogLeaderBoard(j, i)
                 * 对于每一个任务，j就可以遍历这些要求
                 * text就是实际的文本
                 * objetype 这里要么是杀敌 要么是收集 
                 * 可能是monster 也可能是item
                 * 但是本题这里是插入的文本
                 * objectives 其实是一个包含了任务具体完成项的一个字符串集合
                 */
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
                    Logger.Log("objectiveName====>" + ob.ObjectiveName );
                    Logger.Log("objectiveIndex====>" + ob.ObjectiveIndex.ToString());

                    // 这里o.StartsWith(ob.ObjectiveName) 肯定不行 
                    // 因为 objectives 保存的是中文
                    // 而数据库GetAllObjectives这里获取的是英文
                    // 中文：高阶教徒扎古斯的徽记: 0/1
                    // 英文：Head of High Cultist Zangus: 0/1
                    string objectiveToRecord = objectives.FirstOrDefault(o => !string.IsNullOrEmpty(ob.ObjectiveName) && o.StartsWith(ob.ObjectiveName));
                    if (objectiveToRecord != null)
                    {
                        ob.ObjectiveIndex = Array.IndexOf(objectives, objectiveToRecord) + 1; // 不断地更新索引
                    }
                    else
                    {
                        Logger.Log($"Couldn't find matching objective {ob.ObjectiveName} for {QuestTemplate.LogTitle} ({nbAtempts})");
                        recordFailed = true;
                        Thread.Sleep(1000);
                        break;
                    }
                }
                if (!recordFailed)
                {
                    break;
                }
            }

            if (nbAtempts >= nbMaxAttempts)
            {
                Logger.LogError($"Failed to record objectives for {QuestTemplate.LogTitle} after {nbMaxAttempts} attempts");
                Logger.LogError($"但是我强制设置了_objectivesRecorded = true这个标识符 ^_^");
                //_objectivesRecordFailed = true;
                // 看它这里搞半天就是为了设置一个_objectivesRecorded = true

                // 直接强制该了得了
                _objectivesRecordFailed = false;
                _objectivesRecorded = true;

                return;
            }

            Logger.Log($"Objectives for {QuestTemplate.LogTitle} succesfully recorded after {nbAtempts} attempts");
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
        public bool AreDbConditionsMet => QuestTemplate.DBConditionGroups.Count <= 0 || QuestTemplate.DBConditionGroups.Any(condGroup => condGroup.ConditionsMet);

        private readonly Dictionary<QuestStatus, string> _trackerColorsDictionary = new Dictionary<QuestStatus, string>
        {
            {  QuestStatus.Completed, "SkyBlue"},
            {  QuestStatus.Failed, "Red"},
            {  QuestStatus.InProgress, "Gold"},
            {  QuestStatus.None, "Gray"},
            {  QuestStatus.ToPickup, "MediumSeaGreen"},
            {  QuestStatus.ToTurnIn, "RoyalBlue"},
            {  QuestStatus.DBConditionsNotMet, "OliveDrab"},
            {  QuestStatus.Blacklisted, "Red"}
        };
    }
}
