using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQPriorityLoot : State
    {
        public override string DisplayName { get; set; } = "WAQPriorityLoot [SmoothMove - Q]";
        private WoWUnit UnitToLoot { get; set; }
        private List<ulong> UnitsLooted { get; set; } = new List<ulong>();

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || ObjectManager.Me.HealthPercent < 20)
                    return false;

                List<WoWObject> listUnitToLoot = WAQTasks.WAQObjectManager
                    .FindAll(o => o.Type == WoWObjectType.Unit && WAQTasks.EntriesToLoot.Contains(o.Entry) && !UnitsLooted.Contains(o.Guid));

                if (listUnitToLoot.Count <= 0)
                    return false;

                foreach (WoWObject ob in listUnitToLoot)
                {
                    WoWUnit unit = (WoWUnit)ob;
                    if (unit.IsDead && unit.IsLootable && unit.GetDistance < 20)
                    {
                        WAQPath pathToCorpse = ToolBox.GetWAQPath(ObjectManager.Me.Position, unit.Position);
                        if (pathToCorpse.IsReachable)
                        {
                            UnitToLoot = unit;
                            break;
                        }
                        else
                            UnitsLooted.Add(unit.Guid);
                    }
                    UnitToLoot = null;
                }

                if (UnitToLoot != null)
                {
                    DisplayName = $"Priority loot on {UnitToLoot.Name} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            //if (Fight.InFight) Fight.StopFight();
            //LootingTask.Pulse(new List<WoWUnit> { UnitToLoot });
            if (!MoveHelper.IsMovementThreadRunning && UnitToLoot.GetDistance > 3)
            {
                MoveHelper.StopAllMove(true);
                MoveHelper.StartGoToThread(UnitToLoot.Position, null);
            }

            if (UnitToLoot.GetDistance <= 4)
            {
                MoveHelper.StopAllMove(true);
                Logger.Log($"Priority looting {UnitToLoot.Name}");
                Interact.InteractGameObject(UnitToLoot.GetBaseAddress);
                UnitsLooted.Add(UnitToLoot.Guid);
                Main.RequestImmediateTaskUpdate = true;
            }
        }
    }
}
