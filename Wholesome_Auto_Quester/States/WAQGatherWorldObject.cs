/*using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQGatherWorldObject : State
    {
        private IWowObjectScanner _scanner;
        public WAQGatherWorldObject(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override string DisplayName { get; set; } = "Gather world object [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (_scanner.ActiveWoWObject.Item1 != null)
                {
                    DisplayName =
                        $"Gather world object {_scanner.ActiveWoWObject.Item1.Name} for {_scanner.ActiveWoWObject.Item2.TaskName} [SmoothMove - Q]";
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
                MoveHelper.StartGoToThread(gatherTarget.Position, $"Game Object found - Going to {gatherTarget.Name} to gather.");
                return;
            }

            Logger.Log($"Gathering {gameObject.Name}");
            MoveHelper.StopAllMove(true);
            Interact.InteractGameObject(gameObject.GetBaseAddress);
            Usefuls.WaitIsCastingAndLooting();
            Thread.Sleep(200);
            Main.RequestImmediateTaskReset = true;
        }
    }
}*/