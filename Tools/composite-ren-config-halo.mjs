import path from "node:path";
import sharp from "sharp";

const ROOT = "D:/Fractured-Chorus1";
const REN = path.join(ROOT, "Assets/FracturedChorus/Art/Characters/Ren/School/ren_config_pose_v1.png");
const OUT = path.join(ROOT, "Assets/FracturedChorus/Art/Characters/Ren/School/ren_config_pose_fx_v1.png");
const HALO = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/fx_ren_config_halo_chroma_v1.png";

function chromaScore(r, g, b) {
  return Math.hypot(r - 255, g - 0, b - 255);
}

function floodChroma(px, w, h, threshold = 108) {
  const visited = Buffer.alloc(w * h);
  const q = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= w || y >= h) return;
    const id = y * w + x;
    if (visited[id]) return;
    visited[id] = 1;
    q.push(id);
  };
  for (let x = 0; x < w; x++) {
    push(x, 0);
    push(x, h - 1);
  }
  for (let y = 0; y < h; y++) {
    push(0, y);
    push(w - 1, y);
  }
  while (q.length) {
    const id = q.pop();
    const i = id * 4;
    if (chromaScore(px[i], px[i + 1], px[i + 2]) >= threshold) continue;
    px[i + 3] = 0;
    const x = id % w;
    const y = (id / w) | 0;
    push(x - 1, y);
    push(x + 1, y);
    push(x, y - 1);
    push(x, y + 1);
  }
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    if (px[o + 3] > 0 && chromaScore(r, g, b) < 140) px[o + 3] = 0;
    if (px[o + 3] > 0 && r > 170 && b > 170 && g < 110 && r - g > 55) px[o + 3] = 0;
    if (px[o + 3] < 8) {
      px[o] = 0;
      px[o + 1] = 0;
      px[o + 2] = 0;
      px[o + 3] = 0;
    }
  }
}

async function readRgba(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data, w: info.width, h: info.height };
}

function transCount(data, w, h, x, y) {
  let n = 0;
  for (let dy = -1; dy <= 1; dy++) {
    for (let dx = -1; dx <= 1; dx++) {
      if (!dx && !dy) continue;
      const nx = x + dx;
      const ny = y + dy;
      if (nx < 0 || ny < 0 || nx >= w || ny >= h) {
        n++;
        continue;
      }
      if (data[(ny * w + nx) * 4 + 3] < 12) n++;
    }
  }
  return n;
}

function nearDarkHair(data, w, h, x, y) {
  for (let dy = -2; dy <= 2; dy++) {
    for (let dx = -2; dx <= 2; dx++) {
      const nx = x + dx;
      const ny = y + dy;
      if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
      const o = (ny * w + nx) * 4;
      if (data[o + 3] < 80) continue;
      const avg = (data[o] + data[o + 1] + data[o + 2]) / 3;
      const sat = Math.max(data[o], data[o + 1], data[o + 2]) - Math.min(data[o], data[o + 1], data[o + 2]);
      if (avg < 70 && sat < 40) return true;
    }
  }
  return false;
}

function inFace(x, y) {
  return x >= 648 && x <= 778 && y >= 148 && y <= 258;
}

function hologramRgb(r, g, b) {
  return [
    Math.round(r * 0.22 + 255 * 0.78),
    Math.round(g * 0.22 + 110 * 0.78),
    Math.round(b * 0.22 + 210 * 0.78),
  ];
}

function inHairHoloZone(x, y) {
  return y < 228 && x >= 630 && x <= 860;
}

function processRen(src, w, h) {
  const out = Buffer.from(src);
  let holo = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const i = y * w + x;
      const o = i * 4;
      if (src[o + 3] < 10) continue;
      if (!inHairHoloZone(x, y)) continue;
      if (inFace(x, y)) continue;
      const r = src[o];
      const g = src[o + 1];
      const b = src[o + 2];
      const avg = (r + g + b) / 3;
      const sat = Math.max(r, g, b) - Math.min(r, g, b);
      if (sat > 32 || avg < 118 || avg > 210) continue;
      if (!nearDarkHair(src, w, h, x, y)) continue;
      const [nr, ng, nb] = hologramRgb(r, g, b);
      out[o] = nr;
      out[o + 1] = ng;
      out[o + 2] = nb;
      holo++;
    }
  }

  for (let y = 236; y <= 400; y++) {
    let edge = -1;
    for (let x = 740; x <= 920; x++) {
      if (src[(y * w + x) * 4 + 3] >= 80) edge = x;
    }
    if (edge < 0) continue;
    const shred = ((y * 13 + 5) % 7) < 3;
    if (!shred) continue;
    const len = 22 + ((y * 9) % 36);
    const [hr, hg, hb] = hologramRgb(220, 140, 200);
    for (let x = edge + 1; x < Math.min(w, edge + 1 + len); x++) {
      const o = (y * w + x) * 4;
      if (src[o + 3] >= 20) continue;
      const t = 1 - (x - edge) / len;
      out[o] = hr;
      out[o + 1] = (y % 5 === 0) ? 240 : hg;
      out[o + 2] = 255;
      out[o + 3] = Math.max(out[o + 3], Math.round(170 * t));
    }
  }

  return { out, holo };
}

async function glowLayer(renBuf, w, h, sigma, rgb, mul) {
  const alpha = await sharp(renBuf, { raw: { width: w, height: h, channels: 4 } })
    .extractChannel("alpha")
    .blur(sigma)
    .raw()
    .toBuffer();
  const out = Buffer.alloc(w * h * 4);
  for (let i = 0; i < w * h; i++) {
    const a = Math.min(255, Math.round(alpha[i] * mul));
    if (a < 2) continue;
    out[i * 4] = rgb[0];
    out[i * 4 + 1] = rgb[1];
    out[i * 4 + 2] = rgb[2];
    out[i * 4 + 3] = a;
  }
  return sharp(out, { raw: { width: w, height: h, channels: 4 } }).png().toBuffer();
}

async function rimLayer(renBuf, w, h) {
  const src = await sharp(renBuf, { raw: { width: w, height: h, channels: 4 } })
    .ensureAlpha()
    .raw()
    .toBuffer();
  const out = Buffer.alloc(w * h * 4);
  for (let y = 1; y < h - 1; y++) {
    for (let x = 1; x < w - 1; x++) {
      const i = y * w + x;
      if (src[i * 4 + 3] < 40) continue;
      if (transCount(src, w, h, x, y) < 1) continue;
      const o = i * 4;
      out[o] = 255;
      out[o + 1] = 150;
      out[o + 2] = 230;
      out[o + 3] = 180;
    }
  }
  return sharp(out, { raw: { width: w, height: h, channels: 4 } }).blur(1.2).png().toBuffer();
}

const { data: src, w, h } = await readRgba(REN);
const { out: processed, holo } = processRen(src, w, h);
const processedPng = await sharp(processed, { raw: { width: w, height: h, channels: 4 } }).png().toBuffer();

const haloSrc = await readRgba(HALO);
floodChroma(haloSrc.data, haloSrc.w, haloSrc.h, 108);
const haloPng = await sharp(Buffer.from(haloSrc.data), {
  raw: { width: haloSrc.w, height: haloSrc.h, channels: 4 },
})
  .resize(w, h, { fit: "cover" })
  .ensureAlpha()
  .raw()
  .toBuffer();

function occupancy(data, w, h, thresh) {
  const m = Buffer.alloc(w * h);
  for (let i = 0; i < w * h; i++) {
    if (data[i * 4 + 3] >= thresh) m[i] = 1;
  }
  return m;
}

function dilateMask(mask, w, h, radius) {
  const tmp = Buffer.alloc(w * h);
  for (let y = 0; y < h; y++) {
    const row = y * w;
    for (let x = 0; x < w; x++) {
      let hit = 0;
      const x0 = Math.max(0, x - radius);
      const x1 = Math.min(w - 1, x + radius);
      for (let xx = x0; xx <= x1; xx++) {
        if (mask[row + xx]) {
          hit = 1;
          break;
        }
      }
      tmp[row + x] = hit;
    }
  }
  const out = Buffer.alloc(w * h);
  for (let y = 0; y < h; y++) {
    const y0 = Math.max(0, y - radius);
    const y1 = Math.min(h - 1, y + radius);
    for (let x = 0; x < w; x++) {
      let hit = 0;
      for (let yy = y0; yy <= y1; yy++) {
        if (tmp[yy * w + x]) {
          hit = 1;
          break;
        }
      }
      out[y * w + x] = hit;
    }
  }
  return out;
}

const charDilated = dilateMask(occupancy(src, w, h, 8), w, h, 18);
const haloMasked = Buffer.from(haloPng);
for (let i = 0; i < w * h; i++) {
  if (charDilated[i]) haloMasked[i * 4 + 3] = 0;
}
const haloMaskedPng = await sharp(haloMasked, { raw: { width: w, height: h, channels: 4 } }).png().toBuffer();

const [glowWide, glowMid, rim] = await Promise.all([
  glowLayer(processed, w, h, 36, [255, 120, 210], 0.62),
  glowLayer(processed, w, h, 12, [180, 230, 255], 0.62),
  rimLayer(processed, w, h),
]);

const composed = await sharp({
  create: { width: w, height: h, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } },
})
  .composite([
    { input: glowWide, blend: "screen" },
    { input: haloMaskedPng, blend: "screen" },
    { input: glowMid, blend: "screen" },
    { input: rim, blend: "screen" },
    { input: processedPng, blend: "over" },
  ])
  .ensureAlpha()
  .raw()
  .toBuffer();

for (let i = 0; i < w * h; i++) {
  const o = i * 4;
  if (processed[o + 3] < 12) continue;
  composed[o] = processed[o];
  composed[o + 1] = processed[o + 1];
  composed[o + 2] = processed[o + 2];
  composed[o + 3] = processed[o + 3];
}

await sharp(composed, { raw: { width: w, height: h, channels: 4 } })
  .png({ compressionLevel: 9 })
  .toFile(OUT);

console.log(JSON.stringify({ out: path.basename(OUT), holo, w, h }));
