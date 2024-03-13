using ElevenTube_Music.Types;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Windows.Graphics;
using WinRT;
using static Vanara.PInvoke.User32;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ElevenTube_Music.Plugins.MiniPlayer
{
    public sealed partial class MiniPlayer : Window
    {
        private MainWindow mainWindow;
        private int lastX;
        private int lastY;
        private string HiddenIcon = "\uE973";
        private string ShowIcon = "\uE974";
        private Thickness hiddenPadding = new(10, 10, 4, 10);

        WindowsSystemDispatcherQueueHelper wsdqHelper;
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration configurationSource;
        MicaController micaController;

        public MiniPlayer(MainWindow window, List<PluginOption> Options)
        {
            mainWindow = window;
            InitializeComponent();

            wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            mainWindow.VideoDetailReceived += HandleVideoDetailReceived;
            mainWindow.VideoPaused += HandleVideoPaused;
            var windowHandle = new IntPtr((long)AppWindow.Id.Value);

            AppWindow.MoveAndResize(new RectInt32(10, 10, 400, 120));

            if (Options != null)
            {
                PluginOption opacityOption = Options.Find(option => option.Name == "position");

                if (opacityOption != null)
                {
                    string position = opacityOption.Value as string;

                    int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                    int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;

                    if (position == "TopLeft") AppWindow.Move(new PointInt32(10, 10));
                    else if (position == "TopRight")
                    {
                        HiddenIcon = "\uE974";
                        ShowIcon = "\uE973";
                        hiddenPadding = new Thickness(4, 10, 10, 10);
                        AppWindow.Move(new PointInt32(screenWidth - 410, 10));
                    }
                    else if (position == "BottomLeft") AppWindow.Move(new PointInt32(10, screenHeight - 130));
                    else if (position == "BottomRight")
                    {
                        HiddenIcon = "\uE974";
                        ShowIcon = "\uE973";
                        hiddenPadding = new Thickness(4, 10, 10, 10);
                        AppWindow.Move(new PointInt32(screenWidth - 410, screenHeight - 130));
                    }
                }

                PluginOption backdropOption = Options.Find(option => option.Name == "backdrop");

                if (backdropOption != null)
                {
                    string backdrop = backdropOption.Value as string;

                    if (backdrop == "Transparent") TransparentHelper.SetTransparent(this, true);
                    else if (backdrop == "Mica")
                    {
                        configurationSource = null;
                        TrySetMicaBackdrop(false);
                        configurationSource.IsInputActive = true;
                    }
                    else if (backdrop == "MicaAlt")
                    {
                        configurationSource = null;
                        TrySetMicaBackdrop(true);
                        configurationSource.IsInputActive = true;
                    }
                    else
                    {
                        configurationSource = null;
                        TrySetAcrylicBackdrop();
                        configurationSource.IsInputActive = true;
                    }
                }
                else
                {
                    configurationSource = null;
                    TrySetAcrylicBackdrop();
                    configurationSource.IsInputActive = true;
                }
            }
            else
            {
                configurationSource = null;
                TrySetAcrylicBackdrop();
                configurationSource.IsInputActive = true;
            }


            SeekBar.Value = 0;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, e) =>
            {
                SeekBar.Value += 1;

                if (SeekBar.Value >= SeekBar.Maximum)
                {
                    PlayIcon.Glyph = "\uE768";
                    timer.Stop();
                }
            };

            SetWindowLong(windowHandle, WindowLongFlags.GWL_EXSTYLE, (IntPtr)(GetWindowLong(windowHandle, WindowLongFlags.GWL_EXSTYLE) | (int)WindowStylesEx.WS_EX_LAYERED | (int)WindowStylesEx.WS_EX_NOACTIVATE));
        }

        private bool TrySetAcrylicBackdrop()
        {
            if (DesktopAcrylicController.IsSupported())
            {
                configurationSource = new SystemBackdropConfiguration();

                configurationSource.IsInputActive = true;
                switch (((FrameworkElement)this.Content).ActualTheme)
                {
                    case ElementTheme.Dark: configurationSource.Theme = SystemBackdropTheme.Dark; break;
                    case ElementTheme.Light: configurationSource.Theme = SystemBackdropTheme.Light; break;
                    case ElementTheme.Default: configurationSource.Theme = SystemBackdropTheme.Default; break;
                }

                acrylicController = new DesktopAcrylicController();

                acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                acrylicController.SetSystemBackdropConfiguration(configurationSource);
                return true;
            }
            return false;
        }

        private bool TrySetMicaBackdrop(bool alt)
        {
            if (MicaController.IsSupported())
            {
                configurationSource = new SystemBackdropConfiguration();

                configurationSource.IsInputActive = true;
                switch (((FrameworkElement)this.Content).ActualTheme)
                {
                    case ElementTheme.Dark: configurationSource.Theme = SystemBackdropTheme.Dark; break;
                    case ElementTheme.Light: configurationSource.Theme = SystemBackdropTheme.Light; break;
                    case ElementTheme.Default: configurationSource.Theme = SystemBackdropTheme.Default; break;
                }

                micaController = new MicaController();

                if (alt) micaController.Kind = MicaKind.BaseAlt;

                micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                micaController.SetSystemBackdropConfiguration(configurationSource);
                return true;
            }

            return false;
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int screenHeight = Screen.PrimaryScreen.WorkingArea.Height;
            PointInt32 positon = this.AppWindow.Position;
            int x = lastX;
            int y = lastY;
            if (lastX > screenWidth / 2) x = screenWidth - 410;
            if (lastY > screenHeight / 2) y = screenHeight - 130;
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
            lastX = positon.X;
            lastY = positon.Y;
            int x = positon.X;
            int y = positon.Y;
            if (positon.X > screenWidth / 2) x = screenWidth - 50;
            if (positon.Y > screenHeight / 2) y = screenHeight - 40;
            MainGrid.Visibility = Visibility.Collapsed;
            SeekBar.Visibility = Visibility.Collapsed;
            ShowButton.Visibility = Visibility.Visible;
            RootGrid.Margin = new Thickness(0);

            if (positon.X > screenWidth / 2)
            {
                if (positon.Y > screenHeight / 2)
                {
                    this.AppWindow.MoveAndResize(new RectInt32(x + 30, y - 10, 30, 40));
                }
                else
                {
                    this.AppWindow.MoveAndResize(new RectInt32(x + 30, y, 30, 40));
                }
            }
            else
            {
                if (positon.Y > screenHeight / 2)
                {
                    this.AppWindow.MoveAndResize(new RectInt32(x - 20, y - 10, 30, 40));
                }
                else
                {
                    this.AppWindow.MoveAndResize(new RectInt32(x - 20, y, 30, 40));
                }
            }
        }

        private DispatcherTimer timer;
        private void HandleVideoDetailReceived(VideoDetail videoDetail)
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
                PlayIcon.Glyph = "\uE768";
                timer.Stop();
            }
            else
            {
                //Use IsPaused.currentTime
                SeekBar.Value = IsPaused.currentTime;
                PlayIcon.Glyph = "\uE769";
                timer.Start();
            }
        }

        private void HandleSeekBarChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if(Math.Round(Math.Abs(e.NewValue - e.OldValue)) > 1)
            {
                mainWindow.SetPlayerSeek((int)e.NewValue);
            }
        }
    }
}
