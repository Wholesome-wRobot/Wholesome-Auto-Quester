using robotManager.Helpful;
using robotManager.Products;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Wholesome_Auto_Quester.Helpers;

namespace Wholesome_Auto_Quester
{
    public static class AutoUpdater
    {
        public static bool CheckUpdate(string mainVersion)
        {
            Version currentVersion = new Version(mainVersion);

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
                catch (Exception e)
                {
                    Logger.LogError($"Error while deleting dump file: {e}");
                    return true;
                }
            }

            // If last update try was < 30 seconds ago, we exit to avoid looping
            if (timeSinceLastUpdate < 30)
            {
                Logger.Log($"Last update attempt was {timeSinceLastUpdate} seconds ago. Exiting updater.");
                return false;
            }

            try
            {
                WholesomeAQSettings.CurrentSetting.LastUpdateDate = elapsedTicks;
                WholesomeAQSettings.CurrentSetting.Save();

                string onlineDllLink = "https://github.com/Wholesome-wRobot/Wholesome-Auto-Quester/raw/master/Wholesome_Auto_Quester/Compiled/Wholesome_Auto_Quester.dll";
                string onlineVersionLink = "https://raw.githubusercontent.com/Wholesome-wRobot/Wholesome-Auto-Quester/master/Wholesome_Auto_Quester/Compiled/Auto_Version.txt";

                string onlineVersionTxt = new WebClient { Encoding = Encoding.UTF8 }.DownloadString(onlineVersionLink);
                Version onlineVersion = new Version(onlineVersionTxt);

                if (onlineVersion.CompareTo(currentVersion) <= 0)
                {
                    Logger.Log($"Your version is up to date ({currentVersion} / {onlineVersion})");
                    return false;
                }

                byte[] onlineDllContent = new WebClient { Encoding = Encoding.UTF8 }.DownloadData(onlineDllLink);

                if (onlineDllContent != null && onlineDllContent.Length > 0)
                {
                    Logger.Log($"Updating your version {currentVersion} to online Version {onlineVersion}");

                    File.Move(currentFile, oldFile);

                    Logger.Log("Writing file");
                    File.WriteAllBytes(currentFile, onlineDllContent); // Replace user file by online file
                    File.Delete(Others.GetCurrentDirectory + @"Data\AQ.json"); // Delete AQ.json to retrigger an exctraction

                    Thread.Sleep(1000);

                    Logger.LogError($"A new version of the Wholesome Auto Quester has been downloaded, please restart WRobot.".ToUpper() +
                        $"\r{currentVersion} => {onlineVersion}".ToUpper());
                    Products.DisposeProduct();

                    return true;
                }
            }
            catch (Exception e)
            {
                Logging.Write("Auto update: " + e);
            }
            return false;
        }
    }
}
