using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace ElevenTube_Music
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow _appWindow;

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            return AppWindow.GetFromWindowId(myWndId);
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
            if (WebView.CanGoBack)
            {
                WebView.GoBack();
            }
        }

        public async void ControlWebView(string tag)
        {
            if (tag == "home")
            {
                await WebView.ExecuteScriptAsync("document.querySelector(\"#guide-renderer\").querySelector(\"#sections\").childNodes[0].querySelector(\"#items\").childNodes[0].click();");
            }
            else if (tag == "explore")
            {
                await WebView.ExecuteScriptAsync("document.querySelector(\"#guide-renderer\").querySelector(\"#sections\").childNodes[0].querySelector(\"#items\").childNodes[1].click();");
            }
            else if (tag == "library")
            {
                await WebView.ExecuteScriptAsync("document.querySelector(\"#guide-renderer\").querySelector(\"#sections\").childNodes[0].querySelector(\"#items\").childNodes[2].click();");
            }
            else if (tag == "addPlaylist")
            {
                await WebView.ExecuteScriptAsync("document.querySelectorAll(\"#sections\")[0].childNodes[1].querySelector(\"button\").click()");

            }
            else if (tag.Contains("playlist"))
            {
                var SelectedItem = SideNavigation.SelectedItem as NavigationViewItem;
                var item = SelectedItem.Tag;
                int index = int.Parse(item.ToString().Replace("playlist-", ""));

                string script = "document.querySelectorAll(\"#sections\")[0].childNodes[1].querySelector(\"#items\").querySelectorAll(\".title-column\")";


                await WebView.ExecuteScriptAsync(script + "[" + index + "].click();");
            }
        }

        private static NavigationViewItem CreatePlaylistItem(Types.Playlist playlist, int i)
        {
            NavigationViewItem item = new()
            {
                Content = playlist.title,
                Tag = "playlist-" + i
            };
            return item;
        }

        private void Open_Setting(object sender, RoutedEventArgs e)
        {
            Window settingsWindow = new()
            {
                Content = new SettingsPage(),
                Title = "Settings",
            };
            SolidColorBrush background = Microsoft.UI.Xaml.Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            IntPtr s_hwnd = WinRT.Interop.WindowNative.GetWindowHandle(settingsWindow);
            WindowId s_windowId = Win32Interop.GetWindowIdFromWindow(s_hwnd);
            AppWindow s_Window = AppWindow.GetFromWindowId(s_windowId);
            s_Window.TitleBar.BackgroundColor = background.Color;
            s_Window.TitleBar.InactiveBackgroundColor = background.Color;
            s_Window.TitleBar.ButtonBackgroundColor = background.Color;
            s_Window.TitleBar.ButtonInactiveBackgroundColor = background.Color;
            s_Window.Resize(new Windows.Graphics.SizeInt32(1080, 640));
            s_Window.SetIcon("Assets/favicon.ico");

            SetWindowLongPtr(s_hwnd, -8, hwnd);

            var Presenter = OverlappedPresenter.Create();
            Presenter.IsModal = true;
            Presenter.IsMaximizable = false;
            Presenter.IsMinimizable = false;
            s_Window.SetPresenter(Presenter);

            settingsWindow.Activate();
        }
    }
}