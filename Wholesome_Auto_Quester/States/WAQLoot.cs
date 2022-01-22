using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.Bot;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using wManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQLoot : State
    {
        public override string DisplayName { get; set; } = "WAQLoot [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || WAQTasks.WoWObjectInProgress == null)
                    return false;

                WoWObject npc = WAQTasks.WoWObjectInProgress;
                if (WAQTasks.TaskInProgress?.TaskType == TaskType.KillAndLoot && !ObjectManager.Me.InCombatFlagOnly)
                {
                    WoWUnit lootTarget = (WoWUnit)WAQTasks.WoWObjectInProgress;
                    if (lootTarget.IsDead)
                    {
                        DisplayName = $"Loot {WAQTasks.TaskInProgress.TargetName} for {WAQTasks.TaskInProgress.QuestTitle} [SmoothMove - Q]";
                        return true;
                    }
                }

                return false;
            }
        }

        public override void Run()
        {
            WoWObject npc = WAQTasks.WoWObjectInProgress;
            WAQTask task = WAQTasks.TaskInProgress;

            WoWUnit lootTarget = (WoWUnit)npc;

            if (ToolBox.HostilesAreAround(lootTarget)) 
                return;

            Logger.Log($"Looting {lootTarget.Name}");
            LootingTask.Pulse(new List<WoWUnit> { lootTarget });
            if (!lootTarget.IsLootable)
                task.PutTaskOnTimeout("Completed");
            Main.RequestImmediateTaskReset = true;
        }
    }
}
