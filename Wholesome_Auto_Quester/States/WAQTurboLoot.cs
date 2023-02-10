using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager;
using wManager.Wow.ObjectManager;
using robotManager.Helpful;
using System.Linq;
using System.Diagnostics;

namespace Wholesome_Auto_Quester.States
{
    class WAQTurboLoot : State, IWAQState
    {
        public override string DisplayName => "WAQ Turbo Loot";

        private readonly int _lootRange = 35;
        private WoWUnit _unitToLoot;
        private List<(ulong Guid, Stopwatch Watch)> _unitsLooted = new List<(ulong, Stopwatch)>();

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnected
                    || !wManagerSetting.CurrentSetting.LootMobs
                    || !ObjectManager.Me.IsValid
                    || Fight.InFight)
                {
                    return false;
                }

                // Purge cache
                _unitsLooted.RemoveAll(ul => ul.Watch.ElapsedMilliseconds > 120 * 1000);

                Vector3 myPosition = ObjectManager.Me.PositionWithoutType;
                _unitToLoot = ObjectManager.GetWoWUnitLootable()
                    .Where(corpse => !_unitsLooted.Exists(ul => ul.Guid == corpse.Guid))
                    .Where(corpse => corpse?.PositionWithoutType.DistanceTo(myPosition) <= _lootRange)
                    .Where(corpse => !wManagerSetting.IsBlackListedZone(corpse.Position) || corpse.Position.DistanceTo(myPosition) < 5f)
                    .OrderBy(corpse => corpse?.PositionWithoutType.DistanceTo(myPosition))
                    .FirstOrDefault();

                return _unitToLoot != null;
            }
        }


        public override void Run()
        {
            Vector3 myPos = ObjectManager.Me.PositionWithoutType;
            Vector3 corpsePos = _unitToLoot.PositionWithoutType;

            // Loot
            if (myPos.DistanceTo(corpsePos) <= 3.5)
            {
                Logger.Log($"[WAQTurboLoot] Looting {_unitToLoot.Name}");
                MovementManager.StopMove();
                Interact.InteractGameObject(_unitToLoot.GetBaseAddress);
                Thread.Sleep(100);
                _unitsLooted.Add((_unitToLoot.Guid, Stopwatch.StartNew()));
                return;
            }

            // Approach corpse
            if (!MovementManager.InMovement ||
                MovementManager.CurrentPath.Count > 0 && MovementManager.CurrentPath.Last() != corpsePos)
            {
                MovementManager.StopMove();
                List<Vector3> pathToCorpse = PathFinder.FindPath(myPos, corpsePos, out bool resultSuccess);
                if (resultSuccess)
                {
                    MovementManager.Go(pathToCorpse);
                }
                else
                {
                    Logger.LogError($"[WAQTurboLoot] {_unitToLoot.Name}'s corpse seems unreachable. Skipping loot.");
                    _unitsLooted.Add((_unitToLoot.Guid, Stopwatch.StartNew()));
                }
            }
        }
    }
}
