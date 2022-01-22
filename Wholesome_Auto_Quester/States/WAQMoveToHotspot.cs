using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using wManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQMoveToHotspot : State
    {
        public override string DisplayName { get; set; } = "Move to hotspot [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress != null && WAQTasks.WoWObjectInProgress == null)
                {
                    DisplayName =
                        $"Moving to hotspot for {WAQTasks.TaskInProgress.QuestTitle} ({WAQTasks.TaskInProgress.TaskType}) [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;

            if (wManagerSetting.IsBlackListedZone(task.Location))
            {
                Logger.Log("Aborted because BL");
                MoveHelper.StopAllMove();
                Main.RequestImmediateTaskReset = true;
                return;
            }

            if (task.GetDistance <= 20)
            {
                task.PutTaskOnTimeout($"Couldn't find {task.TargetName} ({task.TaskType})");
                Main.RequestImmediateTaskReset = true;
            }

            if (task.Location.DistanceTo(ObjectManager.Me.Position) > 19)
                MoveHelper.StartGoToThread(task.Location, $"Traveling to hotspot for {task.QuestTitle} ({task.TaskType})");

            /*
            if (MoveHelper.CurrentMovementTarget != task.Location)
            {
                Logger.Log($"CHANGING to hotspot for {MoveHelper.CurrentMovementTarget} ({task.Location})");
                MoveHelper.StartGoToThread(task.Location);
                return;
            }

            if (!MoveHelper.IsMovementThreadRunning && task.Location.DistanceTo(ObjectManager.Me.Position) > 19
                || MoveHelper.IsMovementThreadRunning && MoveHelper.CurrentMovementTarget != task.Location)
            {
                Logger.Log($"Traveling to hotspot for {task.QuestTitle} ({task.TaskType})");
                MoveHelper.StartGoToThread(task.Location);
            }*/
        }
    }
}