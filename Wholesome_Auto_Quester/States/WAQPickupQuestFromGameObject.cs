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

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.PickupQuestFromGameObject && WAQTasks.WoWObjectInProgress != null) 
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

            WoWGameObject pickUpTarget = (WoWGameObject)gameObject;
            if (ToolBox.HostilesAreAround(pickUpTarget))
                return;
            float interactDistance = 3.5f + pickUpTarget.Scale;

            if (pickUpTarget.GetDistance > interactDistance) 
            {
                MoveHelper.StartGoToThread(pickUpTarget.Position, $"Game Object found - Going to {pickUpTarget.Name} to pick up {task.QuestTitle}.");
                return;
            }

            if (!ToolBox.IsNpcFrameActive()) 
            {
                MoveHelper.StopAllMove();
                Interact.InteractGameObject(pickUpTarget.GetBaseAddress);
                Usefuls.WaitIsCasting();
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
                    task.PutTaskOnTimeout("Failed pickup gossip");
            }
        }
    }
}