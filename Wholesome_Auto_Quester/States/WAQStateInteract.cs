using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStateInteract : State, IWAQState
    {
        private readonly IWowObjectScanner _scanner;

        public override string DisplayName { get; set; } = "WAQ Interact";

        public WAQStateInteract(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || _scanner.ActiveWoWObject.wowObject == null)
                    return false;

                if (_scanner.ActiveWoWObject.task.InteractionType == TaskInteraction.Interact)
                {
                    DisplayName = _scanner.ActiveWoWObject.task.TaskName;
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            var (gameObject, task) = _scanner.ActiveWoWObject;
            Vector3 myPos = ObjectManager.Me.Position;

            if (ToolBox.ShouldStateBeInterrupted(task, gameObject))
            {
                return;
            }

            if (ToolBox.HostilesAreAround(gameObject, task))
            {
                return;
            }

            float scale = gameObject.Scale;
            if (gameObject.Entry == 190537  // Crashed Plague Sprayer
                //|| gameObject.Entry == 182116 // Fulgore Spore 
                || gameObject.Entry == 179828) // Dark Iron Pillow
            {
                scale = 6;
            }

            float interactDistance = 3f + scale;

            if (MovementManager.CurrentPath.Count <= 2)
            {
                ToolBox.CheckIfZReachable(gameObject.Position);
            }

            if (gameObject.Position.DistanceTo(myPos) > interactDistance)
            {
                MoveHelper.StartGoToThread(gameObject.Position, $"Going to {gameObject.Name} for {task.TaskName}.");
                if (gameObject.Position.DistanceTo(myPos) > interactDistance && gameObject.Position.DistanceTo(myPos) < 10)
                {
                    MovementManager.MoveTo(gameObject.Position);
                    Thread.Sleep(100);
                }
                return;
            }

            MoveHelper.StopAllMove(true);
            Thread.Sleep(200);
            Interact.InteractGameObject(gameObject.GetBaseAddress);
            Thread.Sleep(200);

            task.PostInteraction(gameObject);
        }
    }
}