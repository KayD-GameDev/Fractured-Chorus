import fs from 'node:fs';
import path from 'node:path';
import sharp from 'sharp';

const root = path.resolve(import.meta.dirname, '..');

async function hardenAlpha(inputPath, outputPath, cutoff = 48) {
    const { data, info } = await sharp(inputPath)
        .ensureAlpha()
        .raw()
        .toBuffer({ resolveWithObject: true });

    const { width, height } = info;
    const out = Buffer.from(data);

    for (let i = 3; i < out.length; i += 4) {
        out[i] = out[i] >= cutoff ? 255 : 0;
    }

    await sharp(out, { raw: { width, height, channels: 4 } })
        .png()
        .toFile(outputPath);

    console.log(`hardened alpha on ${path.basename(inputPath)}`);
}

async function cropToInk(inputPath, outputPath, pad = 24) {
    const { data, info } = await sharp(inputPath)
        .ensureAlpha()
        .raw()
        .toBuffer({ resolveWithObject: true });

    const { width, height } = info;
    let minX = width;
    let minY = height;
    let maxX = -1;
    let maxY = -1;

    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const i = (y * width + x) * 4;
            if (data[i + 3] < 12) continue;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
    }

    if (maxX < 0) throw new Error(`No ink in ${inputPath}`);

    minX = Math.max(0, minX - pad);
    minY = Math.max(0, minY - pad);
    maxX = Math.min(width - 1, maxX + pad);
    maxY = Math.min(height - 1, maxY + pad);

    await sharp(inputPath)
        .extract({
            left: minX,
            top: minY,
            width: maxX - minX + 1,
            height: maxY - minY + 1,
        })
        .png()
        .toFile(outputPath);

    console.log(`cropped ${path.basename(inputPath)} -> ${maxX - minX + 1}x${maxY - minY + 1}`);
}

async function removeStartBlueGlow(inputPath, outputPath) {
    const { data, info } = await sharp(inputPath)
        .ensureAlpha()
        .raw()
        .toBuffer({ resolveWithObject: true });

    const { width, height } = info;
    const out = Buffer.from(data);
    const glowBandTop = Math.floor(height * 0.58);

    for (let y = glowBandTop; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const i = (y * width + x) * 4;
            const r = out[i];
            const g = out[i + 1];
            const b = out[i + 2];
            const a = out[i + 3];
            if (a < 8) continue;

            const max = Math.max(r, g, b);
            const min = Math.min(r, g, b);
            const sat = max - min;
            const isBlueGlow =
                b > r + 18 &&
                b > g - 8 &&
                (g > 90 || b > 120) &&
                sat > 24;
            const isCyanRing =
                g > 100 &&
                b > 100 &&
                r < 120 &&
                sat > 30;

            if (isBlueGlow || isCyanRing) {
                out[i + 3] = 0;
            }
        }
    }

    await sharp(out, { raw: { width, height, channels: 4 } })
        .png()
        .toFile(outputPath);

    console.log(`removed blue glow from ${path.basename(inputPath)}`);
}

const renIdle = path.join(root, 'Assets/FracturedChorus/Art/UI/RunMap/Player/runmap_ren_chibi_idle_v1.png');
const renTravel = path.join(root, 'Assets/FracturedChorus/Art/UI/RunMap/Player/runmap_ren_chibi_travel_v1.png');
const startNode = path.join(root, 'Assets/FracturedChorus/Art/UI/RunMap/Nodes/runmap_node_start_v1.png');

const startTmp = startNode + '.tmp.png';

await removeStartBlueGlow(startNode, startTmp);
fs.renameSync(startTmp, startNode);

console.log('done');
