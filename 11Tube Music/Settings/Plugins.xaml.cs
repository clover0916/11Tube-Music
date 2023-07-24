using CommunityToolkit.Labs.WinUI;
using ElevenTube_Music.Types;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;

namespace ElevenTube_Music.Settings
{
    public sealed partial class Plugins : Page
    {

        public Plugins()
        {
            this.InitializeComponent();
            Load_Plugins();
        }

        private async Task<List<string>> GetPluginNamesFromFolderAsync()
        {
            List<string> pluginNames = new List<string>();
            StorageFolder pluginsFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Plugins");
            IReadOnlyList<StorageFolder> subfolders = await pluginsFolder.GetFoldersAsync();

            foreach (StorageFolder subfolder in subfolders)
            {
                pluginNames.Add(subfolder.Name);
            }

            return pluginNames;
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
            var expander = new SettingsExpander
            {
                Header = pluginConfig.display_name ?? pluginConfig.name,
                Description = pluginConfig.description
            };

            // Create a list to store the options
            List<PluginOption> options = new List<PluginOption>();

            if (pluginConfig.option != null)
            {
                foreach (var option in pluginConfig.option)
                {
                    var optionCard = new SettingsCard
                    {
                        Header = option.display_name ?? option.name
                    };
                    var optionStackPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var optionDescription = new TextBlock
                    {
                        Margin = new Thickness(8, 0, 0, 0),
                        Text = option.description,
                        VerticalAlignment = VerticalAlignment.Center,
                        Opacity = 0.6
                    };

                    optionStackPanel.Children.Add(optionDescription);

                    if (option.type == "toggle")
                    {
                        var optionToggleSwitch = new ToggleSwitch
                        {
                            Margin = new Thickness(8, 0, 0, 0),
                            IsOn = false,
                            Tag = option.name
                        };

                        optionToggleSwitch.Toggled += OptionToggleSwitch_Toggled;

                        // Add the option to the list
                        options.Add(new PluginOption { Name = option.name, Value = optionToggleSwitch.IsOn });

                        optionStackPanel.Children.Add(optionToggleSwitch);
                    }
                    else if (option.type == "input")
                    {
                        var optionTextBox = new TextBox
                        {
                            Margin = new Thickness(8, 0, 0, 0),
                            Text = "",
                            Tag = option.name
                        };

                        if (option.placeholder != null)
                        {
                            optionTextBox.PlaceholderText = option.placeholder;
                        }

                        optionTextBox.TextChanged += OptionTextBox_TextChanged;

                        // Add the option to the list
                        options.Add(new PluginOption { Name = option.name, Value = optionTextBox.Text });

                        optionStackPanel.Children.Add(optionTextBox);
                    }

                    optionCard.Content = optionStackPanel;
                    expander.Items.Add(optionCard);
                };
            }

            var versionCard = new SettingsCard
            {
                Header = "Version"
            };
            var versionText = new TextBlock
            {
                IsTextSelectionEnabled = true,
                Text = pluginConfig.version
            };
            versionCard.Content = versionText;
            expander.Items.Add(versionCard);

            var authorCard = new SettingsCard
            {
                Header = "Author"
            };
            var authorStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var authorText = new TextBlock
            {
                Margin = new Thickness(0, 0, 8, 0),
                IsTextSelectionEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                Text = pluginConfig.author.name
            };
            authorStackPanel.Children.Add(authorText);

            pluginConfig.author.contact.ForEach(contact =>
            {
                var Link = new HyperlinkButton
                {
                    Margin = new Thickness(8, 0, 0, 0),
                    Content = contact.name,
                    NavigateUri = new Uri(contact.url)
                };
                authorStackPanel.Children.Add(Link);
            });

            authorCard.Content = authorStackPanel;
            expander.Items.Add(authorCard);

            var toggleSwitch = new ToggleSwitch();
            PluginSetting previousSetting = LoadPreviousSetting(pluginConfig.name);
            toggleSwitch.IsOn = previousSetting.Enable;
            toggleSwitch.Tag = pluginConfig.name;
            toggleSwitch.Toggled += ToggleSwitch_Toggled;

            expander.Content = toggleSwitch;

            SettingsContainer.Children.Add(expander);

            // Save the options for this plugin
            SaveSetting(pluginConfig.name, toggleSwitch.IsOn, options);
        }

        private void ToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            restartCard.Visibility = Visibility.Visible;
            ToggleSwitch toggleSwitch = (ToggleSwitch)sender;

            // Update the plugin setting
            UpdatePluginSetting(toggleSwitch.Tag.ToString(), toggleSwitch.IsOn);
        }

        private void OptionToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            restartCard.Visibility = Visibility.Visible;
            ToggleSwitch toggleSwitch = (ToggleSwitch)sender;
            //SaveSetting(toggleSwitch.Tag.ToString(), toggleSwitch.IsOn);
        }

        private void OptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            restartCard.Visibility = Visibility.Visible;
            TextBox textBox = (TextBox)sender;
            //SaveSetting(textBox.Tag.ToString(), textBox.Text);
        }

        private static PluginSetting ConvertToNewFormat(bool enable, bool oldFormat, List<PluginOption> oldOptions)
        {
            if (oldFormat)
            {
                List<PluginOption> newOptions = new List<PluginOption>();
                foreach (var oldOption in oldOptions)
                {
                    if (oldOption.Value is bool boolValue)
                    {
                        newOptions.Add(new PluginOption { Name = oldOption.Name, Value = boolValue });
                    }
                    else if (oldOption.Value is string stringValue)
                    {
                        newOptions.Add(new PluginOption { Name = oldOption.Name, Value = stringValue });
                    }
                }

                return new PluginSetting
                {
                    Enable = enable,
                    Options = newOptions
                };
            }
            else
            {
                return new PluginSetting
                {
                    Enable = enable,
                    Options = oldOptions
                };
            }
        }

        private static PluginSetting LoadPreviousSetting(string pluginName)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values[pluginName] != null)
            {
                string existingJson = (string)localSettings.Values[pluginName];

                try
                {
                    PluginSetting existingPluginSetting = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginSetting>(existingJson);
                    return existingPluginSetting;
                }
                catch
                {
                    List<PluginOption> oldOptions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PluginOption>>(existingJson);

                    PluginSetting newPluginSetting = ConvertToNewFormat(true, true, oldOptions);
                    return newPluginSetting;
                }
            }
            else
            {
                return new PluginSetting
                {
                    Enable = false,
                    Options = new List<PluginOption>()
                };
            }
        }

        private static void SaveSetting(string pluginName, bool settingValue, List<PluginOption> options)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // Create the PluginSetting object and set its properties
            PluginSetting pluginSetting = new PluginSetting
            {
                Enable = settingValue,
                Options = options
            };

            // Convert the PluginSetting object to JSON
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(pluginSetting);

            localSettings.Values[pluginName] = json;
        }

        private void UpdatePluginSetting(string pluginName, bool settingValue)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // Load the existing JSON for this plugin
            string existingJson = (string)localSettings.Values[pluginName];

            // Convert the existing JSON back to PluginSetting object
            PluginSetting existingPluginSetting = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginSetting>(existingJson);

            // Update the 'Enable' property of the PluginSetting object
            existingPluginSetting.Enable = settingValue;

            // Convert the updated PluginSetting object back to JSON
            string updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(existingPluginSetting);

            // Save the updated JSON
            localSettings.Values[pluginName] = updatedJson;
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }
    }
}
