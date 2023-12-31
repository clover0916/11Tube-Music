using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Windows.Globalization;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ElevenTube_Music.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class General : Page
    {
        public bool IsSaveSession
        {
            get
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values["IsSaveSession"] == null)
                {
                    localSettings.Values["IsSaveSession"] = true;
                }
                return (bool)localSettings.Values["IsSaveSession"];
            }
            set
            {
                Toggle_Changed("IsSaveSession", value);
            }
        }

        public General()
        {
            this.InitializeComponent();
        }

        private void Toggle_Changed(string key, bool value)
        {
            Debug.WriteLine("Toggle_Changed: " + key + " " + value);
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[key] = value;
            restartCard.Visibility = Visibility.Visible;
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem item = (ComboBoxItem)LanguageCombo.SelectedItem;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            if ((string)localSettings.Values["Language"] != item.Tag.ToString())
            {
                ApplicationLanguages.PrimaryLanguageOverride = item.Tag.ToString();
                localSettings.Values["Language"] = item.Tag.ToString();
                restartCard.Visibility = Visibility.Visible;
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }

        private void LanguageCombo_Loaded(object sender, RoutedEventArgs e)
        {
            // Check local settings
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["Language"] == null)
            {
                //Get the language from the system
                string language = ApplicationLanguages.Languages[0];

                localSettings.Values["Language"] = language;

            } else
            {
                string language = (string)localSettings.Values["Language"];
                foreach (ComboBoxItem item in LanguageCombo.Items)
                {
                    if (item.Tag.ToString().Contains(language))
                    {
                        LanguageCombo.SelectedItem = item;
                        break;
                    }
                }
            }
        }
    }
}
