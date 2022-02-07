using robotManager.Helpful;
using Supercluster.KDTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wholesome_Auto_Quester.Bot.GrindManagement;
using Wholesome_Auto_Quester.Bot.QuestManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Bot.TravelManagement;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement
{
    internal class TaskManager : ITaskManager
    {
        private readonly IQuestManager _questManager;
        private readonly IGrindManager _grindManager;
        private readonly IWowObjectScanner _objectScanner;
        private readonly ITravelManager _travelManager;
        private readonly QuestsTrackerGUI _tracker;
        private readonly List<IWAQTask> _taskPile = new List<IWAQTask>();
        private readonly List<IWAQTask> _grindTasks = new List<IWAQTask>();
        private bool _isRunning = false;
        private int _tick;

        public IWAQTask ActiveTask { get; private set; }

        public TaskManager(IWowObjectScanner scanner, IQuestManager questManager, IGrindManager grindManager,
            QuestsTrackerGUI questTrackerGUI, ITravelManager travelManager)
        {
            _travelManager = travelManager;
            _objectScanner = scanner;
            _questManager = questManager;
            _grindManager = grindManager;
            _tracker = questTrackerGUI;
            Initialize();
        }

        public void Initialize()
        {
            _isRunning = true;
            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                    {
                        UpdateTaskPile();
                        BlacklistHelper.CleanupBlacklist();
                        await Task.Delay(1000);
                    }
                }
            });
        }

        public void Dispose()
        {
            _taskPile.Clear();
            _isRunning = false;
        }

        private void AddTaskToPile(IWAQTask task)
        {
            if (!_taskPile.Contains(task))
            {
                _taskPile.Add(task);
            }
            else
            {
                throw new Exception($"Tried to add {task.TaskName} to the TaskPile but it already existed");
            }
        }

        public void UpdateTaskPile()
        {
            _tick++;
            WoWLocalPlayer me = ObjectManager.Me;
            if (me.IsOnTaxi
                || me.IsDead
                || !me.IsValid
                || Fight.InFight
                || _travelManager.TravelInProgress
                || me.HaveBuff("Drink")
                || me.HaveBuff("Food")
                || MoveHelper.IsMovementThreadRunning && MoveHelper.GetCurrentPathRemainingDistance() > 100 && _tick % 5 != 0)
            {
                return;
            }

            if (WholesomeAQSettings.CurrentSetting.GoToMobEntry > 0)
            {
                GenerateSettingTravelTask();
                return;
            }

            _taskPile.Clear();
            Vector3 myPosition = ObjectManager.Me.Position;
            List<IWAQTask> tasksToAdd = new List<IWAQTask>();

            if (WholesomeAQSettings.CurrentSetting.GoToMobEntry <= 0 && !WholesomeAQSettings.CurrentSetting.GrindOnly)
            {
                tasksToAdd.AddRange(_questManager.GetAllQuestTasks());
            }

            // Add grind tasks if nothing else is valid
            if (tasksToAdd.Count <= 0 || tasksToAdd.All(task => task.IsTimedOut))
            {
                if (_grindTasks.Count <= 0)
                {
                    _grindTasks.AddRange(_grindManager.GetGrindTasks());
                    foreach (IWAQTask grindTask in _grindManager.GetGrindTasks())
                    {
                        grindTask.RegisterEntryToScanner(_objectScanner);
                    }
                }
                tasksToAdd.AddRange(_grindTasks);
            }
            else
            {
                if (_grindTasks.Count > 0)
                {
                    foreach (IWAQTask grindTask in _grindTasks)
                    {
                        grindTask.UnregisterEntryToScanner(_objectScanner);
                    }
                    _grindTasks.Clear();
                }
            }

            var spaceTree = BuildTree(tasksToAdd);

            List<GUITask> guiTasks = new List<GUITask>();
            foreach (IWAQTask task in tasksToAdd)
            {
                guiTasks.Add(new GUITask(CalculatePriority(myPosition, spaceTree, task), task));
            }

            guiTasks = guiTasks.OrderBy(task => task.Priority).ToList();
            foreach (GUITask guiTask in guiTasks)
            {
                AddTaskToPile(guiTask.Task);
            }

            _tracker.UpdateTasksList(guiTasks);

            // If a wow object is found, we force the closest task
            if (_objectScanner.ActiveWoWObject != (null, null))
            {
                ActiveTask = _objectScanner.ActiveWoWObject.task;
                return;
            }

            // Get closest task
            IWAQTask closestTask = _taskPile.Find(task => !task.IsTimedOut && !wManagerSetting.IsBlackListedZone(task.Location));

            // Check if travel is needed
            if (_travelManager.IsTravelRequired(closestTask))
            {
                ActiveTask = closestTask;
                return;
            }

            WAQPath pathToClosestTask = ToolBox.GetWAQPath(ObjectManager.Me.Position, closestTask.Location);

            if (!pathToClosestTask.IsReachable)
            {
                closestTask.PutTaskOnTimeout("Unreachable (1)", 60*60*3, true);
                BlacklistHelper.AddZone(closestTask.Location, 5, "Unreachable (1)");
                return;
            }

            if (pathToClosestTask.Distance > myPosition.DistanceTo(closestTask.Location) * 2)
            {
                int closestTaskPriorityScore = CalculatePriority(myPosition, spaceTree, closestTask);

                for (int i = 0; i < _taskPile.Count - 1; i++)
                {
                    if (i > 2) break;
                    if (!_taskPile[i].IsTimedOut)
                    {
                        WAQPath pathToNewTask = ToolBox.GetWAQPath(myPosition, _taskPile[i].Location);
                        if (!pathToNewTask.IsReachable)
                        {
                            _taskPile[i].PutTaskOnTimeout("Unreachable (2)", 60*60*3, true);
                            BlacklistHelper.AddZone(closestTask.Location, 5, "Unreachable (2)");
                            continue;
                        }

                        int newTaskPriority = CalculatePriority(myPosition, spaceTree, _taskPile[i]);

                        if (newTaskPriority < closestTaskPriorityScore)
                        {
                            closestTaskPriorityScore = newTaskPriority;
                            closestTask = _taskPile[i];
                            pathToClosestTask = pathToNewTask;
                        }

                        if (closestTaskPriorityScore < _taskPile[i + 1].Location.DistanceTo(myPosition))
                            break;
                    }
                }
            }

            ActiveTask = closestTask;
        }

        private void GenerateSettingTravelTask()
        {
            if (_taskPile.Count <= 0)
            {
                DB _db = new DB();
                ModelCreatureTemplate template = _db.QueryCreatureTemplateByEntry(WholesomeAQSettings.CurrentSetting.GoToMobEntry);
                _db.Dispose();

                if (template?.Creatures.Count > 0)
                {
                    AddTaskToPile(new WAQTaskSettingTravel(template));
                }
                else
                {
                    Logger.LogError($"Couldn't find NPC {WholesomeAQSettings.CurrentSetting.GoToMobEntry}");
                }
            }
        }

        private int CalculatePriority(Vector3 myPosition, KDTree<float, IWAQTask> spaceTree, IWAQTask task)
        {
            const double magic = 1.32;

            float taskDistance = myPosition.DistanceTo(task.Location);
            var priority = (int)System.Math.Pow(taskDistance, magic);

            var locationWeight = 1.0;
            var neighbours = spaceTree.RadialSearch(new float[] { task.Location.X, task.Location.Y, task.Location.Z }, 64.0f);
            foreach (var (_, neighbour) in neighbours)
            {
                locationWeight += task.SpatialWeight;
            }
            priority = (int)(priority / System.Math.Pow(locationWeight, magic));

            priority >>= task.PriorityShift;

            if (task.WorldMapArea.Continent != ContinentHelper.MyMapArea.Continent)
            {
                priority <<= 10;
            }
            return priority;
        }

        private double Distance(float[] x, float[] y)
        {
            double dist = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }
            return dist;
        }

        private KDTree<float, IWAQTask> BuildTree(List<IWAQTask> tasks)
        {
            var tasksVectors = tasks.Select(x => new float[] { x.Location.X, x.Location.Y, x.Location.Z }).ToArray();
            var tasksArray = tasks.ToArray();
            return new KDTree<float, IWAQTask>(3, tasksVectors, tasksArray, Distance);
        }
    }
}
