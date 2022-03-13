using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    internal class WAQStateClearPath : State, IWAQState
    {
        public override string DisplayName { get; set; } = "WAQ Clearing path";
        private WoWUnit _unitToClear = null;
        private IWowObjectScanner _scanner;
        public List<(Vector3 a, Vector3 b)> LinesToCheck = new List<(Vector3 a, Vector3 b)>(); // For Radar 3D

        public WAQStateClearPath(IWowObjectScanner scanner, int priority)
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
                    || ObjectManager.Me.InCombatFlagOnly
                    || Fight.InFight
                    || MovementManager.CurrentPath == null
                    || MovementManager.CurrentPath.Count <= 0
                    || (!MovementManager.InMoveTo && !MovementManager.InMovement)
                    || (ObjectManager.Me.IsMounted && MovementManager.CurrentPath.Last().DistanceTo(ObjectManager.Me.Position) > 120)
                    || ObjectManager.Me.GetDurabilityPercent < 20)
                    return false;

                List<Vector3> currentPath = MovementManager.CurrentPath;
                Vector3 nextNode = MovementManager.CurrentMoveTo;
                Vector3 myPosition = ObjectManager.Me.Position;
                List<(Vector3 a, Vector3 b)> linesToCheck = new List<(Vector3, Vector3)>();
                bool nextNodeFound = false;
                for (int i = 0; i < currentPath.Count; i++)
                {
                    // break on last node unless it's the only node
                    if (i >= currentPath.Count - 1 && linesToCheck.Count > 0)
                    {
                        break;
                    }

                    // skip nodes behind me
                    if (!nextNodeFound)
                    {
                        if (currentPath[i] != nextNode)
                        {
                            continue;
                        }
                        nextNodeFound = true;
                    }

                    // Ignore if too far
                    if (linesToCheck.Count > 2 && currentPath[i].DistanceTo(myPosition) > 50)
                    {
                        break;
                    }

                    // Path ahead of me
                    if (linesToCheck.Count <= 0)
                    {
                        linesToCheck.Add((myPosition, currentPath[i]));
                        if (currentPath.Count > i + 1)
                        {
                            linesToCheck.Add((currentPath[i], currentPath[i + 1]));
                        }
                    }
                    else
                    {
                        linesToCheck.Add((currentPath[i], currentPath[i + 1]));
                    }
                }
                LinesToCheck = linesToCheck;

                // Check if enemies along the lines
                List<WoWUnit> units = ObjectManager.GetObjectWoWUnit()
                    .FindAll(u => u.IsAttackable
                        && u.Reaction == wManager.Wow.Enums.Reaction.Hostile
                        && u.IsAlive
                        && u.IsValid
                        && !u.IsElite
                        && !u.IsTaggedByOther
                        && !u.PlayerControlled
                        && u.Position.DistanceTo(myPosition) < 50
                        && u.Level < ObjectManager.Me.Level + 4)
                    .OrderBy(u => u.Position.DistanceTo(myPosition))
                    .ToList();

                // Check for hostiles along the lines
                foreach ((Vector3 a, Vector3 b) line in linesToCheck)
                {
                    if (_unitToClear == null)
                    {
                        foreach (WoWUnit unit in units)
                        {
                            if (_scanner.ActiveWoWObject.wowObject != null && _scanner.ActiveWoWObject.wowObject.Guid == unit.Guid)
                            {
                                continue;
                            }
                            if (wManager.wManagerSetting.IsBlackListedZone(unit.Position) 
                                || !ToolBox.IHaveLineOfSightOn(unit))
                            {
                                continue;
                            }
                            if (ToolBox.GetZDistance(unit.Position) < 10 && ToolBox.PointDistanceToLine(line.a, line.b, unit.Position) < 20)
                            {
                                _unitToClear = unit;
                                break;
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                return _unitToClear != null;
            }
        }
        public override void Run()
        {
            WoWUnit unitToClear = _unitToClear;
            DisplayName = $"WAQ Clearing path against {unitToClear.Name}";
            Logger.Log($"WAQ Clearing path against {unitToClear.Name}");
            Fight.StartFight(unitToClear.Guid);
            _unitToClear = null;
        }
    }
}
