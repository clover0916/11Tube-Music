using ElevenTube_Music.Types;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Windows.Forms;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using static Vanara.PInvoke.User32;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ElevenTube_Music.Plugins.MiniPlayer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MiniPlayer : Window
    {
        private MainWindow mainWindow;
        public MiniPlayer(MainWindow window, List<PluginOption> Options)
        {
            mainWindow=window;
            this.InitializeComponent();
            mainWindow.VideoDetailReceived += HandleVideoDetailReceived;
            mainWindow.VideoPaused += HandleVideoPaused;
            var windowHandle = new IntPtr((long)this.AppWindow.Id.Value);

            this.AppWindow.MoveAndResize(new RectInt32(0, 0, 400, 110));

            if (Options != null)
            {
                PluginOption opacityOption = Options.Find(option => option.Name == "position");
                
                if (opacityOption != null)
                {
                    string position = opacityOption.Value as string;

                    int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                    int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
                    
                    if (position=="TopLeft") this.AppWindow.Move(new PointInt32(10, 10));
                    else if (position=="TopRight") this.AppWindow.Move(new PointInt32(screenWidth - 410,0));
                    else if (position=="BottomLeft") this.AppWindow.Move(new PointInt32(0, screenHeight-120));
                    else if (position== "BottomRight") this.AppWindow.Move(new PointInt32(screenWidth - 410, screenHeight - 120));
                }
            }

            SeekBar.Value = 0;
            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, e) =>
            {
                SeekBar.Value += 1;

                if (SeekBar.Value >= SeekBar.Maximum)
                {
                    PlayIcon.Symbol = Symbol.Play;
                    timer.Stop();
                }
            };
            SeekBar.ValueChanged += HandleSeekBarChanged;
            PlayButton.Click += PlayButton_Click;
            PreviousButton.Click += PreviousButton_Click;
            NextButton.Click += NextButton_Click;

            SetWindowLong(windowHandle, WindowLongFlags.GWL_EXSTYLE, (IntPtr)(GetWindowLong(windowHandle, WindowLongFlags.GWL_EXSTYLE) | (int)WindowStylesEx.WS_EX_LAYERED | (int)WindowStylesEx.WS_EX_NOACTIVATE));
        }
        private DispatcherTimer timer;
        private void HandleVideoDetailReceived(Types.VideoDetail videoDetail)
        {
            SeekBar.Maximum = Convert.ToDouble(videoDetail.lengthSeconds);
            SongName.Text = videoDetail.title;
            ArtistName.Text = videoDetail.author;
            ThumbnailImage.Source = new BitmapImage(new Uri(videoDetail.thumbnail.thumbnails[0].url));
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.NextMusic();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.PreviousMusic();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.PlayPauseMusic();
        }

        private void HandleVideoPaused(IsPaused IsPaused)
        {
            if (IsPaused.paused)
            {
                SeekBar.Value = IsPaused.currentTime;
                PlayIcon.Symbol = Symbol.Play;
                timer.Stop();
            }
            else
            {
                //Use IsPaused.currentTime
                SeekBar.Value = IsPaused.currentTime;
                PlayIcon.Symbol = Symbol.Pause;
                timer.Start();
            }
        }

        private void HandleSeekBarChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if(Math.Abs(e.NewValue - e.OldValue) > 1)
            {
                mainWindow.SetPlayerSeek((int)e.NewValue);
            }
        }
    }
}
