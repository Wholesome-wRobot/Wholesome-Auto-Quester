using System;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.States;
using wManager.Wow.Bot.States;
using wManager.Wow.Helpers;

namespace Wholesome_Auto_Quester.Bot
{
    internal static class Bot
    {
        private static readonly Engine Fsm = new Engine();

        internal static bool Pulse()
        {
            try
            {
                // Attach onlevelup for spell book:
                EventsLua.AttachEventLua("PLAYER_LEVEL_UP", m => OnLevelUp());
                EventsLua.AttachEventLua("PLAYER_ENTERING_WORLD", m => ScreenReloaded());

                // Update spell list
                SpellManager.UpdateSpellBook();

                // Load CC:
                CustomClass.LoadCustomClass();

                // FSM
                Fsm.States.Clear();

                Fsm.AddState(new Relogger { Priority = 200 });
                Fsm.AddState(new Pause { Priority = 33 });
                Fsm.AddState(new Resurrect { Priority = 32 });
                Fsm.AddState(new MyMacro { Priority = 31 });
                Fsm.AddState(new IsAttacked { Priority = 30 });
                Fsm.AddState(new Regeneration { Priority = 29 });
                Fsm.AddState(new Looting { Priority = 28 });
                Fsm.AddState(new FlightMasterTakeTaxiState { Priority = 27 });
                Fsm.AddState(new Farming { Priority = 26 });
                Fsm.AddState(new Trainers { Priority = 25 });
                Fsm.AddState(new ToTown { Priority = 25 });

                // WAQ tasks
                Fsm.AddState(new WAQGoTo { Priority = 15 });
                Fsm.AddState(new WAQKill { Priority = 14 });
                Fsm.AddState(new WAQKillAndLoot { Priority = 13 });
                Fsm.AddState(new WAQPickupWorldObject { Priority = 12 });
                Fsm.AddState(new WAQPickupQuest { Priority = 11 });
                Fsm.AddState(new WAQTurnInQuest { Priority = 10 });

                Fsm.AddState(new Grinding { Priority = 2 });
                Fsm.AddState(new MovementLoop { Priority = 1 });

                Fsm.AddState(new Idle { Priority = 0 });

                Fsm.States.Sort();
                Fsm.StartEngine(10, "_AutoQuester");

                StopBotIf.LaunchNewThread();

                return true;
            }
            catch (Exception e)
            {
                try
                {
                    Dispose();
                }
                catch
                {
                }
                Logging.WriteError("Bot > Bot  > Pulse(): " + e);
                return false;
            }
        }

        internal static void Dispose()
        {
            try
            {
                CustomClass.DisposeCustomClass();
                Fsm.StopEngine();
                Fight.StopFight();
                MovementManager.StopMove();
            }
            catch (Exception e)
            {
                Logging.WriteError("Bot > Bot  > Dispose(): " + e);
            }
        }

        private static void OnLevelUp()
        {
            Logging.Write("Level UP! Reload Fight Class.");
            SpellManager.UpdateSpellBook();
            CustomClass.ResetCustomClass();

            if (ToolBox.GetWoWVersion() == "3.3.5")
            {
                DBQueriesWotlk dbWotlk = new DBQueriesWotlk();
                dbWotlk.GetAvailableQuests();
            }
        }

        private static void ScreenReloaded()
        {
        }
    }
}