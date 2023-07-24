using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using NAudio.CoreAudioApi;

namespace ElevenTube_Music
{
    public sealed partial class MainWindow : Window
    {
        public double Volume { get; set; } = 100;
        private bool IsMute = false;
        private double lastVolume;

        private void Volume_Changed(object sender, RangeBaseValueChangedEventArgs e)
        {
            IsMute = false;
            double newVolume = e.NewValue;

            if (newVolume == 0)
            {
                Volume_State.Glyph = "\uE74F";
                Volume_State_Flyout.Glyph = "\uE74F";
            }
            else if (newVolume > 0 && newVolume <= 33)
            {
                Volume_State.Glyph = "\uE992";
                Volume_State_Flyout.Glyph = "\uE992";
            }
            else if (newVolume > 33 && newVolume <= 66)
            {
                Volume_State.Glyph = "\uE993";
                Volume_State_Flyout.Glyph = "\uE993";
            }
            else if (newVolume > 66)
            {
                Volume_State.Glyph = "\uE994";
                Volume_State_Flyout.Glyph = "\uE994";
            }

            Change_Volume(newVolume);
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