using robotManager.FiniteStateMachine;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQGoTo : State
    {
        public override string DisplayName { get; set; } = "Go To [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.Explore)
                {
                    DisplayName = $"Explore {WAQTasks.TaskInProgress.Location} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;

            if (task.GetDistance < 2f) {
                MoveHelper.StopAllMove();
                Logger.Log($"Reached exploration hotspot for {task.QuestTitle}");
                WAQTasks.UpdateTasks();
                return;
            }
            
            if (!MoveHelper.IsMovementThreadRunning ||
                MoveHelper.CurrentMovementTarget?.DistanceTo(task.Location) > 8) {
                Logger.Log($"Moving to Hotspot for {task.QuestTitle} (Explore).");
                MoveHelper.StartGoToThread(task.Location, precise: true);
            }
        }
    }
}
