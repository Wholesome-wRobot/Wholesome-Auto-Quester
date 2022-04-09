using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using WholesomeToolbox;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQForceResurrection : State, IWAQState
    {
        public override string DisplayName { get; set; } = "WAQ Force Spirit Resurrection";
        private WoWUnit _spiritHealer;

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || ObjectManager.Me.IsAlive)
                    return false;

                _spiritHealer = null;

                if (WTGear.GetAvergaeDurability() <= 5)
                {
                    if (!WTEffects.HasDebuff("Ghost"))
                    {
                        WTLua.ClickOnFrameButton("StaticPopup1Button1");
                    }
                    else
                    {
                        List<WoWUnit> units = ObjectManager.GetObjectWoWUnit();
                        _spiritHealer = units.Find(u => u.Name == "Spirit Healer");
                    }
                }

                return _spiritHealer != null;
            }
        }

        public override void Run()
        {
            Thread.Sleep(1000);
            if (_spiritHealer.GetDistance > 5)
            {
                if (!MoveHelper.IsMovementThreadRunning)
                {
                    MoveHelper.StartGoToThread(_spiritHealer.Position, $"Moving to Spirit Healer");
                }
            }
            else
            {
                MoveHelper.StopAllMove(true);
                Interact.InteractGameObject(_spiritHealer.GetBaseAddress);
                for (int i = 0; i < 2; i++)
                {
                    Thread.Sleep(1000);
                    WTLua.ClickOnFrameButton("StaticPopup1Button1");
                }
            }
        }
    }
}
