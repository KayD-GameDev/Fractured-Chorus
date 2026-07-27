import path from 'node:path';
import sharp from 'sharp';

const ART = 'Assets/FracturedChorus/Art/UI/Combat/Timeline/LeftRail';
const HEADER = { width: 211, height: 358 };
const AVATAR_CENTERS_FROM_TOP = [186, 241, 295];
const AVATAR_SLOT = 40;
const AVATAR_CENTER_X = 22;

const variants = {
    current: {
        clefAlpha: 0.85,
        chipCenterY: 51.7,
        clefCenterY: 122.2
    },
    proposal: {
        clefAlpha: 0.62,
        chipCenterY: 92,
        clefCenterY: 200
    }
};

const variantName = process.argv[2] ?? 'current';
const variant = variants[variantName];
if (!variant) throw new Error(`variant phải là: ${Object.keys(variants).join(' | ')}`);

const layout = [
    { file: 'treble_clef_v4.png', rect: { w: 136.78, h: 152.45 }, center: { x: 102.3, y: variant.clefCenterY }, alpha: variant.clefAlpha },
    { file: 'phase_label_v4.png', rect: { w: 170, h: 62 }, topLeft: { x: 20, y: 6 }, alpha: 1 },
    { file: 'phase_chip_v3.png', rect: { w: 118, h: 44 }, center: { x: 105.5, y: variant.chipCenterY }, alpha: 1 }
];

async function fitted(file, rect, alpha) {
    const src = sharp(path.join(ART, file));
    const meta = await src.metadata();
    const scale = Math.min(rect.w / meta.width, rect.h / meta.height);
    const width = Math.max(1, Math.round(meta.width * scale));
    const height = Math.max(1, Math.round(meta.height * scale));
    let pipeline = src.resize(width, height, { kernel: 'lanczos3' });
    if (alpha < 1) {
        const { data, info } = await pipeline.raw().toBuffer({ resolveWithObject: true });
        for (let i = 3; i < data.length; i += 4) data[i] = Math.round(data[i] * alpha);
        pipeline = sharp(data, { raw: { width: info.width, height: info.height, channels: 4 } });
    }
    return { buffer: await pipeline.png().toBuffer(), width, height };
}

const background = await sharp(path.join(ART, 'left_rail_bg_v1.png'))
    .resize(HEADER.width, HEADER.height, { fit: 'fill' })
    .png()
    .toBuffer();

const composites = [];
for (const item of layout) {
    const art = await fitted(item.file, item.rect, item.alpha);
    const left = item.topLeft
        ? Math.round(item.topLeft.x + (item.rect.w - art.width) / 2)
        : Math.round(item.center.x - art.width / 2);
    const top = item.topLeft
        ? Math.round(item.topLeft.y + (item.rect.h - art.height) / 2)
        : Math.round(item.center.y - art.height / 2);
    composites.push({ input: art.buffer, left, top });
    console.log(`${item.file}: ${art.width}x${art.height} @ y ${top}..${top + art.height}`);
}

const ring = await fitted('lane_avatar_ring_v1.png', { w: AVATAR_SLOT, h: AVATAR_SLOT }, 1);
for (const centerY of AVATAR_CENTERS_FROM_TOP) {
    composites.push({
        input: ring.buffer,
        left: Math.round(AVATAR_CENTER_X - ring.width / 2),
        top: Math.round(centerY - ring.height / 2)
    });
}
console.log(`avatars: y ${AVATAR_CENTERS_FROM_TOP.map((c) => `${c - AVATAR_SLOT / 2}..${c + AVATAR_SLOT / 2}`).join(', ')}`);

const output = `Tools/left_rail_preview_${variantName}.png`;
await sharp(background).composite(composites).png().toFile(output);
console.log(`preview -> ${output}`);
