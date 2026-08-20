import path from "node:path";
import sharp from "sharp";

const SRC = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit/ui_config_slider_v1.png";
const OUT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";

const { data, info } = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const w = info.width;
const h = info.height;
const px = data;

function lum(o) {
  return (px[o] + px[o + 1] + px[o + 2]) / 3;
}

let sumX = 0;
let sumY = 0;
let n = 0;
for (let y = 0; y < h; y++) {
  for (let x = 0; x < w; x++) {
    const o = (y * w + x) * 4;
    if (px[o + 3] < 80) continue;
    if (lum(o) < 210) continue;
    if (px[o + 1] < 180) continue;
    sumX += x;
    sumY += y;
    n++;
  }
}

const cx = Math.round(sumX / Math.max(1, n));
const cy = Math.round(sumY / Math.max(1, n));
const handleR = 48;
const x0 = Math.max(0, cx - handleR);
const y0 = Math.max(0, cy - handleR);
const x1 = Math.min(w, cx + handleR);
const y1 = Math.min(h, cy + handleR);

const handle = Buffer.alloc((x1 - x0) * (y1 - y0) * 4);
for (let y = y0; y < y1; y++) {
  for (let x = x0; x < x1; x++) {
    const s = (y * w + x) * 4;
    const d = ((y - y0) * (x1 - x0) + (x - x0)) * 4;
    const dx = x - cx;
    const dy = y - cy;
    const dist = Math.hypot(dx, dy);
    handle[d] = px[s];
    handle[d + 1] = px[s + 1];
    handle[d + 2] = px[s + 2];
    handle[d + 3] = dist > handleR - 2 ? 0 : px[s + 3];
  }
}

const empty = Buffer.alloc(w * h * 4);
const fill = Buffer.alloc(w * h * 4);
const sampleX = Math.min(w - 1, cx + handleR + 18);
for (let y = 0; y < h; y++) {
  const so = (y * w + sampleX) * 4;
  for (let x = 0; x < w; x++) {
    const o = (y * w + x) * 4;
    empty[o] = px[so];
    empty[o + 1] = px[so + 1];
    empty[o + 2] = px[so + 2];
    empty[o + 3] = px[so + 3];

    const a = px[o + 3];
    if (a < 20) continue;
    const onBar = Math.abs(y - cy) <= 12 && x < cx - 10 && a > 40;
    const bright = lum(o) > 70 && px[o] > 110 && px[o + 2] > 130 && px[o + 1] < 160;
    if (onBar || (x < cx - 10 && bright)) {
      fill[o] = px[o];
      fill[o + 1] = px[o + 1];
      fill[o + 2] = px[o + 2];
      fill[o + 3] = a;
    }
  }
}

let fillMinX = w;
let fillMaxX = 0;
let fillMinY = h;
let fillMaxY = 0;
for (let y = 0; y < h; y++) {
  for (let x = 0; x < w; x++) {
    const o = (y * w + x) * 4;
    if (fill[o + 3] < 16) continue;
    fillMinX = Math.min(fillMinX, x);
    fillMaxX = Math.max(fillMaxX, x);
    fillMinY = Math.min(fillMinY, y);
    fillMaxY = Math.max(fillMaxY, y);
  }
}

const fw = Math.max(8, fillMaxX - fillMinX + 1);
const fh = Math.max(8, fillMaxY - fillMinY + 1);
const fillCrop = Buffer.alloc(fw * fh * 4);
for (let y = 0; y < fh; y++) {
  for (let x = 0; x < fw; x++) {
    const s = ((fillMinY + y) * w + (fillMinX + x)) * 4;
    const d = (y * fw + x) * 4;
    fillCrop[d] = fill[s];
    fillCrop[d + 1] = fill[s + 1];
    fillCrop[d + 2] = fill[s + 2];
    fillCrop[d + 3] = fill[s + 3];
  }
}

await sharp(handle, { raw: { width: x1 - x0, height: y1 - y0, channels: 4 } })
  .png()
  .toFile(path.join(OUT, "ui_config_slider_handle_v1.png"));
await sharp(empty, { raw: { width: w, height: h, channels: 4 } })
  .png()
  .toFile(path.join(OUT, "ui_config_slider_track_v1.png"));
await sharp(fillCrop, { raw: { width: fw, height: fh, channels: 4 } })
  .png()
  .toFile(path.join(OUT, "ui_config_slider_fill_v1.png"));

console.log(JSON.stringify({ w, h, cx, cy, n, handle: [x1 - x0, y1 - y0], fill: [fw, fh, fillMinX, fillMinY] }));
