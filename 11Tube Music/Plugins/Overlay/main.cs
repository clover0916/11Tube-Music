using System;
using Microsoft.UI.Windowing;
using Vanara.PInvoke;
using Windows.Graphics;
using WinRT.Interop;

namespace ElevenTube_Music.Plugins.Overlay
{
    internal class main
    {
        private OverlayWindow overlayWindow;

        public void Main(MainWindow window)
        {
            overlayWindow = new OverlayWindow(window);

            overlayWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

            overlayWindow.AppWindow.MoveAndResize(new RectInt32(0, 0, 400, 100));

            window.Closed += (s, e) => overlayWindow.Close();

            var windowHandle = new IntPtr((long)overlayWindow.AppWindow.Id.Value);

            overlayWindow.Activate();
        }
    }
}
