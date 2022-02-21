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
            RecordUnreachables.IsChecked = WholesomeAQSettings.CurrentSetting.RecordUnreachables;
            GrindOnly.IsChecked = WholesomeAQSettings.CurrentSetting.GrindOnly;
            ContinentTravel.IsChecked = WholesomeAQSettings.CurrentSetting.ContinentTravel;
            AbandonUnfit.IsChecked = WholesomeAQSettings.CurrentSetting.AbandonUnfitQuests;
            GoToMobEntry.Value = WholesomeAQSettings.CurrentSetting.GoToMobEntry;
            StopAtLevel.Value = WholesomeAQSettings.CurrentSetting.StopAtLevel;
        }

        private void StopAtLevelChanged(object sender, RoutedEventArgs e)
        {
            if (StopAtLevel.Value != null)
                WholesomeAQSettings.CurrentSetting.StopAtLevel = (int)StopAtLevel.Value;
            else
                WholesomeAQSettings.CurrentSetting.StopAtLevel = 0;
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private void GoToMobEntryChanged(object sender, RoutedEventArgs e)
        {
            if (GoToMobEntry.Value != null)
                WholesomeAQSettings.CurrentSetting.GoToMobEntry = (int)GoToMobEntry.Value;
            else
                WholesomeAQSettings.CurrentSetting.GoToMobEntry = 0;
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private void AbandonUnfitChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.AbandonUnfitQuests = (bool)AbandonUnfit.IsChecked;
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private void ContinentTravelChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.ContinentTravel = (bool)ContinentTravel.IsChecked;
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private void GrindOnlyChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.GrindOnly = (bool)GrindOnly.IsChecked;
            WholesomeAQSettings.CurrentSetting.Save();
        }

        private void RecordUnreachablesChanged(object sender, RoutedEventArgs e)
        {
            WholesomeAQSettings.CurrentSetting.RecordUnreachables = (bool)RecordUnreachables.IsChecked;
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
            if (LevelDeltaMinus.Value != null)
                WholesomeAQSettings.CurrentSetting.LevelDeltaMinus = (int)LevelDeltaMinus.Value;
            else
                WholesomeAQSettings.CurrentSetting.LevelDeltaMinus = 0;
            WholesomeAQSettings.CurrentSetting.Save();
            DeltaDetails.Text = GetDeltaDetailsString();
        }

        private void LevelDeltaPlusChanged(object sender, RoutedEventArgs e)
        {
            if (LevelDeltaPlus.Value != null)
                WholesomeAQSettings.CurrentSetting.LevelDeltaPlus = (int)LevelDeltaPlus.Value;
            else
                WholesomeAQSettings.CurrentSetting.LevelDeltaPlus = 0;
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
