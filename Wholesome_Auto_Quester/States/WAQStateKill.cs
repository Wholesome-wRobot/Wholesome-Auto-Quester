using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStateKill : State
    {
        private IWowObjectScanner _scanner;
        public WAQStateKill(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override string DisplayName { get; set; } = "Grind creature [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || _scanner.ActiveWoWObject == (null, null)
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (_scanner.ActiveWoWObject.Item2.InteractionType == TaskInteraction.Kill)
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

            WoWUnit killTarget = (WoWUnit)gameObject;
            float distanceToTarget = killTarget.GetDistance;

            //Check if we have vision, it might be a big detour
            if (TraceLine.TraceLineGo(ObjectManager.Me.Position, killTarget.Position, CGWorldFrameHitFlags.HitTestSpellLoS | CGWorldFrameHitFlags.HitTestLOS))
            {
                distanceToTarget = ToolBox.GetWAQPath(ObjectManager.Me.Position, killTarget.Position).Distance;
                Thread.Sleep(1000);
            }

            if (distanceToTarget > 40)
            {
                if (!MoveHelper.IsMovementThreadRunning || killTarget.Position.DistanceTo(MoveHelper.CurrentTarget) > 10)
                {
                    MoveHelper.StartGoToThread(killTarget.Position, null);
                }
                return;
            }

            MountTask.DismountMount(false, false);

            if (ToolBox.HostilesAreAround(killTarget, task))
            {
                return;
            }

            Logger.Log($"Unit found - Fighting {killTarget.Name}");
            Fight.StartFight(killTarget.Guid);

            task.PostInteraction(killTarget);
        }
    }
}
