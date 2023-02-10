using System;
using System.Windows;
using System.Windows.Controls;
using wManager.Wow.ObjectManager;

namespace Wholesome_Auto_Quester.GUI
{
    public partial class ProductSettingsControl : UserControl
    {
        public ProductSettingsControl()
        {
            InitializeComponent();
            DataContext = WholesomeAQSettings.CurrentSetting;
            DeltaDetails.Text = GetDeltaDetailsString();
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private string GetDeltaDetailsString()
        {
            int deltaMinus = Math.Max((int)ObjectManager.Me.Level - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus, 1);
            int deltaPlus = Math.Max((int)ObjectManager.Me.Level + WholesomeAQSettings.CurrentSetting.LevelDeltaPlus, 1);
            return $"You will do quests from level {deltaMinus} to level {deltaPlus}";
        }
    }
}
