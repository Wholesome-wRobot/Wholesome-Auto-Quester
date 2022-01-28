using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database.Models;
using Wholesome_Auto_Quester.Helpers;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.States
{
    public class WAQTravel : State
    {
        public override string DisplayName { get; set; } = "WAQ Travel [SmoothMove - Q]";

        private ModelWorldMapArea TravelDestinationWMArea;
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
                    || WAQTasks.TaskInProgress == null)
                    return false;

                if (TravelHelper.NeedToTravelTo(WAQTasks.TaskInProgress))
                {
                    TravelDestinationWMArea = WAQTasks.DestinationWMArea;
                    TravelDestinationPosition = WAQTasks.TaskInProgress.Location;
                    DisplayName = $"WAQ Travel to {TravelDestinationWMArea.areaName} [SmoothMove - Q]";
                    return true;
                }

                return false;
            }
        }

        public override void Run()
        {
            ModelWorldMapArea myCurrentWMArea = TravelHelper.GetWorldMapAreaFromPoint(ObjectManager.Me.Position, Usefuls.ContinentId);
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
                            TravelHelper.ZeppelinTirisfalToOrgrimmar();
                        }
                        else
                        {
                            TravelHelper.ShipBootyBayToRatchet();
                        }
                    }
                    if (TravelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        // To EK south
                        if (TravelHelper.ShouldTakeZeppelinTirisfalToStranglethorn(myCurrentWMArea, WAQTasks.TaskInProgress))
                        {
                            TravelHelper.ZeppelingTirisfalToStrangelthorn();
                        }
                        // To EK north
                        if (TravelHelper.ShouldTakeZeppelinStranglethornToTirisfal(myCurrentWMArea, WAQTasks.TaskInProgress))
                        {
                            TravelHelper.ZeppelingStrangelthornToTirisfal();
                        }
                    }
                    // To Outlands
                    if (TravelDestinationWMArea.Continent == WAQContinent.Outlands)
                    {
                        TravelHelper.PortalBlastedLandsToOutlands();
                    }
                    // To Northrend
                    if (TravelDestinationWMArea.Continent == WAQContinent.Northrend)
                    {
                        TravelHelper.ZeppelinTirisfalToOrgrimmar();
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
                            TravelHelper.PortalFromOrgrimmarToBlastedLands();
                        }
                        else
                        {
                            if (TravelDestinationPosition.X > -2384) // above wetlands
                            {
                                TravelHelper.ZeppelinKalimdorToTirisfal();
                            }
                            else
                            {
                                TravelHelper.ShipRatchetToBootyBay();
                            }
                        }
                    }
                    // To Outlands
                    if (TravelDestinationWMArea.Continent == WAQContinent.Outlands)
                    {
                        TravelHelper.PortalFromOrgrimmarToBlastedLands();
                    }
                    // To Northrend
                    if (TravelDestinationWMArea.Continent == WAQContinent.Northrend)
                    {
                        TravelHelper.ZeppelinOrgrimmarToNorthrend();
                    }
                }

                // From Outlands
                if (myCurrentWMArea.Continent == WAQContinent.Outlands)
                {
                    // To Kalimdor
                    if (TravelDestinationWMArea.Continent == WAQContinent.Kalimdor)
                    {
                        TravelHelper.PortalShattrathToOrgrimmar();
                    }
                    // To EK
                    if (TravelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        TravelHelper.PortalShattrathToOrgrimmar();
                    }
                    // To Northrend
                    if (TravelDestinationWMArea.Continent == WAQContinent.Northrend)
                    {
                        TravelHelper.PortalShattrathToOrgrimmar();
                    }
                }

                // From Northrend
                if (myCurrentWMArea.Continent == WAQContinent.Northrend)
                {
                    // To Kalimdor
                    if (TravelDestinationWMArea.Continent == WAQContinent.Kalimdor)
                    {
                        TravelHelper.PortalDalaranToOrgrimmar();
                    }
                    // To EK
                    if (TravelDestinationWMArea.Continent == WAQContinent.EasternKingdoms)
                    {
                        TravelHelper.PortalDalaranToUndercity();
                    }
                    // To Outland
                    if (TravelDestinationWMArea.Continent == WAQContinent.Outlands)
                    {
                        TravelHelper.HordePortalDalaranToShattrath();
                    }
                }
            }

            Logger.Log($"RESET TRAVELER");
            MoveHelper.StopAllMove(true);
            InTravel = false;
            TravelDestinationPosition = null;
            TravelDestinationWMArea = null;
        }
    }
}
