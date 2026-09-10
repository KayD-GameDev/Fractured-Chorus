import sharp from "sharp";
import fs from "fs";
import path from "path";

function chromaScore(r, g, b) {
  return Math.hypot(r - 255, g - 0, b - 255);
}

async function matteFile(input, output, { crop = true, pad = 16 } = {}) {
  const { data, info } = await sharp(input).ensureAlpha().raw().toBuffer({
    resolveWithObject: true,
  });
  const w = info.width;
  const h = info.height;
  const px = data;
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
    if (chromaScore(px[i], px[i + 1], px[i + 2]) >= 100) continue;
    px[i + 3] = 0;
    const x = id % w;
    const y = (id / w) | 0;
    push(x - 1, y);
    push(x + 1, y);
    push(x, y - 1);
    push(x, y + 1);
  }

  for (let y = 1; y < h - 1; y++) {
    for (let x = 1; x < w - 1; x++) {
      const i = (y * w + x) * 4;
      if (px[i + 3] === 0) continue;
      let hit = false;
      for (const [dx, dy] of [
        [-1, 0],
        [1, 0],
        [0, -1],
        [0, 1],
      ]) {
        if (px[((y + dy) * w + (x + dx)) * 4 + 3] === 0) {
          hit = true;
          break;
        }
      }
      if (!hit) continue;
      const mag = Math.min(px[i], px[i + 2]);
      const spill = Math.max(0, mag - px[i + 1]);
      if (spill > 12) {
        px[i] = Math.max(0, px[i] - spill * 0.92);
        px[i + 2] = Math.max(0, px[i + 2] - spill * 0.92);
        px[i + 3] = Math.max(24, Math.round(px[i + 3] * (1 - spill / 280)));
      }
      if (chromaScore(px[i], px[i + 1], px[i + 2]) < 130) {
        px[i + 3] = 0;
      }
    }
  }

  let out = sharp(Buffer.from(px), {
    raw: { width: w, height: h, channels: 4 },
  });

  if (crop) {
    let minX = w;
    let minY = h;
    let maxX = 0;
    let maxY = 0;
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        if (px[(y * w + x) * 4 + 3] < 8) continue;
        if (x < minX) minX = x;
        if (y < minY) minY = y;
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
      }
    }
    if (maxX > minX && maxY > minY) {
      const left = Math.max(0, minX - pad);
      const top = Math.max(0, minY - pad);
      const width = Math.min(w - left, maxX - minX + 1 + pad * 2);
      const height = Math.min(h - top, maxY - minY + 1 + pad * 2);
      out = out.extract({ left, top, width, height });
    }
  }

  await out.png().toFile(output);
}

async function contentBounds(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({
    resolveWithObject: true,
  });
  let minX = info.width;
  let minY = info.height;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < info.height; y++) {
    for (let x = 0; x < info.width; x++) {
      if (data[(y * info.width + x) * 4 + 3] <= 16) continue;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }
  return {
    left: minX,
    top: minY,
    width: maxX - minX + 1,
    height: maxY - minY + 1,
    canvasW: info.width,
    canvasH: info.height,
  };
}

async function normalizeToRen(inputRaw, outputFinal, renRef) {
  const ren = await contentBounds(renRef);
  const tmp = inputRaw.replace(/\.png$/i, "_matted.png");
  await matteFile(inputRaw, tmp, { crop: true, pad: 16 });

  const srcMeta = await sharp(tmp).metadata();
  const scale = ren.height / srcMeta.height;
  const nw = Math.round(srcMeta.width * scale);
  const nh = ren.height;
  const resized = await sharp(tmp)
    .resize(nw, nh, { fit: "inside" })
    .png()
    .toBuffer();

  const left = ren.left + Math.round((ren.width - nw) / 2);
  const top = ren.top;

  await sharp({
    create: {
      width: ren.canvasW,
      height: ren.canvasH,
      channels: 4,
      background: { r: 0, g: 0, b: 0, alpha: 0 },
    },
  })
    .composite([{ input: resized, top, left }])
    .png()
    .toFile(outputFinal);

  fs.unlinkSync(tmp);
  const out = await sharp(outputFinal).metadata();
  console.log(path.basename(outputFinal), `${out.width}x${out.height}`);
}

const renRef =
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/Characters/Ren/VnBust/ren_bust_neutral_v1.png";
const srcDir = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";

const jobs = [
  [
    `${srcDir}/charlotte_bust_neutral_v1_raw.png`,
    "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/Characters/Charlotte/VnBust/charlotte_bust_neutral_v1.png",
  ],
  [
    `${srcDir}/coda_bust_neutral_v1_raw.png`,
    "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/Characters/Coda/VnBust/coda_bust_neutral_v1.png",
  ],
];

for (const [raw, out] of jobs) {
  await normalizeToRen(raw, out, renRef);
}
