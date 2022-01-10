using robotManager.FiniteStateMachine;
using System.Threading;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

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

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.Kill)
                {
                    DisplayName = $"Kill {WAQTasks.TaskInProgress.CreatureTemplate.name} for {WAQTasks.TaskInProgress.Quest.LogTitle} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WAQTask task = WAQTasks.TaskInProgress;

            if (WAQTasks.TaskInProgressWoWObject != null)
            {
                if (WAQTasks.TaskInProgressWoWObject.Type != WoWObjectType.Unit) {
                    Logger.LogError($"Expected a WoWUnit for Kill but got {WAQTasks.TaskInProgressWoWObject.Type} instead.");
                    return;
                }

                var killTarget = (WoWUnit) WAQTasks.TaskInProgressWoWObject;
                if(!wManager.wManagerSetting.IsBlackListed(killTarget.Guid))
                {
                    Logger.Log($"Unit found - Fighting {killTarget.Name}");
                    MoveHelper.StopCurrentMovementThread();
                    Fight.StartFight(killTarget.Guid);
                    Thread.Sleep(200);
                    if (killTarget.IsDead) task.PutTaskOnTimeout("Completed");
                }
            }
            else
            {
                if (!MoveHelper.IsMovementThreadRunning ||
                    MoveHelper.CurrentMovementTarget.DistanceTo(task.Location) > 8) {
                    
                    Logger.Log($"Moving to Hotspot for {task.Quest.LogTitle} (Kill).");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 8f);
                }
                if (task.GetDistance <= 12f) {
                    task.PutTaskOnTimeout("No creature to kill in sight");
                    // MoveHelper.StopAllMove();
                } else if (ToolBox.DangerousEnemiesAtLocation(task.Location) && WAQTasks.TasksPile.FindAll(t => t.POIEntry == task.CreatureTemplate.entry).Count > 1) {
                    task.PutTaskOnTimeout("Dangerous mobs in the area");
                }
            }
        }
    }
}
