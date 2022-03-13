using robotManager.Helpful;
using Supercluster.KDTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                        await Task.Delay(500);
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
                || MoveHelper.IsMovementThreadRunning && MoveHelper.GetCurrentPathRemainingDistance() > 200 && _tick % 5 != 0)
            {
                return;
            }

            Stopwatch watch = new Stopwatch();
            watch.Start();

            _taskPile.Clear();

            Vector3 myPosition = ObjectManager.Me.Position;
            List<IWAQTask> tasksToAdd = new List<IWAQTask>();

            if (WholesomeAQSettings.CurrentSetting.GoToMobEntry > 0)
            {
                DB _db = new DB();
                ModelCreatureTemplate template = _db.QueryCreatureTemplateByEntry(WholesomeAQSettings.CurrentSetting.GoToMobEntry);
                _db.Dispose();

                if (template?.Creatures.Count > 0)
                {
                    tasksToAdd.Add(new WAQTaskSettingTravel(template));
                }
                else
                {
                    Logger.LogError($"Couldn't find NPC {WholesomeAQSettings.CurrentSetting.GoToMobEntry}");
                    return;
                }
            }

            if (WholesomeAQSettings.CurrentSetting.GoToMobEntry <= 0 && !WholesomeAQSettings.CurrentSetting.GrindOnly)
            {
                tasksToAdd.AddRange(_questManager.GetAllValidQuestTasks());
            }

            // Add grind tasks if nothing else is valid
            if (tasksToAdd.Count <= 0)
            {
                if (_grindTasks.Count <= 0)
                {
                    List<IWAQTask> allGrindTasks = _grindManager.GetGrindTasks();
                    _grindTasks.AddRange(allGrindTasks);
                    foreach (IWAQTask grindTask in allGrindTasks)
                    {
                        grindTask.RegisterEntryToScanner(_objectScanner);
                    }
                }
                _tracker.UpdateInvalids(_grindTasks.FindAll(task => !task.IsValid));
                tasksToAdd.AddRange(_grindTasks.FindAll(task => task.IsValid));
            }
            else
            {
                _tracker.UpdateInvalids(_questManager.GetAllInvalidQuestTasks());
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

            if (_taskPile.Count <= 0)
            {
                Logger.LogError($"No task available");
                return;
            }

            // If a wow object is found, we force the closest task
            if (_objectScanner.ActiveWoWObject != (null, null))
            {
                ActiveTask = _objectScanner.ActiveWoWObject.task;
                Logger.LogWatchTask($"TASKM FORCE CLOSEST", watch.ElapsedMilliseconds);
                return;
            }

            // Get closest task
            IWAQTask closestTask = _taskPile[0];

            // Check if travel is needed
            if (_travelManager.IsTravelRequired(closestTask))
            {
                ActiveTask = closestTask;
                Logger.LogWatchTask($"TASKM TRAVEL REQUIRED", watch.ElapsedMilliseconds);
                return;
            }
            /*
            // We already are on that task
            if (closestTask == ActiveTask)
            {
                Logger.LogWatchTask($"TASKM ALREADY ON CLOSEST", watch.ElapsedMilliseconds);
                return;
            }
            */
            WAQPath pathToClosestTask = ToolBox.GetWAQPath(ObjectManager.Me.Position, closestTask.Location);

            // Avoid snap back and forth
            if (ActiveTask != null
                && MoveHelper.IsMovementThreadRunning)
            {
                float remainingDistance = MoveHelper.GetCurrentPathRemainingDistance();
                if (remainingDistance > 200 && pathToClosestTask.Distance > remainingDistance)
                {
                    Logger.LogWatchTask($"TASKM AVOID SNAP", watch.ElapsedMilliseconds);
                    return;
                }
            }

            if (pathToClosestTask.Distance > myPosition.DistanceTo(closestTask.Location) * 2)
            {
                int closestTaskPriorityScore = CalculatePriority(myPosition, spaceTree, closestTask);
                int nbReachAttempts = 0;

                for (int i = 0; i < _taskPile.Count - 1; i++)
                {

                    if (nbReachAttempts > 2)
                    {
                        break;
                    }
                    nbReachAttempts++;
                    WAQPath pathToNewTask = ToolBox.GetWAQPath(myPosition, _taskPile[i].Location);

                    int newTaskPriority = CalculatePriority(myPosition, spaceTree, _taskPile[i]);

                    if (newTaskPriority < closestTaskPriorityScore)
                    {
                        closestTaskPriorityScore = newTaskPriority;
                        closestTask = _taskPile[i];
                    }

                    if (closestTaskPriorityScore < _taskPile[i + 1].Location.DistanceTo(myPosition))
                        break;
                }
            }

            // only set new task on long distance if it's far apart from previous
            if (closestTask != null && ActiveTask != null
                && MoveHelper.IsMovementThreadRunning
                && MoveHelper.GetCurrentPathRemainingDistance() > 200
                && ActiveTask.Location.DistanceTo(closestTask.Location) < 500)
            {
                Logger.LogWatchTask($"TASKM TOO CLOSE TO SWITCH", watch.ElapsedMilliseconds);
                return;
            }

            Logger.LogWatchTask($"TASKM FOUND ACTIVE", watch.ElapsedMilliseconds);
            ActiveTask = closestTask;
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
