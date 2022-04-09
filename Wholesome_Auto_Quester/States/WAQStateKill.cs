using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStateKill : State, IWAQState
    {
        private readonly IWowObjectScanner _scanner;

        public WAQStateKill(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override string DisplayName { get; set; } = "WAQ Kill";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || _scanner.ActiveWoWObject.wowObject == null
                    || (_scanner.ActiveWoWObject.task.InteractionType != TaskInteraction.Kill
                        && _scanner.ActiveWoWObject.task.InteractionType != TaskInteraction.KillAndLoot)
                    || !ObjectManager.Me.IsValid)
                    return false;

                DisplayName = _scanner.ActiveWoWObject.task.TaskName;
                return true;
            }
        }

        public override void Run()
        {
            var (gameObject, task) = _scanner.ActiveWoWObject;

            if (ToolBox.ShouldStateBeInterrupted(task, gameObject))
            {
                return;
            }

            Vector3 myPos = ObjectManager.Me.Position;
            WoWUnit killTarget = (WoWUnit)gameObject;
            Vector3 targetPos = killTarget.Position;
            float distanceToTarget = myPos.DistanceTo(targetPos);

            if (ToolBox.HostilesAreAround(killTarget, task))
            {
                return;
            }

            //Check if we have vision, it might be a big detour
            if (!ToolBox.IHaveLineOfSightOn(killTarget))
            {
                distanceToTarget = ToolBox.GetWAQPath(myPos, targetPos).Distance;
            }

            if (distanceToTarget > 40)
            {
                if (!MoveHelper.IsMovementThreadRunning || targetPos.DistanceTo(MoveHelper.CurrentTarget) > 10)
                {
                    MoveHelper.StartGoToThread(targetPos, null);
                }
                return;
            }

            if (WTLocation.GetZDifferential(targetPos) > targetPos.DistanceTo2D(myPos) && MovementManager.CurrentPath.Count <= 3)
            {
                BlacklistHelper.AddNPC(killTarget.Guid, "Z differential too large");
                return;
            }

            MountTask.DismountMount(false, false);

            Logger.Log($"Unit found - Fighting {killTarget.Name}");
            Fight.StartFight(killTarget.Guid);

            task.PostInteraction(killTarget);
        }
    }
}
