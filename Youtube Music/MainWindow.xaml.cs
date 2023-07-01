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

namespace Youtube_Music
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

            ContentFrame.Navigate(typeof(MainPage));
            NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
        }

        private void NavigationViewControl_ItemInvoked(NavigationView sender,
              NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer != null)
            {
                var tag = args.InvokedItemContainer.Tag.ToString();
                Frame mainFrame = (Frame)ContentFrame;
                MainPage mainPage = (MainPage)mainFrame.Content;
                mainPage.ControlWebView(tag);
            }
        }

        private void NavigationViewControl_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack) ContentFrame.GoBack();
        }

        public void Navigation_Back_Enable(bool isEnable)
        {
            NavigationViewControl.IsBackEnabled = true;
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NavigationViewControl.IsBackEnabled = ContentFrame.CanGoBack;
        }

        private void Open_Setting(object sender, RoutedEventArgs e)
        {
            Window settingsWindow = new Window();
            //SettingPageÇÃíÜÇï\é¶
            settingsWindow.Content = new SettingsPage();
            settingsWindow.Title = "ê›íË";
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
            settingsWindow.Activate();
        }
    }
}
