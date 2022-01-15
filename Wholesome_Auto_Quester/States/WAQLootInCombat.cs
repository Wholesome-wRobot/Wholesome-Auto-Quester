using robotManager.FiniteStateMachine;
using FlXProfiles;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
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
                    || !ObjectManager.Me.IsValid
                    || ObjectManager.Me.HealthPercent < 40)
                    return false;

                if (WAQTasks.TaskInProgress?.TaskType == TaskType.KillAndLoot && WAQTasks.TaskInProgressWoWObject?.Type == WoWObjectType.Unit)
                {
                    WoWUnit lootTarget = (WoWUnit)WAQTasks.TaskInProgressWoWObject;
                    if (lootTarget.IsDead && lootTarget.IsLootable && ObjectManager.Me.InCombatFlagOnly && lootTarget.GetDistance < 25)
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

            if (lootTarget.GetDistance > 3)
                MoveHelper.StartGoToThread(npc.Position);

            if (lootTarget.GetDistance <= 4)
            {
                Logger.Log($"Looting {lootTarget.Name}");
                Interact.InteractGameObject(lootTarget.GetBaseAddress);
            }
            if (!lootTarget.IsLootable)
                task.PutTaskOnTimeout("Completed");
            WAQTasks.UpdateTasks();
        }
    }
}
