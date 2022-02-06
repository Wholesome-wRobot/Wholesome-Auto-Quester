using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement
{
    internal class WowObjectScanner : IWowObjectScanner
    {
        private readonly Dictionary<int, List<IWAQTask>> _scannerRegistry = new Dictionary<int, List<IWAQTask>>(); // object entry => associated tasks
        private readonly QuestsTrackerGUI _guiTracker;
        private readonly object _scannerLock = new object();
        private bool _isRunning = false;

        public (WoWObject wowObject, IWAQTask task) ActiveWoWObject { get; private set; } = (null, null);

        public WowObjectScanner(QuestsTrackerGUI tracker)
        {
            _guiTracker = tracker;
            Initialize();
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += LuaEventHandler;
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
                        Pulse();
                        await Task.Delay(1000);

                    }
                }
            });
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= LuaEventHandler;
            //ObjectManagerEvents.OnObjectManagerPulsed += OnObjectManagerPulse;
        }

        public void Dispose()
        {
            _isRunning = false;
            //ObjectManagerEvents.OnObjectManagerPulsed -= OnObjectManagerPulse;
            lock (_scannerLock)
            {
                _scannerRegistry.Clear();
            }
        }

        private void LuaEventHandler(string eventid, List<string> args)
        {
            lock (_scannerLock)
            {
                switch (eventid)
                {
                    case "PLAYER_LEVEL_UP":
                        ActiveWoWObject = (null, null);
                        break;
                }
            }
        }

        private void Pulse()
        {
            lock (_scannerLock)
            {
                WoWLocalPlayer me = ObjectManager.Me;
                if (me.IsOnTaxi
                    || me.IsDead
                    || !me.IsValid
                    || me.HaveBuff("Drink")
                    || me.HaveBuff("Food"))
                {
                    return;
                }

                _guiTracker.UpdateScanReg(_scannerRegistry);

                List<WoWObject> allObjects = ObjectManager.GetObjectWoW()
                    .Where(o => o.GetDistance < 60)
                    .OrderBy(o => o.GetDistance)
                    .ToList();

                List<WoWObject>  listSurroundingPOIs = allObjects
                    .FindAll(wowObject => !wManagerSetting.IsBlackListed(wowObject.Guid)
                        && !wManagerSetting.IsBlackListedZone(wowObject.Position)
                        && wowObject.IsValid
                        && wowObject.Guid > 0
                        && _scannerRegistry.ContainsKey(wowObject.Entry)
                        && _scannerRegistry[wowObject.Entry].Count > 0
                        && _scannerRegistry[wowObject.Entry].Any(task => !task.IsTimedOut)
                        && _scannerRegistry[wowObject.Entry].Any(task => task.IsObjectValidForTask(wowObject)))
                    .ToList();

                if (listSurroundingPOIs.Count > 0)
                {
                    WoWObject closestObject = listSurroundingPOIs[0];
                    WAQPath pathToClosestObject = ToolBox.GetWAQPath(me.Position, closestObject.Position);

                    if (!pathToClosestObject.IsReachable)
                    {
                        BlacklistHelper.AddNPC(closestObject.Guid, "Unreachable (3)");
                        return;
                    }

                    if (pathToClosestObject.Distance > closestObject.GetDistance * 2)
                    {
                        int nbObject = listSurroundingPOIs.Count;
                        for (int i = 1; i < nbObject - 1; i++)
                        {
                            WAQPath pathToNewObject = ToolBox.GetWAQPath(me.Position, listSurroundingPOIs[i].Position);

                            if (!pathToNewObject.IsReachable)
                            {
                                Logger.Log($"Blacklisting {listSurroundingPOIs[i].Name} {listSurroundingPOIs[i].Guid} because it's unreachable");
                                BlacklistHelper.AddNPC(listSurroundingPOIs[i].Guid, "Unreachable (4)");
                                break;
                            }

                            if (pathToNewObject.Distance < pathToClosestObject.Distance)
                            {
                                pathToClosestObject = pathToNewObject;
                                closestObject = listSurroundingPOIs[i];
                            }

                            float flyDistanceToNextObject = listSurroundingPOIs[i + 1].GetDistance;
                            if (pathToClosestObject.Distance < flyDistanceToNextObject)
                            {
                                break;
                            }
                        }
                    }

                    if (pathToClosestObject.IsReachable)
                    {
                        IWAQTask associatedTask = GetTaskMatchingWithObject(closestObject);
                        if (associatedTask != null && associatedTask.IsObjectValidForTask(closestObject))
                        {
                            ActiveWoWObject = (closestObject, associatedTask);
                            return;
                        }
                    }
                }
                ActiveWoWObject = (null, null);
            }
        }

        private IWAQTask GetTaskMatchingWithObject(WoWObject closestObject)
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
            lock (_scannerLock)
            {
                if (_scannerRegistry.TryGetValue(entry, out List<IWAQTask> taskList))
                {
                    if (!taskList.Contains(task))
                    {
                        taskList.Add(task);
                        Logger.LogDebug($"Added ({entry}) {task.TaskName} to the scanner regsitry ({task.Location})");
                    }
                }
                else
                {
                    _scannerRegistry[entry] = new List<IWAQTask>() { task };
                    Logger.LogDebug($"Added ({entry}) {task.TaskName} to the scanner regsitry (didn't exist) ({task.Location})");
                }
            }
        }

        public void RemoveFromScannerRegistry(int entry, IWAQTask task)
        {
            lock (_scannerLock)
            {
                if (_scannerRegistry.TryGetValue(entry, out List<IWAQTask> taskList))
                {
                    if (!taskList.Remove(task))
                    {
                        throw new System.Exception($"[Scanner] Tried to remove {task.TaskName} from the entry {entry} but it wasn't in the list ({task.Location})");
                    }
                    Logger.LogDebug($"Removed ({entry}) {task.TaskName} from the scanner regsitry ({task.Location})");
                    if (taskList.Count <= 0)
                    {
                        _scannerRegistry.Remove(entry);
                        Logger.LogDebug($"Removed ENTRY {entry} from the scanner registry ({task.Location})");
                    }
                }
                else
                {
                    throw new System.Exception($"[Scanner] Tried to remove {task.TaskName} but the entry {entry} didn't exist ({task.Location})");
                }
            }
        }
    }
}
