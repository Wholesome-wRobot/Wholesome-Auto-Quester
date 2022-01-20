using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
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

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.Explore
                    && !wManager.wManagerSetting.IsBlackListedZone(WAQTasks.TaskInProgress.Location))
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
            //WAQPath pathToTask = WAQTasks.PathToCurrentTask;

            if (task.GetDistance < 3) 
            {
                MoveHelper.StopAllMove();
                Logger.Log($"Reached exploration hotspot for {task.QuestTitle}");
                task.PutTaskOnTimeout("Completed");
                Main.RequestImmediateTaskUpdate = true;
                return;
            }
            
            if (!MoveHelper.IsMovementThreadRunning && task.Location.DistanceTo(ObjectManager.Me.Position) > 2) 
            {
                Logger.Log($"Traveling to Hotspot for {task.QuestTitle} (Explore).");
                //MoveHelper.StartMoveAlongToTaskThread(pathToTask.Path, task);
                MoveHelper.StartGoToThread(task.Location);
            }
        }
    }
}
