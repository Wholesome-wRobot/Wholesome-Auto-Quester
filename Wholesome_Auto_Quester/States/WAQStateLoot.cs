using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStateLoot : State, IWAQState
    {
        private readonly IWowObjectScanner _scanner;

        public WAQStateLoot(IWowObjectScanner scanner)
        {
            _scanner = scanner;
        }

        public override string DisplayName { get; set; } = "WAQ Loot";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || _scanner.ActiveWoWObject.wowObject == null
                    || _scanner.ActiveWoWObject.task.InteractionType != TaskInteraction.KillAndLoot
                    || !ObjectManager.Me.IsValid)
                    return false;

                var (gameObject, task) = _scanner.ActiveWoWObject;
                WoWUnit unitToLoot = (WoWUnit)gameObject;

                if (unitToLoot.IsDead
                    && unitToLoot.IsLootable)
                {
                    DisplayName = task.TaskName;
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            var (gameObject, task) = _scanner.ActiveWoWObject;

            WoWUnit lootTarget = (WoWUnit)gameObject;

            if (ToolBox.HostilesAreAround(lootTarget, task))
            {
                return;
            }

            ToolBox.CheckIfZReachable(gameObject.Position);

            Logger.Log($"Looting {lootTarget.Name}");
            LootingTask.Pulse(new List<WoWUnit> { lootTarget });

            task.PostInteraction(lootTarget);
        }
    }
}
