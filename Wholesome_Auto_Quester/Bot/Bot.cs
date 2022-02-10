using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using System;
using System.Collections.Generic;
using System.Drawing;
using Wholesome_Auto_Quester.Bot.GrindManagement;
using Wholesome_Auto_Quester.Bot.QuestManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement;
using Wholesome_Auto_Quester.Bot.TaskManagement.Tasks;
using Wholesome_Auto_Quester.Bot.TravelManagement;
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
    internal class Bot
    {
        private readonly Engine Fsm = new Engine();
        private readonly List<StuckCounter> ListStuckCounters = new List<StuckCounter>();
        private IWowObjectScanner _objectScanner;
        private ITaskManager _taskManager;
        private IQuestManager _questManager;
        private IGrindManager _grindManager;
        private TravelManager _travelManager;
        private QuestsTrackerGUI _questTrackerGui;

        internal bool Pulse(QuestsTrackerGUI tracker)
        {
            try
            {
                _questTrackerGui = tracker;
                _travelManager = new TravelManager();
                _grindManager = new GrindManager();
                _objectScanner = new WowObjectScanner(_questTrackerGui);
                _questManager = new QuestManager(_objectScanner, _questTrackerGui);
                _taskManager = new TaskManager(_objectScanner, _questManager, _grindManager, _questTrackerGui, _travelManager);
                if (WholesomeAQSettings.CurrentSetting.ActivateQuestsGUI)
                {
                    _questTrackerGui.ShowWindow();
                }

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
                Fsm.AddState(new NPCScanState { Priority = 37 });
                Fsm.AddState(new Pause { Priority = 36 });
                Fsm.AddState(new Resurrect { Priority = 35 });

                Fsm.AddState(new WAQExitVehicle { Priority = 34 });

                Fsm.AddState(new MyMacro { Priority = 33 });

                Fsm.AddState(new WAQStatePriorityLoot(_objectScanner, 31));
                Fsm.AddState(new WAQDefend { Priority = 30 });

                Fsm.AddState(new Regeneration { Priority = 29 });

                Fsm.AddState(new WAQStateLoot(_objectScanner, 28));

                Fsm.AddState(new Looting { Priority = 27 });
                Fsm.AddState(new FlightMasterTakeTaxiState { Priority = 26 });
                Fsm.AddState(new Trainers { Priority = 23 });
                Fsm.AddState(new ToTown { Priority = 22 });

                Fsm.AddState(new WAQStateTravel(_taskManager, _travelManager, 21));

                Fsm.AddState(new WAQStateInteract(_objectScanner, 15));
                Fsm.AddState(new WAQStateKill(_objectScanner, 14));

                Fsm.AddState(new WAQStateMoveToHotspot(_taskManager, 7));

                Fsm.AddState(new MovementLoop { Priority = 1 });

                Fsm.AddState(new Idle { Priority = 0 });

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
                _grindManager?.Dispose();
                _objectScanner?.Dispose();
                _questManager?.Dispose();
                _taskManager?.Dispose();
                _travelManager?.Dispose();

                _questTrackerGui?.HideWindow();

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
            SpellManager.UpdateSpellBook();
            CustomClass.ResetCustomClass();
            Talent.DoTalents();
        }

        private void ScreenReloaded()
        {
        }

        private void Radar3DOnDrawEvent()
        {
            
            if (_travelManager.TravelInProgress)
            {
                Radar3D.DrawString($"{ContinentHelper.MyMapArea.Continent} - {ContinentHelper.MyMapArea.areaName} " +
                    $"=> {_taskManager.ActiveTask.WorldMapArea.Continent} - {_taskManager.ActiveTask.WorldMapArea.areaName}",
                    new Vector3(30, 330, 0), 10, Color.PaleGoldenrod);
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