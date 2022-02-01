using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStateInteract : State
    {
        private IWowObjectScanner _scanner;
        public WAQStateInteract(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override string DisplayName { get; set; } = "Kill [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (_scanner.ActiveWoWObject.Item1 != null && _scanner.ActiveWoWObject.Item2.InteractionType == TaskInteraction.Interact)
                {
                    DisplayName = $"{_scanner.ActiveWoWObject.Item2.TaskName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            var (gameObject, task) = _scanner.ActiveWoWObject;

            if (ToolBox.ShouldStateBeInterrupted(task, gameObject))
            {
                return;
            }

            if (ToolBox.HostilesAreAround(gameObject, task))
            {
                return;
            }

            float interactDistance = 3.5f + gameObject.Scale;

            if (gameObject.GetDistance > interactDistance)
            {
                MoveHelper.StartGoToThread(gameObject.Position, $"Going to {gameObject.Name} to interact.");
                return;
            }

            MoveHelper.StopAllMove(true);
            Interact.InteractGameObject(gameObject.GetBaseAddress);

            task.PostInteraction(gameObject);
        }
    }
}