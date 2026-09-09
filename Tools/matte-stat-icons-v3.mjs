import sharp from 'sharp';
import path from 'path';

const srcDir = 'C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/';
const outDir = 'D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/Icons/';

const files = [
  'ui_stat_icon_strength_v3.png',
  'ui_stat_icon_magic_v3.png',
  'ui_stat_icon_endurance_v3.png',
  'ui_stat_icon_heartbeat_v3.png',
  'ui_stat_icon_luck_v3.png',
];

const TARGET = { r: 45, g: 50, b: 80 }; // #2D3250

function isBg(r, g, b) {
  // magenta / hot pink
  if (r > 170 && b > 130 && g < 170 && r + b > 2.1 * g) return true;
  // near white
  if (r > 248 && g > 248 && b > 248) return true;
  // pure black leftover from previous matte
  if (r < 8 && g < 8 && b < 8) return true;
  return false;
}

function bgSoft(r, g, b) {
  if (r > 140 && b > 100 && g < 180 && r + b - 2 * g > 60) {
    return Math.min(1, (r + b - 2 * g - 60) / 160);
  }
  return 0;
}

for (const f of files) {
  const src = srcDir + f;
  const dest = outDir + f.replace('_v3', '_v2');
  const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width;
  const h = info.height;
  const out = Buffer.alloc(w * h * 4);

  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    let r = data[o];
    let g = data[o + 1];
    let b = data[o + 2];
    let a = data[o + 3];

    if (isBg(r, g, b)) {
      continue;
    }

    const soft = bgSoft(r, g, b);
    a = Math.round(a * (1 - soft));
    if (a < 10) continue;

    // keep luminance of original for AA fringe, but paint target color
    const lum = (r + g + b) / (3 * 255);
    // dark icon body → full target; light fringe → lower alpha
    const body = lum < 0.55 ? 1 : Math.max(0, 1 - (lum - 0.55) / 0.45);
    a = Math.round(a * body);
    if (a < 10) continue;

    out[o] = TARGET.r;
    out[o + 1] = TARGET.g;
    out[o + 2] = TARGET.b;
    out[o + 3] = a;
  }

  // bbox
  let minX = w;
  let minY = h;
  let maxX = -1;
  let maxY = -1;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (out[(y * w + x) * 4 + 3] < 12) continue;
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x);
      maxY = Math.max(maxY, y);
    }
  }
  if (maxX < 0) {
    console.log('EMPTY', f);
    continue;
  }

  const pad = Math.round(Math.max(maxX - minX, maxY - minY) * 0.12);
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(w - 1, maxX + pad);
  maxY = Math.min(h - 1, maxY + pad);
  const cw = maxX - minX + 1;
  const ch = maxY - minY + 1;
  const side = Math.max(cw, ch);
  const square = Buffer.alloc(side * side * 4);
  const ox = Math.floor((side - cw) / 2);
  const oy = Math.floor((side - ch) / 2);
  for (let y = 0; y < ch; y++) {
    for (let x = 0; x < cw; x++) {
      const s = ((minY + y) * w + (minX + x)) * 4;
      const d = ((oy + y) * side + (ox + x)) * 4;
      square[d] = out[s];
      square[d + 1] = out[s + 1];
      square[d + 2] = out[s + 2];
      square[d + 3] = out[s + 3];
    }
  }

  await sharp(square, { raw: { width: side, height: side, channels: 4 } })
    .resize(256, 256, { kernel: sharp.kernel.lanczos3 })
    .png()
    .toFile(dest);

  const check = await sharp(dest).ensureAlpha().stats();
  console.log(path.basename(dest), 'ok channels', check.channels.map((c) => c.mean.toFixed(1)).join(','));
}
