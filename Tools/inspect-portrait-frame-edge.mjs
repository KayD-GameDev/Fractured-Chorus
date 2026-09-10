import sharp from "sharp";
import fs from "fs";
import path from "path";

const SRC = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit/ui_stat_panel_portrait_v2.png";
const BACKUP = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit/ui_stat_panel_portrait_v2_backup.png";

const { data, info } = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const { width: w, height: h } = info;
const A = 8;

function alpha(x, y) {
  return data[(y * w + x) * 4 + 3];
}

const right = [];
const left = [];
for (let y = 0; y < h; y++) {
  let r = -1;
  let l = w;
  for (let x = w - 1; x >= 0; x--) {
    if (alpha(x, y) > A) {
      r = x;
      break;
    }
  }
  for (let x = 0; x < w; x++) {
    if (alpha(x, y) > A) {
      l = x;
      break;
    }
  }
  right.push(r);
  left.push(l);
}

function median(arr) {
  const s = [...arr].filter((v) => v >= 0 && v < w).sort((a, b) => a - b);
  return s[Math.floor(s.length / 2)];
}

const y0 = Math.floor(h * 0.18);
const y1 = Math.floor(h * 0.82);
const midRight = right.slice(y0, y1);
const midLeft = left.slice(y0, y1);
const medR = median(midRight);
const medL = median(midLeft);
const maxR = Math.max(...midRight);
const minL = Math.min(...midLeft);

const tabRowsR = [];
const tabRowsL = [];
for (let y = y0; y < y1; y++) {
  if (right[y] > medR + 6) tabRowsR.push({ y, x: right[y], extra: right[y] - medR });
  if (left[y] < medL - 6) tabRowsL.push({ y, x: left[y], extra: medL - left[y] });
}

console.log({ w, h, medL, medR, minL, maxR, tabRowsR: tabRowsR.length, tabRowsL: tabRowsL.length });
if (tabRowsR.length) {
  console.log("right tab y", tabRowsR[0].y, "->", tabRowsR[tabRowsR.length - 1].y, "maxExtra", Math.max(...tabRowsR.map((t) => t.extra)));
}
if (tabRowsL.length) {
  console.log("left tab y", tabRowsL[0].y, "->", tabRowsL[tabRowsL.length - 1].y, "maxExtra", Math.max(...tabRowsL.map((t) => t.extra)));
}

const sample = [];
for (let y = 0; y < h; y += 40) sample.push({ y, l: left[y], r: right[y] });
console.log(sample);

const APPLY = process.argv.includes("--apply");
if (!APPLY) process.exit(0);

if (!fs.existsSync(BACKUP)) {
  fs.copyFileSync(SRC, BACKUP);
  console.log("backup", BACKUP);
}

const out = Buffer.from(data);
const fade = 4;

function setA(x, y, a) {
  out[(y * w + x) * 4 + 3] = a;
}

const limitR = medR + 1;
const limitL = medL - 1;
for (let y = y0; y < y1; y++) {
  for (let x = limitR + 1; x < w; x++) {
    const d = x - limitR;
    const a = alpha(x, y);
    if (a === 0) continue;
    if (d > fade) setA(x, y, 0);
    else setA(x, y, Math.round((a * (fade - d)) / fade));
  }
  for (let x = 0; x < limitL; x++) {
    const d = limitL - x;
    const a = alpha(x, y);
    if (a === 0) continue;
    if (d > fade) setA(x, y, 0);
    else setA(x, y, Math.round((a * (fade - d)) / fade));
  }
}

await sharp(out, { raw: { width: w, height: h, channels: 4 } }).png().toFile(SRC);
console.log("wrote", SRC);
