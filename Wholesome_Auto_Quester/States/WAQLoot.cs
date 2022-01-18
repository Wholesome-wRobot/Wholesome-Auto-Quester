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
                    || !ObjectManager.Me.IsValid)
                    return false;

                WoWObject npc = WAQTasks.WoWObjectInProgress;
                if (WAQTasks.TaskInProgress?.TaskType == TaskType.KillAndLoot
                    && npc != null
                    && npc.Type == WoWObjectType.Unit 
                    && !wManagerSetting.IsBlackListed(npc.Guid)
                    && !wManagerSetting.IsBlackListedZone(npc.Position)
                    && !ObjectManager.Me.InCombatFlagOnly)
                {
                    WoWUnit lootTarget = (WoWUnit)WAQTasks.WoWObjectInProgress;
                    if (lootTarget.IsLootable)
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

            ToolBox.CheckSpotAround(lootTarget);

            if (MoveHelper.IsMovementThreadRunning) MoveHelper.StopAllMove();
            MoveHelper.StopCurrentMovementThread();

            Logger.Log($"Looting {lootTarget.Name}");
            LootingTask.Pulse(new List<WoWUnit> { lootTarget });
            if (!lootTarget.IsLootable)
                task.PutTaskOnTimeout("Completed");
            Main.RequestImmediateTaskUpdate = true;
        }
    }
}
