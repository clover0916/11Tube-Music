using ElevenTube_Music.Settings.Types;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Calls;
using Windows.Storage;

namespace ElevenTube_Music
{

    public sealed partial class MainWindow : Window
    {
        private IReadOnlyList<StorageFolder> plugins;
        public double Volume { get; set; } = 100;
        private bool IsMute = false;
        private double lastVolume;
        public bool Volume_Button_IsEnabled { get; set; } = false;

        public MainWindow()
        {
            this.InitializeComponent();

            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
        }

        private void NavigationViewControl_ItemInvoked(NavigationView sender,
              NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                var tag = args.InvokedItemContainer.Tag.ToString();
                ControlWebView(tag);
            }
        }

        private void NavigationViewControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            WebView.GoBack();
            if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/explore"))
            {
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[1];
            }
            else if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/library"))
            {
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[2];
            } else if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/")) {
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
            }
        }

        private void Open_Setting(object sender, RoutedEventArgs e)
        {
            Window settingsWindow = new()
            {
                Content = new SettingsPage(),
                Title = "Ý’è"
            };
            SolidColorBrush background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;
            SolidColorBrush buttonHover = Application.Current.Resources["TextOnAccentFillColorSecondaryBrush"] as SolidColorBrush;
            SolidColorBrush buttonPressed = Application.Current.Resources["TextOnAccentFillColorSecondaryBrush"] as SolidColorBrush;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(settingsWindow);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.TitleBar.BackgroundColor = background.Color;
            appWindow.TitleBar.InactiveBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonHoverBackgroundColor = buttonHover.Color;
            appWindow.TitleBar.ButtonPressedBackgroundColor = buttonPressed.Color;
            Windows.UI.Color w1 = new() { A = 255, R = 255, G = 255, B = 255 };
            appWindow.TitleBar.ButtonForegroundColor = w1;
            appWindow.TitleBar.ButtonHoverForegroundColor = w1;
            appWindow.TitleBar.ButtonPressedForegroundColor = w1;
            appWindow.Resize(new Windows.Graphics.SizeInt32(1080, 640));
            settingsWindow.Activate();
        }

        private void CoreWebView2_SourceChanged(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2SourceChangedEventArgs args)
        {
            if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/"))
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["LastUrl"] = WebView.Source.AbsoluteUri;
            }
        }

        private async void WebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            await WebView.ExecuteScriptAsync(@"
                const audioCtx = new AudioContext();
                const oscillator = audioCtx.createOscillator();
                oscillator.type = 'square';
                oscillator.frequency.value = 0;
                oscillator.connect(audioCtx.destination);
                oscillator.start();
                setTimeout(() => {
                    oscillator.stop();
                }, 1000);");
            NavigationViewControl.IsBackEnabled = true;

            loadingBar.Visibility = Visibility.Collapsed;
            WebView.Visibility = Visibility.Visible;
            await Task.Delay(500);
            WebView.Opacity = 1;

            Volume_Button_IsEnabled = true;
            var volume = Get_Volume();
            Volume = (double)(volume * 100);
            Bindings.Update();

            await WebView.ExecuteScriptAsync(@"
                const hideElements = [
                    { tabId: 'SPunlimited' },
                    { tabId: 'FEmusic_home' },
                    { tabId: 'FEmusic_explore' },
                    { tabId: 'FEmusic_library_landing' }
                ];

                hideElements.forEach(element => {
                    const elements = document.querySelectorAll('[tab-id=' + element.tabId + ']');
                    elements.forEach(element => {
                        element.style.display = 'none';
                    });
                });
            ");
        }

        public async void ControlWebView(string tag)
        {
            if (tag == "home")
            {
                await WebView.ExecuteScriptAsync("document.querySelector('[tab-id=\"FEmusic_home\"]').click();");
            }
            else if (tag == "explore")
            {
                await WebView.ExecuteScriptAsync("document.querySelector('[tab-id=\"FEmusic_explore\"]').click();");
            }
            else if (tag == "library")
            {
                await WebView.ExecuteScriptAsync("document.querySelector('[tab-id=\"FEmusic_library_landing\"]').click();");
            }
        }

        private async void WebView_NavigationStarting(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            if (args.Uri.ToLower().Contains("music.youtube.com/"))
            {
                await Load_Plugins(sender);

                await Task.Delay(1000);
                PluginLoadingText.Text = "Loaded";
                PluginLoading.Visibility = Visibility.Collapsed;
                pluginLoadedCheck.Visibility = Visibility.Visible;
                await Task.Delay(1000);
                PluginLoadingText.Text = "Plugins";
                pluginLoadedCheck.Visibility = Visibility.Collapsed;
            }

        }

        private async Task Load_Plugins(WebView2 sender)
        {
            StorageFolder pluginsFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Plugins");
            plugins = await pluginsFolder.GetFoldersAsync();

            bool isPlugins = false;

            foreach (StorageFolder plugin in plugins)
            {
                Debug.WriteLine(plugin.Name);
                StorageFile config = await plugin.GetFileAsync("config.json");
                string configJson = await FileIO.ReadTextAsync(config);
                PluginConfig pluginConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginConfig>(configJson);

                if (Plugin_IsEnabled(pluginConfig.name))
                {
                    openPluginsButton.IsEnabled = true;
                    isPlugins = true;
                    Grid grid = CreatePluginGrid(plugin.Name);
                    PluginList.Children.Add(grid);

                    if (pluginConfig.type == "Javascript")
                    {
                        Debug.WriteLine("Enabled");
                    }
                    StorageFile file = await plugin.GetFileAsync("index.js");
                    string text = await FileIO.ReadTextAsync(file);
                    await sender.CoreWebView2.ExecuteScriptAsync(text);
                }
            }

            if (!isPlugins)
            {
                openPluginsButton.Content = "No Plugins";
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
                TextBlock textBlock = grid.Children[0] as TextBlock;
                textBlock.Text = "Loaded " + plugin.Name;
                ProgressRing progressRing = grid.Children[1] as ProgressRing;
                progressRing.Visibility = Visibility.Collapsed;
                FontIcon icon = grid.Children[2] as FontIcon;
                icon.Visibility = Visibility.Visible;
            }
        }

        private static bool Plugin_IsEnabled(string pluginName)
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

        private void Volume_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            IsMute = false;
            double newVolume = e.NewValue;

            if (newVolume == 0)
            {
                Volume_State.Glyph = "\uE74F";
                Volume_State_Flyout.Glyph = "\uE74F";
            }
            else if (newVolume > 0 && newVolume <= 33)
            {
                Volume_State.Glyph = "\uE992";
                Volume_State_Flyout.Glyph = "\uE992";
            }
            else if (newVolume > 33 && newVolume <= 66)
            {
                Volume_State.Glyph = "\uE993";
                Volume_State_Flyout.Glyph = "\uE993";
            }
            else if (newVolume > 66)
            {
                Volume_State.Glyph = "\uE994";
                Volume_State_Flyout.Glyph = "\uE994";
            }

            Change_Volume(newVolume);
        }

        private void Change_Volume(double newVolume)
        {
            MMDeviceEnumerator deviceEnumerator = new();

            // Get the default audio endpoint device (rendering endpoint)
            MMDevice defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            // Set the volume level for the default device
            SessionCollection sessions = defaultDevice.AudioSessionManager.Sessions;

            //Find this prosses
            for (int i = 0; i < sessions.Count; i++)
            {
                var ProcessID = sessions[i].GetProcessID;
                if (WebView.CoreWebView2 == null)
                    break;
                var WebViewProcesses = WebView.CoreWebView2.Environment.GetProcessInfos();

                foreach (var WebViewProcess in WebViewProcesses)
                {
                    if (WebViewProcess.ProcessId == ProcessID)
                    {
                        sessions[i].SimpleAudioVolume.Volume = (float)newVolume / 100;
                    }
                }
            }
        }

        private float Get_Volume()
        {
            MMDeviceEnumerator deviceEnumerator = new();

            // Get the default audio endpoint device (rendering endpoint)
            MMDevice defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            // Set the volume level for the default device
            SessionCollection sessions = defaultDevice.AudioSessionManager.Sessions;

            //Find this prosses
            for (int i = 0; i < sessions.Count; i++)
            {
                var ProcessID = sessions[i].GetProcessID;
                if (WebView.CoreWebView2 == null)
                    break;
                var WebViewProcesses = WebView.CoreWebView2.Environment.GetProcessInfos();

                foreach (var WebViewProcess in WebViewProcesses)
                {
                    if (WebViewProcess.ProcessId == ProcessID)
                    {
                        return sessions[i].SimpleAudioVolume.Volume;
                    }
                }
            }
            return 0;
        }

        private void Toggle_Mute(object sender, RoutedEventArgs e)
        {
            IsMute = !IsMute;
            if (IsMute)
            {
                Volume_State.Glyph = "\uE74F";
                Volume_State_Flyout.Glyph = "\uE74F";
                lastVolume = Get_Volume() * 100;
                Change_Volume(0);
            }
            else
            {
                Volume_State.Glyph = "\uE994";
                Volume_State_Flyout.Glyph = "\uE994";
                Change_Volume(lastVolume);
            }
        }

        private void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["IsSaveSession"] == null)
            {
                localSettings.Values["IsSaveSession"] = true;
            }
            if ((bool)localSettings.Values["IsSaveSession"] == true)
            {
                if (localSettings.Values["LastUrl"] != null)
                {
                    WebView.Source = new Uri((string)localSettings.Values["LastUrl"]);
                }
                WebView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            }
        }
    }
}