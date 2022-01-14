using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQLootInCombat : State
    {
        public override string DisplayName { get; set; } = "WAQLootInCombat [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                WoWObject npc = WAQTasks.TaskInProgressWoWObject;
                if (WAQTasks.TaskInProgress?.TaskType == TaskType.KillAndLoot && npc?.Type == WoWObjectType.Unit)
                {
                    WoWUnit lootTarget = (WoWUnit)WAQTasks.TaskInProgressWoWObject;
                    if (lootTarget.IsLootable && ObjectManager.Me.InCombatFlagOnly)
                    {
                        DisplayName = $"Loot in combat {WAQTasks.TaskInProgress.TargetName} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
                        return true;
                    }
                }

                return false;
            }
        }

        public override void Run()
        {
            WoWObject npc = WAQTasks.TaskInProgressWoWObject;
            WAQTask task = WAQTasks.TaskInProgress;

            WoWUnit lootTarget = (WoWUnit)npc;

            if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();
            MoveHelper.StopCurrentMovementThread();

            Logger.Log($"Looting {lootTarget.Name}");
            LootingTask.Pulse(new List<WoWUnit> { lootTarget });

            if (!lootTarget.IsLootable)
                task.PutTaskOnTimeout("Completed");
        }
    }
}
