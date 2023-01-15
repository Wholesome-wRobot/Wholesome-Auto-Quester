using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Helpers;
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
        private List<ulong> _unitsLooted = new List<ulong>();

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
                if (_unitsLooted.Count > 20)
                {
                    _unitsLooted.RemoveRange(0, 10);
                }

                _unitToLoot = (WoWUnit)_scanner.ActiveWoWObject.wowObject;
                _associatedTask = _scanner.ActiveWoWObject.task;

                if (_unitToLoot == null 
                    || _associatedTask == null
                    || _unitToLoot.IsAlive
                    || _unitToLoot.Position.DistanceTo(ObjectManager.Me.Position) > _lootRange
                    || !_unitToLoot.IsLootable
                    || _unitsLooted.Contains(_unitToLoot.Guid))
                {
                    return false;
                }

                return true;
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
                    Logger.LogError($"[WAQPriorityLoot] {_unitToLoot.Name}'s corpse seems unreachable. Skipping loot.");
                    _unitsLooted.Add(_unitToLoot.Guid);
                }
            }

            _associatedTask.PostInteraction(_unitToLoot);
        }
    }
}
