using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States {
    class WAQTurnInQuestToNpc : State {
        public override string DisplayName { get; set; } = "Turn in quest [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.TurnInQuestToCreature) {
                    DisplayName =
                        $"Turning in {WAQTasks.TaskInProgress.QuestTitle} to NPC {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject npcObject = WAQTasks.WoWObjectInProgress;
            //WAQPath pathToTask = WAQTasks.PathToCurrentTask;

            if (ToolBox.ShouldStateBeInterrupted(task, npcObject, WoWObjectType.Unit))
                return;

            if (npcObject != null) 
            {
                WoWUnit turnInTarget = (WoWUnit)npcObject;
                ToolBox.CheckSpotAround(turnInTarget);

                if (!turnInTarget.InInteractDistance()) 
                {
                    if (!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget?.DistanceTo(turnInTarget.PositionWithoutType) > 4) 
                    {
                        MoveHelper.StartGoToThread(turnInTarget.PositionWithoutType, randomizeEnd: 3f);
                        Logger.Log($"NPC found - Going to {turnInTarget.Name} to turn in {task.QuestTitle}.");
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive()) 
                {
                    Interact.InteractGameObject(turnInTarget.GetBaseAddress);
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
                    task.PutTaskOnTimeout("No NPC in sight for quest turn-in");
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}