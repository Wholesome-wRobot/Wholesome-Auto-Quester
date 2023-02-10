using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQStatePriorityLoot : State, IWAQState
    {
        private readonly IWowObjectScanner _scanner;

        public override string DisplayName { get; set; } = "WAQ Priority Loot";

        private readonly int _lootRange = 15;
        private WoWUnit _unitToLoot;
        private IWAQTask _associatedTask;
        private List<(ulong Guid, Stopwatch Watch)> _unitsLooted = new List<(ulong, Stopwatch)>();

        public WAQStatePriorityLoot(IWowObjectScanner scanner)
        {
            _scanner = scanner;
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

                // Purge cache
                _unitsLooted.RemoveAll(ul => ul.Watch.ElapsedMilliseconds > 120 * 1000);

                int unitToLootEntry = _scanner.ActiveWoWObject.wowObject.Entry;
                _associatedTask = _scanner.ActiveWoWObject.task;

                Vector3 myPosition = ObjectManager.Me.PositionWithoutType;
                _unitToLoot = ObjectManager.GetWoWUnitLootable()
                    .Where(corpse => corpse.Entry == unitToLootEntry && !_unitsLooted.Exists(ul => ul.Guid == corpse.Guid))
                    .Where(corpse => corpse?.PositionWithoutType.DistanceTo(myPosition) <= _lootRange)
                    .Where(corpse => !wManagerSetting.IsBlackListedZone(corpse.Position) || corpse.Position.DistanceTo(myPosition) < 5f)
                    .OrderBy(corpse => corpse?.PositionWithoutType.DistanceTo(myPosition))
                    .FirstOrDefault();

                return _unitToLoot != null && _associatedTask != null;
            }
        }

        public override void Run()
        {
            Vector3 myPos = ObjectManager.Me.PositionWithoutType;
            Vector3 corpsePos = _unitToLoot.PositionWithoutType;

            // Loot
            if (myPos.DistanceTo(corpsePos) <= 3.5)
            {
                Logger.Log($"[WAQPriorityLoot] Looting {_unitToLoot.Name}");
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
                    Logger.LogError($"[WAQPriorityLoot] {_unitToLoot.Name}'s corpse seems unreachable. Skipping loot.");
                    _unitsLooted.Add((_unitToLoot.Guid, Stopwatch.StartNew()));
                }
            }

            _associatedTask.PostInteraction(_unitToLoot);
        }
    }
}
