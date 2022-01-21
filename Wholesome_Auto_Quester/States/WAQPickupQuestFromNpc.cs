using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
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
                        $"Pick up quest {WAQTasks.TaskInProgress.QuestTitle} from {WAQTasks.TaskInProgress.TargetName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() 
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject npcObject = WAQTasks.WoWObjectInProgress;
            //WAQPath pathToTask = WAQTasks.PathToCurrentTask;

            if (ToolBox.ShouldStateBeInterrupted(task, npcObject, WoWObjectType.Unit))
                return;

            if (npcObject != null) 
            {
                WoWUnit pickUpTarget = (WoWUnit)npcObject;

                ToolBox.CheckSpotAround(pickUpTarget);

                if (!pickUpTarget.InInteractDistance()) 
                {
                    if (!MoveHelper.IsMovementThreadRunning
                       || MoveHelper.CurrentMovementTarget?.DistanceTo(pickUpTarget.PositionWithoutType) > 4)
                    {
                        Logger.Log($"NPC found - Going to {pickUpTarget.Name} to pick up {task.QuestTitle}.");
                        MoveHelper.StartGoToThread(pickUpTarget.PositionWithoutType);
                    }
                    return;
                }

                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();

                if (!ToolBox.IsNpcFrameActive())
                {
                    MoveHelper.StopAllMove();
                    Interact.InteractGameObject(pickUpTarget.GetBaseAddress);
                    Thread.Sleep(500);
                    if (!ToolBox.IsNpcFrameActive())
                        task.PutTaskOnTimeout($"Couldn't open quest frame");
                }
                else 
                {
                    if (ToolBox.GossipPickUpQuest(task.QuestTitle, task.QuestId))
                    {
                        Main.RequestImmediateTaskReset = true;
                        Thread.Sleep(1000);
                    }
                    else
                        task.PutTaskOnTimeout("Failed PickUp Gossip");
                }
            } 
            else 
            {
                if (!MoveHelper.IsMovementThreadRunning && task.Location.DistanceTo(ObjectManager.Me.Position) > 12) 
                {
                    Logger.Log($"Traveling to QuestGiver for {task.QuestTitle} (PickUp).");
                    //MoveHelper.StartMoveAlongToTaskThread(pathToTask.Path, task);
                    MoveHelper.StartGoToThread(task.Location);
                }
                if (task.GetDistance <= 13) 
                {
                    task.PutTaskOnTimeout("No NPC in sight for quest pickup");
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}