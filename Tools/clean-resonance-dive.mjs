import fs from "fs";
import path from "path";
import sharp from "sharp";

const DIRS = [
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive",
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive",
];

function lum(r, g, b) {
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function floodMatte(data, width, height, maxLum, maxChroma) {
  const n = width * height;
  const seen = Buffer.alloc(n);
  const q = [];

  const seed = (i) => {
    const o = i * 4;
    const a = data[o + 3];
    const L = lum(data[o], data[o + 1], data[o + 2]);
    const C = chroma(data[o], data[o + 1], data[o + 2]);
    return a < 12 || (L < maxLum && C < maxChroma);
  };

  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const i = y * width + x;
    if (seen[i] || !seed(i)) return;
    seen[i] = 1;
    q.push(i);
  };

  for (let x = 0; x < width; x += 1) {
    push(x, 0);
    push(x, height - 1);
  }
  for (let y = 0; y < height; y += 1) {
    push(0, y);
    push(width - 1, y);
  }

  while (q.length) {
    const i = q.pop();
    data[i * 4 + 3] = 0;
    const x = i % width;
    const y = (i / width) | 0;
    push(x - 1, y);
    push(x + 1, y);
    push(x, y - 1);
    push(x, y + 1);
  }
}

function bbox(data, width, height) {
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      if (data[(y * width + x) * 4 + 3] < 16) continue;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }
  return { minX, minY, maxX, maxY, w: maxX - minX + 1, h: maxY - minY + 1 };
}

async function load(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data: Buffer.from(data), width: info.width, height: info.height };
}

async function main() {
  const art = DIRS[0];
  const names = fs.readdirSync(art).filter((f) => f.endsWith(".png"));
  const fx = new Set(["08_Glow.png", "09_Particles.png", "10_Wave.png", "11_Scanline.png", "12_Shadow.png", "02_Border.png"]);

  const keyed = {};
  for (const name of names) {
    const img = await load(path.join(art, name));
    floodMatte(img.data, img.width, img.height, fx.has(name) ? 22 : 11, fx.has(name) ? 18 : 10);
    keyed[name] = img;
  }

  const lock = keyed["state_normal.png"];
  const box = bbox(lock.data, lock.width, lock.height);
  const padX = Math.round(box.w * 0.04);
  const padY = Math.round(box.h * 0.06);
  const left = Math.max(0, box.minX - padX);
  const top = Math.max(0, box.minY - padY);
  const width = Math.min(lock.width - left, box.w + padX * 2);
  const height = Math.min(lock.height - top, box.h + padY * 2);
  console.log("crop", { left, top, width, height, srcBox: box });

  for (const name of names) {
    const img = keyed[name];
    const png = await sharp(img.data, { raw: { width: img.width, height: img.height, channels: 4 } })
      .extract({ left, top, width, height })
      .png()
      .toBuffer();
    for (const dir of DIRS) {
      fs.writeFileSync(path.join(dir, name), png);
    }
    console.log("wrote", name);
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
