﻿using robotManager.Events;
using robotManager.Helpful;
using robotManager.Products;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wholesome_Auto_Quester;
using Wholesome_Auto_Quester.Bot;
using Wholesome_Auto_Quester.GUI;
using Wholesome_Auto_Quester.Helpers;
using WholesomeToolbox;
using wManager;
using wManager.Plugin;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using static wManager.Wow.Helpers.Channel;
using static wManager.Wow.Helpers.PathFinderClass.FindPathPacket;

public class Main : IProduct
{
    public static readonly string ProductName = "Wholesome Auto Quester";
    public static readonly string FileName = "Wholesome_Auto_Quester";
    private ProductSettingsControl _settingsUserControl;
    private readonly QuestsTrackerGUI tracker = new QuestsTrackerGUI();
    private readonly WAQBot _bot = new WAQBot();
    private readonly string version = FileVersionInfo.GetVersionInfo(Others.GetCurrentDirectory + $@"\Products\{FileName}.dll").FileVersion;

    public bool IsStarted { get; private set; }

    public void Initialize()
    {
        try
        {
            if (Information.Version.StartsWith("1.7.2"))
            {
                MessageBox.Show($"This version of WRobot ({Information.Version}) is incompatible with the Auto Quester");
                Logger.LogError($"This version of WRobot ({Information.Version}) is incompatible with the Auto Quester");
                return;
            }
            WholesomeAQSettings.Load();
        }
        catch (Exception e)
        {
            Logging.WriteError("Main > Initialize(): " + e);
        }
    }

    public void Start()
    {
        try
        {
            if (ObjectManager.Me.Level >= WholesomeAQSettings.CurrentSetting.StopAtLevel)
            {
                Logger.Log($"You are at, or above your maximum set level ({WholesomeAQSettings.CurrentSetting.StopAtLevel}). Stopping.");
                return;
            }

            if (AutoUpdater.CheckUpdate(version))
            {
                return;
            }

            if (WholesomeAQSettings.CurrentSetting.ActivateQuestsGUI)
            {
                tracker.ShowWindow();
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            string zipPath = Others.GetCurrentDirectory + @"Data\AQ.zip";
            string jsonPath = Others.GetCurrentDirectory + @"Data\AQ.json";

            // Regenerate JSON every time when in dev mode
            if (WholesomeAQSettings.CurrentSetting.DevMode)
            {
                File.Delete(jsonPath);
            }

            // unzip json into data folder
            if (!File.Exists(jsonPath))
            {
                Logger.Log($"Extracting AQ.json to your data folder");
                File.Delete(zipPath);
                using (Stream compressedStream = assembly.GetManifestResourceStream("Wholesome_Auto_Quester.Database.AQ.zip"))
                {
                    using (FileStream outputFileStream = new FileStream(zipPath, FileMode.CreateNew, FileAccess.Write))
                    {
                        compressedStream.CopyTo(outputFileStream);
                        compressedStream.Close();
                    }
                }
                ZipFile.ExtractToDirectory(zipPath, Others.GetCurrentDirectory + @"Data");
                File.Delete(zipPath);
            }

            if (!WholesomeAQSettings.CurrentSetting.RecordUnreachables)
            {
                WholesomeAQSettings.CurrentSetting.RecordedUnreachables.Clear();
                WholesomeAQSettings.CurrentSetting.Save();
            }

            IsStarted = true;
            WTSettings.AddRecommendedBlacklistZones();
            LoggingEvents.OnAddLog += AddLogHandler;
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += EventsWithArgsHandler;
            EventsLua.AttachEventLua("PLAYER_DEAD", e => PlayerDeadHandler(e));

            if (!Products.IsStarted)
            {
                IsStarted = false;
                return;
            }

            Task.Run(async () =>
            {
                while (IsStarted)
                {
                    try
                    {
                        if (Conditions.InGameAndConnectedAndProductStartedNotInPause)
                        {
                            Quest.RequestQuestsCompleted();
                            Quest.ConsumeQuestsCompletedRequest();
                        }
                        await Task.Delay(5 * 1000);
                    }
                    catch (Exception arg)
                    {
                        Logging.WriteError(string.Concat(arg));
                    }
                }
            });

            if (_bot.Pulse(tracker, this))
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
            wManagerSetting.GetListZoneBlackListed().Clear();
            tracker.HideWindow();
            LoggingEvents.OnAddLog -= AddLogHandler;
            EventsLuaWithArgs.OnEventsLuaStringWithArgs -= EventsWithArgsHandler;
            _bot.Dispose();
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

    // LOG EVENTS
    private void AddLogHandler(Logging.Log log)
    {
        if (log.Text == "[Fight] Mob seem bugged" && ObjectManager.Target.Guid > 0)
        {
            Logger.Log("这个怪>>" + ObjectManager.Target +"<<是bug怪，拉黑");
            BlacklistHelper.AddNPC(ObjectManager.Target.Guid, "Mob seem bugged");
        }
        else if (log.Text == "PathFinder server seem down, use offline pathfinder.")
        {
            Stop();
            MessageBox.Show("The pathfinder server is down, please close and resart WRobot");
            Logger.LogError($"The pathfinder server is down, please close and resart WRobot");
        }
    }

    // GUI
    public UserControl Settings
    {
        get
        {
            try
            {
                if (_settingsUserControl == null)
                {
                    _settingsUserControl = new ProductSettingsControl();
                }
                return _settingsUserControl;
            }
            catch (Exception e)
            {
                Logger.Log("> Main > Settings(): " + e);
            }

            return null;
        }
    }

    private void PlayerDeadHandler(object context)
    {
        BlacklistHelper.AddZone(ObjectManager.Me.Position, 20, "Death");
    }

    private void EventsWithArgsHandler(string id, List<string> args)
    {
        if (id == "UI_ERROR_MESSAGE" && args[0] == "You cannot attack that target.")
        {
            if (ObjectManager.Target != null)
            {
                BlacklistHelper.AddNPC(ObjectManager.Target.Guid, $"Can't attack this target");
            }
        }
    }
}