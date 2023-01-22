using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System.Collections.Generic;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
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
                        WTGossip.ClickOnFrameButton("StaticPopup1Button1");
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
                if (!MovementManager.InMovement)
                {
                    List<Vector3> pathToSpiritHealer = PathFinder.FindPath(_spiritHealer.Position);
                    Logger.Log($"Moving to Spirit Healer");
                    MovementManager.Go(pathToSpiritHealer);
                }
            }
            else
            {
                MovementManager.StopMove();
                Interact.InteractGameObject(_spiritHealer.GetBaseAddress);
                for (int i = 0; i < 2; i++)
                {
                    Thread.Sleep(1000);
                    WTGossip.ClickOnFrameButton("StaticPopup1Button1");
                }
            }
        }
    }
}
