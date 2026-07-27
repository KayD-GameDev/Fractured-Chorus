import path from 'node:path';
import sharp from 'sharp';

const ALPHA_NOISE_FLOOR = 2;
const INK_THRESHOLD = 8;
const NEAR_WHITE_LEVEL = 200;
const NEAR_WHITE_SATURATION = 30;

function parseArgs(argv) {
    const positional = [];
    const flags = {};
    for (let i = 0; i < argv.length; i++) {
        const token = argv[i];
        if (token.startsWith('--')) {
            const key = token.slice(2);
            const next = argv[i + 1];
            if (next === undefined || next.startsWith('--')) {
                flags[key] = true;
            } else {
                flags[key] = next;
                i++;
            }
        } else {
            positional.push(token);
        }
    }
    return { positional, flags };
}

function parseSize(value, label) {
    if (!value) return null;
    const match = /^(\d+)x(\d+)$/i.exec(String(value));
    if (!match) throw new Error(`${label} phải có dạng WxH, nhận "${value}"`);
    return { width: Number(match[1]), height: Number(match[2]) };
}

function matte(data, width, height, knee, alphaGamma) {
    const out = Buffer.alloc(width * height * 4);
    for (let i = 0; i < width * height; i++) {
        const o = i * 4;
        const srcAlpha = data[o + 3] / 255;
        const r = data[o];
        const g = data[o + 1];
        const b = data[o + 2];
        const luma = (Math.max(r, g, b) / 255) * srcAlpha;
        let alpha = Math.min(1, luma / knee);
        if (alphaGamma !== 1) alpha = Math.pow(alpha, alphaGamma);
        const alpha8 = Math.round(alpha * 255);
        if (alpha8 < ALPHA_NOISE_FLOOR) continue;
        const norm = Math.max(luma, 1 / 255);
        out[o] = Math.min(255, Math.round(r / 255 / norm * 255));
        out[o + 1] = Math.min(255, Math.round(g / 255 / norm * 255));
        out[o + 2] = Math.min(255, Math.round(b / 255 / norm * 255));
        out[o + 3] = alpha8;
    }
    return out;
}

function inkBounds(data, width, height) {
    let minX = width;
    let minY = height;
    let maxX = -1;
    let maxY = -1;
    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            if (data[(y * width + x) * 4 + 3] < INK_THRESHOLD) continue;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
    }
    if (maxX < 0) throw new Error('Ảnh rỗng sau khi matte — kiểm tra nền gen có phải đen tuyệt đối không.');
    return { left: minX, top: minY, width: maxX - minX + 1, height: maxY - minY + 1 };
}

function audit(data, width, height) {
    let nonZero = 0;
    let semi = 0;
    let nearWhite = 0;
    let borderAlpha = 0;
    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const o = (y * width + x) * 4;
            const a = data[o + 3];
            const onBorder = x < 2 || y < 2 || x >= width - 2 || y >= height - 2;
            if (onBorder && a > borderAlpha) borderAlpha = a;
            if (a === 0) continue;
            nonZero++;
            if (a < 255) semi++;
            const r = data[o];
            const g = data[o + 1];
            const b = data[o + 2];
            const max = Math.max(r, g, b);
            const min = Math.min(r, g, b);
            if (a > NEAR_WHITE_LEVEL && max > NEAR_WHITE_LEVEL && max - min < NEAR_WHITE_SATURATION) nearWhite++;
        }
    }
    const bounds = inkBounds(data, width, height);
    return {
        canvas: `${width}x${height}`,
        ink: `${bounds.width}x${bounds.height}`,
        inkFill: `${(bounds.width / width * 100).toFixed(1)}% W / ${(bounds.height / height * 100).toFixed(1)}% H`,
        semiAlphaPct: nonZero ? semi / nonZero * 100 : 0,
        nearWhitePct: nonZero ? nearWhite / nonZero * 100 : 0,
        borderMaxAlpha: borderAlpha,
        centerOffset: {
            x: (bounds.left + bounds.width / 2) - width / 2,
            y: (bounds.top + bounds.height / 2) - height / 2
        }
    };
}

function printReport(label, report) {
    console.log(`\n[${label}]`);
    console.log(`  canvas           ${report.canvas}`);
    console.log(`  ink bbox         ${report.ink}  (${report.inkFill})`);
    console.log(`  G1 semi-alpha    ${report.semiAlphaPct.toFixed(1)}%  ${report.semiAlphaPct >= 15 ? 'PASS' : 'FAIL (<15%)'}`);
    console.log(`  G2 border alpha  ${report.borderMaxAlpha}  ${report.borderMaxAlpha === 0 ? 'PASS' : 'FAIL (!=0)'}`);
    console.log(`  G3 near-white    ${report.nearWhitePct.toFixed(1)}%  ${report.nearWhitePct <= 5 ? 'PASS' : 'FAIL (>5%)'}`);
    console.log(`  G6 center offset ${report.centerOffset.x.toFixed(1)}, ${report.centerOffset.y.toFixed(1)} px  ${Math.abs(report.centerOffset.x) <= 2 && Math.abs(report.centerOffset.y) <= 2 ? 'PASS' : 'FAIL (>2px)'}`);
}

async function readRgba(file) {
    const image = sharp(file).ensureAlpha();
    const { data, info } = await image.raw().toBuffer({ resolveWithObject: true });
    return { data, width: info.width, height: info.height };
}

async function main() {
    const { positional, flags } = parseArgs(process.argv.slice(2));
    if (positional.length < 1) {
        console.error('Usage: node Tools/neon-matte.mjs <in.png> [out.png] --canvas WxH --ink WxH [--fit inside|fill] [--knee 0.9] [--alpha-gamma 1] [--report-only]');
        process.exit(1);
    }

    const input = path.resolve(positional[0]);
    const reportOnly = Boolean(flags['report-only']);

    if (reportOnly) {
        const src = await readRgba(input);
        printReport(`AUDIT ${path.basename(input)}`, audit(src.data, src.width, src.height));
        return;
    }

    const output = path.resolve(positional[1] ?? input);
    const canvas = parseSize(flags.canvas, '--canvas');
    const ink = parseSize(flags.ink, '--ink');
    if (!canvas || !ink) throw new Error('Thiếu --canvas WxH hoặc --ink WxH');

    const knee = Number(flags.knee ?? 0.9);
    const alphaGamma = Number(flags['alpha-gamma'] ?? 1);

    const src = await readRgba(input);
    console.log(`\n[SOURCE ${path.basename(input)}] ${src.width}x${src.height}`);

    const matted = matte(src.data, src.width, src.height, knee, alphaGamma);
    const bounds = inkBounds(matted, src.width, src.height);
    console.log(`  matted ink bbox  ${bounds.width}x${bounds.height} @ ${bounds.left},${bounds.top}`);

    const trimmed = await sharp(matted, { raw: { width: src.width, height: src.height, channels: 4 } })
        .extract(bounds)
        .resize(ink.width, ink.height, { fit: flags.fit === 'fill' ? 'fill' : 'inside', kernel: 'lanczos3' })
        .raw()
        .toBuffer({ resolveWithObject: true });

    const padLeft = Math.round((canvas.width - trimmed.info.width) / 2);
    const padTop = Math.round((canvas.height - trimmed.info.height) / 2);

    await sharp({
        create: {
            width: canvas.width,
            height: canvas.height,
            channels: 4,
            background: { r: 0, g: 0, b: 0, alpha: 0 }
        }
    })
        .composite([{
            input: trimmed.data,
            raw: { width: trimmed.info.width, height: trimmed.info.height, channels: 4 },
            left: padLeft,
            top: padTop
        }])
        .png({ compressionLevel: 9 })
        .toFile(output);

    const result = await readRgba(output);
    printReport(`OUTPUT ${path.basename(output)}`, audit(result.data, result.width, result.height));
}

main().catch((error) => {
    console.error(`neon-matte failed: ${error.message}`);
    process.exit(1);
});
