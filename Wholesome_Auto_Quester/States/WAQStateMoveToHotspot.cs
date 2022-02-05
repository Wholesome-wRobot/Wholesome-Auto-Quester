using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStateMoveToHotspot : State
    {
        private ITaskManager _taskManager;
        public override string DisplayName { get; set; } = "WAQ Move to hotspot";

        public WAQStateMoveToHotspot(ITaskManager taskManager, int priority)
        {
            _taskManager = taskManager;
            Priority = priority;
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
                Logger.Log("Aborted because BL");
                MoveHelper.StopAllMove(true);
                return;
            }

            if (task.Location.DistanceTo(ObjectManager.Me.Position) <= task.SearchRadius && WholesomeAQSettings.CurrentSetting.GoToMobEntry <= 0)
            {
                task.PutTaskOnTimeout($"Couldn't find target");
            }

            if (task.Location.DistanceTo(ObjectManager.Me.Position) > 19)
            {
                MoveHelper.StartGoToThread(task.Location, $"Moving to hotspot for {task.TaskName}");
            }
        }
    }
}