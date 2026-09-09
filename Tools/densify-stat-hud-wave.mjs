import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const ROOT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu";
const HUD = path.join(ROOT, "Decor/Hud");
const HUD_SRC = path.join(ROOT, "_ref/_ref_ringhud_waveform_src.png");
const FRAME_COUNT = 24;

function toAlpha(px) {
  for (let i = 0; i < px.length; i += 4) {
    const m = Math.max(px[i], px[i + 1], px[i + 2]);
    const a = Math.max(0, Math.min(255, (m - 12) * 5));
    px[i + 3] = a;
    if (a < 8) {
      px[i] = 0;
      px[i + 1] = 0;
      px[i + 2] = 0;
      px[i + 3] = 0;
    }
  }
}

function extract(px, w, h, left, top, width, height) {
  left = Math.max(0, left);
  top = Math.max(0, top);
  width = Math.min(w - left, width);
  height = Math.min(h - top, height);
  const out = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y++) {
    px.copy(out, y * width * 4, ((top + y) * w + left) * 4, ((top + y) * w + left + width) * 4);
  }
  return { buf: out, width, height };
}

function inkBounds(px, w, h, pad = 6) {
  let minX = w;
  let minY = h;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[(y * w + x) * 4 + 3] < 12) continue;
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x);
      maxY = Math.max(maxY, y);
    }
  }
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(w - 1, maxX + pad);
  maxY = Math.min(h - 1, maxY + pad);
  return { minX, minY, maxX, maxY };
}

function cropInk(px, w, h, pad = 6) {
  const b = inkBounds(px, w, h, pad);
  return extract(px, w, h, b.minX, b.minY, b.maxX - b.minX + 1, b.maxY - b.minY + 1);
}

function blobs(px, w, h, minArea) {
  const seen = Buffer.alloc(w * h);
  const out = [];
  const ink = (id) => px[id * 4 + 3] >= 14;
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
        const nbs = [id - 1, id + 1, id - w, id + w];
        for (const n of nbs) {
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

function columnFill(px, w, h) {
  let cols = 0;
  for (let x = 0; x < w; x++) {
    for (let y = 0; y < h; y++) {
      if (px[(y * w + x) * 4 + 3] >= 16) {
        cols++;
        break;
      }
    }
  }
  return cols / w;
}

function dilateX(src, w, h, radius = 1) {
  const out = Buffer.from(src);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const o = (y * w + x) * 4;
      if (src[o + 3] >= 16) continue;
      let best = -1;
      let srcO = -1;
      for (let d = 1; d <= radius; d++) {
        const l = x - d;
        const r = x + d;
        if (l >= 0 && src[(y * w + l) * 4 + 3] >= 16) {
          best = src[(y * w + l) * 4 + 3];
          srcO = (y * w + l) * 4;
          break;
        }
        if (r < w && src[(y * w + r) * 4 + 3] >= 16) {
          best = src[(y * w + r) * 4 + 3];
          srcO = (y * w + r) * 4;
          break;
        }
      }
      if (srcO >= 0 && best >= 16) src.copy(out, o, srcO, srcO + 4);
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
        0.08 * Math.sin(nx * 8.4 + phase) +
        0.045 * Math.sin(nx * 19.2 - phase * 1.15) +
        0.02 * Math.sin(nx * 5.1 + phase * 0.55);
      const scale = Math.max(0.9, Math.min(1.08, wave));
      const keep = Math.min(5, baseH);
      const barH = Math.max(keep + 1, baseH * scale);
      const destTop = col.bot - barH + 1;
      for (let y = Math.max(0, Math.floor(destTop)); y <= col.bot; y++) {
        const fromBot = col.bot - y;
        let srcY;
        if (fromBot < keep) {
          srcY = col.bot - fromBot;
        } else {
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
    frames.push(out);
  }
  return frames;
}

async function load(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const px = Buffer.from(data);
  toAlpha(px);
  return { px, w: info.width, h: info.height };
}

async function saveRaw(buf, width, height, dest) {
  await sharp(buf, { raw: { width, height, channels: 4 } }).png().toFile(dest);
}

const hud = await load(HUD_SRC);
const hudCrop = extract(hud.px, hud.w, hud.h, 575, 78, 430, 88);
let wave = cropInk(hudCrop.buf, hudCrop.width, hudCrop.height, 4);
wave.buf = dilateX(wave.buf, wave.width, wave.height, 1);
wave = cropInk(wave.buf, wave.width, wave.height, 3);

const targetW = 640;
const targetH = Math.max(120, Math.round(targetW * (wave.height / wave.width)));
const scaled = await sharp(wave.buf, {
  raw: { width: wave.width, height: wave.height, channels: 4 },
})
  .resize(targetW, targetH, { fit: "fill", kernel: sharp.kernel.lanczos3 })
  .ensureAlpha()
  .raw()
  .toBuffer({ resolveWithObject: true });
wave = { buf: Buffer.from(scaled.data), width: scaled.info.width, height: scaled.info.height };
wave.buf = dilateX(wave.buf, wave.width, wave.height, 1);
const fill = columnFill(wave.buf, wave.width, wave.height);

await saveRaw(wave.buf, wave.width, wave.height, path.join(HUD, "ui_stat_hud_wave_base_v1.png"));
const frames = waveformFrames(wave.buf, wave.width, wave.height, FRAME_COUNT);
for (let i = 0; i < frames.length; i++) {
  const dest = path.join(HUD, `ui_stat_hud_wave_${String(i).padStart(2, "0")}.png`);
  if (!fs.existsSync(dest + ".meta")) {
    throw new Error("missing meta " + dest);
  }
  await saveRaw(frames[i], wave.width, wave.height, dest);
}

console.log(JSON.stringify({ fill, size: [wave.width, wave.height], source: "hud-src-unthinned" }));
