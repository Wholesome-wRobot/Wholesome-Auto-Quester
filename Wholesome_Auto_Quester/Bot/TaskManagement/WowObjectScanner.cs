using robotManager.Helpful;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Dictionary<ulong, int> _scanned = new Dictionary<ulong, int>(); // guid, times scanned

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
                        await Task.Delay(500);
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

        private void HandleSpecialInteractions(WoWObject wowObject)
        {
            // Slag Pit quary gate door
            if (wowObject.Entry == 161536
                && wowObject.GetDistance < 5)
            {
                Interact.InteractGameObject(wowObject.GetBaseAddress);
            }
        }

        private void MarkAsUnreachable(WoWObject obj)
        {
            IWAQTask associatedTask = GetTaskMatchingWithObject(obj);
            BlacklistHelper.AddZone(obj.Position, 5, "[Scanner] Unreachable");
            associatedTask.PutTaskOnTimeout("[Scanner] Unreachable", 60 * 60 * 3, true);
            associatedTask.RecordAsUnreachable();
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

                Stopwatch watch = new Stopwatch();
                watch.Start();

                _guiTracker.UpdateScanReg(GuiScanEntries);

                List<WoWObject> allObjects = ObjectManager.GetObjectWoW();

                foreach (WoWObject wowObject in allObjects)
                {
                    HandleSpecialInteractions(wowObject);
                }

                Vector3 myPos = me.Position;
                List<WoWObject> listSurroundingPOIs = allObjects
                    .FindAll(wowObject =>
                        wowObject.IsValid
                        && wowObject.Guid > 0
                        && _scannerRegistry.ContainsKey(wowObject.Entry)
                        && _scannerRegistry[wowObject.Entry].Any(task => task.IsValid)
                        && _scannerRegistry[wowObject.Entry].Any(task => task.IsObjectValidForTask(wowObject))
                        && !wManagerSetting.IsBlackListed(wowObject.Guid)
                        && !wManagerSetting.IsBlackListedZone(wowObject.Position)
                        && wowObject.Position.DistanceTo(myPos) < 60)
                    .OrderBy(wowObject => wowObject.Position.DistanceTo(myPos))
                    .ToList();

                listSurroundingPOIs.RemoveAll(wowObject => _scanned.ContainsKey(wowObject.Guid) && _scanned[wowObject.Guid] > 3);

                if (listSurroundingPOIs.Count > 0)
                {
                    WoWObject closestObject = listSurroundingPOIs[0];
                    WAQPath pathToClosestObject = ToolBox.GetWAQPath(me.Position, closestObject.Position);

                    if (closestObject.Position.DistanceTo(myPos) > 5 && !pathToClosestObject.IsReachable)
                    {
                        MarkAsUnreachable(closestObject);
                        Logger.LogWatchScanner($"SCANNER MARKED UNREACHABLE", watch.ElapsedMilliseconds);
                        return;
                    }

                    // Avoid snap back and forth
                    if (ActiveWoWObject.wowObject != null
                        && MoveHelper.IsMovementThreadRunning
                        && pathToClosestObject.Distance > MoveHelper.GetCurrentPathRemainingDistance() - 15)
                    {
                        Logger.LogWatchScanner($"SCANNER AVOID SNAP", watch.ElapsedMilliseconds);
                        return;
                    }

                    if (pathToClosestObject.Distance > closestObject.Position.DistanceTo(myPos) * 1.5)
                    {
                        int nbObject = listSurroundingPOIs.Count;
                        for (int i = 1; i < nbObject - 1; i++)
                        {
                            WAQPath pathToNewObject = ToolBox.GetWAQPath(me.Position, listSurroundingPOIs[i].Position);

                            if (listSurroundingPOIs[i].Position.DistanceTo(myPos) > 5 && !pathToNewObject.IsReachable)
                            {
                                MarkAsUnreachable(listSurroundingPOIs[i]);
                                break;
                            }

                            if (pathToNewObject.Distance < pathToClosestObject.Distance)
                            {
                                pathToClosestObject = pathToNewObject;
                                closestObject = listSurroundingPOIs[i];
                            }

                            float flyDistanceToNextObject = listSurroundingPOIs[i + 1].Position.DistanceTo(myPos);
                            if (pathToClosestObject.Distance < flyDistanceToNextObject)
                            {
                                break;
                            }
                        }
                    }

                    if (closestObject.Guid <= 0)
                    {
                        ActiveWoWObject = (null, null);
                        return;
                    }

                    IWAQTask associatedTask = GetTaskMatchingWithObject(closestObject);
                    if (associatedTask != null)
                    {
                        ActiveWoWObject = (closestObject, associatedTask);
                        Logger.LogWatchScanner($"SCANNER FOUND OBJECT", watch.ElapsedMilliseconds);
                        return;
                    }
                }

                if (_scanned.Count > 3) _scanned.Clear();

                // scanner Unsnapped
                if (ActiveWoWObject.wowObject != null)
                {
                    if (_scanned.TryGetValue(ActiveWoWObject.wowObject.Guid, out int amount))
                    {
                        _scanned[ActiveWoWObject.wowObject.Guid]++;
                        if (_scanned[ActiveWoWObject.wowObject.Guid] > 3)
                            Logger.LogError($"{ActiveWoWObject.wowObject.Name} has been temporarily banned from the scanner");
                    }
                    else
                        _scanned.Add(ActiveWoWObject.wowObject.Guid, 1);
                }

                Logger.LogWatchScanner($"SCANNER FOUND NOTHING", watch.ElapsedMilliseconds);
                ActiveWoWObject = (null, null);
            }
        }

        private IWAQTask GetTaskMatchingWithObject(WoWObject closestObject)
        {
            lock (_scannerLock)
            {
                if (closestObject == null)
                {
                    throw new System.Exception($"[Scanner] Tried to get a task matching with the active object entry but it was null");
                }

                if (_scannerRegistry.TryGetValue(closestObject.Entry, out List<IWAQTask> taskList))
                {
                    return taskList
                        .Where(task => task.IsObjectValidForTask(closestObject) && task.IsValid)
                        .OrderBy(task => task.Location.DistanceTo(closestObject.Position))
                        .FirstOrDefault();
                }
                else
                {
                    throw new System.Exception($"[Scanner] Tried to get a task matching with the object entry {closestObject.Entry} but the entry didn't exist");
                }
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

        private List<GUIScanEntry> GuiScanEntries
        {
            get
            {
                lock (_scannerLock)
                {
                    List<GUIScanEntry> scanEntries = new List<GUIScanEntry>();
                    foreach (KeyValuePair<int, List<IWAQTask>> entry in _scannerRegistry)
                    {
                        foreach (IWAQTask task in entry.Value)
                        {
                            if (!scanEntries.Exists(entry => entry.TaskName == task.TaskName))
                            {
                                scanEntries.Add(new GUIScanEntry(entry.Key, task));
                            }
                            else
                            {
                                scanEntries.Find(entry => entry.TaskName == task.TaskName).AddOne(task);
                            }
                        }
                    }
                    return scanEntries;
                }
            }
        }
    }
}
