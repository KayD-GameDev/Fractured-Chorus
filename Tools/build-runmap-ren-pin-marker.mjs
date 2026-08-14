import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';

const root = path.resolve(import.meta.dirname, '..');
const pinRefPath = path.join(
    root,
    'Assets/FracturedChorus/Art/UI/RunMap/Player/_ref_runmap_pin_marker_v1.png',
);
const avatarPath = path.join(
    root,
    'Assets/FracturedChorus/Art/UI/Combat/Timeline/LeftRail/Avatars/ren_chibi_avatar_v1.png',
);
const outPath = path.join(
    root,
    'Assets/FracturedChorus/Art/UI/RunMap/Player/runmap_ren_pin_marker_v1.png',
);
const idlePath = path.join(
    root,
    'Assets/FracturedChorus/Art/UI/RunMap/Player/runmap_ren_chibi_idle_v1.png',
);
const travelPath = path.join(
    root,
    'Assets/FracturedChorus/Art/UI/RunMap/Player/runmap_ren_chibi_travel_v1.png',
);

const CURSOR_PIN =
    'C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_empty-window_images_image-1285912c-4779-4f91-9055-97d17d53b31e.png';

const HOLE_CENTER_Y_RATIO = 0.34;
const HOLE_DIAMETER_RATIO = 0.36;
const AVATAR_SCALE = 0.92;

const PIN_HIGHLIGHT = { r: 140, g: 243, b: 255 };
const PIN_BODY = { r: 34, g: 211, b: 238 };
const PIN_SHADOW = { r: 0, g: 140, b: 179 };
const PIN_DEEP = { r: 8, g: 88, b: 112 };

function colorDistance(r, g, b, sr, sg, sb) {
    return Math.abs(r - sr) + Math.abs(g - sg) + Math.abs(b - sb);
}

function lerp(a, b, t) {
    return a + (b - a) * t;
}

function lerpColor(c1, c2, t) {
    return {
        r: Math.round(lerp(c1.r, c2.r, t)),
        g: Math.round(lerp(c1.g, c2.g, t)),
        b: Math.round(lerp(c1.b, c2.b, t)),
    };
}

function floodOuterBackground(data, width, height, tolerance) {
    const mask = new Uint8Array(width * height);
    const queue = [];
    const corners = [
        [0, 0],
        [width - 1, 0],
        [0, height - 1],
        [width - 1, height - 1],
    ];

    let bgR = 0;
    let bgG = 0;
    let bgB = 0;
    for (const [x, y] of corners) {
        const i = (y * width + x) * 4;
        bgR += data[i];
        bgG += data[i + 1];
        bgB += data[i + 2];
    }
    bgR /= corners.length;
    bgG /= corners.length;
    bgB /= corners.length;

    const matchesBg = (index) => {
        const src = index * 4;
        return colorDistance(data[src], data[src + 1], data[src + 2], bgR, bgG, bgB) <= tolerance;
    };

    const seed = (x, y) => {
        const index = y * width + x;
        if (mask[index] || !matchesBg(index)) {
            return;
        }
        mask[index] = 1;
        queue.push(index);
    };

    for (let x = 0; x < width; x++) {
        seed(x, 0);
        seed(x, height - 1);
    }
    for (let y = 0; y < height; y++) {
        seed(0, y);
        seed(width - 1, y);
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
            if (next < 0 || mask[next]) {
                continue;
            }
            if (matchesBg(next)) {
                mask[next] = 1;
                queue.push(next);
            }
        }
    }

    return mask;
}

function isHolePixel(r, g, b) {
    return r >= 235 && g >= 235 && b >= 235;
}

function isPinPixel(r, g, b) {
    return r > 60 && r > g + 20 && r > b + 10;
}

function recolorPinPixel(r, g, b, x, width) {
    const side = x / Math.max(1, width - 1);
    const brightness = Math.min(1, (r * 0.55 + g * 0.2 + b * 0.25) / 255);
    const highlightMix = (1 - side) * 0.55 + brightness * 0.45;

    let color;
    if (brightness > 0.72) {
        color = lerpColor(PIN_BODY, PIN_HIGHLIGHT, highlightMix);
    } else if (brightness > 0.42) {
        color = lerpColor(PIN_SHADOW, PIN_BODY, brightness * 1.4);
    } else {
        color = lerpColor(PIN_DEEP, PIN_SHADOW, brightness * 2.2);
    }

    if (side > 0.62) {
        color = lerpColor(color, PIN_SHADOW, (side - 0.62) * 1.4);
    }

    return color;
}

function buildPinFrame(width, height, data) {
    const outerBg = floodOuterBackground(data, width, height, 28);
    const out = Buffer.alloc(width * height * 4);

    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const index = y * width + x;
            const src = index * 4;
            const r = data[src];
            const g = data[src + 1];
            const b = data[src + 2];
            const dst = src;

            if (outerBg[index] || isHolePixel(r, g, b) || !isPinPixel(r, g, b)) {
                out[dst] = 0;
                out[dst + 1] = 0;
                out[dst + 2] = 0;
                out[dst + 3] = 0;
                continue;
            }

            const tinted = recolorPinPixel(r, g, b, x, width);
            out[dst] = tinted.r;
            out[dst + 1] = tinted.g;
            out[dst + 2] = tinted.b;
            out[dst + 3] = 255;
        }
    }

    return out;
}

function keepMainPinOnly(frame, width, height) {
    const total = width * height;
    const visited = new Uint8Array(total);
    const components = [];

    for (let i = 0; i < total; i++) {
        if (visited[i] || frame[i * 4 + 3] === 0) {
            continue;
        }

        const queue = [i];
        visited[i] = 1;
        let minX = width;
        let minY = height;
        let maxX = -1;
        let maxY = -1;
        let count = 0;

        while (queue.length > 0) {
            const index = queue.pop();
            const x = index % width;
            const y = (index / width) | 0;
            count++;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;

            const neighbors = [
                x > 0 ? index - 1 : -1,
                x < width - 1 ? index + 1 : -1,
                y > 0 ? index - width : -1,
                y < height - 1 ? index + width : -1,
            ];

            for (const next of neighbors) {
                if (next < 0 || visited[next] || frame[next * 4 + 3] === 0) {
                    continue;
                }
                visited[next] = 1;
                queue.push(next);
            }
        }

        const boxW = maxX - minX + 1;
        const boxH = maxY - minY + 1;
        components.push({ count, minX, minY, maxX, maxY, boxW, boxH });
    }

    if (components.length <= 1) {
        return frame;
    }

    components.sort((a, b) => b.count - a.count);
    const main = components[0];

    for (const comp of components.slice(1)) {
        const flatOval =
            comp.maxY > height * 0.78 &&
            comp.boxW > comp.boxH * 1.35 &&
            comp.count < main.count * 0.22;
        if (!flatOval) {
            continue;
        }

        for (let y = comp.minY; y <= comp.maxY; y++) {
            for (let x = comp.minX; x <= comp.maxX; x++) {
                const index = y * width + x;
                const dst = index * 4;
                frame[dst] = 0;
                frame[dst + 1] = 0;
                frame[dst + 2] = 0;
                frame[dst + 3] = 0;
            }
        }
    }

    return frame;
}

async function main() {
    fs.mkdirSync(path.dirname(pinRefPath), { recursive: true });
    if (!fs.existsSync(pinRefPath)) {
        fs.copyFileSync(CURSOR_PIN, pinRefPath);
    }

    const pin = await sharp(pinRefPath).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    const { width, height } = pin.info;

    let pinFrame = buildPinFrame(width, height, pin.data);
    pinFrame = keepMainPinOnly(pinFrame, width, height);

    const holeDiameter = Math.round(width * HOLE_DIAMETER_RATIO);
    const holeCenterX = Math.round(width * 0.5);
    const holeCenterY = Math.round(height * HOLE_CENTER_Y_RATIO);
    const avatarSize = Math.round(holeDiameter * AVATAR_SCALE);

    const avatar = await sharp(avatarPath)
        .resize(avatarSize, avatarSize, { fit: 'cover' })
        .png()
        .toBuffer();

    const avatarLeft = holeCenterX - Math.round(avatarSize / 2);
    const avatarTop = holeCenterY - Math.round(avatarSize / 2);

    const composed = await sharp({
        create: {
            width,
            height,
            channels: 4,
            background: { r: 0, g: 0, b: 0, alpha: 0 },
        },
    })
        .composite([
            { input: avatar, left: avatarLeft, top: avatarTop },
            { input: pinFrame, raw: { width, height, channels: 4 } },
        ])
        .png()
        .toBuffer();

    const { data: raw, info } = await sharp(composed).raw().toBuffer({ resolveWithObject: true });
    let minX = info.width;
    let minY = info.height;
    let maxX = -1;
    let maxY = -1;
    for (let y = 0; y < info.height; y++) {
        for (let x = 0; x < info.width; x++) {
            const a = raw[(y * info.width + x) * 4 + 3];
            if (a <= 0) {
                continue;
            }
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
    }

    const pad = 4;
    minX = Math.max(0, minX - pad);
    minY = Math.max(0, minY - pad);
    maxX = Math.min(info.width - 1, maxX + pad);
    maxY = Math.min(info.height - 1, maxY + pad);

    const cropW = maxX - minX + 1;
    const cropH = maxY - minY + 1;

    await sharp(composed)
        .extract({ left: minX, top: minY, width: cropW, height: cropH })
        .png({ compressionLevel: 6, adaptiveFiltering: true })
        .toFile(outPath);

    fs.copyFileSync(outPath, idlePath);
    fs.copyFileSync(outPath, travelPath);

    console.log(
        JSON.stringify({
            output: path.basename(outPath),
            crop: `${cropW}x${cropH}`,
            palette: ['#8CF3FF', '#22D3EE', '#008CB3'],
        }),
    );
}

main().catch((error) => {
    console.error(error);
    process.exit(1);
});
