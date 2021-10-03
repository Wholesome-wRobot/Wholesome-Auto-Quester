using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States {
    class WAQTurnInQuest : State {
        public override string DisplayName { get; set; } = "Turn in quest [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.TurnInQuest) {
                    DisplayName =
                        $"Turning in {WAQTasks.TaskInProgress.Quest.LogTitle} to {WAQTasks.TaskInProgress.Npc.Name} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject npcObject = WAQTasks.TaskInProgressWoWObject;

            if (npcObject != null) {
                if (npcObject.Type != WoWObjectType.Unit) {
                    Logger.LogError($"Expected a WoWUnit for TurnIn Quest but got {npcObject.Type} instead.");
                    return;
                }

                var turnInTarget = (WoWUnit) npcObject;

                if (!turnInTarget.InInteractDistance()) {
                    if(!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget.DistanceTo(turnInTarget.PositionWithoutType) > 4) {
                        MoveHelper.StartGoToThread(turnInTarget.PositionWithoutType, randomizeEnd: 3f);
                        Logger.Log($"NPC found - Going to {turnInTarget.Name} to turn in {task.Quest.LogTitle}.");
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive()) {
                    Interact.InteractGameObject(turnInTarget.GetBaseAddress);
                } else if (!ToolBox.GossipTurnInQuest(task.Quest.LogTitle)) {
                    Logger.LogError($"Failed PickUp Gossip for {task.Quest.LogTitle}. Timeout");
                    task.PutTaskOnTimeout();
                } else {
                    Thread.Sleep(1000);
                    if (!Quest.HasQuest(task.Quest.Id))
                        task.Quest.MarkAsCompleted();
                }
            } else {
                if (!MoveHelper.IsMovementThreadRunning ||
                    MoveHelper.CurrentMovementTarget.DistanceTo(task.Location) > 8) {
                    if (task.GetDistance <= 12f) {
                        Logger.Log(
                            $"We are close to {ToolBox.GetTaskId(task)} position and no NPC for turn-in in sight. Time out");
                        task.PutTaskOnTimeout();
                        MoveHelper.StopAllMove();
                        return;
                    }

                    Logger.Log($"Moving to QuestGiver for {task.Quest.LogTitle} (TurnIn).");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 8f);
                }
            }
        }
    }
}