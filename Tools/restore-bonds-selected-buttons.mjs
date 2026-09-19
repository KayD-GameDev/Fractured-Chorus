import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/Downloads/Bond UI Pack/Bonds_UI/Generated_Source";
const OUT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/Bonds/Pack";
const FILES = ["03_Menu_Selected.png", "07_Row_Episode_Selected.png"];

function dist(a, b) {
  const dr = a[0] - b[0];
  const dg = a[1] - b[1];
  const db = a[2] - b[2];
  return Math.sqrt(dr * dr + dg * dg + db * db);
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
}

function collectSeeds(data, width, height) {
  const seen = new Set();
  const seeds = [];
  const ring = 18;
  const push = (x, y) => {
    const i = (y * width + x) * 4;
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    const key = ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);
    if (seen.has(key)) return;
    seen.add(key);
    seeds.push([r, g, b]);
  };
  for (let x = 0; x < width; x += 12) {
    for (let y = 0; y < ring; y += 6) push(x, y);
    for (let y = height - ring; y < height; y += 6) push(x, y);
  }
  for (let y = 0; y < height; y += 12) {
    for (let x = 0; x < ring; x += 6) push(x, y);
    for (let x = width - ring; x < width; x += 6) push(x, y);
  }
  return seeds;
}

function isBackground(r, g, b, seeds) {
  const c = chroma(r, g, b);
  const y = luma(r, g, b);
  if (c > 22) return false;
  if (y >= 238) return false;
  let best = 255;
  for (let i = 0; i < seeds.length; i++) {
    const d = dist([r, g, b], seeds[i]);
    if (d < best) best = d;
  }
  return best <= 28;
}

function floodAlpha(data, width, height, seeds) {
  const n = width * height;
  const bg = new Uint8Array(n);
  for (let idx = 0; idx < n; idx++) {
    const i = idx * 4;
    if (isBackground(data[i], data[i + 1], data[i + 2], seeds)) bg[idx] = 1;
  }

  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let head = 0;
  let tail = 0;
  const tryPush = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const idx = y * width + x;
    if (seen[idx] || !bg[idx]) return;
    seen[idx] = 1;
    queue[tail++] = idx;
  };

  for (let x = 0; x < width; x++) {
    tryPush(x, 0);
    tryPush(x, height - 1);
  }
  for (let y = 0; y < height; y++) {
    tryPush(0, y);
    tryPush(width - 1, y);
  }

  while (head < tail) {
    const idx = queue[head++];
    const x = idx % width;
    const y = (idx / width) | 0;
    const i = idx * 4;
    data[i] = 0;
    data[i + 1] = 0;
    data[i + 2] = 0;
    data[i + 3] = 0;
    tryPush(x - 1, y);
    tryPush(x + 1, y);
    tryPush(x, y - 1);
    tryPush(x, y + 1);
  }
}

function largestOpaqueBox(data, width, height, pad) {
  const n = width * height;
  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let best = null;
  for (let start = 0; start < n; start++) {
    if (seen[start] || data[start * 4 + 3] < 40) continue;
    let head = 0;
    let tail = 0;
    queue[tail++] = start;
    seen[start] = 1;
    let minX = width;
    let minY = height;
    let maxX = -1;
    let maxY = -1;
    while (head < tail) {
      const idx = queue[head++];
      const x = idx % width;
      const y = (idx / width) | 0;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
      const tryPush = (nx, ny) => {
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;
        const nidx = ny * width + nx;
        if (seen[nidx] || data[nidx * 4 + 3] < 40) return;
        seen[nidx] = 1;
        queue[tail++] = nidx;
      };
      tryPush(x - 1, y);
      tryPush(x + 1, y);
      tryPush(x, y - 1);
      tryPush(x, y + 1);
    }
    if (!best || tail > best.size) best = { size: tail, minX, minY, maxX, maxY };
  }
  if (!best) return { left: 0, top: 0, width, height };
  const left = Math.max(0, best.minX - pad);
  const top = Math.max(0, best.minY - pad);
  const right = Math.min(width - 1, best.maxX + pad);
  const bottom = Math.min(height - 1, best.maxY + pad);
  return { left, top, width: right - left + 1, height: bottom - top + 1 };
}

const report = [];
for (const file of FILES) {
  const src = path.join(SRC, file);
  const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  floodAlpha(pixels, info.width, info.height, collectSeeds(pixels, info.width, info.height));
  const box = largestOpaqueBox(pixels, info.width, info.height, 8);
  const cropped = await sharp(pixels, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .extract(box)
    .png()
    .toBuffer();
  const outPath = path.join(OUT, file);
  const tmp = `${outPath}.tmp.png`;
  fs.writeFileSync(tmp, cropped);
  fs.copyFileSync(tmp, outPath);
  fs.unlinkSync(tmp);
  report.push({ file, from: [info.width, info.height], to: [box.width, box.height] });
}

console.log(JSON.stringify(report, null, 2));
