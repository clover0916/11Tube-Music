using ElevenTube_Music.Types;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace ElevenTube_Music.Plugins.Overlay
{
    public sealed partial class OverlayWindow : Window
    {
        ComCtl32.SUBCLASSPROC wndProcHandler;

        public OverlayWindow(MainWindow mainWindow, List<PluginOption> Options)
        {
            this.InitializeComponent();
            mainWindow.VideoDetailReceived += HandleVideoDetailReceived;
            mainWindow.VideoPaused += HandleVideoPaused;

            if (Options != null)
            {
                PluginOption opacityOption = Options.Find(option => option.Name == "opacity");

                if (opacityOption != null)
                {
                    RootGrid.Opacity = double.Parse(opacityOption.Value as string);
                }


                PluginOption positionOption = Options.Find(option => option.Name == "positon");
                
                if(positionOption != null)
                { 
                    string position = positionOption.Value as string;
                    Debug.WriteLine("Overlaypositon_" + position);
                    if(position == "TopLeft")
                    {
                        RooStackPanel.HorizontalAlignment = HorizontalAlignment.Left;

                        RooStackPanel.VerticalAlignment = VerticalAlignment.Top;
                    }
                    else if(position== "TopRight")
                    {
                        Debug.WriteLine("TopRight");
                        RooStackPanel.HorizontalAlignment = HorizontalAlignment.Right;

                        RooStackPanel.VerticalAlignment = VerticalAlignment.Top;
                    }
                    else if (position == "BottomLeft")
                    {
                        RooStackPanel.HorizontalAlignment = HorizontalAlignment.Left;

                        RooStackPanel.VerticalAlignment = VerticalAlignment.Bottom;
                    }
                    else if(position =="BottomRight")
                    {
                        RooStackPanel.HorizontalAlignment = HorizontalAlignment.Right;

                        RooStackPanel.VerticalAlignment = VerticalAlignment.Bottom;
                    }
                }
            }


            var windowHandle = new IntPtr((long)this.AppWindow.Id.Value);

            SetWindowPos(windowHandle, new IntPtr(-1), 0, 0, 0, 0, SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);

            DwmApi.DwmExtendFrameIntoClientArea(windowHandle, new DwmApi.MARGINS(0));
            using var rgn = Gdi32.CreateRectRgn(-2, -2, -1, -1);
            DwmApi.DwmEnableBlurBehindWindow(windowHandle, new DwmApi.DWM_BLURBEHIND(true)
            {
                dwFlags = DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_ENABLE | DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_BLURREGION,
                hRgnBlur = rgn
            });
            TransparentHelper.SetTransparent(this, true);

            wndProcHandler = new ComCtl32.SUBCLASSPROC(WndProc);
            ComCtl32.SetWindowSubclass(windowHandle, wndProcHandler, 1, IntPtr.Zero);

            // ウィンドウの拡張スタイルにWS_EX_LAYERED、WS_EX_TRANSPARENT、WS_EX_NOACTIVATEを追加

            SetWindowLong(windowHandle, WindowLongFlags.GWL_EXSTYLE, (IntPtr)(GetWindowLong(windowHandle, WindowLongFlags.GWL_EXSTYLE) | (int)WindowStylesEx.WS_EX_LAYERED | (int)WindowStylesEx.WS_EX_TRANSPARENT | (int)WindowStylesEx.WS_EX_NOACTIVATE));
        }

        private unsafe IntPtr WndProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
        {
            if (uMsg == (uint)WindowMessage.WM_ERASEBKGND)
            {
                if (GetClientRect(hWnd, out var rect))
                {
                    using var brush = Gdi32.CreateSolidBrush(new COLORREF(0, 0, 0));
                    FillRect(wParam, rect, brush);
                    return new IntPtr(1);
                }
            }
            else if (uMsg == (uint)WindowMessage.WM_DWMCOMPOSITIONCHANGED)
            {
                DwmApi.DwmExtendFrameIntoClientArea(hWnd, new DwmApi.MARGINS(0));
                using var rgn = Gdi32.CreateRectRgn(-2, -2, -1, -1);
                DwmApi.DwmEnableBlurBehindWindow(hWnd, new DwmApi.DWM_BLURBEHIND(true)
                {
                    dwFlags = DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_ENABLE | DwmApi.DWM_BLURBEHIND_Mask.DWM_BB_BLURREGION,
                    hRgnBlur = rgn
                });

                return IntPtr.Zero;
            }

            return ComCtl32.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        private DispatcherTimer timer;

        private void HandleVideoDetailReceived(Types.VideoDetail videoDetail)
        {
            ProgressBar.Value = 0;
            ProgressBar.Maximum = Convert.ToDouble(videoDetail.lengthSeconds);
            //一秒間ごとに増やす
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (sender, e) =>
            {
                ProgressBar.Value += 1;
                if (ProgressBar.Value >= ProgressBar.Maximum)
                {
                    timer.Stop();
                }
            };
            SongName.Text = videoDetail.title;
            ArtistName.Text = videoDetail.author;
            ThumbnailImage.Source = new BitmapImage(new Uri(videoDetail.thumbnail.thumbnails[0].url));
        }

        private void HandleVideoPaused(IsPaused IsPaused)
        {
            if (IsPaused.paused)
            {
                timer.Stop();
            }
            else
            {
                //Use IsPaused.currentTime
                ProgressBar.Value = IsPaused.currentTime;
                timer.Start();
            }
        }
    }
}
