import fs from "fs";
import path from "path";
import sharp from "sharp";

const DIR = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/Bonds/Pack";
const files = fs.readdirSync(DIR).filter((f) => f.endsWith(".png")).sort();

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
}

function grayMid(r, g, b, a) {
  if (a < 200) return false;
  const c = chroma(r, g, b);
  const y = luma(r, g, b);
  return c <= 20 && y >= 95 && y <= 235;
}

const report = [];
for (const file of files) {
  const { data, info } = await sharp(path.join(DIR, file)).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const { width, height } = info;
  let opaque = 0;
  let grayMidCount = 0;
  let checkerHits = 0;
  const samples = [];
  for (let y = 0; y < height; y += 2) {
    for (let x = 0; x < width; x += 2) {
      const i = (y * width + x) * 4;
      const r = data[i];
      const g = data[i + 1];
      const b = data[i + 2];
      const a = data[i + 3];
      if (a > 16) opaque += 1;
      if (!grayMid(r, g, b, a)) continue;
      grayMidCount += 1;
      let osc = 0;
      for (const p of [8, 16, 32]) {
        const nx = x + p;
        const ny = y + p;
        if (nx < width) {
          const j = (y * width + nx) * 4;
          if (grayMid(data[j], data[j + 1], data[j + 2], data[j + 3]) &&
              Math.abs(luma(r, g, b) - luma(data[j], data[j + 1], data[j + 2])) >= 22) {
            osc += 1;
          }
        }
        if (ny < height) {
          const j = (ny * width + x) * 4;
          if (grayMid(data[j], data[j + 1], data[j + 2], data[j + 3]) &&
              Math.abs(luma(r, g, b) - luma(data[j], data[j + 1], data[j + 2])) >= 22) {
            osc += 1;
          }
        }
      }
      if (osc >= 2) {
        checkerHits += 1;
        if (samples.length < 6) {
          samples.push({ x, y, r, g, b, a, yv: Number(luma(r, g, b).toFixed(1)) });
        }
      }
    }
  }
  const cells = Math.ceil(width / 2) * Math.ceil(height / 2);
  report.push({
    file,
    size: [width, height],
    opaqueRatio: Number((opaque / cells).toFixed(4)),
    grayMidRatio: Number((grayMidCount / cells).toFixed(4)),
    checkerRatio: Number((checkerHits / cells).toFixed(4)),
    samples,
  });
}

report.sort((a, b) => b.checkerRatio - a.checkerRatio);
console.log(JSON.stringify(report, null, 2));
