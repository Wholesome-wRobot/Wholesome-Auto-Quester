using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TravelManagement;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    public class WAQStateTravel : State
    {
        private readonly ITaskManager _taskManager;
        private readonly TravelManager _travelManager;
        private ModelWorldMapArea _travelDestinationWMArea;
        private ModelWorldMapArea _myWMArea;
        private Vector3 _travelDestinationPosition;

        public override string DisplayName { get; set; } = "WAQ Travel";
        public static bool InTravel { get; private set; } = false;

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
                    || _taskManager.ActiveTask == null)
                    return false;

                if (_travelManager.NeedToTravelTo(_taskManager.ActiveTask, out (ModelWorldMapArea myWma, ModelWorldMapArea destWma) travel))
                {
                    _myWMArea = travel.myWma;
                    _travelDestinationWMArea = travel.destWma;
                    _travelDestinationPosition = _taskManager.ActiveTask.Location;
                    DisplayName = $"Travel to {_travelDestinationWMArea.areaName}";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            InTravel = true;
            MoveHelper.StopAllMove(true);
            // HORDE
            if (ToolBox.IsHorde())
            {
                // From EK
                if (_myWMArea.Continent == WAQContinent.EasternKingdoms)
                {
                    // To Kalimdor
                    if (_travelDestinationWMArea.Continent == WAQContinent.Kalimdor)
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
                    if (_travelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        // To EK south
                        if (_travelManager.ShouldTakeZeppelinTirisfalToStranglethorn(_myWMArea, _taskManager.ActiveTask))
                        {
                            _travelManager.ZeppelingTirisfalToStrangelthorn();
                        }
                        // To EK north
                        if (_travelManager.ShouldTakeZeppelinStranglethornToTirisfal(_myWMArea, _taskManager.ActiveTask))
                        {
                            _travelManager.ZeppelingStrangelthornToTirisfal();
                        }
                    }
                    // To Outlands
                    if (_travelDestinationWMArea.Continent == WAQContinent.Outlands)
                    {
                        _travelManager.PortalBlastedLandsToOutlands();
                    }
                    // To Northrend
                    if (_travelDestinationWMArea.Continent == WAQContinent.Northrend)
                    {
                        _travelManager.ZeppelinTirisfalToOrgrimmar();
                    }
                }

                // From Kalimdor
                if (_myWMArea.Continent == WAQContinent.Kalimdor)
                {
                    // To EK
                    if (_travelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        if (_travelDestinationPosition.X < -3240 && ObjectManager.Me.Level >= 58)
                        {
                            _travelManager.PortalFromOrgrimmarToBlastedLands();
                        }
                        else
                        {
                            if (_travelDestinationPosition.X > -2384) // above wetlands
                            {
                                _travelManager.ZeppelinKalimdorToTirisfal();
                            }
                            else
                            {
                                _travelManager.ShipRatchetToBootyBay();
                            }
                        }
                    }
                    // To Outlands
                    if (_travelDestinationWMArea.Continent == WAQContinent.Outlands)
                    {
                        _travelManager.PortalFromOrgrimmarToBlastedLands();
                    }
                    // To Northrend
                    if (_travelDestinationWMArea.Continent == WAQContinent.Northrend)
                    {
                        _travelManager.ZeppelinOrgrimmarToNorthrend();
                    }
                }

                // From Outlands
                if (_myWMArea.Continent == WAQContinent.Outlands)
                {
                    // To Kalimdor
                    if (_travelDestinationWMArea.Continent == WAQContinent.Kalimdor)
                    {
                        _travelManager.PortalShattrathToOrgrimmar();
                    }
                    // To EK
                    if (_travelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        _travelManager.PortalShattrathToOrgrimmar();
                    }
                    // To Northrend
                    if (_travelDestinationWMArea.Continent == WAQContinent.Northrend)
                    {
                        _travelManager.PortalShattrathToOrgrimmar();
                    }
                }

                // From Northrend
                if (_myWMArea.Continent == WAQContinent.Northrend)
                {
                    // To Kalimdor
                    if (_travelDestinationWMArea.Continent == WAQContinent.Kalimdor)
                    {
                        _travelManager.PortalDalaranToOrgrimmar();
                    }
                    // To EK
                    if (_travelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        _travelManager.PortalDalaranToUndercity();
                    }
                    // To Outland
                    if (_travelDestinationWMArea.Continent == WAQContinent.Outlands)
                    {
                        _travelManager.HordePortalDalaranToShattrath();
                    }
                }
            }

            Logger.Log($"RESET TRAVELER");
            MoveHelper.StopAllMove(true);
            InTravel = false;
            _travelDestinationPosition = null;
            _travelDestinationWMArea = null;
            _myWMArea = null;
        }
    }
}
