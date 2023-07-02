using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.Contacts;
using System.Diagnostics;
using Windows.Storage;
using ElevenTube_Music.Settings.Types;
using CommunityToolkit.Labs.WinUI;
using WinRT;

namespace ElevenTube_Music.Settings
{
    public sealed partial class Plugins : Page
    {
        public Plugins()
        {
            this.InitializeComponent();
            Load_Plugins();
        }

        private async void Load_Plugins()
        {
            StorageFolder pluginsFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Plugins");
            IReadOnlyList<StorageFolder> subfolders = await pluginsFolder.GetFoldersAsync();

            foreach (StorageFolder subfolder in subfolders)
            {
                Debug.WriteLine(subfolder.Name);
                StorageFile config = await subfolder.GetFileAsync("config.json");
                string config_json = await FileIO.ReadTextAsync(config);
                PluginConfig pluginConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginConfig>(config_json);
                Add_SettingsExpander(pluginConfig);
            }
        }

        private void Add_SettingsExpander(PluginConfig pluginConfig)
        {
            var expander = new SettingsExpander();
            expander.Header = pluginConfig.name;
            expander.Description = pluginConfig.description;

            var versionCard = new SettingsCard();
            versionCard.Header = "Version";
            var versionText = new TextBlock();
            versionText.IsTextSelectionEnabled = true;
            versionText.Text = pluginConfig.version;
            versionCard.Content = versionText;
            expander.Items.Add(versionCard);

            var authorCard = new SettingsCard();
            authorCard.Header = "Author";
            var authorStackPanel = new StackPanel();
            authorStackPanel.Orientation = Orientation.Horizontal;
            authorStackPanel.VerticalAlignment = VerticalAlignment.Center;

            var authorText = new TextBlock();
            authorText.Margin = new Thickness(0, 0, 8, 0);
            authorText.IsTextSelectionEnabled = true;
            authorText.VerticalAlignment = VerticalAlignment.Center;
            authorText.Text = pluginConfig.author.name;
            authorStackPanel.Children.Add(authorText);

            pluginConfig.author.contact.ForEach(contact =>
            {
                var Link = new HyperlinkButton();
                Link.Margin = new Thickness(8, 0, 0, 0);
                Link.Content = contact.name;
                Link.NavigateUri = new Uri(contact.url);
                authorStackPanel.Children.Add(Link);
            });

            authorCard.Content = authorStackPanel;
            expander.Items.Add(authorCard);

            var toggleSwitch = new ToggleSwitch();
            bool previousSetting = LoadPreviousSetting(pluginConfig.name);
            toggleSwitch.IsOn = previousSetting;

            toggleSwitch.Toggled += ToggleSwitch_Toggled;

            expander.Content = toggleSwitch;

            SettingsContainer.Children.Add(expander);
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            restartCard.Visibility = Visibility.Visible;
            ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
            var expander = (SettingsCard)toggleSwitch.Parent;
            SaveSetting(expander.Header.ToString(), toggleSwitch.IsOn);
        }

        private bool LoadPreviousSetting(string pluginName)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values[pluginName] != null)
            {
                return (bool)localSettings.Values[pluginName];
            }
            else
            {
                return false;
            }
        }

        private void SaveSetting(string pluginName, bool settingValue)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[pluginName] = settingValue;
        }

        private void restartButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }
    }
}
