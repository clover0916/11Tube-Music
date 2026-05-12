using ElevenTube_Music.Types;
using Microsoft.UI.Dispatching;
using System;

namespace ElevenTube_Music
{
    public sealed class PlaybackSnapshot
    {
        public VideoDetail VideoDetail { get; init; }
        public IsPaused PauseState { get; init; }
        public double CurrentTime { get; init; }
        public DateTimeOffset LastUpdatedAt { get; init; }
        public Playlist[] Playlists { get; init; } = Array.Empty<Playlist>();
    }

    public sealed class PlaybackStateStore
    {
        private readonly object syncRoot = new();
        private DispatcherQueue dispatcherQueue;
        private VideoDetail currentVideoDetail;
        private IsPaused currentPauseState;
        private double currentTime;
        private DateTimeOffset lastUpdatedAt;
        private Playlist[] playlists = Array.Empty<Playlist>();

        private PlaybackStateStore()
        {
            lastUpdatedAt = DateTimeOffset.MinValue;
        }

        public static PlaybackStateStore Instance { get; } = new PlaybackStateStore();

        public event Action<VideoDetail> VideoChanged;
        public event Action<IsPaused> PlaybackChanged;
        public event Action<Playlist[]> PlaylistsChanged;

        public void ConfigureDispatcher(DispatcherQueue queue)
        {
            dispatcherQueue = queue;
        }

        public PlaybackSnapshot GetSnapshot()
        {
            lock (syncRoot)
            {
                return new PlaybackSnapshot
                {
                    VideoDetail = currentVideoDetail,
                    PauseState = currentPauseState,
                    CurrentTime = currentTime,
                    LastUpdatedAt = lastUpdatedAt,
                    Playlists = playlists
                };
            }
        }

        public void UpdateVideoDetail(VideoDetail videoDetail)
        {
            lock (syncRoot)
            {
                currentVideoDetail = videoDetail;
                lastUpdatedAt = DateTimeOffset.UtcNow;
            }

            RaiseOnDispatcher(() => VideoChanged?.Invoke(videoDetail));
        }

        public void UpdatePlaybackState(IsPaused pauseState)
        {
            lock (syncRoot)
            {
                currentPauseState = pauseState;
                currentTime = pauseState?.currentTime ?? currentTime;
                lastUpdatedAt = DateTimeOffset.UtcNow;
            }

            RaiseOnDispatcher(() => PlaybackChanged?.Invoke(pauseState));
        }

        public void UpdatePlaylists(Playlist[] newPlaylists)
        {
            Playlist[] safePlaylists = newPlaylists ?? Array.Empty<Playlist>();
            lock (syncRoot)
            {
                playlists = safePlaylists;
                lastUpdatedAt = DateTimeOffset.UtcNow;
            }

            RaiseOnDispatcher(() => PlaylistsChanged?.Invoke(safePlaylists));
        }

        private void RaiseOnDispatcher(Action action)
        {
            DispatcherQueue queue = dispatcherQueue;
            if (queue == null || queue.HasThreadAccess)
            {
                action();
                return;
            }

            queue.TryEnqueue(() => action());
        }
    }
}
