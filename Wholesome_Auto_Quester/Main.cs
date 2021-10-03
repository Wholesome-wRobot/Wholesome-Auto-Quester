using robotManager.Helpful;
using robotManager.Products;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using FlXProfiles;
using robotManager.Events;
using robotManager.FiniteStateMachine;
using Wholesome_Auto_Quester;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using wManager.Plugin;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class Main : IProduct {
    public const string ProductName = "Wholesome Auto Quester";
    public static readonly QuestsTrackerGUI QuestTrackerGui = new QuestsTrackerGUI();


    public bool IsStarted { get; private set; } = false;
    ProductSettingsControl _settingsUserControl;

    public string version = "0.0.01"; // Must match version in Version.txt

    public void Initialize() {
        try {
            WholesomeAQSettings.Load();
            Logger.Log($"{ProductName} version {version} loaded");

            QuestTrackerGui.MaxWidth = 520;
            QuestTrackerGui.MaxHeight = 650;
            QuestTrackerGui.MinWidth = 520;
            QuestTrackerGui.MinHeight = 650;
            QuestTrackerGui.ResizeMode = System.Windows.ResizeMode.CanResize;
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

    private static void FixDatabasePermissions() {
        string curDir = Others.GetCurrentDirectory;
        if (!File.Exists($"{curDir}Data\\WoWDb335")) {
            Logging.WriteError("Database does not exist!");
            return;
        }
        File.Copy($"{curDir}Data\\WoWDb335", $"{curDir}Data\\WoWDb335-temp");
        File.Delete($"{curDir}Data\\WoWDb335");
        File.Move($"{curDir}Data\\WoWDb335-temp", $"{curDir}Data\\WoWDb335");
    }
    
    public void Start() {
        try {
            // FixDatabasePermissions();
            //AutoUpdater.CheckUpdate(version);
            IsStarted = true;

            Task.Factory.StartNew(() => {
                while (IsStarted) {
                    try {
                        if (Conditions.InGameAndConnectedAndProductStartedNotInPause) {
                            WAQTasks.UpdateStatuses();
                            WAQTasks.UpdateTasks();
                        }
                    } catch (Exception arg) {
                        Logging.WriteError(string.Concat(arg), true);
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
                        Logging.WriteError(string.Concat(arg), true);
                    }

                    Thread.Sleep(1000 * 60 * 15);
                }
            });

            if (ToolBox.GetWoWVersion() == "3.3.5") {
                DBQueriesWotlk dbWotlk = new DBQueriesWotlk();
                dbWotlk.GetAvailableQuests();
            }

            FiniteStateMachineEvents.OnRunState += SmoothMoveKiller;

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

    private static void SmoothMoveKiller(Engine engine, State state, CancelEventArgs cancelable) {
        if (MoveHelper.IsMovementThreadRunning
            && state.DisplayName != "Security/Stop game"
            && !state.DisplayName.Contains("[SmoothMove - Q]")) {
            Logger.LogDebug($"SmoothMove - Q was running while activating state '{state.DisplayName}'. Killing it.");
            MoveHelper.StopCurrentMovementThread();
        }
    }

    public void Stop() {
        try {
            Lua.RunMacroText("/stopcasting");
            MoveHelper.StopAllMove();
            MovementManager.StopMove();

            Radar3D.OnDrawEvent -= Radar3DOnDrawEvent;
            // Radar3D.Stop();

            QuestTrackerGui.HideWindow();

            FiniteStateMachineEvents.OnRunState -= SmoothMoveKiller;

            Bot.Dispose();
            IsStarted = false;
            PluginsManager.DisposeAllPlugins();
            Logging.Status = "Stop Product Complete";
            Logging.Write("Stop Product Complete");
        } catch (Exception e) {
            Logging.WriteError("Main > Stop(): " + e);
        }
    }

    // GUI
    public System.Windows.Controls.UserControl Settings {
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

    private static int GetWowVersion() {
        string[] forWow = wManager.Information.ForWow.Split('.');
        return Int32.Parse(forWow[0]);
    }

    private static void Radar3DOnDrawEvent() {
        if (WAQTasks.TaskInProgress != null) {
            Radar3D.DrawLine(ObjectManager.Me.Position, WAQTasks.TaskInProgress.Location, Color.Blue);
        }

        if (WAQTasks.TaskInProgressWoWObject != null) {
            Radar3D.DrawLine(ObjectManager.Me.Position, WAQTasks.TaskInProgressWoWObject.Position, Color.Yellow);
            Radar3D.DrawCircle(WAQTasks.TaskInProgressWoWObject.Position, 1, Color.Yellow);
        }
    }
}