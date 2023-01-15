using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Bot.TravelManagement;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    public class WAQStateTravel : State, IWAQState
    {
        private readonly IContinentManager _continentManager;
        private readonly ITaskManager _taskManager;
        private readonly TravelManager _travelManager;

        public override string DisplayName { get; set; } = "WAQ Travel";

        public WAQStateTravel(
            ITaskManager taskManager, 
            TravelManager travelManager,
            IContinentManager continentManager)
        {
            _travelManager = travelManager;
            _taskManager = taskManager;
            _continentManager = continentManager;
        }

        public override bool NeedToRun
        {
            get
            {
                if (!Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause
                    || !ObjectManager.Me.IsValid
                    || MoveHelper.IsMovementThreadRunning
                    || ObjectManager.Me.IsOnTaxi
                    || ObjectManager.Me.InCombat
                    || !_travelManager.TravelInProgress)
                {
                    return false;
                }

                DisplayName = $"Traveling ({_continentManager.MyMapArea.areaName} to {_taskManager.ActiveTask.WorldMapArea.areaName})";
                return true;
            }
        }

        public override void Run()
        {
            MoveHelper.StopAllMove(true);
            IWAQTask task = _taskManager.ActiveTask;
            ModelWorldMapArea myArea = _continentManager.MyMapArea;
            ModelWorldMapArea destinationArea = task.WorldMapArea;
            WAQContinent destinationContinent = destinationArea.Continent;
            WAQContinent myContinent = myArea.Continent;

            // ------------------ HORDE ------------------
            if (WTPlayer.IsHorde())
            {
                // From B11 starting zone
                if (myContinent == WAQContinent.BloodElfStartingZone)
                {
                    WTTravel.PortalFromSilvermoonToTirisfal();
                }
                // From EK
                if (myContinent == WAQContinent.EasternKingdoms)
                {
                    // To Kalimdor
                    if (destinationContinent == WAQContinent.Kalimdor)
                    {
                        if (ObjectManager.Me.Position.X > -2384) // above wetlands)
                        {
                            WTTravel.ZeppelinTirisfalToOrgrimmar();
                        }
                        else
                        {
                            WTTravel.ShipBootyBayToRatchet();
                        }
                    }
                    if (destinationContinent == WAQContinent.EasternKingdoms)
                    {
                        // To EK south
                        if (_travelManager.ShouldTravelFromNorthEKToSouthEk(task))
                        {
                            WTTravel.ZeppelingTirisfalToStrangelthorn();
                        }
                        // To EK north
                        if (_travelManager.ShouldTravelFromSouthEKToNorthEK(task))
                        {
                            WTTravel.ZeppelingStrangelthornToTirisfal();
                        }
                    }
                    // To Outlands
                    if (destinationContinent == WAQContinent.Outlands)
                    {
                        WTTravel.PortalBlastedLandsToOutlands();
                    }
                    // To Northrend
                    if (destinationContinent == WAQContinent.Northrend)
                    {
                        // To Northrend
                        if (destinationContinent == WAQContinent.Northrend)
                        {
                            if (task.Location.Y > 265)
                            {
                                WTTravel.ZeppelinTirisfalToOrgrimmar();
                            }
                            else
                            {
                                WTTravel.ZeppelinTirisfalToHowlingFjord();
                            }
                        }
                    }
                }
                // From Kalimdor
                if (myContinent == WAQContinent.Kalimdor)
                {
                    // To EK
                    if (destinationContinent == WAQContinent.EasternKingdoms)
                    {
                        if (task.Location.X < -3240 && ObjectManager.Me.Level >= 58)
                        {
                            WTTravel.PortalFromOrgrimmarToBlastedLands();
                        }
                        else
                        {
                            if (task.Location.X > -2384) // above wetlands
                            {
                                WTTravel.ZeppelinOrgrimmarToTirisfal();
                            }
                            else
                            {
                                WTTravel.ShipRatchetToBootyBay();
                            }
                        }
                    }
                    // To Outlands
                    if (destinationContinent == WAQContinent.Outlands)
                    {
                        WTTravel.PortalFromOrgrimmarToBlastedLands();
                    }
                    // To Northrend
                    if (destinationContinent == WAQContinent.Northrend)
                    {
                        if (task.Location.Y > 265)
                        {
                            WTTravel.ZeppelinOrgrimmarToBoreanTundra();
                        }
                        else
                        {
                            WTTravel.ZeppelinOrgrimmarToTirisfal();
                        }
                    }
                }
                // From Outlands
                if (myContinent == WAQContinent.Outlands)
                {
                    WTTravel.PortalShattrathToOrgrimmar();
                }

                // From Northrend
                if (myContinent == WAQContinent.Northrend)
                {
                    // To Kalimdor
                    if (destinationContinent == WAQContinent.Kalimdor)
                    {
                        WTTravel.PortalDalaranToOrgrimmar();
                    }
                    // To EK
                    if (destinationContinent == WAQContinent.EasternKingdoms)
                    {
                        WTTravel.PortalDalaranToUndercity();
                    }
                    // To Outland
                    if (destinationContinent == WAQContinent.Outlands)
                    {
                        WTTravel.HordePortalDalaranToShattrath();
                    }
                }
            }
            else
            // ------------------ ALLIANCE ------------------
            {
                // From Teldrassil
                if (myContinent == WAQContinent.Teldrassil)
                {
                    if (_travelManager.ShouldTakePortalDarnassusToRutTheran(task))
                    {
                        WTTravel.PortalDarnassusToRutTheran();
                    }
                    else if (_travelManager.ShouldTakePortalRutTheranToDarnassus(task))
                    {
                        WTTravel.PortalRutTheranToDarnassus();
                    }
                    else
                    {
                        WTTravel.ShipRutTheranToDarkshore();
                    }

                }
                // From Azuremyst
                if (myContinent == WAQContinent.DraeneiStartingZone)
                {
                    WTTravel.ShipAzuremystToDarkshore();
                }
                // From Outlands
                if (myContinent == WAQContinent.Outlands)
                {
                    WTTravel.PortalShattrathToIronforge();
                }
                // From Kalimdor
                if (myContinent == WAQContinent.Kalimdor)
                {
                    // To Azuremyst
                    if (destinationContinent == WAQContinent.DraeneiStartingZone)
                    {
                        WTTravel.ShipDarkshoreToAzuremyst();
                    }
                    // To Northrend
                    if (destinationContinent == WAQContinent.Northrend)
                    {
                        WTTravel.ShipDustwallowToMenethil();
                    }
                    // To Teldrassil
                    if (destinationContinent == WAQContinent.Teldrassil)
                    {
                        WTTravel.ShipDarkShoreToRutTheran();
                    }
                    // To EK/Outlands
                    if (destinationContinent == WAQContinent.EasternKingdoms || destinationContinent == WAQContinent.Outlands)
                    {
                        if (ObjectManager.Me.Level < 40)
                        {
                            WTTravel.ShipDarkshoreToStormwind();
                        }
                        else
                        {
                            WTTravel.ShipDustwallowToMenethil();
                        }
                    }
                }
                // From Deeprun Tram
                if (myContinent == WAQContinent.DeeprunTram)
                {
                    if (task.Location.X >= -8118 && destinationContinent != WAQContinent.Kalimdor) // above burning steppes
                    {
                        if (ObjectManager.Me.Position.Y > 1200)
                        {
                            WTTravel.TakeTramFromStormwindToIronforge();
                        }
                        else
                        {
                            WTTravel.ExitDeeprunTramToIronforge();
                        }
                    }
                    else // under burning steppes
                    {
                        if (ObjectManager.Me.Position.Y > 1200)
                        {
                            WTTravel.ExitDeeprunTramToStormwind();
                        }
                        else
                        {
                            WTTravel.TakeTramFromIronforgeToStormwind();
                        }
                    }
                }
                // From EK
                if (myContinent == WAQContinent.EasternKingdoms)
                {
                    // To Northrend
                    if (destinationContinent == WAQContinent.Northrend)
                    {
                        if (task.Location.Y > 265)
                        {
                            WTTravel.ShipStormwindToBoreanTundra();
                        }
                        else
                        {
                            WTTravel.ShipMenethilToHowlingFjord();
                        }
                    }
                    // To Outlands
                    if (destinationContinent == WAQContinent.Outlands)
                    {
                        WTTravel.PortalBlastedLandsToOutlands();
                    }
                    // North/South
                    if (destinationContinent == WAQContinent.EasternKingdoms)
                    {
                        if (_travelManager.ShouldTravelFromNorthEKToSouthEk(task))
                        {
                            WTTravel.EnterIronForgedDeeprunTram();
                        }
                        if (_travelManager.ShouldTravelFromSouthEKToNorthEK(task))
                        {
                            WTTravel.EnterStormwindDeeprunTram();
                        }
                    }
                    // To Kalimdor/Teldrassil
                    if (destinationContinent == WAQContinent.Kalimdor || destinationContinent == WAQContinent.Teldrassil)
                    {
                        if (ObjectManager.Me.Level >= 40)
                        {
                            WTTravel.ShipMenethilToDustwallow();
                        }
                        else
                        {
                            if (myContinent == WAQContinent.DeeprunTram)
                            {
                                WTTravel.TakeTramFromIronforgeToStormwind();
                            }
                            else if (ObjectManager.Me.Position.X > -8118)
                            {
                                WTTravel.EnterIronForgedDeeprunTram();
                            }
                            else
                            {
                                WTTravel.ShipStormwindToDarkshore();
                            }
                        }
                    }
                    // To Azuremyst
                    if (destinationContinent == WAQContinent.DraeneiStartingZone)
                    {
                        WTTravel.ShipStormwindToDarkshore();
                    }
                }
            }

            Logger.Log($"Resetting traveler");
            if (!_travelManager.IsTravelRequired(task))
            {
                _travelManager.ResetTravel();
            }
        }
    }
}
