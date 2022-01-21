using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQKill : State
    {
        public override string DisplayName { get; set; } = "Kill creature [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.Kill 
                    || WAQTasks.TaskInProgress?.TaskType == TaskType.KillAndLoot)
                {
                    DisplayName = $"Kill {WAQTasks.TaskInProgress.TargetName} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.WoWObjectInProgress;
            //WAQPath pathToTask = WAQTasks.PathToCurrentTask;

            if (ToolBox.ShouldStateBeInterrupted(task, gameObject, WoWObjectType.Unit))
                return;

            if (gameObject != null)
            {
                WoWUnit killTarget = (WoWUnit)gameObject;
                ToolBox.CheckSpotAround(killTarget);

                Logger.Log($"Unit found - Fighting {killTarget.Name}");
                MoveHelper.StopAllMove();
                Fight.StartFight(killTarget.Guid);                
                if (killTarget.IsDead && task.TaskType == TaskType.Kill && killTarget.Guid == task.ObjectRealGuid
                    || killTarget.IsDead && !killTarget.IsLootable && task.TaskType == TaskType.KillAndLoot && killTarget.Guid == task.ObjectRealGuid)
                {
                    task.PutTaskOnTimeout("Completed");
                }                
                Main.RequestImmediateTaskReset = true;
            }
            else
            {
                if (!MoveHelper.IsMovementThreadRunning && task.Location.DistanceTo(ObjectManager.Me.Position) > 12) 
                {                    
                    Logger.Log($"Traveling to Hotspot for {task.QuestTitle} (Kill).");
                    //MoveHelper.StartMoveAlongToTaskThread(pathToTask.Path, task);
                    MoveHelper.StartGoToThread(task.Location);
                }
                if (task.GetDistance <= 13) 
                {
                    task.PutTaskOnTimeout("No creature to kill in sight");
                    Main.RequestImmediateTaskUpdate = true;
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}
