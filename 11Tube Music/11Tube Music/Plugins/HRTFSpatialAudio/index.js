console.log("HRTF Spatial Audio loaded");

(() => {
    let state = null;

    const setPannerPosition = (panner, x, y, z, time) => {
        if (panner.positionX) {
            panner.positionX.setValueAtTime(x, time);
            panner.positionY.setValueAtTime(y, time);
            panner.positionZ.setValueAtTime(z, time);
        } else {
            panner.setPosition(x, y, z);
        }
    };

    const setListenerOrientation = (listener, time) => {
        if (listener.forwardX) {
            listener.forwardX.setValueAtTime(0, time);
            listener.forwardY.setValueAtTime(0, time);
            listener.forwardZ.setValueAtTime(-1, time);
            listener.upX.setValueAtTime(0, time);
            listener.upY.setValueAtTime(1, time);
            listener.upZ.setValueAtTime(0, time);
        } else {
            listener.setOrientation(0, 0, -1, 0, 1, 0);
        }
    };

    const createImpulseResponse = (audioContext, duration = 0.18, decay = 2.2, stereoSpread = 0) => {
        const sampleRate = audioContext.sampleRate;
        const length = Math.max(1, Math.floor(sampleRate * duration));
        const impulse = audioContext.createBuffer(2, length, sampleRate);

        for (let channel = 0; channel < impulse.numberOfChannels; channel++) {
            const channelData = impulse.getChannelData(channel);
            for (let i = 0; i < length; i++) {
                const t = i / length;
                const envelope = Math.pow(1 - t, decay);
                const spread = channel === 0 ? 1 - stereoSpread : 1 + stereoSpread;
                channelData[i] = (Math.random() * 2 - 1) * envelope * spread;
            }
        }

        return impulse;
    };

    const resumeAudioContext = async (audioContext) => {
        if (audioContext.state === "suspended") {
            try {
                await audioContext.resume();
            } catch {
            }
        }
    };

    const restoreDefaultRoute = (audioSource, audioContext) => {
        try {
            audioSource.connect(audioContext.destination);
        } catch {
        }
    };

    const cleanup = (restoreAudio = false) => {
        if (!state) {
            return;
        }

        if (state.rafId) {
            cancelAnimationFrame(state.rafId);
        }

        [state.outGain, state.dryGain, state.earlyGain, state.tailGain, state.earlyConvolver, state.tailConvolver, state.panner, state.bassShelf, state.subShelf].forEach((node) => {
            try {
                node.disconnect();
            } catch {
            }
        });

        try {
            state.audioSource.disconnect(state.dryGain);
            state.audioSource.disconnect(state.panner);
        } catch {
        }

        if (restoreAudio) {
            restoreDefaultRoute(state.audioSource, state.audioContext);
        }

        state = null;
    };

    const setup = async (event) => {
        cleanup(true);

        const audioContext = event?.detail?.audioContext;
        const audioSource = event?.detail?.audioSource;
        const video = document.querySelector("video");

        if (!audioContext || !audioSource) {
            return;
        }

        try {
            await resumeAudioContext(audioContext);

            const panner = audioContext.createPanner();
            panner.panningModel = "HRTF";
            panner.distanceModel = "inverse";
            panner.refDistance = 0.75;
            panner.maxDistance = 1000;
            panner.rolloffFactor = 1.1;
            panner.coneInnerAngle = 360;
            panner.coneOuterAngle = 360;
            panner.coneOuterGain = 1;

            const earlyConvolver = audioContext.createConvolver();
            earlyConvolver.normalize = true;
            earlyConvolver.buffer = createImpulseResponse(audioContext, 0.06, 3.4, 0.18);

            const tailConvolver = audioContext.createConvolver();
            tailConvolver.normalize = true;
            tailConvolver.buffer = createImpulseResponse(audioContext, 0.78, 2.1, 0.32);

            const dryGain = audioContext.createGain();
            // Keep a subtle center channel but let HRTF path dominate.
            dryGain.gain.setValueAtTime(0.25, audioContext.currentTime);

            const earlyGain = audioContext.createGain();
            earlyGain.gain.setValueAtTime(0.86, audioContext.currentTime);

            const tailGain = audioContext.createGain();
            tailGain.gain.setValueAtTime(0.62, audioContext.currentTime);

            const bassShelf = audioContext.createBiquadFilter();
            bassShelf.type = "lowshelf";
            bassShelf.frequency.setValueAtTime(180, audioContext.currentTime);
            bassShelf.gain.setValueAtTime(4.5, audioContext.currentTime);

            const subShelf = audioContext.createBiquadFilter();
            subShelf.type = "lowshelf";
            subShelf.frequency.setValueAtTime(68, audioContext.currentTime);
            subShelf.gain.setValueAtTime(3.5, audioContext.currentTime);

            const outGain = audioContext.createGain();
            outGain.gain.setValueAtTime(1.0, audioContext.currentTime);

            try {
                audioSource.disconnect(audioContext.destination);
            } catch {
            }

            audioSource.connect(dryGain);
            audioSource.connect(panner);
            panner.connect(earlyConvolver);
            panner.connect(tailConvolver);
            earlyConvolver.connect(earlyGain);
            tailConvolver.connect(tailGain);
            dryGain.connect(outGain);
            earlyGain.connect(outGain);
            tailGain.connect(outGain);
            outGain.connect(bassShelf);
            bassShelf.connect(subShelf);
            subShelf.connect(audioContext.destination);

            setListenerOrientation(audioContext.listener, audioContext.currentTime);
            setPannerPosition(panner, 0, 0, -1, audioContext.currentTime);

            const startTime = performance.now();
            const animate = (timestamp) => {
                if (!state) {
                    return;
                }

                const isPlaying = !!state.video && !state.video.paused;
                const phase = (timestamp - startTime) * 0.0007;

                // Wider movement for clearer binaural cues while playing.
                const radius = isPlaying ? 1.7 : 0;
                const x = Math.cos(phase) * radius;
                const z = -1.2 + (Math.sin(phase * 0.8) * 0.55 * (isPlaying ? 1 : 0));
                const y = Math.sin(phase * 1.35) * (isPlaying ? 0.13 : 0);

                setPannerPosition(state.panner, x, y, z, state.audioContext.currentTime);
                state.rafId = requestAnimationFrame(animate);
            };

            state = {
                audioContext,
                audioSource,
                video,
                panner,
                earlyConvolver,
                tailConvolver,
                dryGain,
                earlyGain,
                tailGain,
                bassShelf,
                subShelf,
                outGain,
                rafId: requestAnimationFrame(animate),
            };
        } catch (error) {
            console.error("[HRTF Spatial Audio] setup failed:", error);
            restoreDefaultRoute(audioSource, audioContext);
            cleanup(false);
        }
    };

    const tryResumeOnUserAction = () => {
        if (state?.audioContext) {
            resumeAudioContext(state.audioContext);
        }
    };

    document.addEventListener("audioCanPlay", setup, { passive: true });
    document.addEventListener("click", tryResumeOnUserAction, { passive: true });
    document.addEventListener("keydown", tryResumeOnUserAction, { passive: true });
    window.addEventListener("beforeunload", () => cleanup(true), { passive: true });
})();
