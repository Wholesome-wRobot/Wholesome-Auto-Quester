using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FlXProfiles;
using robotManager.Events;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using robotManager.Products;
using Wholesome_Auto_Quester;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using wManager;
using wManager.Plugin;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Main : IProduct {
    public const string ProductName = "Wholesome Auto Quester";
    public const string FileName = "Wholesome_Auto_Quester";
    public static readonly QuestsTrackerGUI QuestTrackerGui = new QuestsTrackerGUI();
    private ProductSettingsControl _settingsUserControl;

    public string version = "0.0.05"; // Must match version in Version.txt

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

            IsStarted = true;

            if (ToolBox.GetWoWVersion() == "3.3.5") {
                var dbWotlk = new DBQueriesWotlk();
                dbWotlk.GetAvailableQuests();
                if (!Products.IsStarted) {
                    IsStarted = false;
                    return;
                }
            }
            
            Task.Factory.StartNew(() => {
                while (IsStarted) {
                    try {
                        if (Conditions.InGameAndConnectedAndProductStartedNotInPause
                                && !ObjectManager.Me.IsOnTaxi
                                && ObjectManager.Me.IsAlive) {
                            WAQTasks.UpdateStatuses();
                            WAQTasks.UpdateTasks();
                        }
                    } catch (Exception arg) {
                        Logging.WriteError(string.Concat(arg));
                    }

                    Thread.Sleep(1000);
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

                    Thread.Sleep(1000 * 60 * 15);
                }
            });
            
            FiniteStateMachineEvents.OnRunState += SmoothMoveKiller;
            LoggingEvents.OnAddLog += AddLogHandler;
            EventsLua.AttachEventLua("PLAYER_DEAD", e => PlayerDeadHandler(e));
            ToolBox.InitializeWAQDoNotSellList();
            
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

            FiniteStateMachineEvents.OnRunState -= SmoothMoveKiller;
            LoggingEvents.OnAddLog -= AddLogHandler;

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
            Logger.Log($"{ObjectManager.Target.Guid} is bugged. Blacklisting.");
            wManagerSetting.AddBlackList(ObjectManager.Target.Guid, isSessionBlacklist: true);
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

    private static void SmoothMoveKiller(Engine engine, State state, CancelEventArgs cancelable) {
        if (MoveHelper.IsMovementThreadRunning
            && state.DisplayName != "Security/Stop game"
            && !state.DisplayName.Contains("[SmoothMove - Q]")) {
            Logger.LogDebug($"SmoothMove - Q was running while activating state '{state.DisplayName}'. Killing it.");
            MoveHelper.StopCurrentMovementThread();
        }
    }

    private static int GetWowVersion() {
        string[] forWow = Information.ForWow.Split('.');
        return int.Parse(forWow[0]);
    }

    private static void Radar3DOnDrawEvent() {
        if (WAQTasks.TaskInProgress != null)
            Radar3D.DrawLine(ObjectManager.Me.Position, WAQTasks.TaskInProgress.Location, Color.Blue);

        if (WAQTasks.TaskInProgressWoWObject != null) {
            Radar3D.DrawLine(ObjectManager.Me.Position, WAQTasks.TaskInProgressWoWObject.Position, Color.Yellow);
            Radar3D.DrawCircle(WAQTasks.TaskInProgressWoWObject.Position, 1, Color.Yellow);
        }
    }

    private void PlayerDeadHandler(object context)
    {
        Logger.Log($"You died. Blacklisting zone.");
        wManagerSetting.AddBlackListZone(ObjectManager.Me.Position, 30, (ContinentId)Usefuls.ContinentId, isSessionBlacklist: true);
    }
}