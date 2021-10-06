using System;
using System.Collections.Generic;
using System.Linq;
using FlXProfiles;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States {
    internal class Defend : State {
        private WoWUnit _defendTarget;
        public override string DisplayName { get; set; } = "Defend";

        public override void Run() {
            if (_defendTarget == null) return;
            var stateName = $"Attacking {_defendTarget?.Name} to defend ourself.";
            DisplayName = stateName;
            Logging.Write(stateName);
            MoveHelper.StopAllMove();
            Fight.StartFight(_defendTarget.Guid);
            _defendTarget = null;
        }

        public override bool NeedToRun {
            get {
                // Check directly attacking units
                Vector3 myPos = ObjectManager.Me.PositionWithoutType;
                List<WoWUnit> justUnits =
                    ObjectManager.GetObjectWoWUnit().ToList();
                IOrderedEnumerable<WoWUnit> attackingMe = ObjectManager.GetUnitAttackPlayer(justUnits)
                    .Where(unit => unit.IsAttackable)
                    .OrderBy(unit => unit.PositionWithoutType.DistanceTo(myPos));

                bool isMounted = ObjectManager.Me.IsMounted;
                if (MoveHelper.IsMovementThreadRunning) {
                    _defendTarget = attackingMe.FirstOrDefault(
                        unit => unit.PositionWithoutType
                            .DistanceTo(MoveHelper.CurrentMovementTarget) < (isMounted ? 26f : 100f));
                } else {
                    _defendTarget = attackingMe.FirstOrDefault();
                }
                // if (isMounted && MoveHelper.IsMovementThreadRunning) {
                //     _defendTarget = attackingMe.FirstOrDefault(unit =>
                //         unit.PositionWithoutType.DistanceTo(MoveHelper.CurrentMovementTarget) < 26);
                // } else {
                //     _defendTarget = attackingMe.FirstOrDefault();
                // }

                if (_defendTarget != null) return true;
                if (!MoveHelper.IsMovementThreadRunning
                    || isMounted && myPos.DistanceTo(MoveHelper.CurrentMovementTarget) > 26) return false;

                // Check possible units on path
                List<Vector3> path = SmoothMove.Move.LatestPath;
                if (!MoveHelper.IsMovementThreadRunning || path == null || path.Count < 2) return false;

                WoWUnit target = null;
                var targetDistance = float.MaxValue;
                Tuple<WoWUnit, Vector3, int>[] possiblePathUnits = justUnits
                    .Where(unit => unit.Reaction <= Reaction.Unfriendly)
                    .Select(unit =>
                        new Tuple<WoWUnit, Vector3, int>(unit, unit.PositionWithoutType, unit.AggroDistance)).ToArray();

                int numEnemiesOnPath = 0;
                for (var i = ToolBox.GetIndexOfClosestPoint(path); i < path.Count - 1; i++) {
                    if (myPos.DistanceTo(path[i + 1]) > 26) break;
                    if (path[i + 1].Type == "Flying") continue;
                    foreach (Tuple<WoWUnit, Vector3, int> foundUnit in possiblePathUnits) {
                        (WoWUnit unit, Vector3 position, int aggroRange) = foundUnit;
                        float distance = myPos.DistanceTo(position);
                        if(ToolBox.PointDistanceToLine(path[i], path[i + 1], position) < aggroRange + 3 &&
                           unit.IsAttackable) {
                            numEnemiesOnPath++;
                            if (distance < targetDistance) {
                                target = unit;
                                targetDistance = distance;
                            }
                        }
                    }
                }

                if (numEnemiesOnPath >= 3 || myPos.DistanceTo(MoveHelper.CurrentMovementTarget) < 80f) {
                    _defendTarget = target;
                }

                if (_defendTarget != null) {
                    Logging.Write($"Found {_defendTarget.Name} on path.");
                    return true;
                }

                return false;
            }
        }
    }
}