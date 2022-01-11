using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using FlXProfiles;
using wManager.Wow.Enums;

namespace Wholesome_Auto_Quester.States {
    class WAQPickupQuestFromGameObject : State {
        public override string DisplayName { get; set; } = "Pick up quest [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.PickupQuestFromGameObject) {
                    DisplayName =
                        $"Pick up quest {WAQTasks.TaskInProgress.QuestTitle} at {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.TaskInProgressWoWObject;

            if (Quest.GetQuestCompleted(task.QuestId)) {
                WAQTasks.MarQuestAsCompleted(task.QuestId);
                return;
            }

            if (gameObject != null) {
                if (gameObject.Type != WoWObjectType.GameObject) {
                    Logger.LogError($"Expected a GameObject for PickUp Quest but got {gameObject.Type} instead.");
                    return;
                }

                var pickUpTarget = (WoWGameObject) gameObject;

                if (pickUpTarget.GetDistance > 4) {
                    if(!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget?.DistanceTo(pickUpTarget.Position) > 4)
                    {
                        Logger.Log($"Game Object found - Going to {pickUpTarget.Name} to pick up {task.QuestTitle}.");
                        MoveHelper.StartGoToThread(pickUpTarget.Position, randomizeEnd: 3f);
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive()) {
                    Interact.InteractGameObject(pickUpTarget.GetBaseAddress);
                    Usefuls.WaitIsCasting();
                } else if (!ToolBox.GossipPickUpQuest(task.QuestTitle)) {
                    task.PutTaskOnTimeout("Failed pickup gossip");
                }
                Thread.Sleep(1000);
            } else {
                if (!MoveHelper.IsMovementThreadRunning ||
                    MoveHelper.CurrentMovementTarget?.DistanceTo(task.Location) > 8) {
                    Logger.Log($"Moving to QuestGiver for {task.QuestTitle}.");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 8f);
                }
                if (task.GetDistance <= 12f) {
                    task.PutTaskOnTimeout("No object in sight for quest pickup");
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}