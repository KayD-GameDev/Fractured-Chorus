import sharp from 'sharp';
import path from 'node:path';

const AVATAR_DIR = 'Tools/out_avatars';
const FRAME = 'Assets/FracturedChorus/Art/UI/Combat/Timeline/LeftRail/lane_avatar_frame_pc_v1.png';
const NAMES = ['ren', 'coda', 'charlotte'];
const VARIANTS = ['bust', 'full'];
const CELL = 128;
const GAP = 24;
const REAL = 48;

async function slot(name, variant, size) {
  const avatar = await sharp(path.join(AVATAR_DIR, `${name}_chibi_avatar_${variant}_v1.png`))
    .resize(size, size, { kernel: 'lanczos3' })
    .png()
    .toBuffer();
  const frame = await sharp(FRAME).resize(size, size, { kernel: 'lanczos3' }).png().toBuffer();
  return sharp(avatar).composite([{ input: frame }]).png().toBuffer();
}

const width = GAP + NAMES.length * (CELL + GAP);
const height = GAP + VARIANTS.length * (CELL + GAP) + (REAL + GAP) * 2;
const layers = [];

for (let v = 0; v < VARIANTS.length; v++) {
  for (let n = 0; n < NAMES.length; n++) {
    layers.push({
      input: await slot(NAMES[n], VARIANTS[v], CELL),
      left: GAP + n * (CELL + GAP),
      top: GAP + v * (CELL + GAP),
    });
  }
}

let rowTop = GAP + VARIANTS.length * (CELL + GAP);
for (let v = 0; v < VARIANTS.length; v++) {
  for (let n = 0; n < NAMES.length; n++) {
    layers.push({
      input: await slot(NAMES[n], VARIANTS[v], REAL),
      left: GAP + n * (CELL + GAP),
      top: rowTop,
    });
  }
  rowTop += REAL + GAP;
}

await sharp({
  create: { width, height, channels: 4, background: { r: 12, g: 14, b: 24, alpha: 1 } },
})
  .composite(layers)
  .png()
  .toFile('Tools/avatar_preview.png');

console.log(`Tools/avatar_preview.png ${width}x${height} — rows: bust@128, full@128, bust@48, full@48`);
