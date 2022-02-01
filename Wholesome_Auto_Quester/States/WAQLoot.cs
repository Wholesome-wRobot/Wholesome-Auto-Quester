/*using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQLoot : State
    {
        private IWowObjectScanner _scanner;
        private ITaskManager _taskManager;

        public WAQLoot(IWowObjectScanner scanner, ITaskManager taskManager, int priority)
        {
            _taskManager = taskManager;
            _scanner = scanner;
            Priority = priority;
        }

        public override string DisplayName { get; set; } = "WAQLoot [SmoothMove - Q]";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || _scanner.ActiveWoWObject == null)
                    return false;

                if (_scanner.ActiveWoWObject is WoWUnit)
                {
                    WoWUnit lootTarget = (WoWUnit)_scanner.ActiveWoWObject;
                    if (lootTarget.IsDead && lootTarget.IsLootable)
                    {
                        DisplayName = $"{_taskManager.ActiveTask.TaskName} [SmoothMove - Q]";
                        return true;
                    }
                }
                else
                {
                    throw new System.Exception($"Tried to loot {_scanner.ActiveWoWObject.Name} but it's not a WoWUnit");
                }

                return false;
            }
        }

        public override void Run()
        {
            WoWObject npc = _scanner.ActiveWoWObject;
            IWAQTask task = _taskManager.ActiveTask;

            WoWUnit lootTarget = (WoWUnit)npc;

            if (ToolBox.HostilesAreAround(lootTarget))
            {
                return;
            }

            Logger.Log($"Looting {lootTarget.Name}");
            LootingTask.Pulse(new List<WoWUnit> { lootTarget });

            if (!lootTarget.IsLootable)
            {
                task.PutTaskOnTimeout("Completed");
            }
        }
    }
}
*/