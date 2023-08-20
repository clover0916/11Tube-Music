using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Windows.Storage;

namespace ElevenTube_Music
{
    public sealed partial class MainWindow : Window
    {
        public void PauseMusic()
        {
            if (!IsPaused)
            {
                PlayPauseMusic();
            }
        }
        public void PlayMusic()
        {
            if (IsPaused)
            {
                PlayPauseMusic();
            }
        }

        public async void PlayPauseMusic()
        {
            await WebView.ExecuteScriptAsync("document.querySelector('#play-pause-button').click()");
        }

        public async void PreviousMusic()
        {
            await WebView.ExecuteScriptAsync("document.querySelector('.previous-button').click()");
        }
        public async void NextMusic()
        {
            await WebView.ExecuteScriptAsync("document.querySelector('.next-button').click()");
        }
        public async void SetPlayerSeek(int time)
        {
            await WebView.ExecuteScriptAsync(String.Format("document.querySelector('.html5-main-video').currentTime ={0}; ",time));
        }

    }
}
