import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const HUD = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/Decor/Hud";
const RING = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1/ui_hud_ring_v2_full.png";
const STYLE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/_ref/_ref_ringstyle_barwave_v1.png";
const FRAME_COUNT = 24;

function punch(px, o) {
  px[o] = 0;
  px[o + 1] = 0;
  px[o + 2] = 0;
  px[o + 3] = 0;
}

function lumaOf(r, g, b) {
  return Math.max(r, g, b);
}

async function load(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data: Buffer.from(data), w: info.width, h: info.height };
}

function extract(px, w, h, left, top, width, height) {
  left = Math.max(0, Math.round(left));
  top = Math.max(0, Math.round(top));
  width = Math.min(w - left, Math.round(width));
  height = Math.min(h - top, Math.round(height));
  const out = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y++) {
    px.copy(out, y * width * 4, ((top + y) * w + left) * 4, ((top + y) * w + left + width) * 4);
  }
  return { buf: out, width, height };
}

function blobs(px, w, h, minArea) {
  const seen = Buffer.alloc(w * h);
  const out = [];
  const ink = (id) => px[id * 4 + 3] >= 14 || lumaOf(px[id * 4], px[id * 4 + 1], px[id * 4 + 2]) > 18;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const start = y * w + x;
      if (seen[start] || !ink(start)) continue;
      const q = [start];
      seen[start] = 1;
      let minX = x;
      let minY = y;
      let maxX = x;
      let maxY = y;
      let area = 0;
      while (q.length) {
        const id = q.pop();
        area++;
        const cx = id % w;
        const cy = (id / w) | 0;
        minX = Math.min(minX, cx);
        minY = Math.min(minY, cy);
        maxX = Math.max(maxX, cx);
        maxY = Math.max(maxY, cy);
        for (const n of [id - 1, id + 1, id - w, id + w]) {
          if (n < 0 || n >= w * h || seen[n] || !ink(n)) continue;
          const nx = n % w;
          const ny = (n / w) | 0;
          if (Math.abs(nx - cx) + Math.abs(ny - cy) !== 1) continue;
          seen[n] = 1;
          q.push(n);
        }
      }
      if (area >= minArea) out.push({ minX, minY, maxX, maxY, area });
    }
  }
  return out.sort((a, b) => b.area - a.area);
}

function buildRingLut(px) {
  const buckets = Array.from({ length: 32 }, () => []);
  for (let i = 0; i < px.length; i += 4) {
    const r = px[i];
    const g = px[i + 1];
    const b = px[i + 2];
    const a = px[i + 3];
    const lum = lumaOf(r, g, b);
    if (a < 20 || lum < 90) continue;
    if (r > g + 36 && r > b + 8) continue;
    buckets[Math.min(31, (lum * 32) >> 8)].push([r, g, b]);
  }
  const centers = buckets.map((list) => {
    if (!list.length) return null;
    let r = 0;
    let g = 0;
    let b = 0;
    for (const c of list) {
      r += c[0];
      g += c[1];
      b += c[2];
    }
    const n = list.length;
    return [Math.round(r / n), Math.round(g / n), Math.round(b / n)];
  });
  let last = [168, 176, 220];
  for (let i = 0; i < centers.length; i++) {
    if (centers[i]) last = centers[i];
    else centers[i] = last;
  }
  last = centers[centers.length - 1];
  for (let i = centers.length - 1; i >= 0; i--) {
    if (!centers[i]) centers[i] = last;
    else last = centers[i];
  }
  const lut = new Array(256);
  for (let i = 0; i < 256; i++) {
    const t = (i / 255) * (centers.length - 1);
    const i0 = Math.floor(t);
    const i1 = Math.min(centers.length - 1, i0 + 1);
    const f = t - i0;
    lut[i] = [
      Math.round(centers[i0][0] * (1 - f) + centers[i1][0] * f),
      Math.round(centers[i0][1] * (1 - f) + centers[i1][1] * f),
      Math.round(centers[i0][2] * (1 - f) + centers[i1][2] * f),
    ];
  }
  return lut;
}

function matteFromLuma(px) {
  for (let i = 0; i < px.length; i += 4) {
    const lum = lumaOf(px[i], px[i + 1], px[i + 2]);
    let a = Math.max(0, Math.min(255, (lum - 10) * 6));
    if (a < 28) {
      punch(px, i);
      continue;
    }
    if (a < 90) a = Math.round((a - 28) * (255 / 62));
    else a = 255;
    px[i + 3] = a;
  }
}

function hardenBars(px, w, h) {
  const out = Buffer.from(px);
  for (let x = 0; x < w; x++) {
    let bot = -1;
    let top = -1;
    for (let y = h - 1; y >= 0; y--) {
      if (px[(y * w + x) * 4 + 3] < 40) {
        if (bot >= 0) break;
        continue;
      }
      if (bot < 0) bot = y;
      top = y;
    }
    if (bot < 0) continue;
    for (let y = 0; y < h; y++) {
      const o = (y * w + x) * 4;
      if (y < top - 1 || y > bot + 1) {
        punch(out, o);
        continue;
      }
      if (y >= top && y <= bot && out[o + 3] < 180) out[o + 3] = 255;
    }
  }
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const o = (y * w + x) * 4;
      if (out[o + 3] < 20) {
        punch(out, o);
        continue;
      }
      let n = 0;
      if (x > 0 && out[(y * w + x - 1) * 4 + 3] >= 20) n++;
      if (x + 1 < w && out[(y * w + x + 1) * 4 + 3] >= 20) n++;
      if (y > 0 && out[((y - 1) * w + x) * 4 + 3] >= 20) n++;
      if (y + 1 < h && out[((y + 1) * w + x) * 4 + 3] >= 20) n++;
      if (n === 0) punch(out, o);
    }
  }
  return out;
}

function applyRingTone(px, lut) {
  for (let i = 0; i < px.length; i += 4) {
    const a = px[i + 3];
    const lum = lumaOf(px[i], px[i + 1], px[i + 2]);
    if (a < 36 && lum < 28) {
      punch(px, i);
      continue;
    }
    const key = Math.max(150, lum);
    const c = lut[key];
    const ice = lum > 190 ? (lum - 190) / 65 : 0;
    px[i] = Math.min(255, Math.round(c[0] * (1 - ice) + 214 * ice));
    px[i + 1] = Math.min(255, Math.round(c[1] * (1 - ice) + 236 * ice));
    px[i + 2] = Math.min(255, Math.round(c[2] * (1 - ice) + 255 * ice));
    px[i + 3] = 255;
  }
}

function paintBar(out, w, h, x0, x1, top, bot, lut) {
  const mid = lut[220][1] >= 160 ? lut[220] : [176, 186, 232];
  const ice = lut[250][2] >= 200 ? lut[250] : [208, 226, 255];
  for (let x = x0; x <= x1; x++) {
    for (let y = top; y <= bot; y++) {
      if (x < 0 || x >= w || y < 0 || y >= h) continue;
      const ty = (bot - y) / Math.max(1, bot - top);
      const c = [
        Math.round(mid[0] * (1 - ty) + ice[0] * ty),
        Math.round(mid[1] * (1 - ty) + ice[1] * ty),
        Math.round(mid[2] * (1 - ty) + ice[2] * ty),
      ];
      const o = (y * w + x) * 4;
      out[o] = c[0];
      out[o + 1] = c[1];
      out[o + 2] = c[2];
      out[o + 3] = 255;
    }
  }
}

function rebuildCleanBars(src, w, h, lut) {
  const cols = new Array(w);
  for (let x = 0; x < w; x++) {
    let top = -1;
    let bot = -1;
    for (let y = 0; y < h; y++) {
      if (src[(y * w + x) * 4 + 3] < 40) continue;
      if (top < 0) top = y;
      bot = y;
    }
    cols[x] = top < 0 ? null : { top, bot };
  }
  const out = Buffer.alloc(w * h * 4);
  let x = 0;
  while (x < w) {
    if (!cols[x]) {
      x++;
      continue;
    }
    const start = x;
    while (x < w && cols[x]) x++;
    let cursor = start;
    while (cursor < x) {
      const remain = x - cursor;
      const barW = remain <= 3 ? remain : remain <= 6 ? 2 : 3;
      let top = h;
      let bot = 0;
      for (let i = cursor; i < cursor + barW; i++) {
        top = Math.min(top, cols[i].top);
        bot = Math.max(bot, cols[i].bot);
      }
      const barH = Math.max(3, bot - top + 1);
      const destBot = h - 3;
      const destTop = Math.max(1, destBot - barH);
      paintBar(out, w, h, cursor, cursor + barW - 1, destTop, destBot, lut);
      cursor += barW + 1;
    }
  }
  return out;
}

function sampleBilinear(src, w, h, x, y) {
  const x0 = Math.max(0, Math.min(w - 1, Math.floor(x)));
  const y0 = Math.max(0, Math.min(h - 1, Math.floor(y)));
  const x1 = Math.min(w - 1, x0 + 1);
  const y1 = Math.min(h - 1, y0 + 1);
  const tx = x - x0;
  const ty = y - y0;
  const i00 = (y0 * w + x0) * 4;
  const i10 = (y0 * w + x1) * 4;
  const i01 = (y1 * w + x0) * 4;
  const i11 = (y1 * w + x1) * 4;
  const out = [0, 0, 0, 0];
  for (let c = 0; c < 4; c++) {
    const a = src[i00 + c] * (1 - tx) + src[i10 + c] * tx;
    const b = src[i01 + c] * (1 - tx) + src[i11 + c] * tx;
    out[c] = a * (1 - ty) + b * ty;
  }
  return out;
}

function waveformFrames(src, w, h, count) {
  const cols = new Array(w);
  for (let x = 0; x < w; x++) {
    let bot = -1;
    let barTop = -1;
    for (let y = h - 1; y >= 0; y--) {
      if (src[(y * w + x) * 4 + 3] < 16) {
        if (bot >= 0) break;
        continue;
      }
      if (bot < 0) bot = y;
      barTop = y;
    }
    cols[x] = bot < 0 ? null : { top: barTop, bot };
  }
  const frames = [];
  for (let f = 0; f < count; f++) {
    const out = Buffer.alloc(w * h * 4);
    const phase = (f / count) * Math.PI * 2;
    for (let x = 0; x < w; x++) {
      const col = cols[x];
      if (!col) continue;
      const baseH = col.bot - col.top + 1;
      const nx = x / Math.max(1, w - 1);
      const wave =
        1 +
        0.07 * Math.sin(nx * 8.4 + phase) +
        0.04 * Math.sin(nx * 19.2 - phase * 1.15) +
        0.018 * Math.sin(nx * 5.1 + phase * 0.55);
      const scale = Math.max(0.92, Math.min(1.07, wave));
      const keep = Math.min(6, baseH);
      const barH = Math.max(keep + 1, baseH * scale);
      const destTop = col.bot - barH + 1;
      for (let y = Math.max(0, Math.floor(destTop)); y <= col.bot; y++) {
        const fromBot = col.bot - y;
        let srcY;
        if (fromBot < keep) srcY = col.bot - fromBot;
        else {
          const t = (fromBot - keep) / Math.max(1, barH - keep);
          srcY = col.bot - keep - t * (baseH - keep);
        }
        const samp = sampleBilinear(src, w, h, x, srcY);
        const o = (y * w + x) * 4;
        out[o] = samp[0];
        out[o + 1] = samp[1];
        out[o + 2] = samp[2];
        out[o + 3] = samp[3];
      }
    }
    frames.push(hardenBars(out, w, h));
  }
  return frames;
}

async function saveRaw(buf, width, height, dest) {
  await sharp(buf, { raw: { width, height, channels: 4 } }).png().toFile(dest);
}

const ring = await load(RING);
const lut = buildRingLut(ring.data);
const style = await load(STYLE);
matteFromLuma(style.data);
const styleBlobs = blobs(style.data, style.w, style.h, 400);
console.log(
  "blobs",
  styleBlobs.slice(0, 6).map((b) => `${b.minX},${b.minY}-${b.maxX},${b.maxY} a${b.area}`),
);
if (styleBlobs.length < 2) throw new Error("expected wave + bar in ringstyle ref");
const waveBox =
  styleBlobs[0].maxY - styleBlobs[0].minY > styleBlobs[1].maxY - styleBlobs[1].minY
    ? styleBlobs[0]
    : styleBlobs[1];
const pad = 6;
let wave = extract(
  style.data,
  style.w,
  style.h,
  waveBox.minX - pad,
  waveBox.minY - pad,
  waveBox.maxX - waveBox.minX + 1 + pad * 2,
  waveBox.maxY - waveBox.minY + 1 + pad * 2,
);

matteFromLuma(wave.buf);

const targetW = 640;
const targetH = 130;
const scaled = await sharp(wave.buf, {
  raw: { width: wave.width, height: wave.height, channels: 4 },
})
  .resize(targetW, targetH, { fit: "fill", kernel: sharp.kernel.nearest })
  .ensureAlpha()
  .raw()
  .toBuffer({ resolveWithObject: true });
wave = { buf: Buffer.from(scaled.data), width: scaled.info.width, height: scaled.info.height };
wave.buf = rebuildCleanBars(wave.buf, wave.width, wave.height, lut);

await saveRaw(wave.buf, wave.width, wave.height, path.join(HUD, "ui_stat_hud_wave_base_v1.png"));
const frames = waveformFrames(wave.buf, wave.width, wave.height, FRAME_COUNT);
for (let i = 0; i < frames.length; i++) {
  frames[i] = rebuildCleanBars(frames[i], wave.width, wave.height, lut);
  const dest = path.join(HUD, `ui_stat_hud_wave_${String(i).padStart(2, "0")}.png`);
  if (!fs.existsSync(dest + ".meta")) throw new Error("missing meta " + dest);
  await saveRaw(frames[i], wave.width, wave.height, dest);
}

const swatch = lut.filter((_, i) => i % 51 === 0);
console.log(JSON.stringify({ size: [wave.width, wave.height], swatch }));
