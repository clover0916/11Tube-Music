using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using NAudio.CoreAudioApi;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
using System;

namespace ElevenTube_Music
{
    public sealed partial class MainWindow : Window
    {
        public double Volume { get; set; } = 100;
        private bool IsMute = false;
        private double lastVolume;

        private void Volume_WheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint pp = e.GetCurrentPoint(sender as UIElement);
            if (pp.Properties.IsHorizontalMouseWheel)
            {
                if (Math.Sign(pp.Properties.MouseWheelDelta) == 1)
                {
                    Volume += 2;

                } else
                {
                    Volume -= 2;
                }
            } else 
            {
                if (Math.Sign(pp.Properties.MouseWheelDelta) == 1)
                {
                    Volume += 2;

                }
                else
                {
                    Volume -= 2;
                }
            }

            if (Volume > 100)
            {
                Volume = 100;
            } else if ( Volume < 0)
            {
                Volume = 0;
            }

            IsMute = false;
            Bindings.Update();

            Volume_Icon_Change(Volume);

            Change_Volume(Volume);
        }

        private void Volume_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            IsMute = false;
            double newVolume = e.NewValue;
            Volume = newVolume;
            Bindings.Update();

            Volume_Icon_Change(newVolume);

            Change_Volume(newVolume);
        }

        private void Volume_Icon_Change(double volume)
        {
            if (volume == 0)
            {
                Volume_State.Glyph = "\uE74F";
                Volume_State_Flyout.Glyph = "\uE74F";
            }
            else if (volume < 33)
            {
                Volume_State.Glyph = "\uE993";
                Volume_State_Flyout.Glyph = "\uE993";
            }
            else if (volume < 66)
            {
                Volume_State.Glyph = "\uE994";
                Volume_State_Flyout.Glyph = "\uE994";
            }
            else
            {
                Volume_State.Glyph = "\uE995";
                Volume_State_Flyout.Glyph = "\uE995";
            }
        }

        private void Toggle_Mute(object sender, RoutedEventArgs e)
        {
            IsMute = !IsMute;
            if (IsMute)
            {
                Volume_State.Glyph = "\uE74F";
                Volume_State_Flyout.Glyph = "\uE74F";
                lastVolume = Get_Volume() * 100;
                Change_Volume(0);
            }
            else
            {
                Volume_State.Glyph = "\uE994";
                Volume_State_Flyout.Glyph = "\uE994";
                Change_Volume(lastVolume);
            }
        }

        private void Change_Volume(double newVolume)
        {
            MMDeviceEnumerator deviceEnumerator = new();

            // Get the default audio endpoint device (rendering endpoint)
            MMDevice defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            // Set the volume level for the default device
            SessionCollection sessions = defaultDevice.AudioSessionManager.Sessions;

            //Find this prosses
            for (int i = 0; i < sessions.Count; i++)
            {
                var ProcessID = sessions[i].GetProcessID;
                if (WebView.CoreWebView2 == null)
                    break;
                var WebViewProcesses = WebView.CoreWebView2.Environment.GetProcessInfos();

                foreach (var WebViewProcess in WebViewProcesses)
                {
                    if (WebViewProcess.ProcessId == ProcessID)
                    {
                        sessions[i].SimpleAudioVolume.Volume = (float)newVolume / 100;
                    }
                }
            }
        }

        private float Get_Volume()
        {
            MMDeviceEnumerator deviceEnumerator = new();

            // Get the default audio endpoint device (rendering endpoint)
            MMDevice defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

            // Set the volume level for the default device
            SessionCollection sessions = defaultDevice.AudioSessionManager.Sessions;

            //Find this prosses
            for (int i = 0; i < sessions.Count; i++)
            {
                var ProcessID = sessions[i].GetProcessID;
                if (WebView.CoreWebView2 == null)
                    break;
                var WebViewProcesses = WebView.CoreWebView2.Environment.GetProcessInfos();

                foreach (var WebViewProcess in WebViewProcesses)
                {
                    if (WebViewProcess.ProcessId == ProcessID)
                    {
                        return sessions[i].SimpleAudioVolume.Volume;
                    }
                }
            }
            return 0;
        }
    }
}