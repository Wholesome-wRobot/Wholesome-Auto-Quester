using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStatePriorityLoot : State
    {
        private IWowObjectScanner _scanner;
        public WAQStatePriorityLoot(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override string DisplayName { get; set; } = "WAQPriorityLoot [SmoothMove - Q]";
        private List<ulong> UnitsLooted { get; set; } = new List<ulong>();

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || _scanner.ActiveWoWObject == (null, null)
                    || ObjectManager.Me.HealthPercent < 20)
                    return false;

                if (_scanner.ActiveWoWObject.Item2.InteractionType == TaskInteraction.KillAndLoot 
                    && !UnitsLooted.Contains(_scanner.ActiveWoWObject.Item1.Guid))
                {
                    DisplayName = $"Priority loot for {_scanner.ActiveWoWObject.Item2.TaskName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            var (gameObject, task) = _scanner.ActiveWoWObject;
            WoWUnit unitToLoot = (WoWUnit)gameObject;

            if (!MoveHelper.IsMovementThreadRunning && unitToLoot.GetDistance > 3)
            {
                MoveHelper.StopAllMove(true);
                MoveHelper.StartGoToThread(unitToLoot.Position, null);
            }

            if (unitToLoot.GetDistance <= 4)
            {
                MoveHelper.StopAllMove(true);
                Logger.Log($"Priority looting {unitToLoot.Name}");
                Interact.InteractGameObject(unitToLoot.GetBaseAddress);
                UnitsLooted.Add(unitToLoot.Guid);
            }

            task.PostInteraction(gameObject);
        }
    }
}
