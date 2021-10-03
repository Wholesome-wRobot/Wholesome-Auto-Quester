using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQKillAndLoot : State
    {
        public override string DisplayName { get; set; } = "Kill and Loot [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.KillAndLoot)
                {
                    DisplayName = $"Kill and Loot {WAQTasks.TaskInProgress.Npc.Name} for {WAQTasks.TaskInProgress.Quest.LogTitle} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            WoWObject npc = WAQTasks.TaskInProgressWoWObject;
            WAQTask task = WAQTasks.TaskInProgress;
            //Logger.Log($"******** [{task.POIEntry}] RUNNING {task.TaskType} TASK {ToolBox.GetTaskId(task)}  ********");

            if (npc != null)
            {
                if (WAQTasks.TaskInProgressWoWObject.Type != WoWObjectType.Unit) {
                    Logger.LogError($"Expected a WoWUnit for Kill & Loot but got {WAQTasks.TaskInProgressWoWObject.Type} instead.");
                    return;
                }
                var killTarget = (WoWUnit) WAQTasks.TaskInProgressWoWObject;
                if(MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();
                Logger.Log($"Unit found - Fighting {killTarget.Name}");
                MoveHelper.StopCurrentMovementThread();
                Fight.StartFight(killTarget.Guid);
                LootingTask.Pulse(new List<WoWUnit> { killTarget });
                Thread.Sleep(200);
            }
            else
            {
                if (!MoveHelper.IsMovementThreadRunning ||
                    MoveHelper.CurrentMovementTarget.DistanceTo(task.Location) > 8) {
                    Logger.Log($"Moving to Hotspot for {task.Quest.LogTitle} (Kill&Loot).");
                    MoveHelper.StartGoToThread(task.Location, randomizeEnd: 8f);
                }
                if (task.GetDistance <= 12f) {
                    Logger.Log($"We are close to {ToolBox.GetTaskId(task)} position and no npc to kill&loot in sight. Time out for {task.Npc.SpawnTimeSecs}s");
                    task.PutTaskOnTimeout();
                    MoveHelper.StopAllMove();
                }
            }
        }
    }
}
