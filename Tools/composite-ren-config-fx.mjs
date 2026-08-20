import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const ROOT = "D:/Fractured-Chorus1";
const REN = path.join(ROOT, "Assets/FracturedChorus/Art/Characters/Ren/School/ren_config_pose_v1.png");
const OUT = path.join(ROOT, "Assets/FracturedChorus/Art/Characters/Ren/School/ren_config_pose_fx_v1.png");
const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const SHARDS = path.join(ROOT, "Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1");

function chromaScore(r, g, b) {
  return Math.hypot(r - 255, g - 0, b - 255);
}

function floodChroma(px, w, h, threshold = 110) {
  const visited = Buffer.alloc(w * h);
  const q = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= w || y >= h) return;
    const id = y * w + x;
    if (visited[id]) return;
    visited[id] = 1;
    q.push(id);
  };
  for (let x = 0; x < w; x++) {
    push(x, 0);
    push(x, h - 1);
  }
  for (let y = 0; y < h; y++) {
    push(0, y);
    push(w - 1, y);
  }
  while (q.length) {
    const id = q.pop();
    const i = id * 4;
    if (chromaScore(px[i], px[i + 1], px[i + 2]) >= threshold) continue;
    px[i + 3] = 0;
    const x = id % w;
    const y = (id / w) | 0;
    push(x - 1, y);
    push(x + 1, y);
    push(x, y - 1);
    push(x, y + 1);
  }
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    const spill = Math.max(0, Math.min(r, b) - g);
    if (px[o + 3] > 0 && spill > 24) {
      px[o + 3] = Math.max(0, Math.round(px[o + 3] * (1 - spill / 280)));
    }
    if (px[o + 3] > 0 && chromaScore(r, g, b) < 140) {
      px[o + 3] = 0;
    }
    if (px[o + 3] > 0 && r > 170 && b > 170 && g < 110 && r - g > 55) {
      px[o + 3] = 0;
    }
    if (px[o + 3] < 8) {
      px[o] = 0;
      px[o + 1] = 0;
      px[o + 2] = 0;
      px[o + 3] = 0;
    }
  }
}

function lumaMatteBlack(px, w, h, floor = 28) {
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const max = Math.max(px[o], px[o + 1], px[o + 2]);
    if (max < floor) {
      px[o] = 0;
      px[o + 1] = 0;
      px[o + 2] = 0;
      px[o + 3] = 0;
    }
  }
}

async function readRgba(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data, w: info.width, h: info.height };
}

async function matteChromaToPng(input, w, h) {
  const img = await readRgba(input);
  floodChroma(img.data, img.w, img.h, 108);
  return sharp(Buffer.from(img.data), { raw: { width: img.w, height: img.h, channels: 4 } })
    .resize(w, h, { fit: "cover", kernel: "lanczos3" })
    .png()
    .toBuffer();
}

async function glowLayer(renPath, w, h, sigma, rgb, mul) {
  const alpha = await sharp(renPath).ensureAlpha().extractChannel("alpha").blur(sigma).raw().toBuffer();
  const out = Buffer.alloc(w * h * 4);
  for (let i = 0; i < w * h; i++) {
    const a = Math.min(255, Math.round(alpha[i] * mul));
    if (a < 2) continue;
    out[i * 4] = rgb[0];
    out[i * 4 + 1] = rgb[1];
    out[i * 4 + 2] = rgb[2];
    out[i * 4 + 3] = a;
  }
  return sharp(out, { raw: { width: w, height: h, channels: 4 } }).png().toBuffer();
}

async function shardBuf(name, width, rotate) {
  const file = path.join(SHARDS, name);
  const img = await readRgba(file);
  lumaMatteBlack(img.data, img.w, img.h, 22);
  return sharp(Buffer.from(img.data), { raw: { width: img.w, height: img.h, channels: 4 } })
    .resize({ width, kernel: "lanczos3" })
    .rotate(rotate, { background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png()
    .toBuffer();
}

const { width: W, height: H } = await sharp(REN).metadata();

const [glowWide, glowMid, glowRim, orbits, crystals, sa, sb, sc, sd, se, sf] = await Promise.all([
  glowLayer(REN, W, H, 42, [214, 196, 255], 0.55),
  glowLayer(REN, W, H, 18, [232, 168, 230], 0.42),
  glowLayer(REN, W, H, 6, [170, 230, 255], 0.55),
  matteChromaToPng(path.join(SRC, "fx_ren_config_orbits_chroma_v1.png"), W, H),
  matteChromaToPng(path.join(SRC, "fx_ren_config_crystals_chroma_v1.png"), W, H),
  shardBuf("ui_crystal_shard_a_v1.png", 210, -28),
  shardBuf("ui_crystal_shard_b_v1.png", 168, 18),
  shardBuf("ui_crystal_shard_c_v1.png", 150, 42),
  shardBuf("ui_crystal_shard_a_v1.png", 128, 12),
  shardBuf("ui_crystal_shard_b_v1.png", 110, -40),
  shardBuf("ui_crystal_shard_c_v1.png", 96, 8),
]);

await sharp({
  create: { width: W, height: H, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } },
})
  .composite([
    { input: glowWide, blend: "screen" },
    { input: glowMid, blend: "screen" },
    { input: orbits, blend: "screen" },
    { input: crystals, blend: "over" },
    { input: sa, left: 18, top: 210, blend: "over" },
    { input: sb, left: 790, top: 470, blend: "over" },
    { input: sc, left: 40, top: 980, blend: "over" },
    { input: sd, left: 760, top: 1180, blend: "over" },
    { input: se, left: 8, top: 620, blend: "over" },
    { input: sf, left: 860, top: 160, blend: "over" },
    { input: glowRim, blend: "screen" },
    { input: REN, blend: "over" },
  ])
  .png({ compressionLevel: 9 })
  .toFile(OUT);

const meta = await sharp(OUT).metadata();
console.log(JSON.stringify({ out: path.basename(OUT), w: meta.width, h: meta.height, alpha: meta.hasAlpha }));
