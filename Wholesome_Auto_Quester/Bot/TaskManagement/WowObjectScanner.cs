using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.Bot.TaskManagement
{
    internal class WowObjectScanner : IWowObjectScanner
    {
        private List<WoWObject> _listSurroundingPOIs = new List<WoWObject>();
        private Dictionary<int, List<IWAQTask>> _dicEntriesWaqTasks = new Dictionary<int, List<IWAQTask>>(); // object entry => associated tasks
        private bool _isRunning = false;

        public (WoWObject wowObject, IWAQTask task) ActiveWoWObject { get; private set; } = (null, null);

        public WowObjectScanner()
        {
            Initialize();
        }

        public void Initialize()
        {
            _isRunning = true;
            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    OnObjectManagerPulse();
                    await Task.Delay(1000);
                }
            });
            //ObjectManagerEvents.OnObjectManagerPulsed += OnObjectManagerPulse;
        }

        public void Dispose()
        {
            _isRunning = false;
            //ObjectManagerEvents.OnObjectManagerPulsed -= OnObjectManagerPulse;
        }

        private void OnObjectManagerPulse()
        {
            List<WoWObject> surroundingObjects = ObjectManager.GetObjectWoW()
                .OrderBy(o => o.GetDistance)
                .ToList();

            _listSurroundingPOIs = surroundingObjects
                .FindAll(wowObject => !wManagerSetting.IsBlackListed(wowObject.Guid)
                    && !wManagerSetting.IsBlackListedZone(wowObject.Position)
                    && _dicEntriesWaqTasks.ContainsKey(wowObject.Entry)
                    && _dicEntriesWaqTasks[wowObject.Entry].Count > 0
                    && _dicEntriesWaqTasks[wowObject.Entry][0].IsObjectValidForTask(wowObject))
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
                throw new System.Exception($"Tried to get a task matching with the active object entry but it was null");
            }

            if (_dicEntriesWaqTasks.TryGetValue(closestObject.Entry, out List<IWAQTask> taskList))
            {
                return taskList
                    .Where(task => !task.IsTimedOut)
                    .OrderBy(task => task.Location.DistanceTo(closestObject.Position))
                    .FirstOrDefault();
            }
            else
            {
                throw new System.Exception($"Tried to get a task matching with the object entry {closestObject.Entry} but the entry didn't exist");
            }

        }

        public void AddToDictionary(int entry, IWAQTask task)
        {
            if (_dicEntriesWaqTasks.TryGetValue(entry, out List<IWAQTask> taskList))
            {
                taskList.Add(task);
            }
            else
            {
                _dicEntriesWaqTasks[entry] = new List<IWAQTask>() { task };
            }
        }

        public void RemoveFromDictionary(int entry, IWAQTask task)
        {
            if (_dicEntriesWaqTasks.TryGetValue(entry, out List<IWAQTask> taskList))
            {
                if (!taskList.Remove(task))
                {
                    throw new System.Exception($"Tried to remove {task.TaskName} from the entry {entry} but it wasn't in the list");
                }
            }
            else
            {
                throw new System.Exception($"Tried to remove {task.TaskName} but the entry {entry} didn't exist");
            }
        }
    }
}
