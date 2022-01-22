using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using wManager.Wow.Bot.Tasks;
using System.Threading;

namespace Wholesome_Auto_Quester.States
{
    class WAQKill : State
    {
        public override string DisplayName { get; set; } = "Kill creature [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if ((WAQTasks.TaskInProgress?.TaskType == TaskType.Kill || WAQTasks.TaskInProgress?.TaskType == TaskType.KillAndLoot) 
                    && WAQTasks.WoWObjectInProgress != null)
                {
                    DisplayName = $"Kill {WAQTasks.TaskInProgress.TargetName} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;
            WoWObject gameObject = WAQTasks.WoWObjectInProgress;

            if (ToolBox.ShouldStateBeInterrupted(task, gameObject, WoWObjectType.Unit))
                return;

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
                if (!MoveHelper.IsMovementThreadRunning || killTarget.Position.DistanceTo(MoveHelper.currentTarget) > 10)
                    MoveHelper.StartGoToThread(killTarget.Position, null);
                return;
            }

            //MoveHelper.StopAllMove();
            MountTask.DismountMount(false, false);

            if (ToolBox.HostilesAreAround(killTarget))
                return;

            Logger.Log($"Unit found - Fighting {killTarget.Name}");
            Fight.StartFight(killTarget.Guid);

            if (killTarget.IsDead && task.TaskType == TaskType.Kill
                || killTarget.IsDead && !killTarget.IsLootable && task.TaskType == TaskType.KillAndLoot)
            {
                task.PutTaskOnTimeout("Completed");
                Main.RequestImmediateTaskReset = true;
                return;
            }
        }
    }
}
