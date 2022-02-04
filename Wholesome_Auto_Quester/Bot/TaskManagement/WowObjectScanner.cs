using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement
{
    internal class WowObjectScanner : IWowObjectScanner
    {
        private List<WoWObject> _listSurroundingPOIs = new List<WoWObject>();
        private Dictionary<int, List<IWAQTask>> _scannerRegistry = new Dictionary<int, List<IWAQTask>>(); // object entry => associated tasks
        private bool _isRunning = false;
        private readonly QuestsTrackerGUI _guiTracker;

        public (WoWObject wowObject, IWAQTask task) ActiveWoWObject { get; private set; } = (null, null);

        public WowObjectScanner(QuestsTrackerGUI tracker)
        {
            _guiTracker = tracker;
            Initialize();
        }

        public void Initialize()
        {
            _isRunning = true;
            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    Scan();
                    await Task.Delay(1000);
                }
            });
            //ObjectManagerEvents.OnObjectManagerPulsed += OnObjectManagerPulse;
        }

        public void Dispose()
        {
            _isRunning = false;
            _scannerRegistry.Clear();
            //ObjectManagerEvents.OnObjectManagerPulsed -= OnObjectManagerPulse;
        }

        private void Scan()
        {
            /*foreach(KeyValuePair<int, List<IWAQTask>> entry in _dicEntriesWaqTasks)
            {
                Logger.Log($"{entry.Key} => {entry.Value.Count}");
            }*/
            Logger.Log("SCAN");
            _guiTracker.UpdateScanReg(_scannerRegistry);

            List<WoWObject> surroundingObjects = ObjectManager.GetObjectWoW()
                .OrderBy(o => o.GetDistance)
                .ToList();

            _listSurroundingPOIs = surroundingObjects
                .FindAll(wowObject => !wManagerSetting.IsBlackListed(wowObject.Guid)
                    && !wManagerSetting.IsBlackListedZone(wowObject.Position)
                    && _scannerRegistry.ContainsKey(wowObject.Entry)
                    && _scannerRegistry[wowObject.Entry].Count > 0
                    && _scannerRegistry[wowObject.Entry].Any(task => !task.IsTimedOut)
                    && _scannerRegistry[wowObject.Entry].Any(task => task.IsObjectValidForTask(wowObject)))
                .ToList();

            if (_listSurroundingPOIs.Count > 0)
            {
                WoWObject closestObject = _listSurroundingPOIs[0];
                WAQPath pathToClosestObject = ToolBox.GetWAQPath(ObjectManager.Me.Position, closestObject.Position);

                if (!pathToClosestObject.IsReachable)
                {
                    BlacklistHelper.AddNPC(closestObject.Guid, "Unreachable (3)");
                    return;
                }

                if (pathToClosestObject.Distance > closestObject.GetDistance * 2)
                {
                    int nbObject = _listSurroundingPOIs.Count;
                    for (int i = 1; i < nbObject - 1; i++)
                    {
                        WAQPath pathToNewObject = ToolBox.GetWAQPath(ObjectManager.Me.Position, _listSurroundingPOIs[i].Position);

                        if (!pathToNewObject.IsReachable)
                        {
                            Logger.Log($"Blacklisting {_listSurroundingPOIs[i].Name} {_listSurroundingPOIs[i].Guid} because it's unreachable");
                            BlacklistHelper.AddNPC(_listSurroundingPOIs[i].Guid, "Unreachable (4)");
                            break;
                        }

                        if (pathToNewObject.Distance < pathToClosestObject.Distance)
                        {
                            pathToClosestObject = pathToNewObject;
                            closestObject = _listSurroundingPOIs[i];
                        }

                        float flyDistanceToNextObject = _listSurroundingPOIs[i + 1].GetDistance;
                        if (pathToClosestObject.Distance < flyDistanceToNextObject)
                        {
                            break;
                        }
                    }
                }

                if (pathToClosestObject.IsReachable)
                {
                    ActiveWoWObject = (closestObject, GetTaskMatchingWithObject(closestObject));
                    return;
                }
                else
                {
                    ActiveWoWObject = (null, null);
                }
            }

            ActiveWoWObject = (null, null);
        }

        public IWAQTask GetTaskMatchingWithObject(WoWObject closestObject)
        {
            if (closestObject == null)
            {
                throw new System.Exception($"[Scanner] Tried to get a task matching with the active object entry but it was null");
            }

            if (_scannerRegistry.TryGetValue(closestObject.Entry, out List<IWAQTask> taskList))
            {
                return taskList
                    .Where(task => !task.IsTimedOut && task.IsObjectValidForTask(closestObject))
                    .OrderBy(task => task.Location.DistanceTo(closestObject.Position))
                    .FirstOrDefault();
            }
            else
            {
                throw new System.Exception($"[Scanner] Tried to get a task matching with the object entry {closestObject.Entry} but the entry didn't exist");
            }

        }

        public void AddToScannerRegistry(int entry, IWAQTask task)
        {
            if (_scannerRegistry.TryGetValue(entry, out List<IWAQTask> taskList))
            {
                if (!taskList.Contains(task))
                {
                    taskList.Add(task);
                    Logger.Log($"Added ({entry}) {task.TaskName} to the scanner regsitry ({task.Location})");
                }
            }
            else
            {
                _scannerRegistry[entry] = new List<IWAQTask>() { task };
                Logger.Log($"Added ({entry}) {task.TaskName} to the scanner regsitry (didn't exist) ({task.Location})");
            }
        }

        public void RemoveFromScannerRegistry(int entry, IWAQTask task)
        {
            if (_scannerRegistry.TryGetValue(entry, out List<IWAQTask> taskList))
            {
                if (!taskList.Remove(task))
                {
                    throw new System.Exception($"[Scanner] Tried to remove {task.TaskName} from the entry {entry} but it wasn't in the list ({task.Location})");
                }
                Logger.Log($"Removed ({entry}) {task.TaskName} from the scanner regsitry ({task.Location})");
                if (taskList.Count <= 0)
                {
                    _scannerRegistry.Remove(entry);
                    Logger.Log($"Removed ENTRY {entry} from the scanner registry ({task.Location})");
                }
            }
            else
            {
                throw new System.Exception($"[Scanner] Tried to remove {task.TaskName} but the entry {entry} didn't exist ({task.Location})");
            }
        }
    }
}
