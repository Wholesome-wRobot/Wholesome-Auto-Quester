using System;
using robotManager.Helpful;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using System.ComponentModel;
using System.IO;
using robotManager;

namespace Wholesome_Auto_Quester
{
    [Serializable]
    public class WholesomeAQSettings : Settings
    {
        public static WholesomeAQSettings CurrentSetting { get; set; }

        public WholesomeAQSettings()
        {
            LogDebug = false;

            ConfigWinForm(
                new System.Drawing.Point(400, 400), "Wholesome Auto Quester "
                + Translate.Get("Settings")
            );
        }

        [Category("Misc")]
        [DefaultValue(false)]
        [DisplayName("Log Debug")]
        [Description("For Development purpose")]
        public bool LogDebug { get; set; }

        [Category("Misc")]
        [DefaultValue(false)]
        [DisplayName("Activate Quests GUI")]
        [Description("For Development purpose")]
        public bool ActivateQuestsGUI { get; set; }

        public bool Save()
        {
            try
            {
                return Save(AdviserFilePathAndName("WholesomeAQSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
            }
            catch (Exception e)
            {
                Logging.WriteError("WholesomeAQSettings > Save(): " + e);
                return false;
            }
        }

        public static bool Load()
        {
            try
            {
                if (File.Exists(AdviserFilePathAndName("WholesomeAQSettings",
                    ObjectManager.Me.Name + "." + Usefuls.RealmName)))
                {
                    CurrentSetting = Load<WholesomeAQSettings>(
                        AdviserFilePathAndName("WholesomeAQSettings",
                        ObjectManager.Me.Name + "." + Usefuls.RealmName));
                    return true;
                }
                CurrentSetting = new WholesomeAQSettings();
            }
            catch (Exception e)
            {
                Logging.WriteError("WholesomeAQSettings > Load(): " + e);
            }
            return false;
        }
    }
}
