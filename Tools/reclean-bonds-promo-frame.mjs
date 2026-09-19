import crypto from "crypto";
import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC =
  "C:/Users/Asus/Downloads/Bond UI Pack/Bonds_UI/Generated_Source/11_Frame_Promo.png";
const OUT =
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/Bonds/Pack/11_Frame_Promo.png";
const META = `${OUT}.meta`;
const FILE = "11_Frame_Promo.png";

function dist(a, b) {
  const dr = a[0] - b[0];
  const dg = a[1] - b[1];
  const db = a[2] - b[2];
  return Math.sqrt(dr * dr + dg * dg + db * db);
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
}

function collectSeeds(data, width, height) {
  const seen = new Set();
  const seeds = [];
  const ring = 18;
  const push = (x, y) => {
    const i = (y * width + x) * 4;
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    const key = ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);
    if (seen.has(key)) return;
    seen.add(key);
    seeds.push([r, g, b]);
  };
  for (let x = 0; x < width; x += 12) {
    for (let y = 0; y < ring; y += 6) push(x, y);
    for (let y = height - ring; y < height; y += 6) push(x, y);
  }
  for (let y = 0; y < height; y += 12) {
    for (let x = 0; x < ring; x += 6) push(x, y);
    for (let x = width - ring; x < width; x += 6) push(x, y);
  }
  return seeds;
}

function isBackground(r, g, b, seeds) {
  const c = chroma(r, g, b);
  const y = luma(r, g, b);
  if (y < 22 && c < 18) return true;
  if (c > 22) return false;
  let best = 255;
  for (let i = 0; i < seeds.length; i++) {
    const d = dist([r, g, b], seeds[i]);
    if (d < best) best = d;
  }
  return best <= 28;
}

function punchWindow(data, width, height, seeds) {
  const n = width * height;
  const bg = new Uint8Array(n);
  for (let idx = 0; idx < n; idx++) {
    const i = idx * 4;
    if (isBackground(data[i], data[i + 1], data[i + 2], seeds)) bg[idx] = 1;
  }

  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let head = 0;
  let tail = 0;
  const tryPush = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const idx = y * width + x;
    if (seen[idx] || !bg[idx]) return;
    seen[idx] = 1;
    queue[tail++] = idx;
  };

  for (let x = 0; x < width; x++) {
    tryPush(x, 0);
    tryPush(x, height - 1);
  }
  for (let y = 0; y < height; y++) {
    tryPush(0, y);
    tryPush(width - 1, y);
  }

  const x0 = Math.floor(width * 0.18);
  const x1 = Math.ceil(width * 0.82);
  const y0 = Math.floor(height * 0.18);
  const y1 = Math.ceil(height * 0.82);
  for (let y = y0; y < y1; y += 6) {
    for (let x = x0; x < x1; x += 6) tryPush(x, y);
  }

  while (head < tail) {
    const idx = queue[head++];
    const x = idx % width;
    const y = (idx / width) | 0;
    const i = idx * 4;
    data[i] = 0;
    data[i + 1] = 0;
    data[i + 2] = 0;
    data[i + 3] = 0;
    tryPush(x - 1, y);
    tryPush(x + 1, y);
    tryPush(x, y - 1);
    tryPush(x, y + 1);
  }
}

function stripDarkHalos(data, width, height) {
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const i = (y * width + x) * 4;
      const a = data[i + 3];
      if (a === 0) continue;
      const r = data[i];
      const g = data[i + 1];
      const b = data[i + 2];
      const yv = luma(r, g, b);
      if (yv < 28 && a < 252) {
        data[i] = 0;
        data[i + 1] = 0;
        data[i + 2] = 0;
        data[i + 3] = 0;
        continue;
      }
      if (yv < 12) {
        data[i] = 0;
        data[i + 1] = 0;
        data[i + 2] = 0;
        data[i + 3] = 0;
      }
    }
  }
}

function unpremultiplyRgb(data) {
  for (let i = 0; i < data.length; i += 4) {
    const a = data[i + 3];
    if (a === 0) {
      data[i] = 0;
      data[i + 1] = 0;
      data[i + 2] = 0;
    }
  }
}

function contentBox(data, width, height, pad) {
  let minX = width;
  let minY = height;
  let maxX = -1;
  let maxY = -1;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (data[(y * width + x) * 4 + 3] < 16) continue;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }
  if (maxX < 0) return { left: 0, top: 0, width, height };
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(width - 1, maxX + pad);
  maxY = Math.min(height - 1, maxY + pad);
  return { left: minX, top: minY, width: maxX - minX + 1, height: maxY - minY + 1 };
}

async function main() {
  const srcPath = fs.existsSync(SRC) ? SRC : OUT;
  const { data, info } = await sharp(srcPath).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  const seeds = collectSeeds(pixels, info.width, info.height);
  punchWindow(pixels, info.width, info.height, seeds);
  stripDarkHalos(pixels, info.width, info.height);
  unpremultiplyRgb(pixels);

  let opaque = 0;
  for (let i = 3; i < pixels.length; i += 4) {
    if (pixels[i] > 16) opaque += 1;
  }

  const box = contentBox(pixels, info.width, info.height, 8);
  const cropped = await sharp(pixels, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .extract(box)
    .png()
    .toBuffer();

  fs.writeFileSync(OUT, cropped);
  const existingGuid = fs.existsSync(META)
    ? fs.readFileSync(META, "utf8").match(/^guid: ([a-f0-9]+)/m)?.[1]
    : null;
  if (!existingGuid) {
    throw new Error("missing meta guid for 11_Frame_Promo.png");
  }

  console.log(
    JSON.stringify(
      {
        file: FILE,
        src: srcPath,
        out: OUT,
        guid: existingGuid,
        srcSize: [info.width, info.height],
        outSize: [box.width, box.height],
        opaqueRatio: Number((opaque / (info.width * info.height)).toFixed(4)),
      },
      null,
      2,
    ),
  );
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
