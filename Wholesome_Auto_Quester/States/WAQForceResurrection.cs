using robotManager.FiniteStateMachine;
using System.Collections.Generic;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    class WAQForceResurrection : State, IWAQState
    {
        public override string DisplayName { get; set; } = "WAQ Force Spirit Resurrection";

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || ObjectManager.Me.IsAlive)
                    return false;

                return ToolBox.GetAvergaeDurability() <= 8;
            }
        }

        public override void Run()
        {
            Thread.Sleep(1000);

            if (!ToolBox.HasDebuff("Ghost"))
            {
                Lua.LuaDoString(@$"
                    if GetClickFrame('StaticPopup1Button1'):IsVisible() then
                        StaticPopup1Button1:Click();
                    end
                ");
            }
            else
            {
                List<WoWUnit> units = ObjectManager.GetObjectWoWUnit();
                WoWUnit spiritHealer = units.Find(u => u.Name == "Spirit Healer");
                if (spiritHealer != null)
                {
                    if (spiritHealer.GetDistance > 5)
                    {
                        if (!MoveHelper.IsMovementThreadRunning)
                        {
                            MoveHelper.StartGoToThread(spiritHealer.Position, $"Moving to Spirit Healer");
                        }
                    }
                    else
                    {
                        MoveHelper.StopAllMove(true);
                        Interact.InteractGameObject(spiritHealer.GetBaseAddress);
                        for (int i = 0; i < 2; i++)
                        {
                            Thread.Sleep(1000);
                            Lua.LuaDoString(@$"
                                if GetClickFrame('StaticPopup1Button1'):IsVisible() then
                                    StaticPopup1Button1:Click();
                                end
                            ");
                        }
                    }
                }
                else
                {
                    Logger.LogError("Couldn't find Spirit Healer");
                }
            }
        }
    }
}
