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
        private readonly QuestsTrackerGUI _questsTrackerGUI;
        private bool _isRunning = false;
        private KDTree<float, IWAQTask> _spaceTree = new KDTree<float, IWAQTask>(3, new float[][] { new float[] { 0, 0, 0 } }, new IWAQTask[] { null }, Distance);

        public List<IWAQTask> TaskPile { get; } = new List<IWAQTask>();

        public IWAQTask ActiveTask { get; private set; }

        public TaskManager(IWowObjectScanner scanner, IQuestManager questManager, IGrindManager grindManager, QuestsTrackerGUI questTrackerGUI)
        {
            _objectScanner = scanner;
            _questManager = questManager;
            _grindManager = grindManager;
            _questsTrackerGUI = questTrackerGUI;
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
            if (!TaskPile.Contains(task))
            {
                TaskPile.Add(task);
            }
            else
            {
                throw new Exception($"Tried to add {task.TaskName} to the TaskPile but it already existed");
            }
        }

        public void UpdateTaskPile()
        {
            _questsTrackerGUI.UpdateTasksList(TaskPile);

            if (WholesomeAQSettings.CurrentSetting.GoToMobEntry > 0)
            {
                GenerateSettingTravelTask();
                return;
            }

            TaskPile.Clear();
            Vector3 myPosition = ObjectManager.Me.Position;

            if (WholesomeAQSettings.CurrentSetting.GoToMobEntry <= 0 && !WholesomeAQSettings.CurrentSetting.GrindOnly)
            {
                List<IWAQTask> allQuestTasks = _questManager.GetAllQuestTasks()
                    .Where(task => !wManagerSetting.IsBlackListedZone(task.Location))
                    .OrderBy(task => myPosition.DistanceTo(task.Location))
                    .ToList();

                foreach (IWAQTask task in allQuestTasks)
                {
                    AddTaskToPile(task);
                }
            }

            // Add grind tasks if nothing else is valid
            if (TaskPile.Count <= 0 || TaskPile.All(task => task.IsTimedOut))
            {
                foreach (IWAQTask task in _grindManager.GetGrindTasks())
                {
                    AddTaskToPile(task);
                }
            }

            // If a wow object is found, we force the closest task
            if (_objectScanner.ActiveWoWObject != (null, null))
            {
                ActiveTask = _objectScanner.ActiveWoWObject.Item2;
                return;
            }

            // Get closest task
            IWAQTask closestTask = TaskPile.Find(task => !task.IsTimedOut && !wManagerSetting.IsBlackListedZone(task.Location));
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
                int closestTaskPriorityScore = CalculatePriority(closestTask);

                for (int i = 0; i < TaskPile.Count - 1; i++)
                {
                    if (i > 2) break;
                    if (!TaskPile[i].IsTimedOut)
                    {
                        WAQPath pathToNewTask = ToolBox.GetWAQPath(myPosition, TaskPile[i].Location);
                        if (!pathToNewTask.IsReachable)
                        {
                            TaskPile[i].PutTaskOnTimeout("Unreachable (2)", 600, true);
                            BlacklistHelper.AddZone(closestTask.Location, 5, "Unreachable (2)");
                            continue;
                        }

                        int newTaskPriority = CalculatePriority(TaskPile[i]);

                        if (newTaskPriority < closestTaskPriorityScore)
                        {
                            closestTaskPriorityScore = newTaskPriority;
                            closestTask = TaskPile[i];
                            pathToClosestTask = pathToNewTask;
                        }

                        if (closestTaskPriorityScore < TaskPile[i + 1].Location.DistanceTo(myPosition))
                            break;
                    }
                }
            }

            ActiveTask = closestTask;
        }

        private void GenerateSettingTravelTask()
        {
            if (TaskPile.Count <= 0)
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

        public int CalculatePriority(IWAQTask task)
        {
            const double magic = 1.32;

            float taskDistance = ObjectManager.Me.Position.DistanceTo(task.Location);
            var priority = (int)System.Math.Pow(taskDistance, magic);

            var locationWeight = 1.0;
            var neighbours = _spaceTree.RadialSearch(new float[] { task.Location.X, task.Location.Y, task.Location.Z }, 64.0f);
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
