using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager;
using wManager.Wow.ObjectManager;
using robotManager.Helpful;
using System.Linq;

namespace Wholesome_Auto_Quester.States
{
    class WAQTurboLoot : State, IWAQState
    {
        public override string DisplayName => "WAQ Turbo Loot";

        private readonly int _lootRange = 35;
        private WoWUnit _unitToLoot;
        private List<ulong> _unitsLooted = new List<ulong>();

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
                if (_unitsLooted.Count > 20)
                {
                    _unitsLooted.RemoveRange(0, 10);
                }

                _unitToLoot = null;
                Vector3 myPosition = ObjectManager.Me.PositionWithoutType;
                List<WoWUnit> lootableCorpses = ObjectManager.GetWoWUnitLootable()
                    .Where(corpse => corpse?.PositionWithoutType.DistanceTo(myPosition) <= _lootRange)
                    .Where(corpse => !wManagerSetting.IsBlackListedZone(corpse.Position) || corpse.Position.DistanceTo(myPosition) < 5f)
                    .OrderBy(corpse => corpse?.PositionWithoutType.DistanceTo(myPosition))
                    .ToList();
                foreach (WoWUnit lootableCorpse in lootableCorpses)
                {
                    if (!_unitsLooted.Contains(lootableCorpse.Guid))
                    {
                        _unitToLoot = lootableCorpse;
                        break;
                    }
                }

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
                _unitsLooted.Add(_unitToLoot.Guid);
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
                    _unitsLooted.Add(_unitToLoot.Guid);
                }
            }
        }
    }
}
