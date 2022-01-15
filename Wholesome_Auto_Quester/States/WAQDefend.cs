using System;
using System.Collections.Generic;
using System.Linq;
using FlXProfiles;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States {
    internal class WAQDefend : State {
        private WoWUnit _defendTarget;
        public override string DisplayName { get; set; } = "Defend";

        public override void Run() {
            if (_defendTarget == null) return;
            var stateName = $"Attacking {_defendTarget?.Name} to defend ourself.";
            DisplayName = stateName;
            Logger.Log(stateName);
            MoveHelper.StopAllMove();
            Fight.StartFight(_defendTarget.Guid);
            _defendTarget = null;
        }

        public override bool NeedToRun {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                // Check directly attacking units
                Vector3 myPos = ObjectManager.Me.PositionWithoutType;
                bool isMounted = ObjectManager.Me.IsMounted;
                int myLevel = (int)ObjectManager.Me.Level;
                List<WoWUnit> justUnits = ObjectManager.GetObjectWoWUnit().ToList();
                ulong myGuid = ObjectManager.Me.Guid;
                ulong petGuid = ObjectManager.Pet?.Guid ?? 0U;

                IOrderedEnumerable<WoWUnit> attackingMe = justUnits
                    .Where(unit => {
                        uint unitLevel = unit.Level;
                        if (MoveHelper.CurrentMovementTarget != null && (unitLevel < myLevel - 5 || unitLevel > myLevel + 2)) 
                            return false;
                        return unit.IsAttackable && unit.InCombatFlagOnly && (unit.Target == myGuid || petGuid > 0 && unit.Target == petGuid);
                    })
                    .OrderBy(unit => unit.PositionWithoutType.DistanceTo(myPos));

                if (attackingMe.Count() <= 0) return false;

                _defendTarget = attackingMe.FirstOrDefault();
                //Logger.LogError($"{_defendTarget.Name} is attacking me, target is {_defendTarget.TargetObject?.Name}");

                if (isMounted 
                    && MoveHelper.CurrentMovementTarget != null 
                    && myPos.DistanceTo(MoveHelper.CurrentMovementTarget) > 40) 
                    return false;

                MountTask.DismountMount(false, false, 100);

                if (_defendTarget != null) return true;
                /*
                // Check possible units on path
                List<Vector3> path = SmoothMove.Move.LatestPath;
                if (!MoveHelper.IsMovementThreadRunning || path == null || path.Count < 2) return false;

                WoWUnit target = null;
                var targetDistance = float.MaxValue;
                Tuple<WoWUnit, Vector3, int>[] possiblePathUnits = justUnits
                    .Where(unit => unit.Reaction <= Reaction.Unfriendly)
                    .Select(unit =>
                        new Tuple<WoWUnit, Vector3, int>(unit, unit.PositionWithoutType, unit.AggroDistance)).ToArray();

                // uint myLevel = ObjectManager.Me.Level;
                // bool isAvoiding = false;
                int numEnemiesOnPath = 0;
                for (var i = ToolBox.GetIndexOfClosestPoint(path); i < path.Count - 1; i++) {
                    if (myPos.DistanceTo(path[i + 1]) > 26) break;
                    if (path[i + 1].Type == "Flying") continue;
                    foreach (Tuple<WoWUnit, Vector3, int> foundUnit in possiblePathUnits) {
                        (WoWUnit unit, Vector3 position, int aggroRange) = foundUnit;
                        float distance = myPos.DistanceTo(position);
                        if (ToolBox.PointDistanceToLine(path[i], path[i + 1], position) < aggroRange + 1 &&
                           unit.IsAttackable) {
                            // TODO: Use normal navigator for enemy avoidance to path after last enemy found if dangerous
                            // TODO: Add DangerousMob check to all States
                            // uint unitLevel = unit.Level;
                            // if (unitLevel > myLevel + 2 || unitLevel > myLevel && unit.IsElite) {
                            //     Logging.Write($"Trying to avoid {unit.Name}");
                            //     wManagerSetting.AddBlackListZone(position, aggroRange + 15, true);
                            //     isAvoiding = true;
                            // }
                            numEnemiesOnPath++;
                            if (distance < targetDistance) {
                                target = unit;
                                targetDistance = distance;
                            }
                        }
                    }
                }

                // if (isAvoiding && MoveHelper.IsMovementThreadRunning) {
                //     Logging.Write($"Generating new path without dangerous enemies.");
                //     MoveHelper.StartGoToThread(MoveHelper.CurrentMovementTarget, false);
                //     Thread.Sleep(250);
                //     wManagerSetting.ClearBlacklistOfCurrentProductSession();
                //     return false;
                // }

                if (numEnemiesOnPath >= 3 || myPos.DistanceTo(MoveHelper.CurrentMovementTarget) < 80f) {
                    _defendTarget = target;
                }

                if (_defendTarget != null && !wManager.wManagerSetting.IsBlackListed(_defendTarget.Guid)) {
                    Logging.Write($"Found {_defendTarget.Name} on path.");

                    float defendTargetDistance = ToolBox.CalculatePathTotalDistance(myPos, _defendTarget.Position);
                    if (defendTargetDistance <= 0 || defendTargetDistance > _defendTarget.GetDistance * 2)
                    {
                        Logger.LogError($"Blacklisting {_defendTarget.Name} {_defendTarget.Guid} for 10 seconds because it's unreachable/too far");
                        wManagerSetting.AddBlackList(_defendTarget.Guid, 1000 * 10, true);
                        return false;
                    }

                    return true;
                }
                */
                return false;
            }
        }
    }
}