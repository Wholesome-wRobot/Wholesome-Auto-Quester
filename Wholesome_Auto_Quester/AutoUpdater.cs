using robotManager.Helpful;
using robotManager.Products;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Wholesome_Auto_Quester;
using Wholesome_Auto_Quester.Helpers;

public static class AutoUpdater
{
    private static string _currentVersion = null;
    private static string _onlineVersion = null;

    public static bool CheckUpdate(string mainVersion)
    {
        _currentVersion = mainVersion;
        DateTime dateBegin = new DateTime(2020, 1, 1);
        DateTime currentDate = DateTime.Now;

        long elapsedTicks = currentDate.Ticks - dateBegin.Ticks;
        elapsedTicks /= 10000000;

        double timeSinceLastUpdate = elapsedTicks - WholesomeAQSettings.CurrentSetting.LastUpdateDate;
        string currentFile = Others.GetCurrentDirectory + $@"\Products\{Main.FileName}.dll";
        string oldFile = Others.GetCurrentDirectory + $@"\Products\{Main.FileName} dmp";

        // On supprime la vieille version
        if (File.Exists(oldFile))
        {
            try
            {
                var fs = new FileStream(oldFile, FileMode.Open);
                if (fs.CanWrite)
                {
                    Logger.Log("Deleting dump file");
                    fs.Close();
                    File.Delete(oldFile);
                }
                fs.Close();
            }
            catch
            {
                ShowReloadMessage();
                return true;
            }
        }

        // If last update try was < 10 seconds ago, we exit to avoid looping
        if (timeSinceLastUpdate < 30)
        {
            Logger.Log($"Last update attempt was {timeSinceLastUpdate} seconds ago. Exiting updater.");
            return false;
        }

        try
        {
            WholesomeAQSettings.CurrentSetting.LastUpdateDate = elapsedTicks;
            WholesomeAQSettings.CurrentSetting.Save();
            Logger.Log("Starting updater");
            string onlineFile = "https://github.com/Wholesome-wRobot/Wholesome-Auto-Quester/raw/master/Wholesome_Auto_Quester/Compiled/Wholesome_Auto_Quester.dll";
            string onlineVersion = "https://raw.githubusercontent.com/Wholesome-wRobot/Wholesome-Auto-Quester/master/Wholesome_Auto_Quester/Compiled/Version.txt";

            _onlineVersion = new System.Net.WebClient { Encoding = Encoding.UTF8 }.DownloadString(onlineVersion);

            Logger.Log($"Online Version : {_onlineVersion}");
            if (_onlineVersion == null || _onlineVersion.Length > 10 || _onlineVersion == _currentVersion)
            {
                Logger.Log($"Your version is up to date ({_currentVersion})");
                return false;
            }

            byte[] onlineFileContent = new System.Net.WebClient { Encoding = Encoding.UTF8 }.DownloadData(onlineFile);

            if (onlineFileContent != null && onlineFileContent.Length > 0)
            {
                Logger.Log($"Your version : {_currentVersion}");
                Logger.Log("Trying to update");

                File.Move(currentFile, oldFile);

                Logger.Log("Writing file");
                File.WriteAllBytes(currentFile, onlineFileContent); // replace user file by online file

                Thread.Sleep(2000);

                ShowReloadMessage();
                return true;
            }
        }
        catch (Exception e)
        {
            Logging.Write("Auto update: " + e);
        }
        return false;
    }

    private static void ShowReloadMessage()
    {
        Logger.LogError($"A new version of {Main.ProductName} has been downloaded, please restart WRobot.".ToUpper() +
            $"\r{_currentVersion} => {_onlineVersion}".ToUpper());
        Products.DisposeProduct();
    }

    public static bool CheckDbDownload()
    {
        // Download DB if needed
        if (!File.Exists("Data/WoWDB335"))
        {
            Logger.Log($"Downloading WoWDB335. Please wait...");
            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile("https://s3-eu-west-1.amazonaws.com/wholesome.team/WoWDb335.zip",
                    "Data/wholesome_db_temp.zip");
                }
                catch (WebException e)
                {
                    Logger.LogError($"Failed to download/write Wholesome Database!\n" + e.Message);
                    return false;
                }
            }

            Logger.Log($"Extracting Wholesome Database.");

            System.IO.Compression.ZipFile.ExtractToDirectory("Data/wholesome_db_temp.zip", "Data");
            File.Delete("Data/wholesome_db_temp.zip");

            Logger.Log($"Successfully downloaded Wholesome Database");
        }
        return true;
    }
}
