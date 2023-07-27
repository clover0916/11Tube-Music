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

function setupVideoEventListeners(video) {

    video.onpause = async () => {
        if (isPlaying == true) {
            isFading = true;
            video.play();
            await smoothVolumeTransition(false);
            isPlaying = false;
            video.pause();
            isFading = false;
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
    video.onplay = async () => {
        isPlaying = true;
        if (isFading == false) {
            await smoothVolumeTransition(true);
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
        };
        window.chrome.webview.postMessage(data);
    }
}

function setupLoadstartEventListener(video, api) {
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
    video.addEventListener("loadstart", () => {
        const raw_data = api.getPlayerResponse();
        const data = {
            type: 'videoDetail',
            data: raw_data.videoDetails
        };
        window.chrome.webview.postMessage(data);
    });
}

function smoothVolumeTransition(reverse = false) {
    return new Promise((resolve) => {
        var video = document.querySelector("video");
        var duration = 150;

        var currentTime = Date.now();
        var startVolume = reverse ? 0 : 1;
        var endVolume = reverse ? 1 : 0;

        var intervalId = setInterval(updateVolume, 16);

        function updateVolume() {
            var elapsedTime = Date.now() - currentTime;
            var progress = Math.min(elapsedTime / duration, 1);

            video.volume = startVolume + (endVolume - startVolume) * progress;

            if (progress >= 1) {
                clearInterval(intervalId);
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

    var header = document.querySelector("head");
    var style = document.createElement("style");
    style.innerHTML = `
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
    `;

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
    const video_observer = new MutationObserver(() => {
        const video = document.querySelector('.html5-main-video');
        if (video) {
            setupVideoEventListeners(video);
            setupLoadstartEventListener(video, api);
            video_observer.disconnect();
        }
    });

    const video = document.querySelector('.html5-main-video');
    if (video) {
        setupVideoEventListeners(video);
        setupLoadstartEventListener(video, api);
    } else {
        video_observer.observe(document, { childList: true, subtree: true });
    }

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