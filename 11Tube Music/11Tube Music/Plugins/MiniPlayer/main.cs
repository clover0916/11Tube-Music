using ElevenTube_Music.Plugins.Overlay;
using ElevenTube_Music.Types;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Graphics;
using Windows.UI.WindowManagement;

namespace ElevenTube_Music.Plugins.MiniPlayer
{
    internal class main
    {
        private MiniPlayer miniPlayer;

        public void Main(MainWindow window, List<PluginOption> Options)
        {
            miniPlayer = new MiniPlayer(window, Options);

            OverlappedPresenter op = OverlappedPresenter.Create();

            op.IsResizable = false;
            
            op.IsAlwaysOnTop = true;

            op.SetBorderAndTitleBar(false, false);

            miniPlayer.AppWindow.SetPresenter(op);

            window.Closed += (s, e) => miniPlayer.Close();

            var windowHandle = new IntPtr((long)miniPlayer.AppWindow.Id.Value);

            miniPlayer.Activate();
        }
    }
}
