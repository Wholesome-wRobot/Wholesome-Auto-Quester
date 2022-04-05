using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    internal class WAQBlacklistDanger : State, IWAQState
    {
        public override string DisplayName { get; set; } = "WAQ Blacklist Danger";
        private Vector3 _zoneToBlackList;

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || ObjectManager.Me.InCombatFlagOnly
                    || Fight.InFight
                    || ObjectManager.Me.IsOnTaxi)
                    return false;

                _zoneToBlackList = null;
                List<WoWUnit> hostiles = ToolBox.GetListObjManagerHostiles();

                foreach (WoWUnit hostile in hostiles)
                {
                    if (!wManagerSetting.IsBlackListedZone(hostile.Position))
                    {
                        _zoneToBlackList = hostile.Position;
                    }
                }

                BlacklistHelper.CleanupBlacklist();

                return _zoneToBlackList != null;
            }
        }
        public override void Run()
        {
            BlacklistHelper.AddZone(_zoneToBlackList, 10, "Dangerous zone", 1000);
        }
    }
}
