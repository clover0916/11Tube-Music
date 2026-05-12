class VisualizerCore {
    constructor(audioContext, audioSource, visualizerContainer, canvas, stream) {
        this.audioContext = audioContext;
        this.audioSource = audioSource;
        this.visualizerContainer = visualizerContainer;
        this.canvas = canvas;
        this.stream = stream; //captureStream
        this.avgColor = [255, 255, 255];

        // WebGPU availability check
        console.log("=== WebGPU Availability Check ===");
        console.log("navigator.gpu available:", !!navigator.gpu);
        if (navigator.gpu) {
            console.log("navigator.gpu object:", navigator.gpu);
        } else {
            console.warn("WebGPU is NOT available - will use Canvas 2D");
        }

        this.option = {
            width: 0,
            height: 0,
        };

        this.analyser = this.audioContext.createAnalyser();
        this.analyser.fftSize = 1024;
        this.analyser.smoothingTimeConstant = 0.55;
        this.analyser.maxDecibels = 0;

        this.audioSource.connect(this.analyser);

        this.animationFrameId = null;
        this.renderFrame = this.renderFrame.bind(this);

        this.dataArray = new Uint8Array(this.analyser.frequencyBinCount);
        
        // Fixed number of display bars, independent of FFT size
        this.displayBarCount = 128;
        this.barLevels = new Float32Array(this.displayBarCount);
        this.barData = new Float32Array(this.displayBarCount * 8);
        this.uniformData = new Float32Array(2);
        this.logBinRanges = new Uint16Array(this.displayBarCount * 2);
        this.cachedNyquist = -1;
        this.cachedFftBinCount = -1;

        this.colorCanvas = document.createElement("canvas");
        this.colorContext = this.colorCanvas.getContext("2d", { willReadFrequently: true });
        this.colorSampleSize = 32;
        this.colorSampleInterval = 66;
        this.lastColorSampleTime = 0;
        this.colorCanvas.width = this.colorSampleSize;
        this.colorCanvas.height = this.colorSampleSize;

        this.baseGate = 0.1;
        this.attack = 0.65;
        this.release = 0.24;
        this.logFrequencyScale = true;

        // WebGPU properties
        this.device = null;
        this.queue = null;
        this.context = null;
        this.canvasFormat = null;
        this.pipeline = null;
        this.vertexBuffer = null;
        this.barDataBuffer = null;
        this.colorUniformBuffer = null;
        this.bindGroup = null;
        this.webgpuReady = false;

        // Canvas 2D context - will be initialized only if WebGPU fails
        this.ctx = null;

        // Try WebGPU first (it will initialize Canvas 2D as fallback if needed)
        if (navigator.gpu) {
            console.log("Attempting to initialize WebGPU...");
            this.initWebGPU();
        } else {
            console.log("WebGPU not available, using Canvas 2D only");
            this.ctx = this.canvas.getContext("2d", { alpha: true });
            console.log("Canvas 2D context initialized");
        }
    }

    async initWebGPU() {
        try {
            console.log("[WebGPU Init] Starting initialization...");

            // Step 1: Get WebGPU context directly
            const context = this.canvas.getContext("webgpu");
            console.log("[WebGPU Init] WebGPU context obtained:", context ? "Success" : "Failed");
            
            if (!context) {
                console.warn("[WebGPU Init] Could not get WebGPU context, falling back to Canvas 2D");
                if (!this.ctx) {
                    this.ctx = this.canvas.getContext("2d", { alpha: true });
                    console.log("[WebGPU Init] Canvas 2D context initialized");
                }
                return;
            }

            // Step 2: Request adapter
            const adapter = await navigator.gpu.requestAdapter();
            console.log("[WebGPU Init] Adapter requested:", adapter ? "Success" : "Failed");
            
            if (!adapter) {
                console.warn("[WebGPU Init] No adapter available, falling back to Canvas 2D");
                if (!this.ctx) {
                    this.ctx = this.canvas.getContext("2d", { alpha: true });
                }
                return;
            }

            // Step 3: Create device and queue
            this.device = await adapter.requestDevice();
            this.queue = this.device.queue;
            console.log("[WebGPU Init] Device and queue created");

            // Step 4: Get canvas format and configure context
            const canvasFormat = navigator.gpu.getPreferredCanvasFormat();
            console.log("[WebGPU Init] Canvas format:", canvasFormat);
            
            context.configure({
                device: this.device,
                format: canvasFormat,
                alphaMode: 'premultiplied',
            });
            console.log("[WebGPU Init] Context configured with alpha blending");

            // Step 5: Store context and format
            this.context = context;
            this.canvasFormat = canvasFormat;

            // Step 6: Setup pipeline
            this.setupWebGPUPipeline();
            console.log("[WebGPU Init] Pipeline setup complete");
            
            this.webgpuReady = true;
            console.log("✓ WebGPU initialized successfully!");
        } catch (error) {
            console.error("[WebGPU Init] Error:", error.message);
            console.error("[WebGPU Init] Stack:", error.stack);
            // Fallback to Canvas 2D
            if (!this.ctx) {
                console.log("[WebGPU Init] Initializing Canvas 2D fallback");
                this.ctx = this.canvas.getContext("2d", { alpha: true });
                console.log("[WebGPU Init] Canvas 2D context initialized");
            }
        }
    }

    setupWebGPUPipeline() {
        const shaderCode = `
            struct BarData {
                x: f32,              // bar position x
                width: f32,          // bar width
                height: f32,         // bar height
                colorR: f32,         // color R
                colorG: f32,         // color G
                colorB: f32,         // color B
                opacity: f32,        // alpha
                _padding: f32,       // padding for alignment
            }

            struct Uniforms {
                width: f32,
                height: f32,
            }

            @group(0) @binding(0) var<uniform> uniforms: Uniforms;
            @group(0) @binding(1) var<storage, read> bars: array<BarData>;

            struct VertexOutput {
                @builtin(position) position: vec4<f32>,
                @location(0) color: vec4<f32>,
            }

            @vertex
            fn vertexMain(
                @builtin(vertex_index) vertexIndex: u32,
            ) -> VertexOutput {
                let barIndex = vertexIndex / 6u;
                let vertexInBar = vertexIndex % 6u;
                
                let bar = bars[barIndex];
                let barX = bar.x;
                let barWidth = bar.width;
                let barHeight = bar.height;
                let canvasHeight = uniforms.height;

                var position: vec2<f32>;
                switch(vertexInBar) {
                    case 0u: { position = vec2<f32>(barX, canvasHeight); }
                    case 1u: { position = vec2<f32>(barX + barWidth, canvasHeight); }
                    case 2u: { position = vec2<f32>(barX, canvasHeight - barHeight); }
                    case 3u: { position = vec2<f32>(barX + barWidth, canvasHeight); }
                    case 4u: { position = vec2<f32>(barX + barWidth, canvasHeight - barHeight); }
                    case 5u: { position = vec2<f32>(barX, canvasHeight - barHeight); }
                    default: { position = vec2<f32>(0.0); }
                }

                // Normalize to NDC (Normalized Device Coordinates)
                let ndcPos = (position / vec2<f32>(uniforms.width, uniforms.height)) * 2.0 - 1.0;
                
                var output: VertexOutput;
                output.position = vec4<f32>(ndcPos.x, -ndcPos.y, 0.0, 1.0);
                output.color = vec4<f32>(bar.colorR, bar.colorG, bar.colorB, bar.opacity);
                return output;
            }

            @fragment
            fn fragmentMain(input: VertexOutput) -> @location(0) vec4<f32> {
                return input.color;
            }
        `;

        const shaderModule = this.device.createShaderModule({
            code: shaderCode,
        });

        this.pipeline = this.device.createRenderPipeline({
            layout: "auto",
            vertex: {
                module: shaderModule,
                entryPoint: "vertexMain",
            },
            fragment: {
                module: shaderModule,
                entryPoint: "fragmentMain",
                targets: [
                    {
                        format: this.canvasFormat,
                    },
                ],
            },
            primitive: {
                topology: "triangle-list",
            },
        });

        // Create buffers
        this.colorUniformBuffer = this.device.createBuffer({
            size: 8,
            usage: GPUBufferUsage.UNIFORM | GPUBufferUsage.COPY_DST,
            mappedAtCreation: true,
        });
        new Float32Array(this.colorUniformBuffer.getMappedRange()).set([
            this.option.width,
            this.option.height,
        ]);
        this.colorUniformBuffer.unmap();

        // 128 bars * 8 floats per bar = 1024 floats = 4096 bytes
        this.barDataBuffer = this.device.createBuffer({
            size: this.displayBarCount * 32,
            usage: GPUBufferUsage.STORAGE | GPUBufferUsage.COPY_DST,
        });

        this.bindGroup = this.device.createBindGroup({
            layout: this.pipeline.getBindGroupLayout(0),
            entries: [
                {
                    binding: 0,
                    resource: {
                        buffer: this.colorUniformBuffer,
                    },
                },
                {
                    binding: 1,
                    resource: {
                        buffer: this.barDataBuffer,
                    },
                },
            ],
        });
    }

    frequencyToMel(frequency) {
        return 2595 * Math.log10(1 + frequency / 700);
    }

    melToFrequency(mel) {
        return 700 * (Math.pow(10, mel / 2595) - 1);
    }

    setOption(option) {
        this.option = option;
        this.uniformData[0] = option.width;
        this.uniformData[1] = option.height;
    }

    updateLogBinRangesIfNeeded(fftBinCount, nyquistFrequency) {
        if (this.cachedFftBinCount === fftBinCount && this.cachedNyquist === nyquistFrequency) {
            return;
        }

        this.cachedFftBinCount = fftBinCount;
        this.cachedNyquist = nyquistFrequency;

        const maxMel = this.frequencyToMel(nyquistFrequency);
        for (let i = 0; i < this.displayBarCount; i++) {
            const t = i / this.displayBarCount;
            const tNext = (i + 1) / this.displayBarCount;
            const mel = t * maxMel;
            const melNext = tNext * maxMel;
            const frequency = this.melToFrequency(mel);
            const frequencyNext = this.melToFrequency(melNext);
            const binStart = Math.max(0, Math.floor((frequency / nyquistFrequency) * (fftBinCount - 1)));
            const binEnd = Math.min(fftBinCount - 1, Math.floor((frequencyNext / nyquistFrequency) * (fftBinCount - 1)));
            const idx = i * 2;
            this.logBinRanges[idx] = binStart;
            this.logBinRanges[idx + 1] = binEnd;
        }
    }

    sampleAverageColor(width, height, options = null) {
        const sampleX = options?.x ?? 0;
        const sampleY = options?.y ?? 0;
        const sampleWidth = options?.width ?? width;
        const sampleHeight = options?.height ?? height;

        const imageData = this.colorContext.getImageData(sampleX, sampleY, sampleWidth, sampleHeight);
        const pixels = imageData.data;

        let r = 0;
        let g = 0;
        let b = 0;
        let count = 0;

        for (let i = 0; i < pixels.length; i += 16) {
            r += pixels[i];
            g += pixels[i + 1];
            b += pixels[i + 2];
            count++;
        }

        if (count > 0) {
            return [r / count, g / count, b / count];
        }

        return this.avgColor;
    }

    calculateAverageColor(width, height, options = null) {
        const sampled = this.sampleAverageColor(width, height, options);
        let rr = sampled[0];
        let gg = sampled[1];
        let bb = sampled[2];

        // Avoid muddy near-gray dominant colors that make bars hard to read.
        const max = Math.max(rr, gg, bb);
        const min = Math.min(rr, gg, bb);
        const chroma = max - min;
        if (chroma < 18) {
            rr *= 0.92;
            gg *= 0.92;
            bb *= 0.92;
        }

        this.avgColor = [rr, gg, bb];
    }

    relativeLuminance(rgb) {
        const toLinear = (v) => {
            const s = v / 255;
            return s <= 0.04045 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4);
        };

        const r = toLinear(rgb[0]);
        const g = toLinear(rgb[1]);
        const b = toLinear(rgb[2]);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    adjustColorForContrast(foregroundRgb, backgroundRgb, minDiff = 0.28) {
        const fgLum = this.relativeLuminance(foregroundRgb);
        const bgLum = this.relativeLuminance(backgroundRgb);
        const diff = Math.abs(fgLum - bgLum);

        if (diff >= minDiff) {
            return foregroundRgb;
        }

        // If background is bright, darken bars. If background is dark, brighten bars.
        const darken = bgLum >= 0.5;
        const scale = darken ? 0.55 : 1.35;
        return [
            Math.max(18, Math.min(245, foregroundRgb[0] * scale)),
            Math.max(18, Math.min(245, foregroundRgb[1] * scale)),
            Math.max(18, Math.min(245, foregroundRgb[2] * scale)),
        ];
    }

    updateAverageColor() {
        const now = performance.now();
        if (now - this.lastColorSampleTime < this.colorSampleInterval) {
            return;
        }

        this.lastColorSampleTime = now;

        const width = this.colorSampleSize;
        const height = this.colorSampleSize;

        try {
            const backVideoImage = document.querySelector("#back_video img");
            if (backVideoImage && backVideoImage.complete && backVideoImage.naturalWidth > 0) {
                this.colorContext.drawImage(backVideoImage, 0, 0, width, height);
                // For static images, avoid bottom region where flat colors often dominate.
                const marginX = Math.floor(width * 0.12);
                const marginTop = Math.floor(height * 0.08);
                const sampleWidth = Math.max(1, width - (marginX * 2));
                const sampleHeight = Math.max(1, Math.floor(height * 0.62));
                const dominant = this.sampleAverageColor(width, height, {
                    x: marginX,
                    y: marginTop,
                    width: sampleWidth,
                    height: sampleHeight,
                });

                // Estimate actual bar background from lower band and enforce contrast.
                const lowerBandY = Math.max(0, Math.floor(height * 0.72));
                const lowerBandHeight = Math.max(1, height - lowerBandY);
                const barBackground = this.sampleAverageColor(width, height, {
                    x: marginX,
                    y: lowerBandY,
                    width: sampleWidth,
                    height: lowerBandHeight,
                });

                this.avgColor = this.adjustColorForContrast(dominant, barBackground);
                return;
            }

            const video = document.querySelector("video");
            if (video && video.readyState >= 2 && !video.paused) {
                this.colorContext.drawImage(video, 0, 0, width, height);
                this.calculateAverageColor(width, height);
            } else {
                const songImage = document.querySelector("#song-image img");
                if (songImage && songImage.complete) {
                    songImage.crossOrigin = "anonymous";
                    this.colorContext.drawImage(songImage, 0, 0, width, height);
                    this.calculateAverageColor(width, height);
                }
            }
        } catch {
        }
    }

    renderFrame() {
        this.animationFrameId = requestAnimationFrame(this.renderFrame);
        this.updateAverageColor();

        // Ensure Canvas 2D context exists as fallback
        if (!this.ctx && !this.webgpuReady) {
            this.ctx = this.canvas.getContext("2d", { alpha: true });
        }

        this.analyser.getByteFrequencyData(this.dataArray);

        const displayBarCount = this.displayBarCount;
        const barWidth = (this.option.width / displayBarCount) * 2.5;

        let energy = 0;
        const fftBinCount = this.analyser.frequencyBinCount;
        for (let i = 0; i < fftBinCount; i++) {
            energy += this.dataArray[i] / 255;
        }
        const avgEnergy = energy / fftBinCount;
        const gate = Math.min(0.28, this.baseGate + avgEnergy * 0.18);

        const colorR = this.avgColor[0] / 255;
        const colorG = this.avgColor[1] / 255;
        const colorB = this.avgColor[2] / 255;
        const colorRInt = Math.round(this.avgColor[0]);
        const colorGInt = Math.round(this.avgColor[1]);
        const colorBInt = Math.round(this.avgColor[2]);

        const nyquistFrequency = this.audioContext.sampleRate / 2;
        this.updateLogBinRangesIfNeeded(fftBinCount, nyquistFrequency);

        // Prepare bar data (layout: x, width, height, colorR, colorG, colorB, opacity, padding)
        const barData = this.barData;
        let x = 0;

        for (let i = 0; i < displayBarCount; i++) {
            let raw;
            if (this.logFrequencyScale) {
                const rangeIdx = i * 2;
                const binStart = this.logBinRanges[rangeIdx];
                const binEnd = this.logBinRanges[rangeIdx + 1];

                let sum = 0;
                let count = 0;
                for (let j = binStart; j <= binEnd; j++) {
                    sum += this.dataArray[j];
                    count++;
                }
                raw = (count > 0 ? sum / count : 0) / 255;
            } else {
                const t = i / (displayBarCount - 1);
                const sourceIndex = Math.min(fftBinCount - 1, Math.floor(t * (fftBinCount - 1)));
                const nextIndex = Math.min(fftBinCount - 1, sourceIndex + 1);
                raw = ((this.dataArray[sourceIndex] + this.dataArray[nextIndex]) * 0.5) / 255;
            }

            const normalized = Math.max(0, (raw - gate) / (1 - gate));
            const target = Math.pow(normalized, 1.55);

            const prev = this.barLevels[i];
            const coeff = target > prev ? this.attack : this.release;
            const level = prev + (target - prev) * coeff;
            this.barLevels[i] = level;

            const levelForOpacity = Math.min(1, level * 1.35);
            const opacity = 0.28 + (levelForOpacity * 0.72);
            const drawHeight = level * this.option.height;

            const idx = i * 8;
            barData[idx + 0] = x;           // x
            barData[idx + 1] = barWidth;    // width
            barData[idx + 2] = drawHeight;  // height
            barData[idx + 3] = colorR;      // colorR
            barData[idx + 4] = colorG;      // colorG
            barData[idx + 5] = colorB;      // colorB
            barData[idx + 6] = opacity;     // opacity
            barData[idx + 7] = 0;           // padding

            x += barWidth;
        }

        if (this.webgpuReady && this.device) {
            this.renderWebGPU(barData);
        } else {
            this.renderCanvas2D(displayBarCount, barWidth, colorRInt, colorGInt, colorBInt);
        }
    }

    renderWebGPU(barData) {
        try {
            this.queue.writeBuffer(this.barDataBuffer, 0, barData);
            this.queue.writeBuffer(this.colorUniformBuffer, 0, this.uniformData);

            const commandEncoder = this.device.createCommandEncoder();
            const textureView = this.context.getCurrentTexture().createView();
            const renderPass = commandEncoder.beginRenderPass({
                colorAttachments: [
                    {
                        view: textureView,
                        clearValue: { r: 0, g: 0, b: 0, a: 0 },
                        loadOp: "load",
                        storeOp: "store",
                    },
                ],
            });

            renderPass.setPipeline(this.pipeline);
            renderPass.setBindGroup(0, this.bindGroup);
            renderPass.draw(this.displayBarCount * 6);
            renderPass.end();

            this.queue.submit([commandEncoder.finish()]);
        } catch (error) {
            console.error("[WebGPU Render] Error:", error);
            console.error("[WebGPU Render] Stack:", error.stack);
            this.webgpuReady = false;
        }
    }

    renderCanvas2D(displayBarCount, barWidth, colorRInt, colorGInt, colorBInt) {
        if (!this.ctx) {
            // Initialize Canvas 2D with alpha channel
            this.ctx = this.canvas.getContext("2d", { alpha: true });
        }

        this.ctx.clearRect(0, 0, this.option.width, this.option.height);
        const fillStyleBase = `rgba(${colorRInt}, ${colorGInt}, ${colorBInt},`;

        let x = 0;
        for (let i = 0; i < displayBarCount; i++) {
            const levelForOpacity = Math.min(1, this.barLevels[i] * 1.35);
            const opacity = 0.28 + (levelForOpacity * 0.72);
            const drawHeight = this.barLevels[i] * this.option.height;

            this.ctx.fillStyle = `${fillStyleBase}${opacity})`;
            this.ctx.fillRect(x, this.option.height - drawHeight, barWidth, drawHeight);
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
        }
    }

    start() {
        this.render();
    }

    stop() {
        this.visualizer.stop();
    }
}

console.log("Visualizer loaded");

let visualizer;
let visualizerContainerObserver;

document.addEventListener(
    "audioCanPlay",
    (e) => {
        try {
            if (visualizer) {
                visualizer.stop();
                visualizer = null;
            }

            if (visualizerContainerObserver) {
                visualizerContainerObserver.disconnect();
                visualizerContainerObserver = null;
            }

            const video = document.querySelector("video");
            const visualizerContainer = document.querySelector("#player");

            let canvas = document.getElementById("visualizer");
            if (!canvas) {
                canvas = document.createElement("canvas");
                canvas.id = "visualizer";
                canvas.style.position = "absolute";
                canvas.style.top = "0";
                canvas.style.pointerEvents = "none";
                canvas.style.backgroundColor = "transparent";
                canvas.style.zIndex = "1";
                visualizerContainer.append(canvas);
            }

            const resizeCanvas = () => {
                canvas.width = visualizerContainer.clientWidth;
                canvas.height = visualizerContainer.clientHeight;
            };
            resizeCanvas();

            const stream = video?.captureStream?.();
            visualizer = new Visualizer(
                e.detail.audioContext,
                e.detail.audioSource,
                visualizerContainer,
                canvas,
                stream
            );

            const resizeVisualizer = () => {
                resizeCanvas();
                visualizer.resize(canvas.width, canvas.height);
            };

            resizeVisualizer();

            visualizerContainerObserver = new ResizeObserver(() => {
                resizeVisualizer();
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
