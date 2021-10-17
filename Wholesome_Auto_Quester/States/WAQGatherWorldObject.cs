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
                        $"Gather world object {WAQTasks.TaskInProgress.GameObjectTemplate.name} for {WAQTasks.TaskInProgress.Quest.LogTitle} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;

            if (WAQTasks.TaskInProgressWoWObject != null)
            {
                if (WAQTasks.TaskInProgressWoWObject.Type != WoWObjectType.GameObject) {
                    Logger.LogError(
                        $"Found object ({WAQTasks.TaskInProgressWoWObject.Entry}) for {WAQTasks.TaskInProgress.TaskName} is not a GameObject! " +
                        $"This should not happen. We got {WAQTasks.TaskInProgressWoWObject.Type} instead.");
                    return;
                }

                var gameObject = (WoWGameObject) WAQTasks.TaskInProgressWoWObject;
                if (gameObject.IsGoodInteractDistance) {
                    if(MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();
                    Logger.Log($"Interacting with {gameObject.Name} to pick it up. (Gathering)");
                    Interact.InteractGameObject(gameObject.GetBaseAddress);
                    Usefuls.WaitIsCastingAndLooting();
                    Thread.Sleep(100);
                    WAQTasks.TaskInProgressWoWObject = null;
                } else if (!MoveHelper.IsMovementThreadRunning ||
                              MoveHelper.CurrentMovementTarget.DistanceTo(gameObject.Position) > 4) {
                    Logger.Log($"Moving to {gameObject.Name} (Gathering).");
                    MoveHelper.StartGoToThread(gameObject.Position, randomizeEnd: 3f);
                }
            } else {
                if (!MoveHelper.IsMovementThreadRunning ||
                    MoveHelper.CurrentMovementTarget.DistanceTo(task.Location) > 8) {

                    Logger.Log($"Moving to Hotspot for {task.Quest.LogTitle} (Gather).");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 8f);
                }
                
                if (task.GetDistance <= 12f) {
                    task.PutTaskOnTimeout("No Object to gather in sight");
                } else if (ToolBox.DangerousEnemiesAtLocation(task.Location) && WAQTasks.TasksPile.FindAll(t => t.POIEntry == task.GameObjectTemplate.entry).Count > 1) {
                    task.PutTaskOnTimeout("Dangerous mobs in the area");
                }
            }
        }
    }
}