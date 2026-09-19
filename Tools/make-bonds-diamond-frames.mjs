import fs from "fs";
import path from "path";
import sharp from "sharp";

const GEN = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const PACK = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/Bonds/Pack";

const JOBS = [
  {
    src: "bond_frame_diamond_normal.png",
    dest: "08_Card_Character_Normal.png",
    kind: "normal",
    outer: 492,
    inner: 458,
    glow: 0,
  },
  {
    src: "bond_frame_diamond_selected.png",
    dest: "09_Card_Character_Selected.png",
    kind: "selected",
    outer: 492,
    inner: 428,
    glow: 18,
  },
];

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function isBackground(r, g, b) {
  const y = luma(r, g, b);
  const c = chroma(r, g, b);
  if (y >= 244 && c <= 14) return true;
  if (y <= 14 && c <= 14) return true;
  return false;
}

function ringCover(d, inner, outer, aa) {
  if (d <= inner - aa) return 0;
  if (d < inner) return (d - (inner - aa)) / aa;
  if (d <= outer) return 1;
  if (d < outer + aa) return 1 - (d - outer) / aa;
  return 0;
}

async function rebuild({ src, dest, kind, outer, inner, glow }) {
  const srcPath = path.join(GEN, src);
  const { data, info } = await sharp(srcPath).ensureAlpha().resize(1024, 1024, { fit: "fill" }).raw().toBuffer({
    resolveWithObject: true,
  });
  const w = info.width;
  const h = info.height;
  const cx = (w - 1) / 2;
  const cy = (h - 1) / 2;
  const out = Buffer.alloc(w * h * 4);
  const aa = 1.35;
  let kept = 0;

  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const i = (y * w + x) * 4;
      const r = data[i];
      const g = data[i + 1];
      const b = data[i + 2];
      if (isBackground(r, g, b)) continue;

      const d = Math.abs(x - cx) + Math.abs(y - cy);
      let cover = ringCover(d, inner, outer, aa);
      if (glow > 0 && d > outer && d <= outer + glow) {
        const t = 1 - (d - outer) / glow;
        cover = Math.max(cover, t * t);
      }
      if (cover <= 0.01) continue;

      out[i] = r;
      out[i + 1] = g;
      out[i + 2] = b;
      out[i + 3] = Math.min(255, Math.round(255 * cover));
      kept += 1;
    }
  }

  const destPath = path.join(PACK, dest);
  await sharp(out, { raw: { width: w, height: h, channels: 4 } }).png().toFile(destPath);

  return { dest, kind, kept, size: [w, h], inner, outer, glow };
}

const report = [];
for (const job of JOBS) {
  report.push(await rebuild(job));
}
console.log(JSON.stringify(report, null, 2));
