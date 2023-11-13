using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStateMoveToHotspot : State, IWAQState
    {
        private ITaskManager _taskManager;
        public override string DisplayName { get; set; } = "WAQ Move to hotspot";

        public WAQStateMoveToHotspot(ITaskManager taskManager)
        {
            _taskManager = taskManager;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (_taskManager.ActiveTask != null)
                {
                    DisplayName = $"Moving to hotspot for {_taskManager.ActiveTask.TaskName}";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            IWAQTask task = _taskManager.ActiveTask;

            if (wManagerSetting.IsBlackListedZone(task.Location))
            {
                task.PutTaskOnTimeout("Zone is blacklisted");
                MovementManager.StopMove();
                return;
            }

            if (task.Location.DistanceTo(ObjectManager.Me.Position) <= task.SearchRadius)
            {
                task.PutTaskOnTimeout($"Couldn't find target");
            }

            ToolBox.CheckIfZReachable(task.Location);

            if (task.Location.DistanceTo(ObjectManager.Me.Position) > 19
                && (!MovementManager.InMovement 
                || MovementManager.CurrentPath.Count > 0 && MovementManager.CurrentPath.Last() != task.Location))
            {
                Logger.Log($"Moving to hotspot for {task.TaskName}");
                if (task.Location.DistanceTo(ObjectManager.Me.Position) > 50)
                {
                    MovementManager.StopMove();
                }
                List<Vector3> pathToTask = PathFinder.FindPath(task.Location);
                FlyHelper.FlyTo(task.Location);
                MovementManager.Go(pathToTask);
            }
        }
    }
}