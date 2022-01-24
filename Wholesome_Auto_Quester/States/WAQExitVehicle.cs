using robotManager.FiniteStateMachine;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using Timer = robotManager.Helpful.Timer;

namespace Wholesome_Auto_Quester.States
{
    class WAQExitVehicle : State
    {
        public override string DisplayName { get; set; } = "Exit Vehicle [SmoothMove - Q]";
        private Timer stateTimer = new Timer();

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || !stateTimer.IsReady)
                    return false;

                stateTimer = new Timer(3000);

                if (ObjectManager.Me.PlayerUsingVehicle)
                {
                    DisplayName = $"Exit vehicle [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            Thread.Sleep(1000);
            Logger.Log($"Exiting vehicle");
            Lua.LuaDoString("VehicleExit()");
        }
    }
}
