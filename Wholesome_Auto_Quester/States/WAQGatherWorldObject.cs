using robotManager.FiniteStateMachine;
using System.Threading;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States {
    class WAQGatherWorldObject : State {
        public override string DisplayName { get; set; } = "Gather world object [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.GatherGameObject) {
                    DisplayName =
                        $"Gather world object {WAQTasks.TaskInProgress.TargetName} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.TaskInProgressWoWObject;

            if (gameObject != null)
            {
                if (WAQTasks.TaskInProgressWoWObject.Type != WoWObjectType.GameObject)
                {
                    Logger.LogError($"Expected a GameObject for PickUp Quest but got {gameObject.Type} instead.");
                    return;
                }

                ToolBox.CheckSpotAround(gameObject);

                WoWGameObject gatherTarget = (WoWGameObject)gameObject;

                if (gatherTarget.GetDistance > 3 + gatherTarget.Scale)
                {
                    if (!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget?.DistanceTo(gatherTarget.Position) > 3 + gatherTarget.Scale)
                    {
                        Logger.Log($"Game Object found - Going to {gatherTarget.Name} to gather.");
                        MoveHelper.StartGoToThread(gatherTarget.Position, randomizeEnd: 3f);
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (ObjectManager.Me.Position.DistanceTo(gatherTarget.Position) <= 3 + gatherTarget.Scale)  
                {
                    Logger.Log($"Interacting with {gameObject.Name} to pick it up. (Gathering)");
                    MoveHelper.StopAllMove();
                    Interact.InteractGameObject(gameObject.GetBaseAddress);
                    Usefuls.WaitIsCastingAndLooting();
                    WAQTasks.UpdateTasks();
                }
            } else {
                if (!MoveHelper.IsMovementThreadRunning || MoveHelper.CurrentMovementTarget?.DistanceTo(task.Location) > 15) {

                    Logger.Log($"Moving to Hotspot for {task.QuestTitle} (Gather).");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 15f);
                }
                
                if (task.GetDistance <= 12f) {
                    task.PutTaskOnTimeout("No Object to gather in sight");
                } else if (ToolBox.DangerousEnemiesAtLocation(task.Location) && WAQTasks.TasksPile.FindAll(t => t.TargetEntry == task.TargetEntry).Count > 1) {
                    task.PutTaskOnTimeout("Dangerous mobs in the area");
                }
            }
        }
    }
}