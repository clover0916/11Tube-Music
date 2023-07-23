using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;

namespace ElevenTube_Music
{

    public partial class App : Application
    {
        private Window m_window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();

            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(m_window);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            SolidColorBrush background = Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;
            SolidColorBrush buttonHover = Current.Resources["TextOnAccentFillColorSecondaryBrush"] as SolidColorBrush;
            SolidColorBrush buttonPressed = Current.Resources["TextOnAccentFillColorSecondaryBrush"] as SolidColorBrush;

            appWindow.TitleBar.BackgroundColor = background.Color;
            appWindow.TitleBar.InactiveBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = background.Color;
            appWindow.TitleBar.ButtonHoverBackgroundColor = buttonHover.Color;
            appWindow.TitleBar.ButtonPressedBackgroundColor = buttonPressed.Color;

            appWindow.SetIcon("Assets/favicon.ico");

            m_window.Activate();
        }

    }
}
