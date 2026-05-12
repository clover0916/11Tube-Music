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
using Microsoft.Windows.ApplicationModel.Resources;

namespace ElevenTube_Music
{
    public sealed partial class MainWindow : Window
    {
        private sealed class PluginLoadItem
        {
            public StorageFolder Folder { get; init; }
            public PluginConfig Config { get; init; }
        }

        private IReadOnlyList<StorageFolder> plugins;
        private readonly Dictionary<string, Grid> pluginGrids = new();

        private async Task Load_Plugins(WebView2 sender)
        {
            StorageFolder pluginsFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Plugins");
            plugins = await pluginsFolder.GetFoldersAsync();

            UpgradeSettingsIfNeeded(plugins);

            pluginGrids.Clear();
            PluginList.Children.Clear();

            var enabledPlugins = new List<PluginLoadItem>();

            foreach (StorageFolder plugin in plugins)
            {
                try
                {
                    StorageFile config = await plugin.GetFileAsync("config.json");
                    string configJson = await FileIO.ReadTextAsync(config);
                    PluginConfig pluginConfig = JsonConvert.DeserializeObject<PluginConfig>(configJson);

                    if (pluginConfig != null && Plugin_IsEnabled(pluginConfig.name))
                    {
                        Grid grid = CreatePluginGrid(plugin.Name);
                        pluginGrids[plugin.Name] = grid;
                        PluginList.Children.Add(grid);
                        enabledPlugins.Add(new PluginLoadItem { Folder = plugin, Config = pluginConfig });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Plugin metadata load failed: {plugin.Name} - {ex}");
                }
            }

            if (enabledPlugins.Count == 0)
            {
                var loader = new ResourceLoader();
                openPluginsButton.Content = loader.GetString("No_Plugins");
                openPluginsButton.IsEnabled = false;
                return;
            }

            openPluginsButton.IsEnabled = true;

            var loadTasks = new List<Task>(enabledPlugins.Count);
            foreach (PluginLoadItem plugin in enabledPlugins)
            {
                loadTasks.Add(LoadPluginSafely(sender, plugin));
            }

            await Task.WhenAll(loadTasks);
        }

        private async Task LoadPluginSafely(WebView2 sender, PluginLoadItem plugin)
        {
            try
            {
                Debug.WriteLine(plugin.Folder.Name);

                if (plugin.Config.type == "Javascript")
                {
                    StorageFile file = await plugin.Folder.GetFileAsync("index.js");
                    string text = await FileIO.ReadTextAsync(file);
                    await sender.CoreWebView2.ExecuteScriptAsync(text).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
                }
                else if (plugin.Config.type == "C#")
                {
                    string pluginName = plugin.Config.name;
                    string methodName = "Main";
                    Type pluginType = Type.GetType("ElevenTube_Music.Plugins." + pluginName + ".main");
                    if (pluginType == null)
                    {
                        throw new InvalidOperationException("指定されたプラグインが見つかりませんでした。");
                    }

                    MethodInfo method = pluginType.GetMethod(methodName);
                    if (method == null)
                    {
                        throw new MissingMethodException("指定されたメソッドが見つかりませんでした。");
                    }

                    PluginSetting pluginSetting = GetPluginSetting(pluginName);
                    ParameterInfo[] parameters = method.GetParameters();
                    object instance = Activator.CreateInstance(pluginType);
                    if (instance == null)
                    {
                        throw new InvalidOperationException("プラグインのインスタンス生成に失敗しました。");
                    }

                    var context = new PluginContext(this);
                    var store = PlaybackStateStore.Instance;
                    object[] args;
                    if (parameters.Length >= 3)
                    {
                        args = new object[] { store, context, pluginSetting.Options };
                    }
                    else
                    {
                        args = new object[] { store, context };
                    }

                    await InvokePluginMethodOnUiThread(method, instance, args).WaitAsync(TimeSpan.FromSeconds(10));
                }

                SetPluginGridStatus(plugin.Folder.Name, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Plugin load failed: {plugin.Folder.Name} - {ex}");
                SetPluginGridStatus(plugin.Folder.Name, false);
            }
        }

        private Task InvokePluginMethodOnUiThread(MethodInfo method, object instance, object[] args)
        {
            return RunOnUiThreadAsync(async () =>
            {
                object result = method.Invoke(instance, args);
                if (result is Task taskResult)
                {
                    await taskResult;
                }
            });
        }

        private Task RunOnUiThreadAsync(Func<Task> action)
        {
            if (DispatcherQueue.HasThreadAccess)
            {
                return action();
            }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            bool enqueued = DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            if (!enqueued)
            {
                tcs.SetException(new InvalidOperationException("UI スレッドへの処理登録に失敗しました。"));
            }

            return tcs.Task;
        }

        private static PluginSetting GetPluginSetting(string pluginName)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values[pluginName] is string json && !string.IsNullOrWhiteSpace(json))
            {
                PluginSetting pluginSetting = JsonConvert.DeserializeObject<PluginSetting>(json);
                if (pluginSetting != null)
                {
                    pluginSetting.Options ??= new List<PluginOption>();
                    return pluginSetting;
                }
            }

            return new PluginSetting
            {
                Enable = true,
                Options = new List<PluginOption>()
            };
        }

        private void SetPluginGridStatus(string pluginFolderName, bool isLoaded)
        {
            if (!pluginGrids.TryGetValue(pluginFolderName, out Grid grid))
            {
                return;
            }

            void UpdateUi()
            {
                TextBlock textBlock = grid.Children[0] as TextBlock;
                ProgressRing progressRing = grid.Children[1] as ProgressRing;
                FontIcon icon = grid.Children[2] as FontIcon;

                if (textBlock != null)
                {
                    textBlock.Text = (isLoaded ? "Loaded " : "Failed ") + pluginFolderName;
                }

                if (progressRing != null)
                {
                    progressRing.Visibility = Visibility.Collapsed;
                    progressRing.IsActive = false;
                }

                if (icon != null)
                {
                    icon.Glyph = isLoaded ? "\uE73E" : "\uEA39";
                    icon.Visibility = Visibility.Visible;
                }
            }

            if (DispatcherQueue.HasThreadAccess)
            {
                UpdateUi();
                return;
            }

            DispatcherQueue.TryEnqueue(UpdateUi);
        }

        private void UpgradeSettingsIfNeeded(IReadOnlyList<StorageFolder> plugins)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            foreach (StorageFolder plugin in plugins)
            {
                if (localSettings.Values[plugin.Name] is bool existingValue)
                {
                    var options = new List<PluginOption>
                    {
                        new PluginOption { Name = "Enable", Value = existingValue }
                    };

                    var pluginSetting = new PluginSetting
                    {
                        Enable = existingValue,
                        Options = options
                    };

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(pluginSetting);
                    localSettings.Values[plugin.Name] = json;
                }
            }
        }

        private static bool Plugin_IsEnabled(string pluginName)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            try
            {
                if (localSettings.Values[pluginName] is string json && !string.IsNullOrWhiteSpace(json))
                {
                    PluginSetting pluginSetting = JsonConvert.DeserializeObject<PluginSetting>(json);
                    return pluginSetting?.Enable == true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Plugin setting read failed: {pluginName} - {ex}");
            }

            return false;
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

        private void Open_Plugins_Flyout(object sender, RoutedEventArgs e)
        {
        }
    }
}
