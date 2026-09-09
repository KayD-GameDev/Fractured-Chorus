import fs from "node:fs";
import sharp from "sharp";

const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";
const QA = "D:/Fractured-Chorus1/Tools/_qa_config_clean.png";

const FILL_RGB = [168, 92, 236];
const FILL_GLOW = [196, 110, 255];
const TRACK_RGB = [72, 28, 128];
const TRACK_GLOW = [120, 56, 196];
const RING_RGB = [248, 244, 255];
const RING_GLOW = [176, 84, 255];

function punch(px, o) {
  px[o] = px[o + 1] = px[o + 2] = px[o + 3] = 0;
}

function luma(r, g, b) {
  return (r + g + b) / 3;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function bias(r, g, b) {
  return (r + b) * 0.5 - g;
}

function sdRoundBox(px, py, hw, hh, rad) {
  const ax = Math.abs(px) - hw + rad;
  const ay = Math.abs(py) - hh + rad;
  const ox = Math.max(ax, 0);
  const oy = Math.max(ay, 0);
  return Math.hypot(ox, oy) + Math.min(Math.max(ax, ay), 0) - rad;
}

function glowAlpha(sd, glow) {
  if (sd <= 0) return 255;
  if (sd >= glow) return 0;
  const t = sd / glow;
  return Math.round(255 * Math.exp(-t * t * 3.2));
}

function mixRgb(core, glow, sd) {
  if (sd <= 0) return core;
  const t = Math.min(1, Math.max(0, sd / 6));
  return [
    Math.round(core[0] + (glow[0] - core[0]) * t),
    Math.round(core[1] + (glow[1] - core[1]) * t),
    Math.round(core[2] + (glow[2] - core[2]) * t),
  ];
}

function paintCapsule(w, h, hh, glow, coreRgb, glowRgb) {
  const out = Buffer.alloc(w * h * 4);
  const cx = (w - 1) * 0.5;
  const cy = (h - 1) * 0.5;
  const hw = w * 0.5 - glow - 2;
  const rad = hh;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const sd = sdRoundBox(x - cx, y - cy, hw, hh, rad);
      const a = glowAlpha(sd, glow);
      const o = (y * w + x) * 4;
      if (a < 8) continue;
      const rgb = mixRgb(coreRgb, glowRgb, sd);
      out[o] = rgb[0];
      out[o + 1] = rgb[1];
      out[o + 2] = rgb[2];
      out[o + 3] = a;
    }
  }
  return { data: out, w, h };
}

function paintRing(size, radius, thick, glow) {
  const out = Buffer.alloc(size * size * 4);
  const c = (size - 1) * 0.5;
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const dist = Math.hypot(x - c, y - c);
      const sd = Math.abs(dist - radius) - thick;
      const a = glowAlpha(sd, glow);
      const o = (y * size + x) * 4;
      if (a < 8) continue;
      const rgb = mixRgb(RING_RGB, RING_GLOW, sd);
      out[o] = rgb[0];
      out[o + 1] = rgb[1];
      out[o + 2] = rgb[2];
      out[o + 3] = a;
    }
  }
  return { data: out, w: size, h: size };
}

function paintComposite(w, h) {
  const track = paintCapsule(w, h, 3.2, 6, TRACK_RGB, TRACK_GLOW);
  const fillW = Math.round(w * 0.64);
  const fill = paintCapsule(fillW, h, 6.2, 9, FILL_RGB, FILL_GLOW);
  const handle = paintRing(72, 18, 4.2, 11);
  const out = Buffer.from(track.data);
  const fx = 8;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < fillW; x++) {
      const s = (y * fillW + x) * 4;
      const d = (y * w + (x + fx)) * 4;
      const a = fill.data[s + 3] / 255;
      if (a <= 0) continue;
      out[d] = Math.round(fill.data[s] * a + out[d] * (1 - a));
      out[d + 1] = Math.round(fill.data[s + 1] * a + out[d + 1] * (1 - a));
      out[d + 2] = Math.round(fill.data[s + 2] * a + out[d + 2] * (1 - a));
      out[d + 3] = Math.min(255, Math.round(out[d + 3] + fill.data[s + 3] * (1 - out[d + 3] / 255)));
    }
  }
  const hx = Math.round(w * 0.64) - 4;
  const hy = Math.round((h - 72) / 2);
  for (let y = 0; y < 72; y++) {
    for (let x = 0; x < 72; x++) {
      const s = (y * 72 + x) * 4;
      const dx = hx + x;
      const dy = hy + y;
      if (dx < 0 || dy < 0 || dx >= w || dy >= h) continue;
      const d = (dy * w + dx) * 4;
      const a = handle.data[s + 3] / 255;
      if (a <= 0) continue;
      out[d] = Math.round(handle.data[s] * a + out[d] * (1 - a));
      out[d + 1] = Math.round(handle.data[s + 1] * a + out[d + 1] * (1 - a));
      out[d + 2] = Math.round(handle.data[s + 2] * a + out[d + 2] * (1 - a));
      out[d + 3] = Math.min(255, Math.round(out[d + 3] + handle.data[s + 3] * (1 - out[d + 3] / 255)));
    }
  }
  return { data: out, w, h };
}

async function writePng(img, file) {
  await sharp(img.data, { raw: { width: img.w, height: img.h, channels: 4 } }).png().toFile(file);
}

function coreMask(px, w, h) {
  const mask = new Uint8Array(w * h);
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    const a = px[o + 3];
    const L = luma(r, g, b);
    const C = chroma(r, g, b);
    const B = bias(r, g, b);
    const checker = C < 22 && L > 28 && L < 210 && B < 12;
    if (a < 24 || checker) continue;
    if (L > 92 || (B > 28 && L > 36 && C > 18)) mask[i] = 1;
  }
  return mask;
}

function distanceField(mask, w, h) {
  const dist = new Float32Array(w * h);
  dist.fill(1e6);
  const q = [];
  for (let i = 0; i < w * h; i++) {
    if (!mask[i]) continue;
    dist[i] = 0;
    q.push(i);
  }
  const diag = Math.SQRT2;
  while (q.length) {
    const i = q.shift();
    const x = i % w;
    const y = (i / w) | 0;
    for (let oy = -1; oy <= 1; oy++) {
      for (let ox = -1; ox <= 1; ox++) {
        if (!ox && !oy) continue;
        const nx = x + ox;
        const ny = y + oy;
        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
        const j = ny * w + nx;
        const nd = dist[i] + (ox && oy ? diag : 1);
        if (nd < dist[j]) {
          dist[j] = nd;
          q.push(j);
        }
      }
    }
  }
  return dist;
}

function sampleGlow(px, w, h, mask) {
  let r = 0;
  let g = 0;
  let b = 0;
  let n = 0;
  for (let i = 0; i < w * h; i++) {
    if (mask[i]) continue;
    const o = i * 4;
    if (px[o + 3] < 40) continue;
    const B = bias(px[o], px[o + 1], px[o + 2]);
    const L = luma(px[o], px[o + 1], px[o + 2]);
    if (B > 18 && L > 24 && L < 160) {
      r += px[o];
      g += px[o + 1];
      b += px[o + 2];
      n++;
    }
  }
  if (n < 8) return [176, 84, 255];
  return [Math.round(r / n), Math.round(g / n), Math.round(b / n)];
}

async function rebuildLineArt(file, glowR) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width;
  const h = info.height;
  const mask = coreMask(data, w, h);
  const dist = distanceField(mask, w, h);
  const glow = sampleGlow(data, w, h, mask);
  const out = Buffer.alloc(w * h * 4);
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const d = dist[i];
    if (mask[i]) {
      out[o] = data[o];
      out[o + 1] = data[o + 1];
      out[o + 2] = data[o + 2];
      out[o + 3] = 255;
      continue;
    }
    if (d >= glowR) continue;
    const t = d / glowR;
    const a = Math.round(210 * Math.exp(-t * t * 3.4));
    if (a < 10) continue;
    out[o] = glow[0];
    out[o + 1] = glow[1];
    out[o + 2] = glow[2];
    out[o + 3] = a;
  }
  await sharp(out, { raw: { width: w, height: h, channels: 4 } }).png().toFile(file);
}

function floodDark(px, w, h, lumaMax, chromaMax) {
  const n = w * h;
  const mask = new Uint8Array(n);
  const q = [];
  const trySeed = (x, y) => {
    const i = y * w + x;
    if (mask[i]) return;
    const o = i * 4;
    const L = luma(px[o], px[o + 1], px[o + 2]);
    const C = chroma(px[o], px[o + 1], px[o + 2]);
    const B = bias(px[o], px[o + 1], px[o + 2]);
    if (L > lumaMax || C > chromaMax || B > 18) return;
    mask[i] = 1;
    q.push(i);
  };
  for (let x = 0; x < w; x++) {
    trySeed(x, 0);
    trySeed(x, h - 1);
  }
  for (let y = 0; y < h; y++) {
    trySeed(0, y);
    trySeed(w - 1, y);
  }
  while (q.length) {
    const i = q.pop();
    const x = i % w;
    const y = (i / w) | 0;
    const nb = [x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1];
    for (const j of nb) {
      if (j < 0 || mask[j]) continue;
      const o = j * 4;
      const L = luma(px[o], px[o + 1], px[o + 2]);
      const C = chroma(px[o], px[o + 1], px[o + 2]);
      const B = bias(px[o], px[o + 1], px[o + 2]);
      if (L <= lumaMax && C <= chromaMax && B <= 18) {
        mask[j] = 1;
        q.push(j);
      }
    }
  }
  return mask;
}

async function mattePanel(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width;
  const h = info.height;
  const dead = floodDark(data, w, h, 22, 20);
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    if (dead[i]) {
      punch(data, o);
      continue;
    }
    const r = data[o];
    const g = data[o + 1];
    const b = data[o + 2];
    const L = luma(r, g, b);
    const B = bias(r, g, b);
    if (L < 14 && B < 10) {
      punch(data, o);
      continue;
    }
    if (L < 70 && B > 12) {
      const premul = Math.max(r, g, b, 1);
      const a = Math.min(255, Math.round(premul * 1.05));
      const s = 255 / premul;
      data[o] = Math.min(255, Math.round(r * s));
      data[o + 1] = Math.min(255, Math.round(g * s));
      data[o + 2] = Math.min(255, Math.round(b * s));
      data[o + 3] = a;
    } else {
      data[o + 3] = 255;
    }
  }
  await sharp(data, { raw: { width: w, height: h, channels: 4 } }).png().toFile(file);
}

await writePng(paintCapsule(1024, 44, 6.4, 8, FILL_RGB, FILL_GLOW), `${KIT}/ui_config_slider_fill_v1.png`);
await writePng(paintCapsule(1024, 72, 3.1, 7, TRACK_RGB, TRACK_GLOW), `${KIT}/ui_config_slider_track_v1.png`);
await writePng(paintRing(96, 22, 5, 12), `${KIT}/ui_config_slider_handle_v1.png`);
await writePng(paintComposite(659, 111), `${KIT}/ui_config_slider_v1.png`);

const lineArt = [
  ["ui_config_btn_minus_v1.png", 10],
  ["ui_config_btn_plus_v1.png", 10],
  ["ui_config_speaker_min_v1.png", 11],
  ["ui_config_speaker_max_v1.png", 11],
  ["ui_config_icon_note_v1.png", 12],
  ["ui_config_icon_brightness_v1.png", 12],
  ["ui_config_icon_skip_v1.png", 12],
  ["ui_config_icon_difficulty_v1.png", 12],
  ["ui_config_chip_normal_v1.png", 8],
  ["ui_config_chip_selected_v1.png", 9],
  ["ui_config_toggle_on_v1.png", 10],
  ["ui_config_toggle_off_v1.png", 10],
];

for (const [name, glow] of lineArt) {
  await rebuildLineArt(`${KIT}/${name}`, glow);
}

for (const name of ["ui_config_panel_v1.png", "ui_config_row_panel_v1.png", "ui_config_row_panel_selected_v1.png"]) {
  await mattePanel(`${KIT}/${name}`);
}

const qaFiles = [
  "ui_config_slider_fill_v1.png",
  "ui_config_slider_track_v1.png",
  "ui_config_slider_handle_v1.png",
  "ui_config_chip_selected_v1.png",
  "ui_config_toggle_on_v1.png",
  "ui_config_btn_minus_v1.png",
  "ui_config_icon_note_v1.png",
  "ui_config_speaker_min_v1.png",
];

const loaded = [];
for (const name of qaFiles) {
  const { data, info } = await sharp(`${KIT}/${name}`).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  loaded.push({ data, w: info.width, h: info.height, name });
}
const pad = 16;
const qaW = 980;
const qaH = loaded.reduce((s, x) => s + Math.min(x.h, 140) + pad, pad);
const qa = Buffer.alloc(qaW * qaH * 4);
for (let y = 0; y < qaH; y++) {
  for (let x = 0; x < qaW; x++) {
    const o = (y * qaW + x) * 4;
    qa[o] = 255;
    qa[o + 1] = 0;
    qa[o + 2] = 255;
    qa[o + 3] = 255;
  }
}
let y0 = pad;
for (const img of loaded) {
  const showH = Math.min(img.h, 140);
  const showW = Math.min(img.w, qaW - pad * 2);
  for (let y = 0; y < showH; y++) {
    for (let x = 0; x < showW; x++) {
      const s = (y * img.w + x) * 4;
      const d = ((y0 + y) * qaW + (pad + x)) * 4;
      const a = img.data[s + 3] / 255;
      qa[d] = Math.round(img.data[s] * a + qa[d] * (1 - a));
      qa[d + 1] = Math.round(img.data[s + 1] * a + qa[d + 1] * (1 - a));
      qa[d + 2] = Math.round(img.data[s + 2] * a + qa[d + 2] * (1 - a));
    }
  }
  y0 += showH + pad;
}
await sharp(qa, { raw: { width: qaW, height: qaH, channels: 4 } }).png().toFile(QA);
console.log(JSON.stringify({ qa: QA, n: lineArt.length + 3 }));
