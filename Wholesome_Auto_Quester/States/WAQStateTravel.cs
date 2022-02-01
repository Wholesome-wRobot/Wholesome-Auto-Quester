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
        private ITaskManager _taskManager;
        private TravelManager _travelManager;

        public WAQStateTravel(ITaskManager taskManager, TravelManager travelManager, int priority)
        {
            _travelManager = travelManager;
            _taskManager = taskManager;
            Priority = priority;
        }
        public override string DisplayName { get; set; } = "WAQ Travel [SmoothMove - Q]";

        private ModelWorldMapArea TravelDestinationWMArea;
        private ModelWorldMapArea MyWMArea;
        private Vector3 TravelDestinationPosition;
        public static bool InTravel = false;

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
                    MyWMArea = travel.myWma;
                    TravelDestinationWMArea = travel.destWma;
                    TravelDestinationPosition = _taskManager.ActiveTask.Location;
                    DisplayName = $"WAQ Travel to {TravelDestinationWMArea.areaName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            ModelWorldMapArea myCurrentWMArea = _travelManager.GetWorldMapAreaFromPoint(ObjectManager.Me.Position, Usefuls.ContinentId);
            InTravel = true;
            MoveHelper.StopAllMove(true);
            // HORDE
            if (ToolBox.IsHorde())
            {
                // From EK
                if (myCurrentWMArea.Continent == WAQContinent.EasternKingdoms)
                {
                    // To Kalimdor
                    if (TravelDestinationWMArea.Continent == WAQContinent.Kalimdor)
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
                    if (TravelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        // To EK south
                        if (_travelManager.ShouldTakeZeppelinTirisfalToStranglethorn(myCurrentWMArea, _taskManager.ActiveTask))
                        {
                            _travelManager.ZeppelingTirisfalToStrangelthorn();
                        }
                        // To EK north
                        if (_travelManager.ShouldTakeZeppelinStranglethornToTirisfal(myCurrentWMArea, _taskManager.ActiveTask))
                        {
                            _travelManager.ZeppelingStrangelthornToTirisfal();
                        }
                    }
                    // To Outlands
                    if (TravelDestinationWMArea.Continent == WAQContinent.Outlands)
                    {
                        _travelManager.PortalBlastedLandsToOutlands();
                    }
                    // To Northrend
                    if (TravelDestinationWMArea.Continent == WAQContinent.Northrend)
                    {
                        _travelManager.ZeppelinTirisfalToOrgrimmar();
                    }
                }

                // From Kalimdor
                if (myCurrentWMArea.Continent == WAQContinent.Kalimdor)
                {
                    // To EK
                    if (TravelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        if (TravelDestinationPosition.X < -3240 && ObjectManager.Me.Level >= 58)
                        {
                            _travelManager.PortalFromOrgrimmarToBlastedLands();
                        }
                        else
                        {
                            if (TravelDestinationPosition.X > -2384) // above wetlands
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
                    if (TravelDestinationWMArea.Continent == WAQContinent.Outlands)
                    {
                        _travelManager.PortalFromOrgrimmarToBlastedLands();
                    }
                    // To Northrend
                    if (TravelDestinationWMArea.Continent == WAQContinent.Northrend)
                    {
                        _travelManager.ZeppelinOrgrimmarToNorthrend();
                    }
                }

                // From Outlands
                if (myCurrentWMArea.Continent == WAQContinent.Outlands)
                {
                    // To Kalimdor
                    if (TravelDestinationWMArea.Continent == WAQContinent.Kalimdor)
                    {
                        _travelManager.PortalShattrathToOrgrimmar();
                    }
                    // To EK
                    if (TravelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        _travelManager.PortalShattrathToOrgrimmar();
                    }
                    // To Northrend
                    if (TravelDestinationWMArea.Continent == WAQContinent.Northrend)
                    {
                        _travelManager.PortalShattrathToOrgrimmar();
                    }
                }

                // From Northrend
                if (myCurrentWMArea.Continent == WAQContinent.Northrend)
                {
                    // To Kalimdor
                    if (TravelDestinationWMArea.Continent == WAQContinent.Kalimdor)
                    {
                        _travelManager.PortalDalaranToOrgrimmar();
                    }
                    // To EK
                    if (TravelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        _travelManager.PortalDalaranToUndercity();
                    }
                    // To Outland
                    if (TravelDestinationWMArea.Continent == WAQContinent.Outlands)
                    {
                        _travelManager.HordePortalDalaranToShattrath();
                    }
                }
            }

            Logger.Log($"RESET TRAVELER");
            MoveHelper.StopAllMove(true);
            InTravel = false;
            TravelDestinationPosition = null;
            TravelDestinationWMArea = null;
            MyWMArea = null;
        }
    }
}
