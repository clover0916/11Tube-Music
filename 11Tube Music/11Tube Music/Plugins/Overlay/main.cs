using System;
using ElevenTube_Music.Types;
using System.Collections.Generic;
using Microsoft.UI.Windowing;
using Vanara.PInvoke;
using Windows.Graphics;
using WinRT.Interop;
using System.Windows.Forms;

namespace ElevenTube_Music.Plugins.Overlay
{
    internal class main
    {
        private OverlayWindow overlayWindow;

        public void Main(PlaybackStateStore store, PluginContext context, List<PluginOption> options)
        {
            int windowWidth = Screen.PrimaryScreen.WorkingArea.Width;

            int windowHeight = Screen.PrimaryScreen.WorkingArea.Height;

            overlayWindow = new OverlayWindow(store, context, options);

            overlayWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

            overlayWindow.AppWindow.MoveAndResize(new RectInt32(0, 0, windowWidth, windowHeight));

            context.MainWindow.Closed += (s, e) => overlayWindow.Close();

            var windowHandle = new IntPtr((long)overlayWindow.AppWindow.Id.Value);

            overlayWindow.Activate();
        }
    }
}
