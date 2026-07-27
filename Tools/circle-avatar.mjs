import sharp from 'sharp';
import path from 'node:path';
import fs from 'node:fs/promises';

const DIAMETER = 256;
const BG_TOLERANCE = 34;

async function loadRaw(file) {
  const img = sharp(file).ensureAlpha();
  const { data, info } = await img.raw().toBuffer({ resolveWithObject: true });
  return { data, w: info.width, h: info.height, channels: info.channels };
}

function bgColor({ data, w, h, channels }) {
  const samples = [
    [2, 2], [w - 3, 2], [2, h - 3], [w - 3, h - 3],
    [Math.floor(w / 2), 2], [2, Math.floor(h / 2)],
  ];
  let r = 0, g = 0, b = 0;
  for (const [x, y] of samples) {
    const i = (y * w + x) * channels;
    r += data[i]; g += data[i + 1]; b += data[i + 2];
  }
  const n = samples.length;
  return [Math.round(r / n), Math.round(g / n), Math.round(b / n)];
}

function subjectMask(raw, bg) {
  const { data, w, h, channels } = raw;
  const mask = new Uint8Array(w * h);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const i = (y * w + x) * channels;
      const d = Math.abs(data[i] - bg[0]) + Math.abs(data[i + 1] - bg[1]) + Math.abs(data[i + 2] - bg[2]);
      mask[y * w + x] = d > BG_TOLERANCE ? 1 : 0;
    }
  }
  return mask;
}

function bbox(mask, w, h) {
  let minX = w, minY = h, maxX = -1, maxY = -1;
  const rowCount = new Int32Array(h);
  const colCount = new Int32Array(w);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (!mask[y * w + x]) continue;
      rowCount[y]++; colCount[x]++;
      if (x < minX) minX = x;
      if (x > maxX) maxX = x;
      if (y < minY) minY = y;
      if (y > maxY) maxY = y;
    }
  }
  const minRow = Math.max(2, Math.round(w * 0.004));
  let ty = minY, by = maxY, lx = minX, rx = maxX;
  while (ty < by && rowCount[ty] < minRow) ty++;
  while (by > ty && rowCount[by] < minRow) by--;
  while (lx < rx && colCount[lx] < minRow) lx++;
  while (rx > lx && colCount[rx] < minRow) rx--;
  return { x: lx, y: ty, w: rx - lx + 1, h: by - ty + 1 };
}

function headCenterX(mask, w, box) {
  const band = Math.max(1, Math.round(box.h * 0.22));
  let sum = 0, count = 0;
  for (let y = box.y; y < box.y + band; y++) {
    for (let x = box.x; x < box.x + box.w; x++) {
      if (!mask[y * w + x]) continue;
      sum += x; count++;
    }
  }
  return count > 0 ? sum / count : box.x + box.w / 2;
}

function clampSquare(cx, cy, side, w, h) {
  const s = Math.min(side, w, h);
  let x = Math.round(cx - s / 2);
  let y = Math.round(cy - s / 2);
  x = Math.max(0, Math.min(w - Math.round(s), x));
  y = Math.max(0, Math.min(h - Math.round(s), y));
  return { left: x, top: y, width: Math.round(s), height: Math.round(s) };
}

async function circleMask(diameter, insetPx) {
  const r = diameter / 2 - insetPx;
  const svg = `<svg width="${diameter}" height="${diameter}"><circle cx="${diameter / 2}" cy="${diameter / 2}" r="${r}" fill="#fff"/></svg>`;
  return sharp(Buffer.from(svg)).png().toBuffer();
}

async function renderCircle(file, crop, outPath) {
  const mask = await circleMask(DIAMETER, 1);
  const body = await sharp(file)
    .extract(crop)
    .resize(DIAMETER, DIAMETER, { fit: 'fill', kernel: 'lanczos3' })
    .ensureAlpha()
    .png()
    .toBuffer();
  await sharp(body)
    .composite([{ input: mask, blend: 'dest-in' }])
    .png({ compressionLevel: 9 })
    .toFile(outPath);
}

async function makeAvatars(file, outDir, name) {
  const raw = await loadRaw(file);
  const bg = bgColor(raw);
  const mask = subjectMask(raw, bg);
  const box = bbox(mask, raw.w, raw.h);

  const fullSide = Math.round(Math.max(box.w, box.h) * 1.14);
  const fullCrop = clampSquare(box.x + box.w / 2, box.y + box.h / 2, fullSide, raw.w, raw.h);

  const headX = headCenterX(mask, raw.w, box);
  const bustSide = Math.round(box.h * 0.54);
  const bustCrop = clampSquare(headX, box.y + bustSide * 0.44, bustSide, raw.w, raw.h);

  const fullOut = path.join(outDir, `${name}_chibi_avatar_full_v1.png`);
  const bustOut = path.join(outDir, `${name}_chibi_avatar_bust_v1.png`);
  await renderCircle(file, fullCrop, fullOut);
  await renderCircle(file, bustCrop, bustOut);

  return { name, bg, box, fullCrop, bustCrop, fullOut, bustOut };
}

const [outDir, ...pairs] = process.argv.slice(2);
await fs.mkdir(outDir, { recursive: true });
const report = [];
for (const pair of pairs) {
  const [name, file] = pair.split('=');
  report.push(await makeAvatars(file, outDir, name));
}
console.log(JSON.stringify(report, null, 2));
