using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Microsoft.Windows.ApplicationModel.Resources;

namespace ElevenTube_Music
{
    public sealed partial class MainWindow : Window
    {
        private bool isStarted = false;
        private bool pluginsLoaded = false;
        private bool FullScreen = false;
        public bool Volume_Button_IsEnabled { get; set; } = false;

        private void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            PlaybackStateStore.Instance.ConfigureDispatcher(DispatcherQueue);
            NavigationViewControl.IsBackEnabled = true;
            WebView.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            WebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            WebView.CoreWebView2.ContainsFullScreenElementChanged += (obj, args) =>
            {
                FullScreen = WebView.CoreWebView2.ContainsFullScreenElement;
                if (FullScreen)
                {
                    _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    NavigationViewControl.IsPaneVisible = false;
                    SideNavigation.IsPaneVisible = false;
                }
                else
                {
                    _appWindow.SetPresenter(AppWindowPresenterKind.Default);
                    NavigationViewControl.IsPaneVisible = true;
                    SideNavigation.IsPaneVisible = true;
                }

            };
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values["IsSaveSession"] == null)
            {
                localSettings.Values["IsSaveSession"] = true;
            }
            if ((bool)localSettings.Values["IsSaveSession"])
            {
                if (localSettings.Values["LastUrl"] != null)
                {

                    if (localSettings.Values["LastUrl"].ToString().Contains("watch"))
                    {
                        WebView.Source = new Uri((string)localSettings.Values["LastUrl"]);
                    }
                }
                WebView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            }
        }

        private async void WebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
        }

        private async void CoreWebView2_DOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
        {
            StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFile file = await storageFolder.GetFileAsync("preload.js");
            string preloadScript = await FileIO.ReadTextAsync(file);
            await sender.ExecuteScriptAsync(preloadScript);

            string currentUri = sender.Source ?? WebView.Source?.AbsoluteUri ?? string.Empty;
            bool isMusicPage = currentUri.Contains("music.youtube.com/", StringComparison.OrdinalIgnoreCase);

            if (!pluginsLoaded && isMusicPage)
            {
                pluginsLoaded = true;
                var loader = new ResourceLoader();
                await Load_Plugins(WebView);
                ReplayLatestPlaybackState();

                await Task.Delay(1000);
                PluginLoadingText.Text = loader.GetString("Loaded");
                PluginLoading.Visibility = Visibility.Collapsed;
                pluginLoadedCheck.Visibility = Visibility.Visible;
                await Task.Delay(1000);
                PluginLoadingText.Text = loader.GetString("Plugins");
                pluginLoadedCheck.Visibility = Visibility.Collapsed;
            }

            LoadedStoryboard.Begin();
            await Task.Delay(500);
            loadingBar.Visibility = Visibility.Collapsed;
            WebView.Visibility = Visibility.Visible;
            await Task.Delay(250);
            WebView.Opacity = 1;

            Volume_Button_IsEnabled = true;
            var volume = Get_Volume();
            Volume = (double)(volume * 100);
            Bindings.Update();
        }

        private void ReplayLatestPlaybackState()
        {
            PlaybackSnapshot snapshot = PlaybackStateStore.Instance.GetSnapshot();
            if (snapshot.VideoDetail != null)
            {
                PlaybackStateStore.Instance.UpdateVideoDetail(snapshot.VideoDetail);
            }
            if (snapshot.PauseState != null)
            {
                PlaybackStateStore.Instance.UpdatePlaybackState(snapshot.PauseState);
            }
        }

        private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var data = args.WebMessageAsJson;
            //use Newtonsoft.Json
            var msg = JsonConvert.DeserializeObject<Types.Message>(data);
            var type = msg.Type;
            if (type == "isPaused")
            {
                Debug.WriteLine(msg.Data.ToString());
                Types.IsPaused isPaused = JsonConvert.DeserializeObject<Types.IsPaused>(msg.Data.ToString());
                PlaybackStateStore.Instance.UpdatePlaybackState(isPaused);
            }
            else if (type == "videoDetail")
            {
                Types.VideoDetail videoDetail = JsonConvert.DeserializeObject<Types.VideoDetail>(msg.Data.ToString());
                PlaybackStateStore.Instance.UpdateVideoDetail(videoDetail);
            }
            else if (type == "playlists")
            {
                Types.Playlist[] playlists = JsonConvert.DeserializeObject<Types.Playlist[]>(msg.Data.ToString());
                PlaybackStateStore.Instance.UpdatePlaylists(playlists);
                int i = 0;
                foreach (var playlist in playlists)
                {
                    var item = CreatePlaylistItem(playlist, i);
                    SideNavigation.MenuItems.Add(item);
                    i++;
                }
            }
            else if (type == "popstate")
            {
                string url = msg.Data.ToString();
                Debug.WriteLine(url);
                if (url.Contains("music.youtube.com/explore"))
                {
                    SideNavigation.SelectedItem = SideNavigation.MenuItems[1];
                }
                else if (url.Contains("music.youtube.com/library"))
                {
                    SideNavigation.SelectedItem = SideNavigation.MenuItems[2];
                }
                else if (url.Contains("music.youtube.com/"))
                {
                    SideNavigation.SelectedItem = SideNavigation.MenuItems[0];
                }
            }
        }

        private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
        {
            if (WebView.Source.AbsoluteUri.Contains("music.youtube.com/"))
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["LastUrl"] = WebView.Source.AbsoluteUri;
            }
        }
    }
}
