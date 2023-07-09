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

function setupVideoEventListeners(video) {
    video.onpause = () => {
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
    video.onplay = () => {
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
}

function setupLoadstartEventListener(video, api) {
    video.addEventListener("loadstart", () => {
        const raw_data = api.getPlayerResponse();
        const data = {
            type: 'videoDetail',
            data: raw_data.videoDetails
        };
        window.chrome.webview.postMessage(data);
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

    const api = document.querySelector('#movie_player');
    const video_observer = new MutationObserver(() => {
        const video = document.querySelector('.html5-main-video');
        if (video) {
            setupVideoEventListeners(video);
            setupLoadstartEventListener(video, api);
            video_observer.disconnect();
        }
    });

    if (api) {
        const video = document.querySelector('.html5-main-video');
        if (video) {
            setupVideoEventListeners(video);
            setupLoadstartEventListener(video, api);
        } else {
            video_observer.observe(document, { childList: true, subtree: true });
        }
    } else {
        const api_observer = new MutationObserver(() => {
            const api = document.querySelector('#movie_player');
            if (api) {
                const video = document.querySelector('.html5-main-video');
                if (video) {
                    setupVideoEventListeners(video);
                    setupLoadstartEventListener(video, api);
                } else {
                    video_observer.observe(document, { childList: true, subtree: true });
                }
                api_observer.disconnect();
            }
        });
        api_observer.observe(document, { childList: true, subtree: true });
    }


} catch (e) {
    console.error(e);
}