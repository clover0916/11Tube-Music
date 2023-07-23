class VisualizerCore {
    constructor(audioContext, audioSource, visualizerContainer, canvas, stream) {
        this.audioContext = audioContext;
        this.audioSource = audioSource;
        this.visualizerContainer = visualizerContainer;
        this.canvas = canvas;
        this.stream = stream; //captureStream
        this.averageColor = [0, 0, 0];

        this.option = {
            width: 0,
            height: 0,
        };

        this.analyser = this.audioContext.createAnalyser();
        this.analyser.fftSize = 256;
        this.analyser.smoothingTimeConstant = 0.8;
        this.analyser.maxDecibels = 0;

        this.audioSource.connect(this.analyser);

        this.animationFrameId = null;
        this.renderFrame = this.renderFrame.bind(this);
    }

    setOption(option) {
        this.option = option;
    }

    renderFrame() {
        const canvas_forColor = document.createElement('canvas');
        const context = canvas_forColor.getContext('2d');

        const track = this.stream.getVideoTracks()[0];

        if (track) {
            const imageCapture = new ImageCapture(track);

            imageCapture.grabFrame().then(imageBitmap => {
                canvas_forColor.width = imageBitmap.width;
                canvas_forColor.height = imageBitmap.height;
                context.drawImage(imageBitmap, 0, 0);

                const imageData = context.getImageData(0, 0, canvas_forColor.width, canvas_forColor.height);
                const pixels = imageData.data;

                let r = 0, g = 0, b = 0;

                for (let i = 0; i < pixels.length; i += 4) {
                    r += pixels[i];
                    g += pixels[i + 1];
                    b += pixels[i + 2];
                }

                const totalPixels = pixels.length / 4;
                this.avgColor = [r / totalPixels, g / totalPixels, b / totalPixels];


            });
        } else {
            const song_image = document.querySelector("#song-image img");
            song_image.crossOrigin = "anonymous";
            canvas_forColor.width = song_image.width;
            canvas_forColor.height = song_image.height;
            context.drawImage(song_image, 0, 0);

            const imageData = context.getImageData(0, 0, canvas_forColor.width, canvas_forColor.height);
            const pixels = imageData.data;

            let r = 0, g = 0, b = 0;

            for (let i = 0; i < pixels.length; i += 4) {
                r += pixels[i];
                g += pixels[i + 1];
                b += pixels[i + 2];
            }

            const totalPixels = pixels.length / 4;
            this.avgColor = [r / totalPixels, g / totalPixels, b / totalPixels];
        }



        this.animationFrameId = requestAnimationFrame(this.renderFrame);
        const bufferLength = this.analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);

        this.analyser.getByteFrequencyData(dataArray);

        const barWidth = (this.option.width / bufferLength) * 4.5;
        const ctx = this.canvas.getContext("2d");
        ctx.clearRect(0, 0, this.option.width, this.option.height);
        let x = 0;
        for (let i = 0; i < bufferLength; i++) {
            const barHeight = dataArray[i];
            const opacity = barHeight / 255;
            ctx.fillStyle = `rgba(${this.avgColor[0]}, ${this.avgColor[1]}, ${this.avgColor[2]}, ${opacity})`;
            ctx.fillRect(x, this.option.height - barHeight * (this.option.height / 255), barWidth, barHeight * (this.option.height / 255));
            x += barWidth;
        }
    }

    stop() {
cancelAnimationFrame(this.animationFrameId);
        this.analyser.disconnect();
    }
}

class Visualizer {
    constructor(audioContext, audioSource, visualizerContainer, canvas, stream) {
        this.visualizer = new VisualizerCore(
            audioContext,
            audioSource,
            visualizerContainer,
            canvas,
            stream
        );

        this.animationFrameId = null;
        this.render = this.render.bind(this);
    }

    resize(width, height) {
        this.visualizer.setOption({
            width,
            height,
        });
    }

    render() {
        try {
            this.visualizer.renderFrame();
        } catch (error) {
            console.error("Error rendering frame:", error);
            return;
        }
    }

    start() {
        this.render();
    }

    stop() {
        cancelAnimationFrame(this.animationFrameId);
        this.visualizer.analyser.disconnect();
        this.visualizer.stop()
    }
}



console.log("Visualizer loaded");

var visualizer;

document.addEventListener(
    "audioCanPlay",
    (e) => {
        try {
            if (visualizer) {
                visualizer.stop();
                visualizer = null;
            }

            const video = document.querySelector("video");
            const visualizerContainer = document.querySelector("#player")

            let canvas = document.getElementById("visualizer");
            if (!canvas) {
                canvas = document.createElement("canvas");
                canvas.id = "visualizer";
                canvas.style.position = "absolute";
                canvas.style.top = "0";
                canvas.style.pointerEvents = "none";
                visualizerContainer.append(canvas);
            }

            const resizeCanvas = () => {
                canvas.width = visualizerContainer.clientWidth;
                canvas.height = visualizerContainer.clientHeight;
            };
            resizeCanvas();

            visualizer = new Visualizer(
                e.detail.audioContext,
                e.detail.audioSource,
                visualizerContainer,
                canvas,
                video.captureStream()
            );

            const resizeVisualizer = () => {
                resizeCanvas();
                visualizer.resize(canvas.width, canvas.height);
            };
            resizeVisualizer();
            const visualizerContainerObserver = new ResizeObserver((entries) => {
                entries.forEach((entry) => {
                    resizeVisualizer();
                });
            });
            visualizerContainerObserver.observe(visualizerContainer);

            visualizer.start();
            console.log("Visualizer started");
        } catch (e) {
            console.error(e);
        }
    },
    { passive: true }
);
