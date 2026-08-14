import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';

const root = path.resolve(import.meta.dirname, '..');
const sourcePath = path.join(
    root,
    'Assets/FracturedChorus/Art/Characters/Ren/Chibi/ren_chibi_fullbody_v1.png',
);
const idlePath = path.join(
    root,
    'Assets/FracturedChorus/Art/UI/RunMap/Player/runmap_ren_chibi_idle_v1.png',
);
const travelPath = path.join(
    root,
    'Assets/FracturedChorus/Art/UI/RunMap/Player/runmap_ren_chibi_travel_v1.png',
);

const FLOOD_TOLERANCE = 22;
const EDGE_FEATHER = 16;
const CROP_PAD = 16;

function sampleCornerBackground(data, width, height) {
    const corners = [
        [0, 0],
        [width - 1, 0],
        [0, height - 1],
        [width - 1, height - 1],
    ];

    let r = 0;
    let g = 0;
    let b = 0;
    for (const [x, y] of corners) {
        const i = (y * width + x) * 4;
        r += data[i];
        g += data[i + 1];
        b += data[i + 2];
    }

    return { r: r / 4, g: g / 4, b: b / 4 };
}

function colorDistance(r, g, b, bg) {
    return Math.abs(r - bg.r) + Math.abs(g - bg.g) + Math.abs(b - bg.b);
}

function matchesBackground(r, g, b, bg, tolerance) {
    return colorDistance(r, g, b, bg) <= tolerance;
}

function floodBackgroundMask(data, width, height, bg, tolerance) {
    const total = width * height;
    const isBackground = new Uint8Array(total);
    const queue = [];

    const trySeed = (x, y) => {
        const index = y * width + x;
        if (isBackground[index]) {
            return;
        }

        const src = index * 4;
        if (!matchesBackground(data[src], data[src + 1], data[src + 2], bg, tolerance)) {
            return;
        }

        isBackground[index] = 1;
        queue.push(index);
    };

    for (let x = 0; x < width; x++) {
        trySeed(x, 0);
        trySeed(x, height - 1);
    }

    for (let y = 0; y < height; y++) {
        trySeed(0, y);
        trySeed(width - 1, y);
    }

    while (queue.length > 0) {
        const index = queue.pop();
        const x = index % width;
        const y = (index / width) | 0;
        const neighbors = [
            x > 0 ? index - 1 : -1,
            x < width - 1 ? index + 1 : -1,
            y > 0 ? index - width : -1,
            y < height - 1 ? index + width : -1,
        ];

        for (const next of neighbors) {
            if (next < 0 || isBackground[next]) {
                continue;
            }

            const src = next * 4;
            if (matchesBackground(data[src], data[src + 1], data[src + 2], bg, tolerance)) {
                isBackground[next] = 1;
                queue.push(next);
            }
        }
    }

    return isBackground;
}

function alphaForForegroundPixel(r, g, b, bg) {
    const dist = colorDistance(r, g, b, bg);
    if (dist <= FLOOD_TOLERANCE) {
        return 0;
    }

    if (dist >= FLOOD_TOLERANCE + EDGE_FEATHER) {
        return 255;
    }

    return Math.round(((dist - FLOOD_TOLERANCE) / EDGE_FEATHER) * 255);
}

function unmultiplyRgb(r, g, b, a) {
    if (a <= 0) {
        return [0, 0, 0];
    }

    if (a >= 255) {
        return [r, g, b];
    }

    const alpha = a / 255;
    return [
        Math.min(255, Math.round(r / alpha)),
        Math.min(255, Math.round(g / alpha)),
        Math.min(255, Math.round(b / alpha)),
    ];
}

async function exportRenSprite(inputPath, outputPath) {
    const { data, info } = await sharp(inputPath)
        .ensureAlpha()
        .raw()
        .toBuffer({ resolveWithObject: true });

    const { width, height } = info;
    const bg = sampleCornerBackground(data, width, height);
    const backgroundMask = floodBackgroundMask(data, width, height, bg, FLOOD_TOLERANCE);
    const out = Buffer.alloc(width * height * 4);

    let minX = width;
    let minY = height;
    let maxX = -1;
    let maxY = -1;
    let semi = 0;
    let removed = 0;

    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const index = y * width + x;
            const src = index * 4;
            const r = data[src];
            const g = data[src + 1];
            const b = data[src + 2];

            let a = 0;
            if (!backgroundMask[index]) {
                a = alphaForForegroundPixel(r, g, b, bg);
            } else {
                removed++;
            }

            const dst = src;
            if (a <= 0) {
                out[dst] = 0;
                out[dst + 1] = 0;
                out[dst + 2] = 0;
                out[dst + 3] = 0;
                continue;
            }

            const [ur, ug, ub] = unmultiplyRgb(r, g, b, a);
            out[dst] = ur;
            out[dst + 1] = ug;
            out[dst + 2] = ub;
            out[dst + 3] = a;

            if (a > 0 && a < 255) {
                semi++;
            }

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
    }

    if (maxX < 0) {
        throw new Error(`No visible pixels after matte ${inputPath}`);
    }

    minX = Math.max(0, minX - CROP_PAD);
    minY = Math.max(0, minY - CROP_PAD);
    maxX = Math.min(width - 1, maxX + CROP_PAD);
    maxY = Math.min(height - 1, maxY + CROP_PAD);

    const cropW = maxX - minX + 1;
    const cropH = maxY - minY + 1;

    await sharp(out, { raw: { width, height, channels: 4 } })
        .extract({ left: minX, top: minY, width: cropW, height: cropH })
        .png({ compressionLevel: 6, adaptiveFiltering: true })
        .toFile(outputPath);

    console.log(
        JSON.stringify({
            output: path.basename(outputPath),
            source: path.basename(inputPath),
            bg: { r: Math.round(bg.r), g: Math.round(bg.g), b: Math.round(bg.b) },
            crop: `${cropW}x${cropH}`,
            floodRemoved: removed,
            edgePixels: semi,
        }),
    );
}

if (!fs.existsSync(sourcePath)) {
    console.error(`Missing source: ${sourcePath}`);
    process.exit(1);
}

await exportRenSprite(sourcePath, idlePath);
fs.copyFileSync(idlePath, travelPath);
console.log(`copied -> ${path.basename(travelPath)}`);
