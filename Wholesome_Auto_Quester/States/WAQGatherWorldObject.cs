using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.Threading;

namespace Wholesome_Auto_Quester.States {
    class WAQGatherWorldObject : State {
        public override string DisplayName { get; set; } = "Gather world object [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.GatherGameObject && WAQTasks.WoWObjectInProgress != null) 
                {
                    DisplayName =
                        $"Gather world object {WAQTasks.TaskInProgress.TargetName} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
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

            WoWGameObject gatherTarget = (WoWGameObject)gameObject;
            if (ToolBox.HostilesAreAround(gatherTarget))
                return;
            float interactDistance = 3.5f + gatherTarget.Scale;

            if (gatherTarget.GetDistance > interactDistance)
            {
                if (!MoveHelper.IsMovementThreadRunning)
                {
                    Logger.Log($"Game Object found - Going to {gatherTarget.Name} to gather.");
                    MoveHelper.StartGoToThread(gatherTarget.Position);
                }
                return;
            }

            Logger.Log($"Gathering {gameObject.Name}");
            MoveHelper.StopAllMove();
            Interact.InteractGameObject(gameObject.GetBaseAddress);
            Usefuls.WaitIsCastingAndLooting();
            Thread.Sleep(200);
            Main.RequestImmediateTaskReset = true;
        }
    }
}