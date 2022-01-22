using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.Threading;

namespace Wholesome_Auto_Quester.States {
    class WAQInteractWorldObject : State {
        public override string DisplayName { get; set; } = "Interact with world object [SmoothMove - Q]";

        public override bool NeedToRun {
            get {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.InteractWithWorldObject && WAQTasks.WoWObjectInProgress != null) 
                {
                    DisplayName =
                        $"Interact with world object {WAQTasks.TaskInProgress.TargetName} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
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

            WoWGameObject interactObject = (WoWGameObject)WAQTasks.WoWObjectInProgress;
            if (ToolBox.HostilesAreAround(interactObject))
                return;
            float interactDistance = 3.5f + interactObject.Scale;

            if (interactObject.GetDistance > interactDistance)
            {
                if (!MoveHelper.IsMovementThreadRunning)
                {
                    Logger.Log($"Game Object found - Going to {interactObject.Name} to interact.");
                    MoveHelper.StartGoToThread(interactObject.Position);
                }
                return;
            }

            Logger.Log($"Interacting with {interactObject.Name}");
            MoveHelper.StopAllMove();
            Interact.InteractGameObject(interactObject.GetBaseAddress);
            Usefuls.WaitIsCastingAndLooting();
            Thread.Sleep(200);
            Main.RequestImmediateTaskUpdate = true;
        }
    }
}