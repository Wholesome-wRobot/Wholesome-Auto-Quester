using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStatePriorityLoot : State, IWAQState
    {
        private readonly IWowObjectScanner _scanner;

        public override string DisplayName { get; set; } = "WAQ PriorityLoot";
        private List<ulong> UnitsLooted { get; set; } = new List<ulong>();

        public WAQStatePriorityLoot(IWowObjectScanner scanner, int priority)
        {
            _scanner = scanner;
            Priority = priority;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || _scanner.ActiveWoWObject == (null, null)
                    || _scanner.ActiveWoWObject.task.InteractionType != TaskInteraction.KillAndLoot
                    || ObjectManager.Me.HealthPercent < 20)
                    return false;

                var (gameObject, task) = _scanner.ActiveWoWObject;
                WoWUnit unitToLoot = (WoWUnit)gameObject;

                if (unitToLoot.IsDead
                    && unitToLoot.IsLootable
                    && !UnitsLooted.Contains(unitToLoot.Guid))
                {
                    DisplayName = $"Priority loot for {_scanner.ActiveWoWObject.task.TaskName}";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            var (gameObject, task) = _scanner.ActiveWoWObject;
            WoWUnit unitToLoot = (WoWUnit)gameObject;
            WAQPath pathToCorpse = ToolBox.GetWAQPath(ObjectManager.Me.Position, gameObject.Position);

            if (!pathToCorpse.IsReachable)
            {
                UnitsLooted.Add(unitToLoot.Guid);
                return;
            }

            if (!MoveHelper.IsMovementThreadRunning && unitToLoot.GetDistance > 3)
            {
                MoveHelper.StopAllMove(true);
                MoveHelper.StartGoToThread(unitToLoot.Position, null);
            }

            if (unitToLoot.GetDistance <= 4)
            {
                ToolBox.CheckIfZReachable(gameObject.Position);
                MoveHelper.StopAllMove(true);
                Logger.Log($"Priority looting {unitToLoot.Name}");
                Interact.InteractGameObject(unitToLoot.GetBaseAddress);
                UnitsLooted.Add(unitToLoot.Guid);
                Thread.Sleep(500);
            }

            task.PostInteraction(gameObject);
        }
    }
}
