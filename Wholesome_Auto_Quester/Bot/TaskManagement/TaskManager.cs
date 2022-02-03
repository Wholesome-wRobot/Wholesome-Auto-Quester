using robotManager.Helpful;
using Supercluster.KDTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wholesome_Auto_Quester.Bot.GrindManagement;
using Wholesome_Auto_Quester.Bot.QuestManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
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
        private readonly QuestsTrackerGUI _tracker;
        private bool _isRunning = false;
        private List<IWAQTask> _taskPile = new List<IWAQTask>();

        public IWAQTask ActiveTask { get; private set; }

        public TaskManager(IWowObjectScanner scanner, IQuestManager questManager, IGrindManager grindManager, QuestsTrackerGUI questTrackerGUI)
        {
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
                    UpdateTaskPile();
                    await Task.Delay(1000);
                }
            });
        }

        public void Dispose()
        {
            _isRunning = false;
        }

        private void AddTaskToPile(IWAQTask task)
        {
            if (!_taskPile.Contains(task))
            {
                _taskPile.Add(task);
                task.RegisterEntryToScanner(_objectScanner);
            }
            else
            {
                throw new Exception($"Tried to add {task.TaskName} to the TaskPile but it already existed");
            }
        }
        /*
        private void RemoveFromTaskPile(IWAQTask task)
        {
            if (_taskPile.Contains(task))
            {
                _taskPile.Remove(task);
                task.UnregisterEntryToScanner(_objectScanner);
            }
            else
            {
                throw new Exception($"Tried to remove {task.TaskName} from the TaskPile but it didn't exist");
            }
        }
        */
        public void UpdateTaskPile()
        {
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
                List<IWAQTask> allQuestTasks = _questManager.GetAllQuestTasks()
                    .Where(task => !wManagerSetting.IsBlackListedZone(task.Location))
                    .ToList();

                tasksToAdd.AddRange(allQuestTasks);
            }

            // Add grind tasks if nothing else is valid
            if (WholesomeAQSettings.CurrentSetting.GrindOnly || _taskPile.Count > 0 & _taskPile.All(task => task.IsTimedOut))
            {
                tasksToAdd.AddRange(_grindManager.GetGrindTasks());
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
            WAQPath pathToClosestTask = ToolBox.GetWAQPath(ObjectManager.Me.Position, closestTask.Location);

            if (!pathToClosestTask.IsReachable)
            {
                closestTask.PutTaskOnTimeout("Unreachable (1)", 600, true);
                BlacklistHelper.AddZone(closestTask.Location, 5, "Unreachable (1)");
                //Main.RequestImmediateTaskReset = true;
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
                            _taskPile[i].PutTaskOnTimeout("Unreachable (2)", 600, true);
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

        public int CalculatePriority(Vector3 myPosition, KDTree<float, IWAQTask> spaceTree, IWAQTask task)
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

            if (task.Continent != Usefuls.ContinentId)
            {
                priority <<= 10;
            }

            return priority;
        }

        private static double Distance(float[] x, float[] y)
        {
            double dist = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }
            return dist;
        }

        private static KDTree<float, IWAQTask> BuildTree(List<IWAQTask> tasks)
        {
            var tasksVectors = tasks.Select(x => new float[] { x.Location.X, x.Location.Y, x.Location.Z }).ToArray();
            var tasksArray = tasks.ToArray();
            return new KDTree<float, IWAQTask>(3, tasksVectors, tasksArray, Distance);
        }
    }
}
