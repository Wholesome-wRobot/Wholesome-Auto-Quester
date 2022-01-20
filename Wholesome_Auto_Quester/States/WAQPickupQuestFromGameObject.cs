using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using wManager.Wow.Enums;

namespace Wholesome_Auto_Quester.States {
    class WAQPickupQuestFromGameObject : State {
        public override string DisplayName { get; set; } = "Pick up quest [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.PickupQuestFromGameObject) 
                {
                    DisplayName =
                        $"Pick up quest {WAQTasks.TaskInProgress.QuestTitle} at {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.WoWObjectInProgress;
            //WAQPath pathToTask = WAQTasks.PathToCurrentTask;

            if (ToolBox.ShouldStateBeInterrupted(task, gameObject, WoWObjectType.GameObject))
                return;

            if (gameObject != null) 
            {
                WoWGameObject pickUpTarget = (WoWGameObject)gameObject;
                ToolBox.CheckSpotAround(pickUpTarget);
                float interactDistance = 3.5f + pickUpTarget.Scale;

                if (pickUpTarget.GetDistance > interactDistance) 
                {
                    if (!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget?.DistanceTo(pickUpTarget.Position) > interactDistance)
                    {
                        Logger.Log($"Game Object found - Going to {pickUpTarget.Name} to pick up {task.QuestTitle}.");
                        MoveHelper.StartGoToThread(pickUpTarget.Position);
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive()) 
                {
                    Interact.InteractGameObject(pickUpTarget.GetBaseAddress);
                    Usefuls.WaitIsCasting();
                    Thread.Sleep(500);
                } 
                else 
                {
                    if (ToolBox.GossipPickUpQuest(task.QuestTitle))
                    {
                        Main.RequestImmediateTaskReset = true;
                        Thread.Sleep(1000);
                    }
                    else
                        task.PutTaskOnTimeout("Failed pickup gossip");
                }
            } 
            else 
            {
                if (!MoveHelper.IsMovementThreadRunning && task.Location.DistanceTo(ObjectManager.Me.Position) > 12) 
                {
                    Logger.Log($"Traveling to QuestGiver for {task.QuestTitle}.");
                    //MoveHelper.StartMoveAlongToTaskThread(pathToTask.Path, task);
                    MoveHelper.StartGoToThread(task.Location);
                }
                if (task.GetDistance <= 13) 
                {
                    task.PutTaskOnTimeout("No object in sight for quest pickup");
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}