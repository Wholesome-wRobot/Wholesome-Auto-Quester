using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
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
        private Dictionary<int, float> interactDistances = new Dictionary<int, float>()
        {
            { 190537, 10 }, // Crashed Plague Sprayer
            { 179828, 8 }, // Dark Iron Pillow
        }; // object ID => interact distance

        public override string DisplayName { get; set; } = "WAQ Interact";

        public WAQStateInteract(IWowObjectScanner scanner)
        {
            _scanner = scanner;
        }

        public void Initialize()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += EventsWithArgsHandler;
        }

        public void Dispose()
        {
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= EventsWithArgsHandler;
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
            float interactDistance;
            if (interactDistances.TryGetValue(gameObject.Entry, out float interactDist))
            {
                interactDistance = interactDist;
            }
            else
            {
                interactDistance = 3f + scale; // 3 ?
                interactDistances.Add(gameObject.Entry, interactDistance);
            }

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

        private void EventsWithArgsHandler(string id, List<string> args)
        {
            if (id == "UI_ERROR_MESSAGE" && args[0] == "You are too far away.")
            {
                var (gameObject, task) = _scanner.ActiveWoWObject;
                if (gameObject != null && interactDistances.TryGetValue(gameObject.Entry, out float interactDist))
                {
                    interactDistances[gameObject.Entry] = interactDist - 1;
                    Logger.Log($"Too far away. Setting interact distance for {gameObject.Name} to {interactDistances[gameObject.Entry]}");
                }
            }
        }
    }
}