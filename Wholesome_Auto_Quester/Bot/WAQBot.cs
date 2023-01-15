using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Wholesome_Auto_Quester.Bot.ContinentManagement;
using Wholesome_Auto_Quester.Bot.GrindManagement;
using Wholesome_Auto_Quester.Bot.JSONManagement;
using Wholesome_Auto_Quester.Bot.QuestManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Bot.TravelManagement;
using Wholesome_Auto_Quester.Database.DBC;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using Wholesome_Auto_Quester.States;
using wManager.Events;
using wManager.Wow.Bot.States;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

namespace Wholesome_Auto_Quester.Bot
{
    internal class WAQBot
    {
        private readonly Engine Fsm = new Engine();
        private readonly List<StuckCounter> ListStuckCounters = new List<StuckCounter>();
        private IWowObjectScanner _objectScanner;
        private ITaskManager _taskManager;
        private IQuestManager _questManager;
        private IGrindManager _grindManager;
        private IJSONManager _jsonManager;
        private IContinentManager _continentManager;
        private TravelManager _travelManager;
        private QuestsTrackerGUI _questTrackerGui;
        private IProduct _product;
        private WAQCheckPathAhead _checkPathAheadState;

        internal bool Pulse(QuestsTrackerGUI tracker, IProduct product)
        {
            try
            {
                _product = product;
                _questTrackerGui = tracker;
                _jsonManager = new JSONManager();
                _continentManager = new ContinentManager(_jsonManager);
                _travelManager = new TravelManager(_continentManager);
                _grindManager = new GrindManager(_jsonManager, _continentManager);
                _objectScanner = new WowObjectScanner(_questTrackerGui);
                _questManager = new QuestManager(_objectScanner, _questTrackerGui, _jsonManager, _continentManager);
                _taskManager = new TaskManager(_objectScanner, _questManager, _grindManager, _questTrackerGui, _travelManager, _continentManager);
                DBCFaction.RecordReputations();

                // Attach onlevelup for spell book:
                EventsLua.AttachEventLua("PLAYER_LEVEL_UP", m => OnLevelUp());
                EventsLua.AttachEventLua("PLAYER_ENTERING_WORLD", m => ScreenReloaded());
                EventsLua.AttachEventLua("UPDATE_FACTION", m => OnReputationChange());

                // Update spell list
                SpellManager.UpdateSpellBook();

                // Load CC:
                CustomClass.LoadCustomClass();

                // FSM
                Fsm.States.Clear();

                _checkPathAheadState = new WAQCheckPathAhead(_objectScanner);
                State lootState = WholesomeAQSettings.CurrentSetting.TurboLoot ?
                    new WAQTurboLoot() : new Looting();

                State[] states = new State[]
                {
                    new Relogger(),
                    new NPCScanState(),
                    new Pause(),
                    new WAQForceResurrection(),
                    new Resurrect(),
                    new WAQExitVehicle(),
                    new MyMacro(),
                    //new WAQBlacklistDanger(),
                    new WAQStatePriorityLoot(_objectScanner),
                    new WAQDefend(),
                    new WAQWaitResurrectionSickness(),
                    new Regeneration(),
                    _checkPathAheadState,
                    new WAQStateLoot(_objectScanner), // loot for quests
                    lootState,
                    //new MillingState(),
                    new Farming(),
                    new FarmingRange(),
                    new FlightMasterTakeTaxiState(),
                    new FlightMasterDiscoverState(),
                    new Trainers(),
                    new ToTown(),
                    new WAQStateTravel(_taskManager, _travelManager, _continentManager),
                    new WAQStateInteract(_objectScanner),
                    new WAQStateKill(_objectScanner),
                    new WAQStateMoveToHotspot(_taskManager),
                    new MovementLoop(),
                    new Idle()
                };

                states = states.Reverse().ToArray();

                for (int i = 0; i < states.Length; i++)
                {
                    states[i].Priority = i;
                    Fsm.AddState(states[i]);
                }

                Fsm.States.Sort();
                Fsm.StartEngine(10, "_AutoQuester");

                StopBotIf.LaunchNewThread();

                MovementEvents.OnSeemStuck += SeemStuckHandler;
                Radar3D.OnDrawEvent += Radar3DOnDrawEvent;
                Radar3D.Pulse();

                return true;
            }
            catch (Exception e)
            {
                Dispose();
                Logging.WriteError("Bot > Bot  > Pulse(): " + e);
                return false;
            }
        }

        internal void Dispose()
        {
            try
            {
                _jsonManager?.Dispose();
                _grindManager?.Dispose();
                _objectScanner?.Dispose();
                _questManager?.Dispose();
                _taskManager?.Dispose();
                _travelManager?.Dispose();

                Radar3D.OnDrawEvent -= Radar3DOnDrawEvent;
                MovementEvents.OnSeemStuck -= SeemStuckHandler;

                CustomClass.DisposeCustomClass();
                Fsm.StopEngine();
                Fight.StopFight();
                MoveHelper.StopAllMove(true);
            }
            catch (Exception e)
            {
                Logging.WriteError("Bot > Bot  > Dispose(): " + e);
            }
        }

        private void OnLevelUp()
        {
            if (ObjectManager.Me.Level >= WholesomeAQSettings.CurrentSetting.StopAtLevel)
            {
                Logger.Log($"You have reached your maximum set level ({WholesomeAQSettings.CurrentSetting.StopAtLevel}). Stopping.");
                _product.Dispose();
                return;
            }

            SpellManager.UpdateSpellBook();
            CustomClass.ResetCustomClass();
            Talent.DoTalents();
            wManager.wManagerSetting.ClearBlacklistOfCurrentProductSession();
            BlacklistHelper.AddDefaultBLZones();
        }

        private void OnReputationChange()
        {
            DBCFaction.RecordReputations();
        }

        private void ScreenReloaded()
        {
        }

        private void Radar3DOnDrawEvent()
        {
            if (WholesomeAQSettings.CurrentSetting.DevMode)
            {
                Radar3D.DrawString(Logger.ScannerString, new Vector3(30, 290, 0), 10, Color.LightSteelBlue);
                Radar3D.DrawString(Logger.TaskMString, new Vector3(30, 310, 0), 10, Color.MediumAquamarine);

                if (_travelManager.TravelInProgress)
                {
                    Radar3D.DrawString($"{_continentManager.MyMapArea.Continent} - {_continentManager.MyMapArea.areaName} " +
                        $"=> {_taskManager.ActiveTask.WorldMapArea.Continent} - {_taskManager.ActiveTask.WorldMapArea.areaName}",
                        new Vector3(30, 330, 0), 10, Color.PaleGoldenrod);
                }
                /*
                foreach ((Vector3 a, Vector3 b) line in _clearPathState.LinesToCheck)
                {
                    Radar3D.DrawLine(line.a, line.b, Color.Red);
                }
                */

                foreach (Vector3 point in _checkPathAheadState.PointsAlongPathSegments)
                {
                    Radar3D.DrawCircle(point, 0.2f, Color.Green, true, 150);
                }

                if (_checkPathAheadState.DangerTraceline.a != null && _checkPathAheadState.DangerTraceline.b != null)
                {
                    Radar3D.DrawCircle(_checkPathAheadState.DangerTraceline.a, 0.4f, Color.Red, false, 200);
                    Radar3D.DrawLine(_checkPathAheadState.DangerTraceline.a, _checkPathAheadState.DangerTraceline.b, Color.Red, 200);
                }

                if (_checkPathAheadState.UnitOnPath.unit != null)
                {
                    Radar3D.DrawCircle(_checkPathAheadState.UnitOnPath.unit.PositionWithoutType, 0.4f, Color.Red, true, 200);
                }

                for (int i = 0; i < _checkPathAheadState.LinesToCheck.Count - 1; i++)
                {
                    Radar3D.DrawLine(_checkPathAheadState.LinesToCheck[i], _checkPathAheadState.LinesToCheck[i + 1], Color.OrangeRed, 150);
                }

                if (_taskManager.ActiveTask != null)
                {
                    Radar3D.DrawString(_taskManager.ActiveTask.TaskName, new Vector3(30, 350, 0), 10, Color.PaleTurquoise);
                    Radar3D.DrawLine(ObjectManager.Me.Position, _taskManager.ActiveTask.Location, Color.PaleTurquoise);
                    Radar3D.DrawCircle(_taskManager.ActiveTask.Location, 1.3f, Color.PaleTurquoise);
                }

                if (_objectScanner.ActiveWoWObject.wowObject != null)
                {
                    Radar3D.DrawLine(ObjectManager.Me.Position, _objectScanner.ActiveWoWObject.wowObject.Position, Color.Yellow);
                    Radar3D.DrawCircle(_objectScanner.ActiveWoWObject.wowObject.Position, 1, Color.Yellow);
                    Radar3D.DrawLine(ObjectManager.Me.Position, _objectScanner.ActiveWoWObject.task.Location, Color.GreenYellow);
                    Radar3D.DrawCircle(_objectScanner.ActiveWoWObject.task.Location, 0.7f, Color.GreenYellow);
                    Radar3D.DrawString($"{_objectScanner.ActiveWoWObject.wowObject.Name} ({_objectScanner.ActiveWoWObject.wowObject.Entry})"
                        , new Vector3(30, 370, 0), 10, Color.Yellow);
                    Radar3D.DrawString($"{_objectScanner.ActiveWoWObject.task.TaskName}"
                        , new Vector3(30, 390, 0), 10, Color.GreenYellow);
                }

                if (MoveHelper.IsMovementThreadRunning)
                    Radar3D.DrawString("Movement thread running", new Vector3(30, 410, 0), 10, Color.Green);
                else
                    Radar3D.DrawString("Movement thread not running", new Vector3(30, 410, 0), 10, Color.Red);
            }
        }

        private void SeemStuckHandler()
        {
            if (!MoveHelper.IsMovementThreadRunning)
            {
                return;
            }

            IWAQTask task = _taskManager.ActiveTask;
            WoWObject wowObject = _objectScanner.ActiveWoWObject.wowObject;

            if (wowObject != null)
            {
                StuckCounter existing = ListStuckCounters.Find(sc => sc.WowObject != null && sc.WowObject.Guid == wowObject.Guid);
                if (existing == null)
                    ListStuckCounters.Add(new StuckCounter(task, wowObject));
                else
                    existing.AddToCount();
                return;
            }

            if (task != null)
            {
                StuckCounter existing = ListStuckCounters.Find(sc => sc.Task.Location == task.Location);
                if (existing == null)
                    ListStuckCounters.Add(new StuckCounter(task, null));
                else
                    existing.AddToCount();
                return;
            }
        }
    }
}

public class StuckCounter
{
    public int Count;
    public WoWObject WowObject;
    public IWAQTask Task;
    private Timer _timer = new Timer();

    public StuckCounter(IWAQTask task, WoWObject wowObject)
    {
        Count = 0;
        WowObject = wowObject;
        Task = task;
        AddToCount();
    }

    public void AddToCount()
    {
        if (_timer.IsReady) Count = 0;
        _timer = new Timer(30 * 1000);
        int maxCOunt = 10;
        if (WowObject?.Position.Z > ObjectManager.Me.Position.Z + 20 && WowObject?.Position.DistanceTo2D(ObjectManager.Me.Position) < 10
            || Task?.Location.Z > ObjectManager.Me.Position.Z + 20 && Task?.Location.DistanceTo2D(ObjectManager.Me.Position) < 10)
            maxCOunt = 3;
        Count++;

        if (Count > maxCOunt || !ObjectManager.Me.IsAlive) return;

        if (WowObject != null)
            Logger.Log($"We seem stuck trying to reach object {WowObject.Name} ({Count})");
        else
            Logger.Log($"We seem stuck trying to reach task {Task.TaskName} ({Count})");

        if (Count >= maxCOunt)
        {
            if (WowObject != null)
            {
                Fight.StopFight();
                BlacklistHelper.AddNPC(WowObject.Guid, $"Stuck {maxCOunt} times trying to reach");
                BlacklistHelper.AddZone(WowObject.Position, 5, $"Stuck {maxCOunt} times trying to reach");
                Task.PutTaskOnTimeout($"Stuck {Count} times", 15 * 60, true);
                return;
            }
            if (Task != null)
            {
                Fight.StopFight();
                BlacklistHelper.AddZone(Task.Location, 5, $"Stuck {maxCOunt} times trying to reach");
                Task.PutTaskOnTimeout($"Stuck {Count} times", 15 * 60, true);
            }
        }
    }
}