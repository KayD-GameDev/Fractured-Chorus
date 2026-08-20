import fs from "node:fs";
import sharp from "sharp";

const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";
const FILL = `${KIT}/ui_config_slider_fill_v1.png`;
const QA = "D:/Fractured-Chorus1/Tools/_qa_slider_fill_matte.png";

function luma(r, g, b) {
  return (r + g + b) / 3;
}

function punch(px, o) {
  px[o] = 0;
  px[o + 1] = 0;
  px[o + 2] = 0;
  px[o + 3] = 0;
}

const { data, info } = await sharp(FILL).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const w = info.width;
const h = info.height;
const cy = (h - 1) * 0.5;
const sigma = 4.2;
const capR = 14;
const barHalf = 7;

function stadiumDist(x, y) {
  const yDist = Math.abs(y - cy);
  if (x >= capR && x < w - capR) {
    return yDist;
  }
  const cx = x < capR ? capR : w - 1 - capR;
  return Math.hypot(x - cx, y - cy);
}

for (let y = 0; y < h; y++) {
  const dist = Math.abs(y - cy);
  const gate = Math.exp(-(dist * dist) / (2 * sigma * sigma));
  for (let x = 0; x < w; x++) {
    const o = (y * w + x) * 4;
    const r = data[o];
    const g = data[o + 1];
    const b = data[o + 2];
    const a = data[o + 3];
    if (a < 8) {
      punch(data, o);
      continue;
    }

    const sd = stadiumDist(x, y);
    const L = luma(r, g, b);
    if (sd > barHalf + 4 || L < 88) {
      punch(data, o);
      continue;
    }

    const lumaGate = Math.max(0, Math.min(1, (L - 88) / 55));
    const edge = Math.max(0, 1 - Math.max(0, sd - barHalf) / 4);
    const na = Math.round(a * gate * lumaGate * edge);
    if (na < 14) {
      punch(data, o);
      continue;
    }

    data[o + 3] = Math.min(255, na);
  }
}

await sharp(data, { raw: { width: w, height: h, channels: 4 } }).png().toFile(FILL);

let text = fs.readFileSync(`${FILL}.meta`, "utf8");
text = text.replace(
  /spriteBorder: \{x: [^}]+\}/,
  "spriteBorder: {x: 48, y: 16, z: 48, w: 16}",
);
fs.writeFileSync(`${FILL}.meta`, text);

const qaH = h + 24;
const qaW = Math.min(w, 900);
const qa = Buffer.alloc(qaW * qaH * 4);
for (let y = 0; y < qaH; y++) {
  for (let x = 0; x < qaW; x++) {
    const o = (y * qaW + x) * 4;
    const cell = ((x >> 3) & 1) ^ ((y >> 3) & 1);
    const v = cell ? 210 : 150;
    qa[o] = v;
    qa[o + 1] = v;
    qa[o + 2] = v;
    qa[o + 3] = 255;
  }
}
for (let y = 0; y < h; y++) {
  for (let x = 0; x < qaW; x++) {
    const s = (y * w + x) * 4;
    const d = ((y + 12) * qaW + x) * 4;
    const a = data[s + 3] / 255;
    qa[d] = Math.round(data[s] * a + qa[d] * (1 - a));
    qa[d + 1] = Math.round(data[s + 1] * a + qa[d + 1] * (1 - a));
    qa[d + 2] = Math.round(data[s + 2] * a + qa[d + 2] * (1 - a));
  }
}
await sharp(qa, { raw: { width: qaW, height: qaH, channels: 4 } }).png().toFile(QA);

let a0 = 0;
let opaqueRows = 0;
for (let y = 0; y < h; y++) {
  let rowA = 0;
  for (let x = 0; x < w; x++) {
    const a = data[(y * w + x) * 4 + 3];
    if (a === 0) {
      a0++;
    }
    rowA += a;
  }
  if (rowA / (w * 255) > 0.5) {
    opaqueRows++;
  }
}

console.log(JSON.stringify({
  w,
  h,
  a0pct: +(100 * a0 / (w * h)).toFixed(1),
  opaqueRows,
}));
