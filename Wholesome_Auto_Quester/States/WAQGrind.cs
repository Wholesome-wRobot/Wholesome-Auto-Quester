using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQGrind : State
    {
        public override string DisplayName { get; set; } = "Grind creature [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.Grind)
                {
                    DisplayName = $"Grind {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.WoWObjectInProgress;

            if (gameObject != null)
            {
                WoWUnit killTarget = (WoWUnit)gameObject;
                ToolBox.CheckSpotAround(killTarget);

                Logger.Log($"Unit found - Fighting {killTarget.Name}");
                MoveHelper.StopAllMove();
                Fight.StartFight(killTarget.Guid);
                Main.RequestImmediateTaskReset = true;
                
                if (killTarget.IsDead 
                    && task.TaskType == TaskType.Grind 
                    && killTarget.Guid == task.ObjectRealGuid)
                {
                    Logger.Log($"Task GUID={task.ObjectRealGuid}, npc GUID={killTarget.Guid}");
                    task.PutTaskOnTimeout("Completed");
                }
                Main.RequestImmediateTaskReset = true;

            }
            else
            {
                if (!MoveHelper.IsMovementThreadRunning && task.Location.DistanceTo(ObjectManager.Me.Position) > 12) 
                {                    
                    Logger.Log($"Traveling to Hotspot to grind {task.TargetName}.");
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
