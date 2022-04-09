using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Linq;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    internal class WAQDefend : State, IWAQState
    {
        private WoWUnit _defendTarget;
        public override string DisplayName { get; set; } = "WAQ Defend";

        public override void Run()
        {
            var stateName = $"Defending against {_defendTarget.Name}";
            DisplayName = stateName;
            Logger.Log(stateName);
            MoveHelper.StopAllMove(true);
            Fight.StartFight(_defendTarget.Guid);
            _defendTarget = null;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || !ObjectManager.Me.InCombatFlagOnly
                    || ObjectManager.Me.GetDurabilityPercent < 20)
                {

                    return false;
                }

                // Check directly attacking units
                _defendTarget = null;
                Vector3 myPos = ObjectManager.Me.PositionWithoutType;
                bool isMounted = ObjectManager.Me.IsMounted;
                int myLevel = (int)ObjectManager.Me.Level;
                List<WoWUnit> justUnits = ObjectManager.GetObjectWoWUnit().ToList();
                List<WoWUnit> myPets = justUnits.FindAll(u => u.IsMyPet);
                ulong myGuid = ObjectManager.Me.Guid;
                
                /*bool noMountSet = string.IsNullOrEmpty(wManager.wManagerSetting.CurrentSetting.GroundMountName)
                    && string.IsNullOrEmpty(wManager.wManagerSetting.CurrentSetting.FlyingMountName);*/

                if (isMounted
                    && MoveHelper.IsMovementThreadRunning
                    && WTPathFinder.GetCurrentPathRemainingDistance() > 300)
                    return false;

                IOrderedEnumerable<WoWUnit> attackingMe = justUnits
                    .Where(unit => !unit.PlayerControlled
                        && unit.IsAttackable
                        && (unit.Target == myGuid || myPets.Exists(pet => pet.Guid == unit.Target)))
                    .OrderBy(unit => unit.PositionWithoutType.DistanceTo(myPos));

                if (attackingMe.Count() <= 0)
                    return false;

                if (isMounted
                    && MoveHelper.IsMovementThreadRunning
                    && WTPathFinder.GetCurrentPathRemainingDistance() > 200
                    && attackingMe.Count() <= 2)
                    return false;

                if (isMounted
                    && MoveHelper.IsMovementThreadRunning
                    && WTPathFinder.GetCurrentPathRemainingDistance() > 125
                    && attackingMe.Count() <= 1)
                    return false;

                _defendTarget = attackingMe.FirstOrDefault();
                WAQPath pathToAttackingMe = ToolBox.GetWAQPath(myPos, _defendTarget.Position);

                if (_defendTarget != null && pathToAttackingMe.IsReachable)
                {
                    MountTask.DismountMount(true, wait: 0);
                    return true;
                }

                return false;
            }
        }
    }
}