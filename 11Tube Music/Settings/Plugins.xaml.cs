using CommunityToolkit.Labs.WinUI;
using ElevenTube_Music.Types;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                        VerticalAlignment = VerticalAlignment.Center,
                        Tag = pluginConfig.name
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
                        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

                        string existingJson = (string)localSettings.Values[pluginConfig.name];
                        PluginSetting existingPluginSetting = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginSetting>(existingJson);

                        if (existingPluginSetting != null)
                        {
                            PluginOption existingOption = existingPluginSetting.Options.Find(o => o.Name == option.name);
                            var optionToggleSwitch = new ToggleSwitch
                            {
                                Margin = new Thickness(8, 0, 0, 0),
                                IsOn = (bool)existingOption.Value,
                                Tag = option.name
                            };

                            optionToggleSwitch.Toggled += OptionToggleSwitch_Toggled;

                            // Add the option to the list
                            options.Add(new PluginOption { Name = option.name, Value = optionToggleSwitch.IsOn });

                            optionStackPanel.Children.Add(optionToggleSwitch);
                        }
                    }
                    else if (option.type == "input")
                    {
                        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                        string existingJson = (string)localSettings.Values[pluginConfig.name];
                        Debug.WriteLine(existingJson);
                        PluginSetting existingPluginSetting = null;

                        if (existingJson != null)
                        {
                            existingPluginSetting = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginSetting>(existingJson);
                        }

                        if (existingPluginSetting != null && existingPluginSetting.Options != null)
                        {
                            PluginOption existingOption = existingPluginSetting.Options.Find(o => o.Name == option.name);

                            var optionTextBox = new TextBox
                            {
                                Margin = new Thickness(8, 0, 0, 0),
                                Text = (string)existingOption?.Value ?? "",
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
                        else
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
                    }
                    else if (option.type == "combo")
                    {
                        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                        string existingJson = (string)localSettings.Values[pluginConfig.name];
                        Debug.WriteLine(existingJson);
                        PluginSetting existingPluginSetting = null;
                        if (existingJson != null)
                        {
                            existingPluginSetting = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginSetting>(existingJson);
                        }

                        if (existingPluginSetting != null && existingPluginSetting.Options != null)
                        {
                            PluginOption existingOption = existingPluginSetting.Options.Find(o => o.Name == option.name);
                            if (existingOption != null)
                            {

                                var optionComboBox = new ComboBox
                                {
                                    Margin = new Thickness(8, 0, 0, 0),
                                    Tag = option.name
                                };


                                if (option.placeholder != null)
                                {
                                    optionComboBox.PlaceholderText = option.placeholder;
                                }

                                if (option.values != null)
                                {
                                    foreach (var value in option.values)
                                    {
                                        optionComboBox.Items.Add(value);
                                    }
                                    int selectedIndex = Array.IndexOf(option.values, existingOption.Value);
                                    optionComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
                                }

                                optionComboBox.SelectionChanged += OptionComboBox_SelcetionChanged;

                                options.Add(new PluginOption { Name = option.name, Value = optionComboBox.SelectedItem });

                                optionStackPanel.Children.Add(optionComboBox);
                            }
                            else
                            {
                                var defaultOption = option.default_value;

                                var optionComboBox = new ComboBox
                                {
                                    Margin = new Thickness(8, 0, 0, 0),
                                    Tag = option.name
                                };

                                if (!string.IsNullOrEmpty(defaultOption))
                                {
                                    optionComboBox.PlaceholderText = defaultOption;
                                }

                                if (option.values != null)
                                {
                                    foreach (var value in option.values)
                                    {
                                        optionComboBox.Items.Add(value);
                                    }
                                    optionComboBox.SelectedIndex = 0;
                                }

                                optionComboBox.SelectionChanged += OptionComboBox_SelcetionChanged;

                                options.Add(new PluginOption { Name = option.name, Value = optionComboBox.SelectedItem });

                                optionStackPanel.Children.Add(optionComboBox);
                            }
                        }
                        else
                        {
                            var optionComboBox = new ComboBox
                            {
                                Margin = new Thickness(8, 0, 0, 0),
                                Tag = option.name
                            };
                            if (option.placeholder != null)
                            {
                                optionComboBox.PlaceholderText = option.placeholder;
                            }

                            if (option.values != null)
                            {
                                foreach (var value in option.values)
                                {
                                    optionComboBox.Items.Add(value);
                                }
                                optionComboBox.SelectedIndex = 0;
                            }

                            optionComboBox.SelectionChanged += OptionComboBox_SelcetionChanged;

                            // Add the option to the list
                            options.Add(new PluginOption { Name = option.name, Value = optionComboBox.SelectedItem });

                            optionStackPanel.Children.Add(optionComboBox);
                        }
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

            var contributorsCard = new SettingsCard
            {
                Header = "Contributors"
            };

            var contributorsStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (pluginConfig.contributors != null)
            {

                var contributorsText = new TextBlock
                {
                    Margin = new Thickness(0, 0, 8, 0),
                    IsTextSelectionEnabled = true,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = string.Join(", ", pluginConfig.contributors)
                };

                contributorsStackPanel.Children.Add(contributorsText);

                contributorsCard.Content = contributorsStackPanel;

                expander.Items.Add(contributorsCard);
            }

            var toggleSwitch = new ToggleSwitch();
            PluginSetting previousSetting = LoadPreviousSetting(pluginConfig.name);
            toggleSwitch.IsOn = previousSetting.Enable;
            toggleSwitch.Tag = pluginConfig.name;
            toggleSwitch.Toggled += ToggleSwitch_Toggled;

            expander.Content = toggleSwitch;

            SettingsContainer.Children.Add(expander);
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
            //Get toggleSwitch parent to get plugin name
            StackPanel parent = (StackPanel)VisualTreeHelper.GetParent(toggleSwitch);
            Debug.WriteLine(parent.Tag.ToString());
            //SaveOption(parent.Tag.ToString(), toggleSwitch.Tag.ToString(), toggleSwitch.IsOn);
        }

        private void OptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            restartCard.Visibility = Visibility.Visible;
            TextBox textBox = (TextBox)sender;
            StackPanel parent = (StackPanel)VisualTreeHelper.GetParent(textBox);
            SaveOption(parent.Tag.ToString(), textBox.Tag.ToString(), textBox.Text);
        }

        private void OptionComboBox_SelcetionChanged(object sender, SelectionChangedEventArgs e)
        {
            restartCard.Visibility = Visibility.Visible;
            ComboBox comboBox = (ComboBox)sender;
            StackPanel parent = (StackPanel)VisualTreeHelper.GetParent(comboBox);
            SaveOption(parent.Tag.ToString(), comboBox.Tag.ToString(), comboBox.SelectedItem);
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

        private static void SaveOption(string pluginName, string optionName, object optionValue)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            string existingJson = localSettings.Values[pluginName] as string;
            PluginSetting existingPluginSetting = null;

            if (!string.IsNullOrEmpty(existingJson))
            {
                existingPluginSetting = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginSetting>(existingJson);
            }

            if (existingPluginSetting == null)
            {
                existingPluginSetting = new PluginSetting { Options = new List<PluginOption>() };
            }

            PluginOption existingOption = null;
            if (existingPluginSetting.Options != null)
            {
                existingOption = existingPluginSetting.Options.Find(option => option.Name == optionName);
            }

            if (existingOption != null)
            {
                existingOption.Value = optionValue;
            }
            else
            {
                existingPluginSetting.Options ??= new List<PluginOption>();
                existingPluginSetting.Options.Add(new PluginOption
                {
                    Name = optionName,
                    Value = optionValue
                });
            }

            localSettings.Values[pluginName] = Newtonsoft.Json.JsonConvert.SerializeObject(existingPluginSetting);
        }




        private void UpdatePluginSetting(string pluginName, bool settingValue)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            string existingJson = (string)localSettings.Values[pluginName];

            if (existingJson != null)
            {
                PluginSetting existingPluginSetting = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginSetting>(existingJson);
                existingPluginSetting.Enable = settingValue;
                string updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(existingPluginSetting);
                localSettings.Values[pluginName] = updatedJson;
            }
            else
            {
                localSettings.Values[pluginName] = Newtonsoft.Json.JsonConvert.SerializeObject(new PluginSetting
                {
                    Enable = settingValue,
                    Options = null
                }); ;
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
        }
    }
}
