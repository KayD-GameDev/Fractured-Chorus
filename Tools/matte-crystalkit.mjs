import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const CRYSTAL = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit";
const REF = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/_ref";
const SCREEN = path.join(REF, "_ref_stats_mock_screen.png");

function luma(r, g, b) {
  return (r + g + b) / 3;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function punch(px, o) {
  px[o] = px[o + 1] = px[o + 2] = px[o + 3] = 0;
}

function floodDark(px, w, h, lumaMax, chromaMax) {
  const n = w * h;
  const mask = new Uint8Array(n);
  const q = new Int32Array(n);
  let qs = 0;
  let qe = 0;
  const trySeed = (x, y) => {
    const i = y * w + x;
    if (mask[i]) return;
    const o = i * 4;
    const L = luma(px[o], px[o + 1], px[o + 2]);
    const C = chroma(px[o], px[o + 1], px[o + 2]);
    if (L > lumaMax || C > chromaMax) return;
    mask[i] = 1;
    q[qe++] = i;
  };
  for (let x = 0; x < w; x++) {
    trySeed(x, 0);
    trySeed(x, h - 1);
  }
  for (let y = 0; y < h; y++) {
    trySeed(0, y);
    trySeed(w - 1, y);
  }
  while (qs < qe) {
    const i = q[qs++];
    const x = i % w;
    const y = (i / w) | 0;
    const nb = [x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1];
    for (const j of nb) {
      if (j < 0 || mask[j]) continue;
      const o = j * 4;
      const L = luma(px[o], px[o + 1], px[o + 2]);
      const C = chroma(px[o], px[o + 1], px[o + 2]);
      if (L <= lumaMax && C <= chromaMax) {
        mask[j] = 1;
        q[qe++] = j;
      }
    }
  }
  return mask;
}

async function matteFile(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width;
  const h = info.height;
  const px = data;
  const dead = floodDark(px, w, h, 26, 28);
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    if (dead[i]) {
      punch(px, o);
      continue;
    }
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    const L = luma(r, g, b);
    if (L < 12) {
      punch(px, o);
      continue;
    }
    if (L < 58) {
      const premul = Math.max(r, g, b, 1);
      if (premul < 14) {
        punch(px, o);
        continue;
      }
      const s = 255 / premul;
      px[o] = Math.min(255, Math.round(r * s));
      px[o + 1] = Math.min(255, Math.round(g * s));
      px[o + 2] = Math.min(255, Math.round(b * s));
      px[o + 3] = Math.min(255, Math.round(premul * 1.08));
    } else {
      px[o + 3] = 255;
    }
  }
  await sharp(px, { raw: { width: w, height: h, channels: 4 } }).png().toFile(file);
}

const files = fs.readdirSync(CRYSTAL).filter((n) => n.endsWith(".png"));
for (const name of files) {
  await matteFile(path.join(CRYSTAL, name));
}

await sharp(path.join(REF, "_ref_stats_mock_full.png"))
  .extract({ left: 0, top: 4, width: 1024, height: 416 })
  .resize(1920, 1080, { fit: "fill" })
  .png()
  .toFile(SCREEN);

console.log(JSON.stringify({ kit: files.length, screen: SCREEN }));
