using DiscordRPC;
using DiscordRPC.Logging;
using ElevenTube_Music.Types;
using System;
using System.Diagnostics;

namespace ElevenTube_Music.Plugins.DiscordRichPresence
{
    internal class main
    {
        const string CLIENT_ID = "1126193880493199431";
        public DiscordRpcClient client;
        private VideoDetail VideoDetail;
        private RichPresence Presence;

        public void Main(MainWindow window)
        {
            Presence = new RichPresence()
            {
                Details = "Nothing is playing",
                Buttons = new[]                {
                    new Button() { Label = "Get 11Tube Music", Url = "https://github.com/clover0916/11Tube-Music" }
                }
            };
            window.VideoDetailReceived += HandleVideoDetailReceived;
            window.VideoPaused += HandleVideoPaused;

            client = new DiscordRpcClient(CLIENT_ID);

            client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            client.OnReady += (sender, e) =>
            {
                Debug.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            client.OnPresenceUpdate += (sender, e) =>
            {
                Debug.WriteLine("Received Update! {0}", e.Presence);
            };

            client.Initialize();

            client.SetPresence(Presence);
        }

        private void HandleVideoDetailReceived(Types.VideoDetail videoDetail)
        {
            VideoDetail = videoDetail;
            Presence.Details = VideoDetail.title;
            Presence.State = VideoDetail.author;
            Presence.Assets = new Assets();
            Presence.Assets.LargeImageKey = VideoDetail.thumbnail.thumbnails[0].url;
            Presence.Assets.LargeImageText = VideoDetail.title;
            Presence.Assets.SmallImageKey = "play";
            Presence.Assets.SmallImageText = "Playing";
            Presence.Buttons = new[]            {
                new Button() { Label = "Listen on YouTube", Url = "https://music.youtube.com/watch?v=" + VideoDetail.videoId },
                new Button() { Label = "Get 11Tube Music", Url = "https://github.com/clover0916/11Tube-Music" }
            };
            Presence.Timestamps = new Timestamps()
            {
                End = DateTime.UtcNow + TimeSpan.FromSeconds(Convert.ToDouble(VideoDetail.lengthSeconds))
            };

            client.SetPresence(Presence);
        }

        private void HandleVideoPaused(IsPaused IsPaused)
        {
            if (IsPaused.paused)
            {
                Presence.Assets.SmallImageKey = "pause";
                Presence.Assets.SmallImageText = "Paused";
                Presence.Timestamps = null;
                client.SetPresence(Presence);
            }
            else
            {
                if (VideoDetail == null)
                {
                    return;
                }
                Presence.Assets.SmallImageKey = "play";
                Presence.Assets.SmallImageText = "Playing";
                Presence.Timestamps = new Timestamps()
                {
                    End = DateTime.UtcNow + TimeSpan.FromSeconds(Convert.ToDouble(VideoDetail.lengthSeconds) - Convert.ToDouble(IsPaused.currentTime))
                };
                client.SetPresence(Presence);
            }
        }
    }
}
