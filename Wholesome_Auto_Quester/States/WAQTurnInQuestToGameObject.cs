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
                        $"Turning in {WAQTasks.TaskInProgress.QuestTitle} at game object {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
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
                if (gameObject.Type != WoWObjectType.GameObject) 
                {
                    Logger.LogError($"Expected a GameObject for TurnIn Quest but got {gameObject.Type} instead.");
                    return;
                }

                ToolBox.CheckSpotAround(gameObject);

                var turnInTarget = (WoWGameObject) gameObject;

                if (turnInTarget.GetDistance > 4) 
                {
                    if (!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget?.DistanceTo(turnInTarget.Position) > 4) 
                    {
                        MoveHelper.StartGoToThread(turnInTarget.Position, randomizeEnd: 3f);
                        Logger.Log($"Game Object found - Going to {turnInTarget.Name} to turn in {task.QuestTitle}.");
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive()) 
                {
                    Interact.InteractGameObject(turnInTarget.GetBaseAddress);
                    Usefuls.WaitIsCasting();
                } 
                else 
                {
                    if (ToolBox.GossipTurnInQuest(task.QuestTitle))
                    {
                        Thread.Sleep(1000);
                        if (!Quest.HasQuest(task.QuestId))
                            WAQTasks.MarQuestAsCompleted(task.QuestId);
                        WAQTasks.UpdateTasks();
                    }
                    else
                        task.PutTaskOnTimeout("Failed PickUp Gossip");
                }
            } 
            else 
            {
                if (!MoveHelper.IsMovementThreadRunning || MoveHelper.CurrentMovementTarget?.DistanceTo(task.Location) > 15) 
                {
                    Logger.Log($"Moving to QuestEnder for {task.QuestTitle}.");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 8f);
                }
                if (task.GetDistance <= 15f) 
                {
                    task.PutTaskOnTimeout("No Object in sight for quest turn-in");
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}