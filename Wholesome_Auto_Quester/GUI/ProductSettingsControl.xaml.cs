using System.Windows;
using System.Windows.Controls;

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
