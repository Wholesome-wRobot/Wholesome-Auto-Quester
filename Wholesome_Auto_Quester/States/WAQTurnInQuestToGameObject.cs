using robotManager.FiniteStateMachine;
using System.Threading;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQTurnInQuestToGameObject : State {
        public override string DisplayName { get; set; } = "Turn in quest [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.TurnInQuestToGameObject) {
                    DisplayName =
                        $"Turning in {WAQTasks.TaskInProgress.Quest.LogTitle} at game object {WAQTasks.TaskInProgress.GameObjectTemplate.name} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.TaskInProgressWoWObject;

            if (gameObject != null) {
                if (gameObject.Type != WoWObjectType.GameObject) {
                    Logger.LogError($"Expected a GameObject for TurnIn Quest but got {gameObject.Type} instead.");
                    return;
                }

                var turnInTarget = (WoWGameObject) gameObject;

                if (turnInTarget.GetDistance > 4) {
                    if(!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget.DistanceTo(turnInTarget.Position) > 4) {
                        MoveHelper.StartGoToThread(turnInTarget.Position, randomizeEnd: 3f);
                        Logger.Log($"Game Object found - Going to {turnInTarget.Name} to turn in {task.Quest.LogTitle}.");
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive()) {
                    Interact.InteractGameObject(turnInTarget.GetBaseAddress);
                    Usefuls.WaitIsCasting();
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
                    Logger.Log($"Moving to QuestEnder for {task.Quest.LogTitle}.");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 8f);
                }
                if (task.GetDistance <= 12f) {
                    Logger.Log(
                        $"We are close to {task.TaskName} position and no NPC for turn-in in sight. Time out");
                    task.PutTaskOnTimeout();
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}