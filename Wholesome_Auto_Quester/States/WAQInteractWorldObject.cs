using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States {
    class WAQInteractWorldObject : State {
        public override string DisplayName { get; set; } = "Interact with world object [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.InteractWithWorldObject) {
                    DisplayName =
                        $"Interact with world object {WAQTasks.TaskInProgress.TargetName} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() 
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.WoWObjectInProgress;
            WAQPath pathToTask = WAQTasks.PathToCurrentTask;

            if (ToolBox.ShouldStateBeInterrupted(task, gameObject, WoWObjectType.GameObject))
                return;

            if (gameObject != null)
            {
                WoWGameObject interactObject = (WoWGameObject)WAQTasks.WoWObjectInProgress;
                ToolBox.CheckSpotAround(interactObject);

                if (interactObject.IsGoodInteractDistance) 
                {
                    if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();
                    Logger.Log($"Interacting with {interactObject.Name}.");
                    Interact.InteractGameObject(interactObject.GetBaseAddress);
                    Usefuls.WaitIsCastingAndLooting();
                    Main.RequestImmediateTaskUpdate = true;
                } 
                else if (!MoveHelper.IsMovementThreadRunning ||
                    MoveHelper.CurrentMovementTarget?.DistanceTo(interactObject.Position) > 4) 
                {
                    Logger.Log($"Moving to {interactObject.Name} (Gathering).");
                    MoveHelper.StartGoToThread(interactObject.Position, randomizeEnd: 3f);
                }
            } 
            else 
            {
                if (!MoveHelper.IsMovementThreadRunning ||
                    MoveHelper.CurrentMovementTarget?.DistanceTo(task.Location) > 15f) 
                {
                    Logger.Log($"Traveling to Hotspot for {task.QuestTitle} (Gather).");
                    MoveHelper.StartMoveAlongToTaskThread(pathToTask.Path, task);
                }
                
                if (task.GetDistance <= 12f) 
                {
                    task.PutTaskOnTimeout("No object to gather in sight");
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}