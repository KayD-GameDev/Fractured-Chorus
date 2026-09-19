import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const PARTICLES = "d:/Fractured-Chorus1/Assets/FracturedChorus/VFX/ButterflyTransition/Particles";
const SOURCE = "d:/Fractured-Chorus1/Assets/FracturedChorus/VFX/ButterflyTransition/Sprites/_source";

const MAP = [
  ["fc_bt_src_glitter.png", "fc_bt_trail_glitter.png", 12],
  ["fc_bt_src_star_dust.png", "fc_bt_trail_star_dust.png", 12],
  ["fc_bt_src_triangle_lg.png", "fc_bt_trail_triangle_lg.png", 18],
  ["fc_bt_src_triangle_sm.png", "fc_bt_trail_triangle_sm.png", 14],
  ["fc_bt_src_waveform.png", "fc_bt_trail_waveform.png", 8],
  ["fc_bt_src_note.png", "fc_bt_trail_note.png", 16],
  ["fc_bt_src_diamond.png", "fc_bt_trail_diamond.png", 16],
  ["fc_bt_src_shard.png", "fc_bt_trail_shard.png", 16],
];

function sampleKey(data, width, height) {
  const pts = [
    [2, 2],
    [width - 3, 2],
    [2, height - 3],
    [width - 3, height - 3],
    [width >> 1, 2],
    [2, height >> 1],
    [width - 3, height >> 1],
    [width >> 1, height - 3],
  ];
  let r = 0;
  let g = 0;
  let b = 0;
  for (const [x, y] of pts) {
    const o = (y * width + x) * 4;
    r += data[o];
    g += data[o + 1];
    b += data[o + 2];
  }
  const n = pts.length;
  return [r / n, g / n, b / n];
}

function isCyanOrWhite(r, g, b) {
  const luma = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  if (luma > 210 && Math.max(r, g, b) - Math.min(r, g, b) < 55) return true;
  if (g > 95 && b > 120 && g + b > r * 1.45) return true;
  if (b > 140 && g > 70 && r < 200 && b >= r * 0.85) return true;
  return false;
}

function keyVfx(data, width, height) {
  const [kr, kg, kb] = sampleKey(data, width, height);
  const out = Buffer.alloc(data.length);
  for (let i = 0; i < width * height; i++) {
    const o = i * 4;
    let r = data[o];
    let g = data[o + 1];
    let b = data[o + 2];
    const dist = Math.hypot(r - kr, g - kg, b - kb);
    const magBias = (r + b) * 0.5 - g;
    const cyanBias = g + b - r;
    let a;
    if (isCyanOrWhite(r, g, b) && dist > 24) {
      a = 255;
    } else {
      const t0 = 26;
      const t1 = 92;
      if (dist <= t0) a = 0;
      else if (dist >= t1) a = 255;
      else a = Math.round(((dist - t0) / (t1 - t0)) * 255);
    }
    if (magBias > 36 && cyanBias < 90) {
      a = Math.round(a * Math.max(0, 1 - (magBias - 36) / 80));
    }
    if (a <= 2) {
      r = 0;
      g = 0;
      b = 0;
      a = 0;
    } else if (magBias > 8) {
      const k = Math.min(1, magBias / 90);
      r = Math.round(r * (1 - k) + 140 * k * 0.25);
      g = Math.min(255, Math.round(g + magBias * 0.55));
      b = Math.min(255, Math.round(b + magBias * 0.12));
    }
    out[o] = r;
    out[o + 1] = g;
    out[o + 2] = b;
    out[o + 3] = a;
  }
  return out;
}

function cropAlpha(data, width, height, pad) {
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (data[(y * width + x) * 4 + 3] > 12) {
        if (x < minX) minX = x;
        if (y < minY) minY = y;
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
      }
    }
  }
  if (maxX < minX) return { data, width, height };
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(width - 1, maxX + pad);
  maxY = Math.min(height - 1, maxY + pad);
  const w = maxX - minX + 1;
  const h = maxY - minY + 1;
  const out = Buffer.alloc(w * h * 4);
  for (let y = 0; y < h; y++) {
    data.copy(out, y * w * 4, ((minY + y) * width + minX) * 4, ((minY + y) * width + minX + w) * 4);
  }
  return { data: out, width: w, height: h };
}

async function savePng(buf, width, height, filePath) {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  await sharp(buf, { raw: { width, height, channels: 4 } }).png().toFile(filePath);
  console.log("wrote", path.basename(filePath), width, height);
}

function makeCanvas(width, height) {
  return { data: Buffer.alloc(width * height * 4), width, height };
}

function setPixel(img, x, y, r, g, b, a) {
  if (x < 0 || y < 0 || x >= img.width || y >= img.height || a <= 0) return;
  const o = (y * img.width + x) * 4;
  const da = img.data[o + 3] / 255;
  const sa = a / 255;
  const outA = sa + da * (1 - sa);
  if (outA <= 0) return;
  img.data[o] = Math.round((r * sa + img.data[o] * da * (1 - sa)) / outA);
  img.data[o + 1] = Math.round((g * sa + img.data[o + 1] * da * (1 - sa)) / outA);
  img.data[o + 2] = Math.round((b * sa + img.data[o + 2] * da * (1 - sa)) / outA);
  img.data[o + 3] = Math.round(outA * 255);
}

function glowBloom() {
  const w = 512;
  const h = 256;
  const img = makeCanvas(w, h);
  const cx = w * 0.5;
  const cy = h * 0.5;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const dx = (x - cx) / (w * 0.46);
      const dy = (y - cy) / (h * 0.34);
      const d = Math.hypot(dx, dy);
      if (d >= 1) continue;
      const t = 1 - d;
      const a = Math.pow(t, 2.15) * 175;
      setPixel(img, x, y, 234 * t + 140 * (1 - t) * 0.35, 251 * t + 210 * (1 - t) * 0.45, 255, a);
    }
  }
  return img;
}

function glowRim(size) {
  const img = makeCanvas(size, size);
  const cx = size * 0.5;
  const cy = size * 0.5;
  const radius = size * 0.46;
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const d = Math.hypot(x - cx, y - cy) / radius;
      if (d >= 1) continue;
      const a = Math.pow(1 - d, 2.4) * 165;
      if (a <= 1) continue;
      setPixel(img, x, y, 255, 255, 255, a);
    }
  }
  return img;
}

async function processOne(srcName, dstName, pad) {
  const src = path.join(SRC, srcName);
  fs.copyFileSync(src, path.join(SOURCE, srcName));
  const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const keyed = keyVfx(data, info.width, info.height);
  const cropped = cropAlpha(keyed, info.width, info.height, pad);
  await savePng(cropped.data, cropped.width, cropped.height, path.join(PARTICLES, dstName));
  return cropped;
}

async function makeFreqFromWave() {
  const src = path.join(PARTICLES, "fc_bt_trail_waveform.png");
  const { data, info } = await sharp(src)
    .resize(256, 64, { fit: "fill" })
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  await savePng(data, info.width, info.height, path.join(PARTICLES, "fc_bt_trail_freq.png"));
}

fs.mkdirSync(SOURCE, { recursive: true });
fs.mkdirSync(PARTICLES, { recursive: true });

for (const [src, dst, pad] of MAP) {
  await processOne(src, dst, pad);
}
const bloom = glowBloom();
await savePng(bloom.data, bloom.width, bloom.height, path.join(PARTICLES, "fc_bt_glow_bloom.png"));
const rim = glowRim(256);
await savePng(rim.data, rim.width, rim.height, path.join(PARTICLES, "fc_bt_glow_rim.png"));
await makeFreqFromWave();
console.log("trail particles processed");
