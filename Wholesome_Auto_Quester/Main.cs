using robotManager.Helpful;
using robotManager.Products;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using Wholesome_Auto_Quester;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.Database;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using wManager.Plugin;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using Timer = robotManager.Helpful.Timer;

public class Main : IProduct
{
    public static string productName = "Wholesome Auto Quester";
    public static int wowVersion = GetWowVersion();
    public static QuestsTrackerGUI questTrackerGUI = new QuestsTrackerGUI();

    private readonly BackgroundWorker _getQuestsFromDbThread = new BackgroundWorker();
    private readonly BackgroundWorker _updateSurroundingsAndTasksThread = new BackgroundWorker();
    private Timer _dbPulseTimer = new Timer();
    private Timer _surroundingPulseTimer = new Timer();

    public bool IsStarted { get; private set; } = false;
    ProductSettingsControl _settingsUserControl;

    private DB database;

    public string version = "0.0.01"; // Must match version in Version.txt

    public void Initialize()
    {
        try
        {
            WholesomeAQSettings.Load();
            Logger.Log($"{productName} version {version} loaded");

            questTrackerGUI.MaxWidth = 520;
            questTrackerGUI.MaxHeight = 650;
            questTrackerGUI.MinWidth = 520;
            questTrackerGUI.MinHeight = 650;
            questTrackerGUI.ResizeMode = System.Windows.ResizeMode.CanResize;
            questTrackerGUI.Title = "Wholesome Quest Tracker";
            questTrackerGUI.SaveWindowPosition = true;
        }
        catch (Exception e)
        {
            Logging.WriteError("Main > Initialize(): " + e);
        }
    }

    public void Dispose()
    {
        try
        {
            Stop();
            Logging.Status = "Dispose Product Complete";
            Logging.Write("Dispose Product Complete");
        }
        catch (Exception e)
        {
            Logging.WriteError("Main > Dispose(): " + e);
        }
    }

    public void Start()
    {
        try
        {
            //AutoUpdater.CheckUpdate(version);
            IsStarted = true;

            database = new DB();
            /*
            if (ToolBox.GetWoWVersion() == "2.4.3")
                DBQueriesTBC.Initialize(database);
            */
            if (ToolBox.GetWoWVersion() == "3.3.5")
                DBQueriesWotlk.Initialize(database);

            if (WholesomeAQSettings.CurrentSetting.ActivateQuestsGUI)
                questTrackerGUI.ShowWindow();

            _updateSurroundingsAndTasksThread.DoWork += UpdateSurroundingsAndTasksPulse;
            _updateSurroundingsAndTasksThread.RunWorkerAsync();
            _getQuestsFromDbThread.DoWork += GetQuestsFromDbPulse;
            _getQuestsFromDbThread.RunWorkerAsync();

            Radar3D.Pulse();
            Radar3D.OnDrawEvent += Radar3DOnDrawEvent;

            if (Bot.Pulse())
            {
                PluginsManager.LoadAllPlugins();
                Logging.Status = "Start Product Complete";
                Logging.Write("Start Product Complete");
            }
            else
            {
                IsStarted = false;
                Logging.Status = "Start Product failed";
                Logging.Write("Start Product failed");
            }
        }
        catch (Exception e)
        {
            IsStarted = false;
            Logging.WriteError("Main > Start(): " + e);
        }
    }

    public void Stop()
    {
        try
        {
            Lua.RunMacroText("/stopcasting");
            MovementManager.StopMove();

            Radar3D.OnDrawEvent -= Radar3DOnDrawEvent;
            Radar3D.Stop();

            questTrackerGUI.HideWindow();

            _updateSurroundingsAndTasksThread.DoWork -= UpdateSurroundingsAndTasksPulse;
            _updateSurroundingsAndTasksThread.Dispose();
            _getQuestsFromDbThread.DoWork -= GetQuestsFromDbPulse;
            _getQuestsFromDbThread.Dispose();

            database.Dispose();
            Bot.Dispose();
            IsStarted = false;
            PluginsManager.DisposeAllPlugins();
            Logging.Status = "Stop Product Complete";
            Logging.Write("Stop Product Complete");
        }
        catch (Exception e)
        {
            Logging.WriteError("Main > Stop(): " + e);
        }
    }

    // Update from DB
    private void GetQuestsFromDbPulse(object sender, DoWorkEventArgs args)
    {
        while (IsStarted)
        {
            try
            {
                if (Conditions.InGameAndConnectedAndProductStartedNotInPause 
                    && IsStarted
                    & _dbPulseTimer.IsReady)
                {
                    Quest.RequestQuestsCompleted();
                    Quest.ConsumeQuestsCompletedRequest();

                    if (ToolBox.GetWoWVersion() == "3.3.5")
                        DBQueriesWotlk.GetAvailableQuests();

                    _dbPulseTimer = new Timer(1000*60*15);
                }
            }
            catch (Exception arg)
            {
                Logging.WriteError(string.Concat(arg), true);
            }
        }
    }

    // Update Surroundings
    private void UpdateSurroundingsAndTasksPulse(object sender, DoWorkEventArgs args)
    {
        while (IsStarted)
        {
            try
            {
                if (Conditions.InGameAndConnectedAndProductStartedNotInPause 
                    && IsStarted
                    && _surroundingPulseTimer.IsReady)
                {
                    WAQTasks.UpdateStatuses();
                    WAQTasks.UpdateTasks();
                    _surroundingPulseTimer = new Timer(1000);
                }
            }
            catch (Exception arg)
            {
                Logging.WriteError(string.Concat(arg), true);
            }
        }
    }

    // GUI
    public System.Windows.Controls.UserControl Settings
    {
        get
        {
            try
            {
                if (_settingsUserControl == null)
                    _settingsUserControl = new ProductSettingsControl();
                return _settingsUserControl;
            }
            catch (Exception e)
            {
                Logger.Log("> Main > Settings(): " + e);
            }
            return null;
        }
    }

    private static int GetWowVersion()
    {
        string[] forWow = wManager.Information.ForWow.Split(new Char[] { '.' });
        return Int32.Parse(forWow[0]);
    }

    private static void Radar3DOnDrawEvent()
    {
        if (WAQTasks.TaskInProgress != null)
        {
            Radar3D.DrawLine(ObjectManager.Me.Position, WAQTasks.TaskInProgress.Location, Color.Blue);
        }

        if (WAQTasks.TaskInProgressWoWObject != null)
        {
            Radar3D.DrawLine(ObjectManager.Me.Position, WAQTasks.TaskInProgressWoWObject.Position, Color.Yellow);
            Radar3D.DrawCircle(WAQTasks.TaskInProgressWoWObject.Position, 1, Color.Yellow);
        }
    }
}
