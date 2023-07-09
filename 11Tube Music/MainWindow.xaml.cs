using ElevenTube_Music.Settings.Types;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;

namespace ElevenTube_Music
{

    public sealed partial class MainWindow : Window
    {
        private bool isStarted = false;
        private IReadOnlyList<StorageFolder> plugins;
        public double Volume { get; set; } = 100;
        private bool IsMute = false;
        private double lastVolume;
        public bool Volume_Button_IsEnabled { get; set; } = false;
        public Types.VideoDetail VideoDetail;
        public bool IsPaused;
        public double CurrentTime;

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
            }
            else if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/"))
            {
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
            }
        }

        private void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            NavigationViewControl.IsBackEnabled = true;
            WebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
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

        private async void CoreWebView2_DOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
        {
            StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFile file = await storageFolder.GetFileAsync("preload.js");
            string preloadScript = await FileIO.ReadTextAsync(file);
            await sender.ExecuteScriptAsync(preloadScript);

            loadingBar.Visibility = Visibility.Collapsed;
            WebView.Visibility = Visibility.Visible;
            await Task.Delay(500);
            WebView.Opacity = 1;

            Volume_Button_IsEnabled = true;
            var volume = Get_Volume();
            Volume = (double)(volume * 100);
            Bindings.Update();
        }

        private async void WebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (args.Uri.ToLower().Contains("music.youtube.com/") && isStarted == false)
            {
                isStarted = true;
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

        private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
        {
            if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/"))
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["LastUrl"] = WebView.Source.AbsoluteUri;
            }
        }

        public event Action<Types.VideoDetail> VideoDetailReceived;
        public event Action<Types.IsPaused> VideoPaused;

        private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var data = args.WebMessageAsJson;
            //use Newtonsoft.Json
            var msg = JsonConvert.DeserializeObject<Types.Message>(data);
            var type = msg.Type;
            if (type == "isPaused")
            {
                Debug.WriteLine(msg.Data.ToString());
                Types.IsPaused isPaused = JsonConvert.DeserializeObject<Types.IsPaused>(msg.Data.ToString());
                IsPaused = isPaused.paused;
                CurrentTime = isPaused.currentTime;
                VideoPaused?.Invoke(isPaused);
            }
            else if (type == "videoDetail")
            {
                Types.VideoDetail videoDetail = JsonConvert.DeserializeObject<Types.VideoDetail>(msg.Data.ToString());
                VideoDetail = videoDetail;
                VideoDetailReceived?.Invoke(VideoDetail);
            }
        }


        private void Open_Setting(object sender, RoutedEventArgs e)
        {
            Window settingsWindow = new()
            {
                Content = new SettingsPage(),
                Title = "設定"
            };
            SolidColorBrush background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;
            SolidColorBrush buttonHover = Application.Current.Resources["TextOnAccentFillColorSecondaryBrush"] as SolidColorBrush;
            SolidColorBrush buttonPressed = Application.Current.Resources["TextOnAccentFillColorSecondaryBrush"] as SolidColorBrush;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            Microsoft.UI.Windowing.AppWindow Window = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            IntPtr s_hwnd = WinRT.Interop.WindowNative.GetWindowHandle(settingsWindow);
            WindowId s_windowId = Win32Interop.GetWindowIdFromWindow(s_hwnd);
            Microsoft.UI.Windowing.AppWindow s_Window = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(s_windowId);
            s_Window.TitleBar.BackgroundColor = background.Color;
            s_Window.TitleBar.InactiveBackgroundColor = background.Color;
            s_Window.TitleBar.ButtonBackgroundColor = background.Color;
            s_Window.TitleBar.ButtonInactiveBackgroundColor = background.Color;
            s_Window.TitleBar.ButtonHoverBackgroundColor = buttonHover.Color;
            s_Window.TitleBar.ButtonPressedBackgroundColor = buttonPressed.Color;
            Windows.UI.Color w1 = new() { A = 255, R = 255, G = 255, B = 255 };
            s_Window.TitleBar.ButtonForegroundColor = w1;
            s_Window.TitleBar.ButtonHoverForegroundColor = w1;
            s_Window.TitleBar.ButtonPressedForegroundColor = w1;
            s_Window.Resize(new Windows.Graphics.SizeInt32(1080, 640));
            s_Window.SetIcon("Assets/favicon.ico");

            SetWindowLongPtr(s_hwnd, -8, hwnd);

            var Presenter = OverlappedPresenter.Create();
            Presenter.IsModal = true;
            s_Window.SetPresenter(Presenter);

            settingsWindow.Activate();
        }

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

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
                PluginConfig pluginConfig = JsonConvert.DeserializeObject<PluginConfig>(configJson);

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
                                object instance = Activator.CreateInstance(pluginType);
                                method.Invoke(instance, new[] { this });
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

    }
}