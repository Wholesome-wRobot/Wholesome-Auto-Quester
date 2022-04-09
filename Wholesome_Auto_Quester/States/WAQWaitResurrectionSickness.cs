using robotManager.FiniteStateMachine;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQWaitResurrectionSickness : State, IWAQState
    {
        public override string DisplayName { get; set; } = "WAQ Wait resurrection sickness";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid)
                    return false;

                return WTEffects.HasDebuff("Resurrection Sickness", loops: 3);
            }
        }

        public override void Run()
        {
            Thread.Sleep(1000);
            if (ItemsManager.HasItemById(6948) && WTItem.GetItemCooldown(6948) <= 0)
            {
                ItemsManager.UseItem(6948);
            }
            Usefuls.WaitIsCasting();
        }
    }
}
