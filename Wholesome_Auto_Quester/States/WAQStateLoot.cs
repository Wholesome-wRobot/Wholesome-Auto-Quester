using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStateLoot : State
    {
        private IWowObjectScanner _scanner;
        public WAQStateLoot(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override string DisplayName { get; set; } = "Kill and Loot [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || _scanner.ActiveWoWObject == (null, null)
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (_scanner.ActiveWoWObject.Item2.InteractionType == TaskInteraction.KillAndLoot)
                {
                    DisplayName = $"{_scanner.ActiveWoWObject.Item2.TaskName} [SmoothMove - Q]";
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

            Logger.Log($"Looting {lootTarget.Name}");
            LootingTask.Pulse(new List<WoWUnit> { lootTarget });

            task.PostInteraction(lootTarget);
        }
    }
}
