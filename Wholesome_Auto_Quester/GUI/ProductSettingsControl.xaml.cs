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
            LogDebug.IsChecked = WholesomeAQSettings.CurrentSetting.LogDebug;
            ActivateQuestsGUI.IsChecked = WholesomeAQSettings.CurrentSetting.ActivateQuestsGUI;
            DevMode.IsChecked = WholesomeAQSettings.CurrentSetting.DevMode;
            LevelDeltaMinus.Value = WholesomeAQSettings.CurrentSetting.LevelDeltaMinus;
            LevelDeltaPlus.Value = WholesomeAQSettings.CurrentSetting.LevelDeltaPlus;
            DeltaDetails.Text = GetDeltaDetailsString();
            SmoothMove.IsChecked = WholesomeAQSettings.CurrentSetting.SmoothMove;
            GrindOnly.IsChecked = WholesomeAQSettings.CurrentSetting.GrindOnly;
        }

        private void GrindOnlyChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.GrindOnly = (bool)GrindOnly.IsChecked;
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private void SmoothMoveChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.SmoothMove = (bool)SmoothMove.IsChecked;
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private string GetDeltaDetailsString()
        {
            int deltaMinus = Math.Max((int)ObjectManager.Me.Level - WholesomeAQSettings.CurrentSetting.LevelDeltaMinus, 1);
            int deltaPlus = Math.Max((int)ObjectManager.Me.Level + WholesomeAQSettings.CurrentSetting.LevelDeltaPlus, 1);
            return $"You will do quests from level {deltaMinus} to level {deltaPlus}";
        }

        private void LevelDeltaMinusChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.LevelDeltaMinus = (int)LevelDeltaMinus.Value;
            WholesomeAQSettings.CurrentSetting.Save();
            DeltaDetails.Text = GetDeltaDetailsString();
        }

        private void LevelDeltaPlusChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.LevelDeltaPlus = (int)LevelDeltaPlus.Value;
            WholesomeAQSettings.CurrentSetting.Save();
            DeltaDetails.Text = GetDeltaDetailsString();
        }

        private void DevModeChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.DevMode = (bool)DevMode.IsChecked;
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private void LogDebugChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.LogDebug = (bool)LogDebug.IsChecked;
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private void ActivateQuestsGUIChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.ActivateQuestsGUI = (bool)ActivateQuestsGUI.IsChecked;
            WholesomeAQSettings.CurrentSetting.Save();
        }
    }
}
