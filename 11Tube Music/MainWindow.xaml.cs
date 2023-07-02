using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Composition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.WebUI;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI.Composition;
using Windows.Storage;
using ElevenTube_Music.Settings.Types;
using System.Diagnostics;
using Microsoft.UI.Windowing;

namespace ElevenTube_Music
{

    public sealed partial class MainWindow : Window
    {

        public MainWindow()
        {
            this.InitializeComponent();

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            SolidColorBrush background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;
            SolidColorBrush buttonHover = Application.Current.Resources["ButtonPointerOverBackgroundThemeBrush"] as SolidColorBrush;
            SolidColorBrush buttonPressed = Application.Current.Resources["ButtonPressedBackgroundThemeBrush"] as SolidColorBrush;

            appWindow.TitleBar.BackgroundColor = background.Color;
            appWindow.TitleBar.InactiveBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonHoverBackgroundColor = buttonHover.Color;
            appWindow.TitleBar.ButtonPressedBackgroundColor = buttonPressed.Color;

            Windows.UI.Color w1 = new Windows.UI.Color() { A = 255, R = 255, G = 255, B = 255 };
            appWindow.TitleBar.ButtonForegroundColor = w1;
            appWindow.TitleBar.ButtonHoverForegroundColor = w1;
            appWindow.TitleBar.ButtonPressedForegroundColor = w1;

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
			if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/"))
			{
				NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
			} else if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/explore"))
			{
				NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[1];
			} else if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/library"))
			{
				  NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[2];
			}
        }

        private void Open_Setting(object sender, RoutedEventArgs e)
        {
            Window settingsWindow = new Window();
            //SettingPage‚Ì’†‚ð•\Ž¦
            settingsWindow.Content = new SettingsPage();
            settingsWindow.Title = "Ý’è";
            SolidColorBrush background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;
            SolidColorBrush buttonHover = Application.Current.Resources["ButtonPointerOverBackgroundThemeBrush"] as SolidColorBrush;
            SolidColorBrush buttonPressed = Application.Current.Resources["ButtonPressedBackgroundThemeBrush"] as SolidColorBrush;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(settingsWindow);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.TitleBar.BackgroundColor = background.Color;
            appWindow.TitleBar.InactiveBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonHoverBackgroundColor = buttonHover.Color;
            appWindow.TitleBar.ButtonPressedBackgroundColor = buttonPressed.Color;
            Windows.UI.Color w1 = new Windows.UI.Color() { A = 255, R = 255, G = 255, B = 255 };
            appWindow.TitleBar.ButtonForegroundColor = w1;
            appWindow.TitleBar.ButtonHoverForegroundColor = w1;
            appWindow.TitleBar.ButtonPressedForegroundColor = w1;
            appWindow.Resize(new Windows.Graphics.SizeInt32(1080, 640));
            appWindow.Show();
        }

        private async void WebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
			NavigationViewControl.IsBackEnabled = true;

            loadingBar.Visibility = Visibility.Collapsed;
            WebView.Visibility = Visibility.Visible;
            await Task.Delay(500);
            WebView.Opacity = 1;
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
            StorageFolder pluginsFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("Plugins");
            IReadOnlyList<StorageFolder> subfolders = await pluginsFolder.GetFoldersAsync();

            foreach (StorageFolder subfolder in subfolders)
            {
                StorageFile config = await subfolder.GetFileAsync("config.json");
                string config_json = await FileIO.ReadTextAsync(config);
                PluginConfig pluginConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<PluginConfig>(config_json);
                
                if (Plugin_IsEnabled(pluginConfig.name))
                {
                    Debug.WriteLine("Enabled");
                    StorageFile file = await subfolder.GetFileAsync("index.js");
                    string text = await FileIO.ReadTextAsync(file);
                    await sender.CoreWebView2.ExecuteScriptAsync(text);
                }
                Debug.WriteLine(subfolder.Name);
            }
        }

        private bool Plugin_IsEnabled(string pluginName)
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
    }
}
