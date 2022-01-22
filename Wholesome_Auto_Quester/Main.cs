using robotManager.Events;
using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wholesome_Auto_Quester;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using wManager.Events;
using wManager.Plugin;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Main : IProduct {
    public const string ProductName = "Wholesome Auto Quester";
    public const string FileName = "Wholesome_Auto_Quester";
    public static readonly QuestsTrackerGUI QuestTrackerGui = new QuestsTrackerGUI();
    private List<StuckCounter> ListStuckCounters = new List<StuckCounter>();
    private ProductSettingsControl _settingsUserControl;
    public static bool RequestImmediateTaskUpdate;
    public static bool RequestImmediateTaskReset;

    public string version = "0.0.20"; // Must match version in Version.txt

    public bool IsStarted { get; private set; }

    public void Initialize() {
        try {
            WholesomeAQSettings.Load();
            Logger.Log($"{ProductName} version {version} loaded");

            QuestTrackerGui.MaxWidth = 520;
            QuestTrackerGui.MaxHeight = 650;
            QuestTrackerGui.MinWidth = 520;
            QuestTrackerGui.MinHeight = 650;
            QuestTrackerGui.ResizeMode = ResizeMode.CanResize;
            QuestTrackerGui.Title = "Wholesome Quest Tracker";
            QuestTrackerGui.SaveWindowPosition = true;
        } catch (Exception e) {
            Logging.WriteError("Main > Initialize(): " + e);
        }
    }

    public void Dispose() {
        try {
            Stop();
            Logging.Status = "Dispose Product Complete";
            Logging.Write("Dispose Product Complete");
        } catch (Exception e) {
            Logging.WriteError("Main > Dispose(): " + e);
        }
    }

    public void Start() {        
        try
        {
            if (AutoUpdater.CheckUpdate(version))
                return;

            if (!AutoUpdater.CheckDbDownload())
                return;

            IsStarted = true;
            ToolBox.UpdateCompletedQuests();
            ToolBox.InitializeWAQSettings();
            //FiniteStateMachineEvents.OnRunState += SmoothMoveKiller;
            LoggingEvents.OnAddLog += AddLogHandler;
            MovementEvents.OnSeemStuck += SeemStuckHandler;
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += EventsWithArgsHandler;
            EventsLua.AttachEventLua("PLAYER_DEAD", e => PlayerDeadHandler(e));

            var dbWotlk = new DBQueriesWotlk();
            dbWotlk.GetAvailableQuests();
            if (!Products.IsStarted) 
            {
                IsStarted = false;
                return;
            }
            
            Task.Factory.StartNew(() => {
                while (IsStarted) {
                    try {
                        if (Conditions.InGameAndConnectedAndProductStartedNotInPause)
                        {
                            robotManager.Helpful.Timer maxWaitTime = new robotManager.Helpful.Timer(1000);
                            while (!maxWaitTime.IsReady)
                            {
                                if (RequestImmediateTaskReset)
                                {
                                    WAQTasks.TaskInProgress = null;
                                    WAQTasks.WoWObjectInProgress = null;
                                    break;
                                }
                                if (RequestImmediateTaskUpdate)
                                    break;
                                Thread.Sleep(25);
                            }
                            BlacklistHelper.CleanupBlacklist();
                            WAQTasks.UpdateStatuses();
                            WAQTasks.UpdateTasks();
                            RequestImmediateTaskUpdate = false;
                            RequestImmediateTaskReset = false;
                        }
                    } catch (Exception arg) {
                        Logging.WriteError(string.Concat(arg));
                    }
                }
            });

            Task.Factory.StartNew(() => {
                while (IsStarted) {
                    try {
                        if (Conditions.InGameAndConnectedAndProductStartedNotInPause) {
                            Quest.RequestQuestsCompleted();
                            Quest.ConsumeQuestsCompletedRequest();
                        }
                    } catch (Exception arg) {
                        Logging.WriteError(string.Concat(arg));
                    }

                    Thread.Sleep(1000 * 60 * 5);
                }
            });
            
            if (Bot.Pulse()) {                
                if (WholesomeAQSettings.CurrentSetting.ActivateQuestsGUI)
                    QuestTrackerGui.ShowWindow();
                
                Radar3D.OnDrawEvent += Radar3DOnDrawEvent;
                Radar3D.Pulse();

                PluginsManager.LoadAllPlugins();

                Logging.Status = "Start Product Complete";
                Logging.Write("Start Product Complete");
            } else {
                IsStarted = false;
                Logging.Status = "Start Product failed";
                Logging.Write("Start Product failed");
            }
        } catch (Exception e) {
            IsStarted = false;
            Logging.WriteError("Main > Start(): " + e);
        }
    }

    public void Stop() {
        try {
            Lua.RunMacroText("/stopcasting");
            MoveHelper.StopAllMove();

            Radar3D.OnDrawEvent -= Radar3DOnDrawEvent;
            // Radar3D.Stop();

            QuestTrackerGui.HideWindow();

            //FiniteStateMachineEvents.OnRunState -= SmoothMoveKiller;
            LoggingEvents.OnAddLog -= AddLogHandler;
            MovementEvents.OnSeemStuck -= SeemStuckHandler;
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= EventsWithArgsHandler;

            Bot.Dispose();
            IsStarted = false;
            PluginsManager.DisposeAllPlugins();
            Logging.Status = "Stop Product Complete";
            Logging.Write("Stop Product Complete");
        } catch (Exception e) {
            Logging.WriteError("Main > Stop(): " + e);
        }
    }

    // LOG EVENTS
    private void AddLogHandler(Logging.Log log)
    {
        if (log.Text == "[Fight] Mob seem bugged" && ObjectManager.Target.Guid > 0)
        {
            BlacklistHelper.AddNPC(ObjectManager.Target.Guid, "Mob seem bugged");
            //wManagerSetting.AddBlackList(ObjectManager.Target.Guid, isSessionBlacklist: true);
        }
    }

    // GUI
    public UserControl Settings {
        get {
            try {
                if (_settingsUserControl == null)
                    _settingsUserControl = new ProductSettingsControl();
                return _settingsUserControl;
            } catch (Exception e) {
                Logger.Log("> Main > Settings(): " + e);
            }

            return null;
        }
    }
    /*
    private static void SmoothMoveKiller(Engine engine, State state, CancelEventArgs cancelable) {
        if (MoveHelper.IsMovementThreadRunning
            && state.DisplayName != "Security/Stop game"
            && !state.DisplayName.Contains("[SmoothMove - Q]")) {
            Logger.LogDebug($"SmoothMove - Q was running while activating state '{state.DisplayName}'. Killing it.");
            MoveHelper.StopCurrentMovementThread();
        }
    }
    */
    private static void Radar3DOnDrawEvent() {
        if (WAQTasks.TaskInProgress != null)
        {
            Radar3D.DrawString(WAQTasks.TaskInProgress.TaskName, new Vector3(30, 200, 0), 10, Color.AliceBlue);
            Radar3D.DrawLine(ObjectManager.Me.Position, WAQTasks.TaskInProgress.Location, Color.AliceBlue);
        }

        if (WAQTasks.WoWObjectInProgress != null) {
            Radar3D.DrawLine(ObjectManager.Me.Position, WAQTasks.WoWObjectInProgress.Position, Color.Yellow);
            Radar3D.DrawCircle(WAQTasks.WoWObjectInProgress.Position, 1, Color.Yellow);
            Radar3D.DrawString(WAQTasks.WoWObjectInProgress.Name, new Vector3(30, 220, 0), 10, Color.Yellow);
        }
        if (MoveHelper.IsMovementThreadRunning)
            Radar3D.DrawString("Movement thread running", new Vector3(30, 240, 0), 10, Color.Green);
        else
            Radar3D.DrawString("Movement thread not running", new Vector3(30, 240, 0), 10, Color.Red);
    }

    private void PlayerDeadHandler(object context)
    {
        BlacklistHelper.AddZone(ObjectManager.Me.Position, 20, "Death");
        //wManagerSetting.AddBlackListZone(ObjectManager.Me.Position, 20, (ContinentId)Usefuls.ContinentId, isSessionBlacklist: true);
    }

    private void SeemStuckHandler()
    {
        WAQTask task = WAQTasks.TaskInProgress;
        WoWObject wowObject = WAQTasks.WoWObjectInProgress;

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

    private void EventsWithArgsHandler(string id, List<string> args)
    {
        if (id == "UI_ERROR_MESSAGE" && args[0] == "You cannot attack that target.")
        {
            if (ObjectManager.Target != null)
                BlacklistHelper.AddNPC(ObjectManager.Target.Guid, $"Can't attack this target");
        }
    }
}

public class StuckCounter
{
    public int Count;
    public WoWObject WowObject;
    public WAQTask Task;
    private robotManager.Helpful.Timer _timer = new robotManager.Helpful.Timer();

    public StuckCounter(WAQTask task, WoWObject wowObject)
    {
        Count = 0;
        WowObject = wowObject;
        Task = task;
        AddToCount();
    }

    public void AddToCount()
    {
        if (_timer.IsReady) Count = 0;
        _timer = new robotManager.Helpful.Timer(30 * 1000);
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
                Task.PutTaskOnTimeout(15 * 60, $"Stuck {Count} times");
                return;
            }
            if (Task != null)
            {
                Fight.StopFight();
                BlacklistHelper.AddZone(Task.Location, 5, $"Stuck {maxCOunt} times trying to reach");
                Task.PutTaskOnTimeout(15 * 60, $"Stuck {Count} times");
            }
            Main.RequestImmediateTaskReset = true;
        }
    }
}