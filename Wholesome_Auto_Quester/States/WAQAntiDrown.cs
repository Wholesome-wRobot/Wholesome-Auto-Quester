using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    internal class WAQAntiDrown : State, IWAQState
    {
        public override string DisplayName { get; set; } = "WAQ Anti Drown";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || ObjectManager.Me.BreathTimerLeft <= 0
                    || ObjectManager.Me.BreathTimerLeft > 40000)
                {
                    return false;
                }

                return true;
            }
        }

        public override void Run()
        {
            Logger.Log("[WAQ Anti Drown] Resurfacing");
            MovementManager.StopMove();
            while (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                && ObjectManager.Me.BreathTimerLeft > 0)
            {
                Lua.LuaDoString($"JumpOrAscendStart()");
                Thread.Sleep(1000);
            }
        }
    }
}
