using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
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

        public override void Run() 
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.WoWObjectInProgress;
            //WAQPath pathToTask = WAQTasks.PathToCurrentTask;

            if (ToolBox.ShouldStateBeInterrupted(task, gameObject, WoWObjectType.GameObject))
                return;

            if (gameObject != null) 
            {
                WoWGameObject turnInTarget = (WoWGameObject)gameObject;
                ToolBox.CheckSpotAround(turnInTarget);

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
                        Main.RequestImmediateTaskReset = true;
                        Thread.Sleep(1000);
                        if (!Quest.HasQuest(task.QuestId))
                            WAQTasks.MarQuestAsCompleted(task.QuestId);
                    }
                    else
                        task.PutTaskOnTimeout("Failed PickUp Gossip");
                }
            } 
            else 
            {
                if (!MoveHelper.IsMovementThreadRunning && task.Location.DistanceTo(ObjectManager.Me.Position) > 12) 
                {
                    Logger.Log($"Traveling to QuestEnder for {task.QuestTitle}.");
                    //MoveHelper.StartMoveAlongToTaskThread(pathToTask.Path, task);
                    MoveHelper.StartGoToThread(task.Location);
                }
                if (task.GetDistance <= 13) 
                {
                    task.PutTaskOnTimeout("No Object in sight for quest turn-in");
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}