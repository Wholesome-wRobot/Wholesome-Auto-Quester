using robotManager.Helpful;
using System.Drawing;

namespace Wholesome_Auto_Quester.Helpers
{
    public static class Logger
    {
        public static void Log(string str)
        {
            Logging.Write($"[{Main.productName}] " + str, Logging.LogType.Normal, Color.Brown);
        }

        public static void LogDebug(string str)
        {
            if (WholesomeAQSettings.CurrentSetting.LogDebug)
                Logging.Write($"[{Main.productName}] " + str, Logging.LogType.Debug, Color.BlueViolet);
        }
    }
}
