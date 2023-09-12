using ElevenTube_Music.Types;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;

namespace ElevenTube_Music
{
    public sealed partial class MainWindow : Window
    {
        private IReadOnlyList<StorageFolder> plugins;

        private async Task Load_Plugins(WebView2 sender)
        {
            StorageFolder pluginsFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Plugins");
            plugins = await pluginsFolder.GetFoldersAsync();

            UpgradeSettingsIfNeeded(plugins);

            bool isPlugins = false;

            foreach (StorageFolder plugin in plugins)
            {
                Debug.WriteLine(plugin.Name);
                StorageFile config = await plugin.GetFileAsync("config.json");
                string configJson = await FileIO.ReadTextAsync(config);
                Types.PluginConfig pluginConfig = JsonConvert.DeserializeObject<Types.PluginConfig>(configJson);

                if (Plugin_IsEnabled(pluginConfig.name))
                {
                    openPluginsButton.IsEnabled = true;
                    isPlugins = true;
                    Grid grid = CreatePluginGrid(plugin.Name);
                    PluginList.Children.Add(grid);

                    if (pluginConfig.type == "Javascript")
                    {
                        StorageFile file = await plugin.GetFileAsync("index.js");
                        string text = await FileIO.ReadTextAsync(file);
                        await sender.CoreWebView2.ExecuteScriptAsync(text);
                    }
                    else if (pluginConfig.type == "C#")
                    {
                        string pluginName = pluginConfig.name;
                        string methodName = "Main";
                        Type pluginType = Type.GetType("ElevenTube_Music.Plugins." + pluginName + ".main");
                        if (pluginType != null)
                        {
                            MethodInfo method = pluginType.GetMethod(methodName);
                            if (method != null)
                            {
                                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                                if (localSettings.Values[pluginName] != null)
                                {
                                    PluginSetting pluginSetting = JsonConvert.DeserializeObject<PluginSetting>(localSettings.Values[pluginName].ToString());
                                    ParameterInfo[] parameters = method.GetParameters();
                                    if(parameters.Length>=2 && parameters[1] != null)
                                    {
                                        object instance = Activator.CreateInstance(pluginType);
                                        object[] methodParams = new object[] { this, pluginSetting.Options };

                                        method.Invoke(instance, methodParams);
                                    }
                                    else
                                    {
                                        object instance = Activator.CreateInstance(pluginType);
                                        method.Invoke(instance, new[] { this });
                                    }
                                }
                            }
                            else
                            {
                                Debug.WriteLine("指定されたメソッドが見つかりませんでした。");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("指定されたプラグインが見つかりませんでした。");
                        }
                    }
                }
            }

            if (!isPlugins)
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                openPluginsButton.Content = resourceLoader.GetString("No_Plugins");
            }
        }

        private void UpgradeSettingsIfNeeded(IReadOnlyList<StorageFolder> plugins)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            foreach (StorageFolder plugin in plugins)
            {
                // Check if the existing setting is a bool value (old format)
                if (localSettings.Values[plugin.Name] is bool existingValue)
                {
                    // Convert the old bool value to the new format with a list of PluginOption
                    var options = new List<PluginOption>
                        {
                            new PluginOption { Name = "Enable", Value = existingValue }
                        };

                    // Create the PluginSetting object with the new format
                    var pluginSetting = new PluginSetting
                    {
                        Enable = existingValue,
                        Options = options
                    };

                    // Convert the PluginSetting object to JSON and save it as the new format
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(pluginSetting);
                    localSettings.Values[plugin.Name] = json;
                }
            }
        }

        private static bool Plugin_IsEnabled(string pluginName)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values[pluginName] != null)
            {
                PluginSetting pluginSetting = JsonConvert.DeserializeObject<PluginSetting>(localSettings.Values[pluginName].ToString());
                return pluginSetting.Enable;
            }
            else
            {
                return false;
            }
        }


        private static Grid CreatePluginGrid(string pluginName)
        {
            Grid grid = new()
            {
                Name = pluginName,
                Margin = new Thickness(4, 8, 4, 8)
            };

            ColumnDefinition column1 = new()
            {
                Width = new GridLength(1, GridUnitType.Star)
            };

            ColumnDefinition column2 = new()
            {
                Width = new GridLength(1, GridUnitType.Auto)
            };

            grid.ColumnDefinitions.Add(column1);
            grid.ColumnDefinitions.Add(column2);

            TextBlock textBlock = new()
            {
                Text = "Loading " + pluginName,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 14,
                Margin = new Thickness(0, 0, 8, 0)
            };

            Grid.SetColumn(textBlock, 0);

            ProgressRing progressRing = new()
            {
                IsActive = true,
                Height = 16,
                Width = 16
            };

            FontIcon icon = new()
            {
                Glyph = "\uE73E",
                FontSize = 16,
                Margin = new Thickness(8, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };

            Grid.SetColumn(progressRing, 1);
            Grid.SetColumn(icon, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(progressRing);
            grid.Children.Add(icon);

            return grid;
        }

        private async void Open_Plugins_Flyout(object sender, RoutedEventArgs e)
        {
            await Task.Delay(500);
            foreach (StorageFolder plugin in plugins)
            {
                Grid grid = PluginList.FindName(plugin.Name) as Grid;
                if (grid == null)
                {
                    return;
                }
                TextBlock textBlock = grid.Children[0] as TextBlock;
                textBlock.Text = "Loaded " + plugin.Name;
                ProgressRing progressRing = grid.Children[1] as ProgressRing;
                progressRing.Visibility = Visibility.Collapsed;
                FontIcon icon = grid.Children[2] as FontIcon;
                icon.Visibility = Visibility.Visible;
            }
        }
    }
}