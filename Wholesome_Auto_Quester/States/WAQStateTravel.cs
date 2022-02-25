using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Bot.TravelManagement;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    public class WAQStateTravel : State, IWAQState
    {
        private readonly ITaskManager _taskManager;
        private readonly TravelManager _travelManager;

        public override string DisplayName { get; set; } = "WAQ Travel";

        public WAQStateTravel(ITaskManager taskManager, TravelManager travelManager, int priority)
        {
            _travelManager = travelManager;
            _taskManager = taskManager;
            Priority = priority;
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

                DisplayName = $"Traveling ({ContinentHelper.MyMapArea.areaName} to {_taskManager.ActiveTask.WorldMapArea.areaName})";
                return true;
            }
        }

        public override void Run()
        {
            MoveHelper.StopAllMove(true);
            IWAQTask task = _taskManager.ActiveTask;
            ModelWorldMapArea myArea = ContinentHelper.MyMapArea;
            ModelWorldMapArea destinationArea = task.WorldMapArea;
            WAQContinent destinationContinent = destinationArea.Continent;
            WAQContinent myContinent = myArea.Continent;

            // HORDE
            if (ToolBox.IsHorde())
            {
                // From B11 starting zone
                if (myContinent == WAQContinent.BloodElfStartingZone)
                {
                    _travelManager.PortalFromSilvermoonToTirisfal();
                }

                // From EK
                if (myContinent == WAQContinent.EasternKingdoms)
                {
                    // To Kalimdor
                    if (destinationContinent == WAQContinent.Kalimdor)
                    {
                        if (ObjectManager.Me.Position.X > -2384) // above wetlands)
                        {
                            _travelManager.ZeppelinTirisfalToOrgrimmar();
                        }
                        else
                        {
                            _travelManager.ShipBootyBayToRatchet();
                        }
                    }
                    if (destinationContinent == WAQContinent.EasternKingdoms)
                    {
                        // To EK south
                        if (_travelManager.ShouldTakeZeppelinTirisfalToStranglethorn(task))
                        {
                            _travelManager.ZeppelingTirisfalToStrangelthorn();
                        }
                        // To EK north
                        if (_travelManager.ShouldTakeZeppelinStranglethornToTirisfal(task))
                        {
                            _travelManager.ZeppelingStrangelthornToTirisfal();
                        }
                    }
                    // To Outlands
                    if (destinationContinent == WAQContinent.Outlands)
                    {
                        _travelManager.PortalBlastedLandsToOutlands();
                    }
                    // To Northrend
                    if (destinationContinent == WAQContinent.Northrend)
                    {
                        // To Northrend
                        if (destinationContinent == WAQContinent.Northrend)
                        {
                            if (task.Location.Y > 265)
                            {
                                _travelManager.ZeppelinTirisfalToOrgrimmar();
                            }
                            else
                            {
                                _travelManager.ZeppelinTirisfalToHowlingFjord();
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
                            _travelManager.PortalFromOrgrimmarToBlastedLands();
                        }
                        else
                        {
                            if (task.Location.X > -2384) // above wetlands
                            {
                                _travelManager.ZeppelinOrgrimmarToTirisfal();
                            }
                            else
                            {
                                _travelManager.ShipRatchetToBootyBay();
                            }
                        }
                    }
                    // To Outlands
                    if (destinationContinent == WAQContinent.Outlands)
                    {
                        _travelManager.PortalFromOrgrimmarToBlastedLands();
                    }
                    // To Northrend
                    if (destinationContinent == WAQContinent.Northrend)
                    {
                        if (task.Location.Y > 265)
                        {
                            _travelManager.ZeppelinOrgrimmarToBoreanTundra();
                        }
                        else
                        {
                            _travelManager.ZeppelinOrgrimmarToTirisfal();
                        }
                    }
                }

                // From Outlands
                if (myContinent == WAQContinent.Outlands)
                {
                    _travelManager.PortalShattrathToOrgrimmar();
                }

                // From Northrend
                if (myContinent == WAQContinent.Northrend)
                {
                    // To Kalimdor
                    if (destinationContinent == WAQContinent.Kalimdor)
                    {
                        _travelManager.PortalDalaranToOrgrimmar();
                    }
                    // To EK
                    if (destinationContinent == WAQContinent.EasternKingdoms)
                    {
                        _travelManager.PortalDalaranToUndercity();
                    }
                    // To Outland
                    if (destinationContinent == WAQContinent.Outlands)
                    {
                        _travelManager.HordePortalDalaranToShattrath();
                    }
                }
            }

            Logger.Log($"RESET TRAVELER");
            if (!_travelManager.IsTravelRequired(task))
            {
                _travelManager.ResetTravel();
            }
        }
    }
}
