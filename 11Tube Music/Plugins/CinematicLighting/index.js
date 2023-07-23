console.log("Cinematic lighting loaded");

if (!pv) {
    var pv = {
        map: function (array, f) {
            var o = {};
            return f ? array.map(function (d, i) {
                o.index = i;
                return f.call(o, d);
            }) : array.slice();
        },
        naturalOrder: function (a, b) {
            return (a < b) ? -1 : ((a > b) ? 1 : 0);
        },
        sum: function (array, f) {
            var o = {};
            return array.reduce(f ? function (p, d, i) {
                o.index = i;
                return p + f.call(o, d);
            } : function (p, d) {
                return p + d;
            }, 0);
        },
        max: function (array, f) {
            return Math.max.apply(null, f ? pv.map(array, f) : array);
        }
    }
}

document.addEventListener(
    "audioCanPlay",
    (e) => {
        try {
            const video = document.querySelector("video");
            const playerContainer = document.querySelector("#player");

            let backVideo = document.getElementById("back_video");
            let backPlane = document.getElementById("back_plane");

            if (!backVideo) {
                backVideo = document.createElement("div");
                backVideo.id = "back_video";
                backVideo.style.position = "absolute";
                backVideo.style.top = "0";
                backVideo.style.left = "0";
                backVideo.style.width = "100%";
                backVideo.style.height = "100%";
                backVideo.style.zIndex = "-1";
                backVideo.style.objectFit = "cover";
                backVideo.style.pointerEvents = "none";
                backVideo.style.opacity = "0.5";
                backVideo.style.filter = "blur(100px)";

                playerContainer.appendChild(backVideo);
            }

            if (!backPlane) {
                backPlane = document.createElement("div");
                backPlane.id = "back_plane";
                backPlane.style.position = "absolute";
                backPlane.style.top = "0";
                backPlane.style.left = "0";
                backPlane.style.width = "100%";
                backPlane.style.height = "100%";
                backPlane.style.zIndex = "-1";
                backPlane.style.backgroundColor = "rgb(0,0,0)";
                backPlane.style.pointerEvents = "none";
                backPlane.style.opacity = "0.5";
                backPlane.style.filter = "blur(100px)";
                playerContainer.appendChild(backPlane);
            }

            const stream = video.captureStream();
            const track = stream.getVideoTracks()[0];

            if (track) {
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');

                canvas.style.width = "100%";
                canvas.style.height = "100%";

                backVideo.innerHTML = "";
                backVideo.appendChild(canvas);

                function drawVideoFrame() {
                    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                    requestAnimationFrame(drawVideoFrame);
                }

                drawVideoFrame();


                requestAnimationFrame(() => getAverageColor(track, backPlane));
            } else {
                const getPalette = () => {
                    const song_image = document.querySelector("#song-image img");

                    const clone = song_image.cloneNode(true);
                    clone.style.width = "100%";
                    clone.style.height = "100%";
                    clone.style.margin = "0";
                    clone.style.padding = "0";
                    backVideo.innerHTML = "";
                    backVideo.appendChild(clone);

                    const canvas_forColor = document.createElement('canvas');
                    const context = canvas_forColor.getContext('2d');

                    song_image.crossOrigin = "anonymous";
                    canvas_forColor.width = song_image.width;
                    canvas_forColor.height = song_image.height;
                    context.drawImage(song_image, 0, 0);

                    const pixelCount = canvas_forColor.width * canvas_forColor.height;

                    const pixelArray = createPixelArray(context.getImageData(0, 0, canvas_forColor.width, canvas_forColor.height).data, pixelCount, 10);

                    const cmap = MMCQ.quantize(pixelArray, 5);
                    const palette = cmap ? cmap.palette() : null;

                    return palette;
                };

                const retryUntilSuccess = () => {
                    const palette = getPalette();

                    if (!palette) {
                        setTimeout(retryUntilSuccess, 1000);
                    } else {
                        const dominantColor = palette[0];
                        fadeBackground(backPlane, dominantColor);
                    }
                };

                retryUntilSuccess();
            }
        } catch (error) {
            console.error("Error rendering frame:", error);
        }
    },
    { passive: true }
);


function getAverageColor(track, backPlane) {
    try {
        const canvas_forColor = document.createElement('canvas');
        const context = canvas_forColor.getContext('2d');

        const imageCapture = new ImageCapture(track);

        imageCapture.grabFrame().then(imageBitmap => {
            canvas_forColor.width = imageBitmap.width;
            canvas_forColor.height = imageBitmap.height;
            context.drawImage(imageBitmap, 0, 0);

            const pixelCount = canvas_forColor.width * canvas_forColor.height;

            const pixelArray = createPixelArray(context.getImageData(0, 0, canvas_forColor.width, canvas_forColor.height).data, pixelCount, 10);

            const cmap = MMCQ.quantize(pixelArray, 5);
            const palette = cmap ? cmap.palette() : null;

            const dominantColor = palette[0];

            fadeBackground(backPlane, dominantColor);
        });

        requestAnimationFrame(() => getAverageColor(track, backPlane));
    } catch (error) {
        console.error("Error rendering frame:", error);
    }
}

function fadeBackground(element, targetColor) {
    const currentColor = getRGBColor(element.style.backgroundColor);

    const deltaR = targetColor[0] - currentColor[0];
    const deltaG = targetColor[1] - currentColor[1];
    const deltaB = targetColor[2] - currentColor[2];

    const steps = 10;
    let currentStep = 0;

    //const duration = 500;

    function updateBackground() {
        const progress = currentStep / steps;

        const r = Math.round(currentColor[0] + deltaR * progress);
        const g = Math.round(currentColor[1] + deltaG * progress);
        const b = Math.round(currentColor[2] + deltaB * progress);

        element.style.backgroundColor = `rgb(${r}, ${g}, ${b})`;

        currentStep++;

        if (currentStep <= steps) {
            requestAnimationFrame(updateBackground);
        }
    }

    requestAnimationFrame(updateBackground);
}

function getRGBColor(colorString) {
    const matches = colorString.match(/rgb\((\d+), (\d+), (\d+)\)/);
    if (matches) {
        const [, r, g, b] = matches;
        return [parseInt(r), parseInt(g), parseInt(b)];
    }
    return [0, 0, 0];
}

function createPixelArray(imgData, pixelCount, quality) {
    const pixels = imgData;
    const pixelArray = [];

    for (let i = 0, offset, r, g, b, a; i < pixelCount; i = i + quality) {
        offset = i * 4;
        r = pixels[offset + 0];
        g = pixels[offset + 1];
        b = pixels[offset + 2];
        a = pixels[offset + 3];

        if (typeof a === 'undefined' || a >= 125) {
            if (!(r > 250 && g > 250 && b > 250)) {
                pixelArray.push([r, g, b]);
            }
        }
    }
    return pixelArray;
}



var MMCQ = (function () {
    var sigbits = 5,
        rshift = 8 - sigbits,
        maxIterations = 1000,
        fractByPopulations = 0.75;


    function getColorIndex(r, g, b) {
        return (r << (2 * sigbits)) + (g << sigbits) + b;
    }


    function PQueue(comparator) {
        var contents = [],
            sorted = false;

        function sort() {
            contents.sort(comparator);
            sorted = true;
        }

        return {
            push: function (o) {
                contents.push(o);
                sorted = false;
            },
            peek: function (index) {
                if (!sorted) sort();
                if (index === undefined) index = contents.length - 1;
                return contents[index];
            },
            pop: function () {
                if (!sorted) sort();
                return contents.pop();
            },
            size: function () {
                return contents.length;
            },
            map: function (f) {
                return contents.map(f);
            },
            debug: function () {
                if (!sorted) sort();
                return contents;
            }
        };
    }

    function VBox(r1, r2, g1, g2, b1, b2, histo) {
        var vbox = this;
        vbox.r1 = r1;
        vbox.r2 = r2;
        vbox.g1 = g1;
        vbox.g2 = g2;
        vbox.b1 = b1;
        vbox.b2 = b2;
        vbox.histo = histo;
    }
    VBox.prototype = {
        volume: function (force) {
            var vbox = this;
            if (!vbox._volume || force) {
                vbox._volume = ((vbox.r2 - vbox.r1 + 1) * (vbox.g2 - vbox.g1 + 1) * (vbox.b2 - vbox.b1 + 1));
            }
            return vbox._volume;
        },
        count: function (force) {
            var vbox = this,
                histo = vbox.histo;
            if (!vbox._count_set || force) {
                var npix = 0,
                    i, j, k, index;
                for (i = vbox.r1; i <= vbox.r2; i++) {
                    for (j = vbox.g1; j <= vbox.g2; j++) {
                        for (k = vbox.b1; k <= vbox.b2; k++) {
                            index = getColorIndex(i, j, k);
                            npix += (histo[index] || 0);
                        }
                    }
                }
                vbox._count = npix;
                vbox._count_set = true;
            }
            return vbox._count;
        },
        copy: function () {
            var vbox = this;
            return new VBox(vbox.r1, vbox.r2, vbox.g1, vbox.g2, vbox.b1, vbox.b2, vbox.histo);
        },
        avg: function (force) {
            var vbox = this,
                histo = vbox.histo;
            if (!vbox._avg || force) {
                var ntot = 0,
                    mult = 1 << (8 - sigbits),
                    rsum = 0,
                    gsum = 0,
                    bsum = 0,
                    hval,
                    i, j, k, histoindex;
                for (i = vbox.r1; i <= vbox.r2; i++) {
                    for (j = vbox.g1; j <= vbox.g2; j++) {
                        for (k = vbox.b1; k <= vbox.b2; k++) {
                            histoindex = getColorIndex(i, j, k);
                            hval = histo[histoindex] || 0;
                            ntot += hval;
                            rsum += (hval * (i + 0.5) * mult);
                            gsum += (hval * (j + 0.5) * mult);
                            bsum += (hval * (k + 0.5) * mult);
                        }
                    }
                }
                if (ntot) {
                    vbox._avg = [~~(rsum / ntot), ~~(gsum / ntot), ~~(bsum / ntot)];
                } else {
                    vbox._avg = [~~(mult * (vbox.r1 + vbox.r2 + 1) / 2), ~~(mult * (vbox.g1 + vbox.g2 + 1) / 2), ~~(mult * (vbox.b1 + vbox.b2 + 1) / 2)];
                }
            }
            return vbox._avg;
        },
        contains: function (pixel) {
            var vbox = this,
                rval = pixel[0] >> rshift;
            gval = pixel[1] >> rshift;
            bval = pixel[2] >> rshift;
            return (rval >= vbox.r1 && rval <= vbox.r2 &&
                gval >= vbox.g1 && gval <= vbox.g2 &&
                bval >= vbox.b1 && bval <= vbox.b2);
        }
    };


    function CMap() {
        this.vboxes = new PQueue(function (a, b) {
            return pv.naturalOrder(
                a.vbox.count() * a.vbox.volume(),
                b.vbox.count() * b.vbox.volume()
            )
        });;
    }
    CMap.prototype = {
        push: function (vbox) {
            this.vboxes.push({
                vbox: vbox,
                color: vbox.avg()
            });
        },
        palette: function () {
            return this.vboxes.map(function (vb) {
                return vb.color
            });
        },
        size: function () {
            return this.vboxes.size();
        },
        map: function (color) {
            var vboxes = this.vboxes;
            for (var i = 0; i < vboxes.size(); i++) {
                if (vboxes.peek(i).vbox.contains(color)) {
                    return vboxes.peek(i).color;
                }
            }
            return this.nearest(color);
        },
        nearest: function (color) {
            var vboxes = this.vboxes,
                d1, d2, pColor;
            for (var i = 0; i < vboxes.size(); i++) {
                d2 = Math.sqrt(
                    Math.pow(color[0] - vboxes.peek(i).color[0], 2) +
                    Math.pow(color[1] - vboxes.peek(i).color[1], 2) +
                    Math.pow(color[2] - vboxes.peek(i).color[2], 2)
                );
                if (d2 < d1 || d1 === undefined) {
                    d1 = d2;
                    pColor = vboxes.peek(i).color;
                }
            }
            return pColor;
        },
        forcebw: function () {
            var vboxes = this.vboxes;
            vboxes.sort(function (a, b) {
                return pv.naturalOrder(pv.sum(a.color), pv.sum(b.color))
            });

            var lowest = vboxes[0].color;
            if (lowest[0] < 5 && lowest[1] < 5 && lowest[2] < 5)
                vboxes[0].color = [0, 0, 0];

            var idx = vboxes.length - 1,
                highest = vboxes[idx].color;
            if (highest[0] > 251 && highest[1] > 251 && highest[2] > 251)
                vboxes[idx].color = [255, 255, 255];
        }
    };

    function getHisto(pixels) {
        var histosize = 1 << (3 * sigbits),
            histo = new Array(histosize),
            index, rval, gval, bval;
        pixels.forEach(function (pixel) {
            rval = pixel[0] >> rshift;
            gval = pixel[1] >> rshift;
            bval = pixel[2] >> rshift;
            index = getColorIndex(rval, gval, bval);
            histo[index] = (histo[index] || 0) + 1;
        });
        return histo;
    }

    function vboxFromPixels(pixels, histo) {
        var rmin = 1000000,
            rmax = 0,
            gmin = 1000000,
            gmax = 0,
            bmin = 1000000,
            bmax = 0,
            rval, gval, bval;
        pixels.forEach(function (pixel) {
            rval = pixel[0] >> rshift;
            gval = pixel[1] >> rshift;
            bval = pixel[2] >> rshift;
            if (rval < rmin) rmin = rval;
            else if (rval > rmax) rmax = rval;
            if (gval < gmin) gmin = gval;
            else if (gval > gmax) gmax = gval;
            if (bval < bmin) bmin = bval;
            else if (bval > bmax) bmax = bval;
        });
        return new VBox(rmin, rmax, gmin, gmax, bmin, bmax, histo);
    }

    function medianCutApply(histo, vbox) {
        if (!vbox.count()) return;

        var rw = vbox.r2 - vbox.r1 + 1,
            gw = vbox.g2 - vbox.g1 + 1,
            bw = vbox.b2 - vbox.b1 + 1,
            maxw = pv.max([rw, gw, bw]);
        if (vbox.count() == 1) {
            return [vbox.copy()]
        }
        var total = 0,
            partialsum = [],
            lookaheadsum = [],
            i, j, k, sum, index;
        if (maxw == rw) {
            for (i = vbox.r1; i <= vbox.r2; i++) {
                sum = 0;
                for (j = vbox.g1; j <= vbox.g2; j++) {
                    for (k = vbox.b1; k <= vbox.b2; k++) {
                        index = getColorIndex(i, j, k);
                        sum += (histo[index] || 0);
                    }
                }
                total += sum;
                partialsum[i] = total;
            }
        } else if (maxw == gw) {
            for (i = vbox.g1; i <= vbox.g2; i++) {
                sum = 0;
                for (j = vbox.r1; j <= vbox.r2; j++) {
                    for (k = vbox.b1; k <= vbox.b2; k++) {
                        index = getColorIndex(j, i, k);
                        sum += (histo[index] || 0);
                    }
                }
                total += sum;
                partialsum[i] = total;
            }
        } else {
            for (i = vbox.b1; i <= vbox.b2; i++) {
                sum = 0;
                for (j = vbox.r1; j <= vbox.r2; j++) {
                    for (k = vbox.g1; k <= vbox.g2; k++) {
                        index = getColorIndex(j, k, i);
                        sum += (histo[index] || 0);
                    }
                }
                total += sum;
                partialsum[i] = total;
            }
        }
        partialsum.forEach(function (d, i) {
            lookaheadsum[i] = total - d
        });

        function doCut(color) {
            var dim1 = color + '1',
                dim2 = color + '2',
                left, right, vbox1, vbox2, d2, count2 = 0;
            for (i = vbox[dim1]; i <= vbox[dim2]; i++) {
                if (partialsum[i] > total / 2) {
                    vbox1 = vbox.copy();
                    vbox2 = vbox.copy();
                    left = i - vbox[dim1];
                    right = vbox[dim2] - i;
                    if (left <= right)
                        d2 = Math.min(vbox[dim2] - 1, ~~(i + right / 2));
                    else d2 = Math.max(vbox[dim1], ~~(i - 1 - left / 2));
                    while (!partialsum[d2]) d2++;
                    count2 = lookaheadsum[d2];
                    while (!count2 && partialsum[d2 - 1]) count2 = lookaheadsum[--d2];
                    vbox1[dim2] = d2;
                    vbox2[dim1] = vbox1[dim2] + 1;
                    return [vbox1, vbox2];
                }
            }

        }
        return maxw == rw ? doCut('r') :
            maxw == gw ? doCut('g') :
                doCut('b');
    }

    function quantize(pixels, maxcolors) {
        if (!pixels.length || maxcolors < 2 || maxcolors > 256) {
            return false;
        }


        var histo = getHisto(pixels),
            histosize = 1 << (3 * sigbits);

        var nColors = 0;
        histo.forEach(function () {
            nColors++
        });
        if (nColors <= maxcolors) {
        }

        var vbox = vboxFromPixels(pixels, histo),
            pq = new PQueue(function (a, b) {
                return pv.naturalOrder(a.count(), b.count())
            });
        pq.push(vbox);

        function iter(lh, target) {
            var ncolors = 1,
                niters = 0,
                vbox;
            while (niters < maxIterations) {
                vbox = lh.pop();
                if (!vbox.count()) {
                    lh.push(vbox);
                    niters++;
                    continue;
                }
                var vboxes = medianCutApply(histo, vbox),
                    vbox1 = vboxes[0],
                    vbox2 = vboxes[1];

                if (!vbox1) {
                    return;
                }
                lh.push(vbox1);
                if (vbox2) {
                    lh.push(vbox2);
                    ncolors++;
                }
                if (ncolors >= target) return;
                if (niters++ > maxIterations) {
                    return;
                }
            }
        }

        iter(pq, fractByPopulations * maxcolors);

        var pq2 = new PQueue(function (a, b) {
            return pv.naturalOrder(a.count() * a.volume(), b.count() * b.volume())
        });
        while (pq.size()) {
            pq2.push(pq.pop());
        }

        iter(pq2, maxcolors - pq2.size());

        var cmap = new CMap();
        while (pq2.size()) {
            cmap.push(pq2.pop());
        }

        return cmap;
    }

    return {
        quantize: quantize
    }
})();