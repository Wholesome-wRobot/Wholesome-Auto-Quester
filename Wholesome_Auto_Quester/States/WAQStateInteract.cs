using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Documents;
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

        public WAQStateInteract(IWowObjectScanner scanner)
        {
            _scanner = scanner;
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
                if (!MovementManager.InMovement)
                {
                    Logger.Log($"Going to {gameObject.Name} for {task.TaskName}.");
                    List<Vector3> pathToGO = PathFinder.FindPath(gameObject.Position);
                    MovementManager.Go(pathToGO);
                }
                return;
            }

            MovementManager.StopMove();
            Thread.Sleep(200);
            Interact.InteractGameObject(gameObject.GetBaseAddress);
            Thread.Sleep(200);

            task.PostInteraction(gameObject);

            Thread.Sleep(1000);
        }
    }
}