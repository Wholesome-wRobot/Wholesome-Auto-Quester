using robotManager.Helpful;
using System.Drawing;

namespace Wholesome_Auto_Quester.Helpers
{
    public class Logger
    {
        public static string ScannerString = "";
        public static string TaskMString = "";


        public static void Log(string str)
        {
            Logging.Write($"[{Main.ProductName}] " + str, Logging.LogType.Normal, Color.Brown);
        }

        public static void LogDebug(string str)
        {
            if (WholesomeAQSettings.CurrentSetting.LogDebug)
            {
                Logging.Write($"[{Main.ProductName}] " + str, Logging.LogType.Debug, Color.Chocolate);
            }
        }

        public static void LogError(string str)
        {
            Logging.Write($"[{Main.ProductName}] " + str, Logging.LogType.Error, Color.Red);
        }

        public static void LogWatchScanner(string str, long timeEllapsed)
        {
            ScannerString = $"{str} [{timeEllapsed}]";

            if (timeEllapsed > 50)
                Logging.Write($"{str} [{timeEllapsed}]", Logging.LogType.Error, Color.DarkMagenta);
        }

        public static void LogWatchTask(string str, long timeEllapsed)
        {
            TaskMString = $"{str} [{timeEllapsed}]";

            if (timeEllapsed > 50)
                Logging.Write($"{str} [{timeEllapsed}]", Logging.LogType.Error, Color.DarkMagenta);
        }
    }
}
