function hideElementWithObserver(tabId) {
    const element = document.querySelector(`[tab-id="${tabId}"]`);
    if (element) {
        element.style.display = 'none';
    } else {
        const observer = new MutationObserver(() => {
            const element = document.querySelector(`[tab-id="${tabId}"]`);
            if (element) {
                element.style.display = 'none';
                observer.disconnect();
            }
        });
        observer.observe(document.documentElement, { childList: true, subtree: true });
    }
}

var isPlaying = false;
var isFading = false;
var lastVideoDetailVideoId = null;
var pendingVideoDetailRetryTimer = null;

if (!window.__11tubeMediaProxied) {
    window.__11tubeMediaProxied = true;
    const originalPause = HTMLMediaElement.prototype.pause;
    
    HTMLMediaElement.prototype.pause = function() {
        if (this.__isFadingOut || this.paused || this.readyState === 0) {
            return originalPause.call(this);
        }

        // Apply fade-out only to the main video element
        if (this.classList && (this.classList.contains('video-stream') || this.classList.contains('html5-main-video'))) {
            this.__isFadingOut = true;
            isFading = true;
            smoothVolumeTransition(false, this).then(() => {
                originalPause.call(this);
                this.__isFadingOut = false;
                isFading = false;
                isPlaying = false;
            });
            return; 
        }

        return originalPause.call(this);
    };
}

window.elevenTubePauseWithFade = async function () {
    const video = document.querySelector('.html5-main-video') || document.querySelector('video');
    if (video) video.pause();
};

window.elevenTubeTogglePlaybackWithFade = async function () {
    const playPauseButton = document.querySelector('#play-pause-button');
    if (playPauseButton) playPauseButton.click();
};

function setupVideoEventListeners(video) {
    if (video.__11tubeVideoEventsBound) {
        return;
    }
    video.__11tubeVideoEventsBound = true;

    video.onpause = async () => {
        rawData = {
            paused: video.paused,
            currentTime: video.currentTime,
        }
        const data = {
            type: 'isPaused',
            data: rawData
        };
        window.chrome.webview.postMessage(data);
    };
    video.onplay = async () => {
        isPlaying = true;
        if (isFading == false && !video.__isFadingOut) {
            await smoothVolumeTransition(true, video);
        }
        rawData = {
            paused: video.paused,
            currentTime: video.currentTime,
        }
        const data = {
            type: 'isPaused',
            data: rawData
        };
        window.chrome.webview.postMessage(data);
    };
    video.onseeking = async () => {
        rawData = {
            paused: video.paused,
            currentTime: video.currentTime,
        }
        const data = {
            type: 'isPaused',
            data: rawData
        }
        window.chrome.webview.postMessage(data);
    }
}

function setupLoadstartEventListener(video, api) {
    if (video.__11tubeLoadstartBound) {
        return;
    }
    video.__11tubeLoadstartBound = true;

    const audioContext = new AudioContext();
    const audioSource = audioContext.createMediaElementSource(video);
    audioSource.connect(audioContext.destination);

    video.addEventListener(
        "loadstart",
        () => {
            video.addEventListener(
                "canplaythrough",
                () => {
                    document.dispatchEvent(
                        new CustomEvent("audioCanPlay", {
                            detail: {
                                audioContext: audioContext,
                                audioSource: audioSource,
                            },
                        })
                    );
                },
                { once: true }
            );
        },
        { passive: true }
    );
    const enqueueVideoDetailSend = () => scheduleVideoDetailSend(api);
    video.addEventListener("loadstart", enqueueVideoDetailSend);
    video.addEventListener("loadedmetadata", enqueueVideoDetailSend);
    video.addEventListener("durationchange", enqueueVideoDetailSend);
    video.addEventListener("ended", enqueueVideoDetailSend);
    video.addEventListener("playing", enqueueVideoDetailSend);
    video.addEventListener("canplay", enqueueVideoDetailSend);
    video.addEventListener("emptied", enqueueVideoDetailSend);
}

function sendVideoDetail(api) {
    const raw_data = api?.getPlayerResponse?.();
    const details = raw_data?.videoDetails;
    if (!details?.videoId) {
        return false;
    }

    if (details.videoId === lastVideoDetailVideoId) {
        return true;
    }

    lastVideoDetailVideoId = details.videoId;
    const data = {
        type: 'videoDetail',
        data: details
    };
    window.chrome.webview.postMessage(data);
    return true;
}

function scheduleVideoDetailSend(api, attempt = 0) {
    const maxAttempts = 24;
    const retryDelaysMs = [25, 25, 50, 50, 75, 100, 125, 150, 200, 250];

    if (pendingVideoDetailRetryTimer) {
        clearTimeout(pendingVideoDetailRetryTimer);
        pendingVideoDetailRetryTimer = null;
    }

    const sent = sendVideoDetail(api);
    if (sent || attempt >= maxAttempts) {
        return;
    }

    const delayIndex = Math.min(attempt, retryDelaysMs.length - 1);
    const retryDelayMs = retryDelaysMs[delayIndex];
    pendingVideoDetailRetryTimer = setTimeout(() => {
        scheduleVideoDetailSend(api, attempt + 1);
    }, retryDelayMs);
}

function smoothVolumeTransition(reverse = false, video = null) {
    return new Promise((resolve) => {
        video = video || document.querySelector('.html5-main-video') || document.querySelector('video');
        if (!video) {
            resolve(false);
            return;
        }

        var duration = 500; // Increased to 500ms for a slower, smoother fade
        var currentTime = Date.now();
        var startVolume = Math.min(Math.max(video.volume, 0), 1);
        var endVolume;

        if (reverse) {
            endVolume = typeof video.__11tubeVolumeBeforePause === 'number'
                ? Math.min(Math.max(video.__11tubeVolumeBeforePause, 0), 1)
                : 1;
            if (startVolume > 0.1 && video.paused) {
                // Instantly drop volume before fading in if starting from stopped state
                startVolume = 0;
                try { video.volume = 0; } catch(e) {}
            }
        } else {
            video.__11tubeVolumeBeforePause = startVolume > 0.1 ? startVolume : 1;
            endVolume = 0;
        }

        if (video.__volumeFadeInterval) {
            clearInterval(video.__volumeFadeInterval);
        }

        video.__volumeFadeInterval = setInterval(updateVolume, 16);

        function updateVolume() {
            var elapsedTime = Date.now() - currentTime;
            var progress = Math.min(elapsedTime / duration, 1);

            video.volume = startVolume + (endVolume - startVolume) * progress;

            if (progress >= 1) {
                clearInterval(video.__volumeFadeInterval);
                video.__volumeFadeInterval = null;
                resolve(true);
            }
        }
    });
}

try {
    const audioCtx = new AudioContext();
    const oscillator = audioCtx.createOscillator();
    oscillator.type = 'square';
    oscillator.frequency.value = 0;
    oscillator.connect(audioCtx.destination);
    oscillator.start();
    setTimeout(() => {
        oscillator.stop();
    }, 1000);

    hideElementWithObserver('FEmusic_home');
    hideElementWithObserver('FEmusic_explore');
    hideElementWithObserver('FEmusic_library_landing');
    hideElementWithObserver('SPunlimited');

    const escapeHTMLPolicy = trustedTypes.createPolicy("default", {
        createHTML: (string) => string
    });

    var header = document.querySelector("head");
    var style = document.createElement("style");
    style.innerHTML = escapeHTMLPolicy.createHTML(`
        #content-wrapper {
            margin-left: 0 !important;
        }
        ytmusic-player-page {
            left: 0 !important;
            width: 100% !important;
        }
        tp-yt-app-drawer {
            display: none !important;
        }
        #guide-button {
            display: none !important;
        }
        #mini-guide-background {
            display: none !important;
        }
        #mini-guide {
            display: none !important;
        }

        ytmusic-app[is-bauhaus-sidenav-enabled]:not([guide-collapsed]) {
            --ytmusic-guide-width: 0px !important;
        }

        ytmusic-immersive-header-renderer[is-bauhaus-sidenav-enabled] .image.ytmusic-immersive-header-renderer {
            margin-left: 0 !important;
        }

        ytmusic-nav-bar[is-bauhaus-sidenav-enabled] .center-content.ytmusic-nav-bar {
            position: relative;
            justify-content: flex-start;
            align-items: center;
            padding-left: 100px !important;
            flex-shrink: 1;
        }
    `);

    header.appendChild(style);

    window.addEventListener('popstate', function (event) {
        const data = {
            type: 'popstate',
            data: document.location.href
        }
        window.chrome.webview.postMessage(data);
    });


    const api = document.querySelector('#movie_player');

    if (api) {
        apiLoaded(api);
    } else {
        const api_observer = new MutationObserver(() => {
            const api = document.querySelector('#movie_player');
            if (api) {
                apiLoaded(api);
                api_observer.disconnect();
            }
        });
        api_observer.observe(document, { childList: true, subtree: true });
    }
} catch (e) {
    console.error(e);
}

function apiLoaded(api) {
    let lastBoundVideo = null;

    const bindVideoIfNeeded = () => {
        const video = document.querySelector('.html5-main-video');
        if (!video || video === lastBoundVideo) {
            return;
        }

        lastBoundVideo = video;
        setupVideoEventListeners(video);
        setupLoadstartEventListener(video, api);
        scheduleVideoDetailSend(api);
    };

    bindVideoIfNeeded();

    const video_observer = new MutationObserver(() => {
        bindVideoIfNeeded();
    });
    video_observer.observe(document, { childList: true, subtree: true });

    const playlist_observer = new MutationObserver(() => {
        const playlistElements = document.querySelectorAll("#sections")[0]?.childNodes[1]?.querySelector("#items")?.querySelectorAll(".title-column");
        if (playlistElements) {
            sendPlaylists(playlistElements);
            playlist_observer.disconnect();
        }
    });
    const playlistElements = document.querySelectorAll("#sections")[0]?.childNodes[1]?.querySelector("#items")?.querySelectorAll(".title-column");
    if (playlistElements) {
        sendPlaylists(playlistElements);
    }
    else {
        playlist_observer.observe(document, { childList: true, subtree: true });
    }

}

function sendSuggestions(suggestionElements) {
    const titles = Array.from(suggestionElements).map(element => element.querySelector('.title').textContent.trim());

    const data = {
        type: 'suggestions',
        data: {
            titles: titles
        }
    };
    window.chrome.webview.postMessage(data);
}

function sendPlaylists(playlistElements) {
    const playlists = Array.from(playlistElements).map(e => {
        return {
            title: e.querySelector(".title").textContent.trim(),
            subtitle: e.querySelector(".subtitle").textContent.trim()
        }
    });

    const data = {
        type: 'playlists',
        data: playlists
    };
    window.chrome.webview.postMessage(data);
};
