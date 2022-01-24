using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    public class WAQTravel : State
    {
        public override string DisplayName
        {
            get { return "WAQ Travel  [SmoothMove - Q]"; }
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause 
                    || !ObjectManager.Me.IsValid)
                    return false;

                if (WAQTasks.TaskInProgress != null && WAQTasks.TaskInProgress.Continent != Usefuls.ContinentId)
                    return true;
                                
                return false;
            }
        }

        public override void Run()
        {
            ContinentId destinationContinent = (ContinentId)WAQTasks.TaskInProgress.Continent;

            // HORDE
            if (ToolBox.IsHorde())
            {
                // From EK
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Azeroth)
                {
                    // To Kalimdor
                    if (destinationContinent == ContinentId.Kalimdor)
                    {
                        Logger.Log("Traveling to Kalimdor");
                        TravelHelper.HordeEKToKalimdor();
                    }
                    // To Outlands
                    if (destinationContinent == ContinentId.Expansion01)
                    {
                        Logger.Log("Traveling to Outland");
                        TravelHelper.HordeEKToOutland();
                    }
                    // To Northrend
                    if (destinationContinent == ContinentId.Northrend)
                    {
                        Logger.Log("Traveling to Northrend");
                        TravelHelper.HordeEKToKalimdor();
                    }
                }

                // From Kalimdor
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Kalimdor)
                {
                    // To EK
                    if (destinationContinent == ContinentId.Azeroth)
                    {
                        Logger.Log("Traveling to Eastern Kingdoms");
                        TravelHelper.HordeKalimdorToEK();
                    }
                    // To Outlands
                    if (destinationContinent == ContinentId.Expansion01)
                    {
                        Logger.Log("Traveling to Outland");
                        TravelHelper.HordeKalimdorToEK();
                    }
                    // To Northrend
                    if (destinationContinent == ContinentId.Northrend)
                    {
                        Logger.Log("Traveling to Northrend");
                        TravelHelper.HordeKalimdorToNorthrend();
                    }
                }

                // From Outlands
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Expansion01)
                {
                    // To Kalimdor
                    if (destinationContinent == ContinentId.Kalimdor)
                    {
                        Logger.Log("Traveling to Kalimdor");
                        TravelHelper.HordeOutlandToKalimdor();
                    }
                    // To EK
                    if (destinationContinent == ContinentId.Azeroth)
                    {
                        Logger.Log("Traveling to Eastern Kingdoms");
                        TravelHelper.HordeOutlandToKalimdor();
                    }
                    // To Northrend
                    if (destinationContinent == ContinentId.Northrend)
                    {
                        Logger.Log("Traveling to Northrend");
                        TravelHelper.HordeOutlandToKalimdor();
                    }
                }

                // From Northrend
                if ((ContinentId)Usefuls.ContinentId == ContinentId.Northrend)
                {
                    // To Kalimdor
                    if (destinationContinent == ContinentId.Kalimdor)
                    {
                        Logger.Log("Traveling to Kalimdor");
                        TravelHelper.HordeNorthrendToKalimdor();
                    }
                    // To EK
                    if (destinationContinent == ContinentId.Azeroth)
                    {
                        Logger.Log("Traveling to Eastern Kingdoms");
                        TravelHelper.HordeNorthrendToEK();
                    }
                    // To Outland
                    if (destinationContinent == ContinentId.Expansion01)
                    {
                        Logger.Log("Traveling to Outland");
                        TravelHelper.HordeNorthrendToOutland();
                    }
                }
            }
        }
    }
}
