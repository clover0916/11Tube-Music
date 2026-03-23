using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;

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

            try
            {
                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(m_window);
                Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                var background = TryGetBrush("ApplicationPageBackgroundThemeBrush")?.Color;
                var buttonHover = TryGetBrush("TextOnAccentFillColorSecondaryBrush")?.Color;
                var buttonPressed = TryGetBrush("TextOnAccentFillColorSecondaryBrush")?.Color;

                if (background.HasValue)
                {
                    appWindow.TitleBar.BackgroundColor = background.Value;
                    appWindow.TitleBar.InactiveBackgroundColor = background.Value;
                    appWindow.TitleBar.ButtonBackgroundColor = background.Value;
                    appWindow.TitleBar.ButtonInactiveBackgroundColor = background.Value;
                }

                if (buttonHover.HasValue)
                {
                    appWindow.TitleBar.ButtonHoverBackgroundColor = buttonHover.Value;
                }

                if (buttonPressed.HasValue)
                {
                    appWindow.TitleBar.ButtonPressedBackgroundColor = buttonPressed.Value;
                }

                string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "favicon.ico");
                if (File.Exists(iconPath))
                {
                    appWindow.SetIcon(iconPath);
                }
            }
            catch
            {
            }

            m_window.Activate();
        }

        private SolidColorBrush TryGetBrush(string key)
        {
            if (Current?.Resources == null || !Current.Resources.ContainsKey(key))
            {
                return null;
            }

            return Current.Resources[key] as SolidColorBrush;
        }

    }
}
