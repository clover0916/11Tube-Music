using ElevenTube_Music.Types;
using Microsoft.UI.Composition.SystemBackdrops;
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
using System.Runtime.InteropServices;
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

            this.AppWindow.MoveAndResize(new RectInt32(0, 0, 400, 120));

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
                    else if (position=="BottomLeft") this.AppWindow.Move(new PointInt32(0, screenHeight-130));
                    else if (position== "BottomRight") this.AppWindow.Move(new PointInt32(screenWidth - 410, screenHeight - 130));
                }

                PluginOption backdropOption = Options.Find(option => option.Name == "backdrop");

                if (backdropOption != null)
                {
                    string backdrop = backdropOption.Value as string;

                    if (backdrop == "Transparent") TransparentHelper.SetTransparent(this, true);
                    else if (backdrop == "Mica") SystemBackdrop = new MicaBackdrop();
                    else if (backdrop == "MicaAlt") SystemBackdrop = new MicaBackdrop() { Kind = MicaKind.BaseAlt };
                    else this.SystemBackdrop = new DesktopAcrylicBackdrop();
                }
                else this.SystemBackdrop = new DesktopAcrylicBackdrop();
            }
            else this.SystemBackdrop = new DesktopAcrylicBackdrop();
            

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
            HideButton.Click += HideButton_Click;
            ShowButton.Click += ReturnButton_Click;
            SetWindowLong(windowHandle, WindowLongFlags.GWL_EXSTYLE, (IntPtr)(GetWindowLong(windowHandle, WindowLongFlags.GWL_EXSTYLE) | (int)WindowStylesEx.WS_EX_LAYERED | (int)WindowStylesEx.WS_EX_NOACTIVATE));
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            PointInt32 positon = this.AppWindow.Position;
            int x = positon.X;
            int y = positon.Y;
            if (positon.X > screenWidth / 2) x = screenWidth - 410;
            if (positon.Y > screenHeight / 2) y = screenHeight - 130;
            MainGrid.Visibility = Visibility.Visible;
            SeekBar.Visibility = Visibility.Visible;
            ShowButton.Visibility = Visibility.Collapsed;
            RootGrid.Margin = new Thickness(8);

            this.AppWindow.MoveAndResize(new RectInt32(x, y, 400, 120));
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            PointInt32 positon = this.AppWindow.Position;
            int x = positon.X;
            int y = positon.Y;
            if (positon.X > screenWidth/2) x= screenWidth-50;
            if (positon.Y > screenHeight/2) y= screenHeight-35;
            MainGrid.Visibility = Visibility.Collapsed;
            SeekBar.Visibility= Visibility.Collapsed;
            ShowButton.Visibility = Visibility.Visible;
            RootGrid.Margin= new Thickness(0);
            
            this.AppWindow.MoveAndResize(new RectInt32(x, y, 40, 25));
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
            if(Math.Abs(e.NewValue - e.OldValue) > 1.1)
            {
                mainWindow.SetPlayerSeek((int)e.NewValue);
            }
        }
    }
}
