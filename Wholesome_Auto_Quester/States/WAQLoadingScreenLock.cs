using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Bot.TravelManagement;
using wManager.Wow.Helpers;

namespace WholesomeDungeonCrawler.States
{
    class WAQLoadingScreenLock : State
    {
        public override string DisplayName => "Loading screen lock";
        ITravelManager _travelManager;
        private Timer _lockTimer = null;

        public WAQLoadingScreenLock(ITravelManager travelManager)
        {
            _travelManager = travelManager;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnected)
                {
                    return false;
                }

                return _travelManager.InLoadingScreen;
            }
        }

        public override void Run()
        {
            MovementManager.StopMove();

            if (_lockTimer == null)
            {
                _lockTimer = new Timer(5000);
            }

            if (_lockTimer.IsReady)
            {
                _travelManager.ResetLoadingScreenLock();
                _lockTimer = null;
            }
        }
    }
}
