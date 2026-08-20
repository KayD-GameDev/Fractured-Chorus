import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";
const FILL = path.join(KIT, "ui_config_slider_fill_v1.png");
const HANDLE = path.join(KIT, "ui_config_slider_handle_v1.png");

function punch(px, o) {
  px[o] = 0;
  px[o + 1] = 0;
  px[o + 2] = 0;
  px[o + 3] = 0;
}

async function load(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data, w: info.width, h: info.height };
}

const fill = await load(FILL);
const fw = fill.w;
const fh = fill.h;
const fpx = fill.data;

let minX = fw;
let maxX = 0;
let minY = fh;
let maxY = 0;
for (let y = 0; y < fh; y++) {
  for (let x = 0; x < fw; x++) {
    if (fpx[(y * fw + x) * 4 + 3] < 12) continue;
    minX = Math.min(minX, x);
    maxX = Math.max(maxX, x);
    minY = Math.min(minY, y);
    maxY = Math.max(maxY, y);
  }
}

const bodyX = Math.min(fw - 2, Math.max(minX + 56, Math.floor((minX + maxX) * 0.55)));
const cutFrom = Math.max(bodyX + 1, maxX - 40);

for (let x = cutFrom; x < fw; x++) {
  for (let y = 0; y < fh; y++) {
    const src = (y * fw + bodyX) * 4;
    const dst = (y * fw + x) * 4;
    fpx[dst] = fpx[src];
    fpx[dst + 1] = fpx[src + 1];
    fpx[dst + 2] = fpx[src + 2];
    fpx[dst + 3] = fpx[src + 3];
  }
}

for (let y = 0; y < fh; y++) {
  const edge = (y * fw + (fw - 1)) * 4;
  punch(fpx, edge);
}

await sharp(fpx, { raw: { width: fw, height: fh, channels: 4 } }).png().toFile(FILL);

let meta = fs.readFileSync(FILL + ".meta", "utf8");
meta = meta.replace(
  /spriteBorder: \{x: [^}]+\}/g,
  "spriteBorder: {x: 24, y: 14, z: 6, w: 14}",
);
fs.writeFileSync(FILL + ".meta", meta);

const handle = await load(HANDLE);
const hw = handle.w;
const hh = handle.h;
const hcx = (hw - 1) * 0.5;
const hcy = (hh - 1) * 0.5;
for (let y = 0; y < hh; y++) {
  for (let x = 0; x < hw; x++) {
    const o = (y * hw + x) * 4;
    const dx = x - hcx;
    const dy = y - hcy;
    const dist = Math.hypot(dx, dy);
    const ang = Math.abs(Math.atan2(dy, dx));
    const horiz = ang < 0.38 || ang > Math.PI - 0.38;
    if (horiz && dist > 30) {
      punch(handle.data, o);
    }
  }
}
await sharp(handle.data, { raw: { width: hw, height: hh, channels: 4 } }).png().toFile(HANDLE);

console.log(JSON.stringify({ fill: [fw, fh], ink: [minX, maxX, minY, maxY], bodyX, cutFrom }));
