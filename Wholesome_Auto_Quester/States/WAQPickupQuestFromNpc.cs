using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using FlXProfiles;
using wManager.Wow.Enums;

namespace Wholesome_Auto_Quester.States {
    class WAQPickupQuestFromNpc : State {
        public override string DisplayName { get; set; } = "Pick up quest [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.PickupQuestFromCreature) {
                    DisplayName =
                        $"Pick up quest {WAQTasks.TaskInProgress.QuestTitle} at {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject npcObject = WAQTasks.TaskInProgressWoWObject;

            if (Quest.GetQuestCompleted(task.QuestId))
            {
                WAQTasks.MarQuestAsCompleted(task.QuestId);
                return;
            }

            if (npcObject != null) {
                if (npcObject.Type != WoWObjectType.Unit) {
                    Logger.LogError($"Expected a WoWUnit for PickUp Quest but got {npcObject.Type} instead.");
                    return;
                }

                ToolBox.ClearSpotAround(npcObject);

                var pickUpTarget = (WoWUnit) npcObject;

                if (!pickUpTarget.InInteractDistance()) {
                    if (!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget?.DistanceTo(pickUpTarget.PositionWithoutType) > 4)
                    {
                        Logger.Log($"NPC found - Going to {pickUpTarget.Name} to pick up {task.QuestTitle}.");
                        MoveHelper.StartGoToThread(pickUpTarget.PositionWithoutType);
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive()) {
                    Interact.InteractGameObject(pickUpTarget.GetBaseAddress);
                } else if (!ToolBox.GossipPickUpQuest(task.QuestTitle)) {
                    task.PutTaskOnTimeout("Failed PickUp Gossip");
                }
                Thread.Sleep(1000);
            } else {
                if (!MoveHelper.IsMovementThreadRunning ||
                    MoveHelper.CurrentMovementTarget?.DistanceTo(task.Location) > 5) {
                    Logger.Log($"Moving to QuestGiver for {task.QuestTitle} (PickUp).");
                    MoveHelper.StartGoToThread(task.Location);
                }
                if (task.GetDistance <= 15f) {
                    task.PutTaskOnTimeout("No NPC in sight for quest pickup");
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}