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
                    DisplayName = $"Kill and Loot {WAQTasks.TaskInProgress.TargetName} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run() {
            WoWObject npc = WAQTasks.TaskInProgressWoWObject;
            WAQTask task = WAQTasks.TaskInProgress;

            if (npc != null)
            {
                if (npc.Type != WoWObjectType.Unit) {
                    Logger.LogError($"Expected a WoWUnit for Kill & Loot but got {npc.Type} instead.");
                    return;
                }
                var killTarget = (WoWUnit)npc;
                if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();
                MoveHelper.StopCurrentMovementThread();
                if (killTarget.IsAlive) {
                    Logger.Log($"Unit found - Fighting {killTarget.Name}");
                    Fight.StartFight(killTarget.Guid);
                }
                Thread.Sleep(200);
                if (killTarget.IsDead) task.PutTaskOnTimeout("Completed");
                if (killTarget.IsLootable && !ObjectManager.Me.InCombatFlagOnly) {
                    Logger.Log($"Looting {killTarget.Name}");
                    LootingTask.Pulse(new List<WoWUnit> { killTarget });
                }
            }
            else
            {
                if (!MoveHelper.IsMovementThreadRunning || MoveHelper.CurrentMovementTarget?.DistanceTo(task.Location) > 8) {
                    Logger.Log($"Moving to Hotspot for {task.QuestTitle} (Kill&Loot).");
                    MoveHelper.StartGoToThread(task.Location, face: !MoveHelper.IsMovementThreadRunning, randomizeEnd: 8f);
                }
                if (task.GetDistance <= 12f) {
                    task.PutTaskOnTimeout("No creature to Kill&Loot in sight");
                } else if (ToolBox.DangerousEnemiesAtLocation(task.Location) && WAQTasks.TasksPile.FindAll(t => t.TargetEntry == task.TargetEntry).Count > 1) {
                    task.PutTaskOnTimeout("Dangerous mobs in the area");
                }
            }
        }
    }
}
