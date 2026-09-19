import { createRequire } from "module";

const require = createRequire(import.meta.url);
const Jimp = require("jimp");

const SRC_CLOUDS =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/statusmenu_bg_clouds_clusters_v2.png";
const SRC_SUN =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/statusmenu_bg_sun_cluster_v3.png";
const OUT_CLOUDS =
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_bg_clouds_strip_v1.png";
const OUT_SUN =
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_bg_sun_rays_overlay_v1.png";

function chromaClouds(im) {
  im.scan(0, 0, im.bitmap.width, im.bitmap.height, (x, y, idx) => {
    const r = im.bitmap.data[idx];
    const g = im.bitmap.data[idx + 1];
    const b = im.bitmap.data[idx + 2];
    const magenta = Math.min(r, b) - g;
    if (r > 150 && b > 150 && g < 140 && magenta > 28) {
      im.bitmap.data[idx + 3] = 0;
      return;
    }
    if (magenta > 12 && r > 120 && b > 120) {
      const t = Math.min(1, magenta / 70);
      im.bitmap.data[idx + 3] = Math.round(im.bitmap.data[idx + 3] * (1 - t));
      im.bitmap.data[idx + 1] = Math.min(255, g + magenta * 0.45);
      im.bitmap.data[idx + 2] = Math.max(0, b - magenta * 0.25);
    }
  });
  return im;
}

function chromaSun(im) {
  im.scan(0, 0, im.bitmap.width, im.bitmap.height, (x, y, idx) => {
    const r = im.bitmap.data[idx];
    const g = im.bitmap.data[idx + 1];
    const b = im.bitmap.data[idx + 2];
    const max = Math.max(r, g, b);
    const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
    if (lum < 40 || max < 108) {
      im.bitmap.data[idx + 3] = 0;
      return;
    }
    const a = Math.min(255, Math.round((lum - 20) * 3.6));
    im.bitmap.data[idx + 3] = Math.min(im.bitmap.data[idx + 3], a);
  });
  return im;
}

function opaqueBBox(im, minA = 18) {
  let minX = im.bitmap.width;
  let minY = im.bitmap.height;
  let maxX = 0;
  let maxY = 0;
  im.scan(0, 0, im.bitmap.width, im.bitmap.height, (x, y, idx) => {
    if (im.bitmap.data[idx + 3] < minA) {
      return;
    }
    if (x < minX) minX = x;
    if (y < minY) minY = y;
    if (x > maxX) maxX = x;
    if (y > maxY) maxY = y;
  });
  if (maxX <= minX || maxY <= minY) {
    throw new Error("empty after chroma");
  }
  return { minX, minY, maxX, maxY };
}

function splitCloudBlobs(im) {
  const w = im.bitmap.width;
  const h = im.bitmap.height;
  const col = new Array(w).fill(0);
  im.scan(0, 0, w, h, (x, y, idx) => {
    if (im.bitmap.data[idx + 3] > 24) {
      col[x] += 1;
    }
  });
  const occupied = col.map((n) => n > 8);
  const ranges = [];
  let start = -1;
  for (let x = 0; x < w; x++) {
    if (occupied[x] && start < 0) {
      start = x;
    }
    if ((!occupied[x] || x === w - 1) && start >= 0) {
      const end = occupied[x] ? x : x - 1;
      ranges.push([start, end]);
      start = -1;
    }
  }
  ranges.sort((a, b) => b[1] - b[0] - (a[1] - a[0]));
  return ranges.slice(0, 2).sort((a, b) => a[0] - b[0]);
}

async function cropPad(im, x0, y0, x1, y1, pad) {
  const cx0 = Math.max(0, x0 - pad);
  const cy0 = Math.max(0, y0 - pad);
  const cx1 = Math.min(im.bitmap.width - 1, x1 + pad);
  const cy1 = Math.min(im.bitmap.height - 1, y1 + pad);
  return im.clone().crop(cx0, cy0, cx1 - cx0 + 1, cy1 - cy0 + 1);
}

async function main() {
  const cloudsSrc = chromaClouds(await Jimp.read(SRC_CLOUDS));
  const sunSrc = chromaSun(await Jimp.read(SRC_SUN));

  const canvasW = 1920;
  const canvasH = 1080;
  const cloudsOut = new Jimp(canvasW, canvasH, 0x00000000);
  const ranges = splitCloudBlobs(cloudsSrc);
  if (ranges.length === 0) {
    throw new Error("no cloud blobs");
  }

  const placements = [
    { x: 220, y: 70, maxW: 520 },
    { x: 1180, y: 160, maxW: 380 },
  ];

  for (let i = 0; i < Math.min(ranges.length, placements.length); i++) {
    const [x0, x1] = ranges[i];
    let minY = cloudsSrc.bitmap.height;
    let maxY = 0;
    cloudsSrc.scan(x0, 0, x1 - x0 + 1, cloudsSrc.bitmap.height, (x, y, idx) => {
      if (cloudsSrc.bitmap.data[idx + 3] < 18) {
        return;
      }
      if (y < minY) minY = y;
      if (y > maxY) maxY = y;
    });
    const piece = await cropPad(cloudsSrc, x0, minY, x1, maxY, 12);
    const p = placements[i];
    if (piece.bitmap.width > p.maxW) {
      piece.resize(p.maxW, Jimp.AUTO);
    }
    cloudsOut.composite(piece, p.x, p.y);
  }

  const sunBox = opaqueBBox(sunSrc, 10);
  const sunPiece = await cropPad(
    sunSrc,
    sunBox.minX,
    sunBox.minY,
    sunBox.maxX,
    sunBox.maxY,
    16,
  );
  if (sunPiece.bitmap.width > 720) {
    sunPiece.resize(720, Jimp.AUTO);
  }
  const sunOut = new Jimp(canvasW, canvasH, 0x00000000);
  sunOut.composite(sunPiece, canvasW - sunPiece.bitmap.width - 40, 36);

  await cloudsOut.writeAsync(OUT_CLOUDS);
  await sunOut.writeAsync(OUT_SUN);
  console.log("wrote", OUT_CLOUDS, cloudsOut.bitmap.width, "x", cloudsOut.bitmap.height);
  console.log("wrote", OUT_SUN, sunOut.bitmap.width, "x", sunOut.bitmap.height);
  console.log("cloud blobs", ranges.length);
}

main();
