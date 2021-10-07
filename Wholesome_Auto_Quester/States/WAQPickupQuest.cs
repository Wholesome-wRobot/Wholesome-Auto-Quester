using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using FlXProfiles;
using wManager.Wow.Enums;
using wManager.Wow.Bot.Tasks;

namespace Wholesome_Auto_Quester.States {
    class WAQPickupQuest : State {
        public override string DisplayName { get; set; } = "Pick up quest [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.PickupQuest) {
                    DisplayName =
                        $"Pick up quest {WAQTasks.TaskInProgress.Quest.LogTitle} at {WAQTasks.TaskInProgress.Npc.Id} - {WAQTasks.TaskInProgress.Npc.Name} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject npcObject = WAQTasks.TaskInProgressWoWObject;

            if (Quest.GetQuestCompleted(task.Quest.Id)) {
                task.Quest.MarkAsCompleted();
                return;
            }

            if (npcObject != null) {
                if (npcObject.Type != WoWObjectType.Unit) {
                    Logger.LogError($"Expected a WoWUnit for PickUp Quest but got {npcObject.Type} instead.");
                    return;
                }

                var pickUpTarget = (WoWUnit) npcObject;

                if (!pickUpTarget.InInteractDistance()) {
                    if(!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget.DistanceTo(pickUpTarget.PositionWithoutType) > 4)
                    {
                        Logger.Log($"NPC found - Going to {pickUpTarget.Name} to pick up {task.Quest.LogTitle}.");
                        MoveHelper.StartGoToThread(pickUpTarget.PositionWithoutType, randomizeEnd: 3f);
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive()) {
                    Interact.InteractGameObject(pickUpTarget.GetBaseAddress);
                } else if (!ToolBox.GossipPickUpQuest(task.Quest.LogTitle)) {
                    Logger.LogError($"Failed PickUp Gossip for {task.Quest.LogTitle}. Timeout");
                    task.PutTaskOnTimeout();
                }
                Thread.Sleep(1000);
            } else {
                if (!MoveHelper.IsMovementThreadRunning ||
                    MoveHelper.CurrentMovementTarget.DistanceTo(task.Location) > 8) {
                    if (task.GetDistance <= 12f) {
                        Logger.Log(
                            $"We are close to {task.TaskName} position and no NPC for pick-up in sight. Time out for {task.Npc.SpawnTimeSecs}s");
                        task.PutTaskOnTimeout();
                        MoveHelper.StopAllMove();
                        return;
                    }

                    Logger.Log($"Moving to QuestGiver for {task.Quest.LogTitle} (PickUp).");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 8f);
                }
            }
        }
    }
}