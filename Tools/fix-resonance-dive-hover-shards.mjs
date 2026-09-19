import fs from "fs";
import path from "path";
import crypto from "crypto";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";
const SHARD_COUNT = 14;

function lum(r, g, b) {
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function writeBoth(name, buf) {
  fs.writeFileSync(path.join(ART, name), buf);
  fs.writeFileSync(path.join(RES, name), buf);
}

function bbox(data, width, height) {
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      if (data[(y * width + x) * 4 + 3] <= 10) continue;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }
  if (maxX < minX) return null;
  return { minX, minY, maxX, maxY, w: maxX - minX + 1, h: maxY - minY + 1 };
}

function keyBackdropOnly(pixels, width, height) {
  const n = width * height;
  const outside = Buffer.alloc(n);
  const queue = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const i = y * width + x;
    if (outside[i]) return;
    const o = i * 4;
    const L = lum(pixels[o], pixels[o + 1], pixels[o + 2]);
    const C = chroma(pixels[o], pixels[o + 1], pixels[o + 2]);
    if (L > 14 || C > 16) return;
    outside[i] = 1;
    queue.push(i);
  };
  for (let x = 0; x < width; x += 1) {
    push(x, 0);
    push(x, height - 1);
  }
  for (let y = 0; y < height; y += 1) {
    push(0, y);
    push(width - 1, y);
  }
  while (queue.length) {
    const i = queue.pop();
    const x = i % width;
    const y = (i / width) | 0;
    push(x + 1, y);
    push(x - 1, y);
    push(x, y + 1);
    push(x, y - 1);
  }
  for (let i = 0; i < n; i += 1) {
    if (!outside[i]) continue;
    const o = i * 4;
    pixels[o] = 0;
    pixels[o + 1] = 0;
    pixels[o + 2] = 0;
    pixels[o + 3] = 0;
  }
}

function isEmpty(data, o) {
  return data[o + 3] < 28;
}

function isIce(data, o) {
  if (data[o + 3] < 80) return false;
  return lum(data[o], data[o + 1], data[o + 2]) > 96;
}

function fillIceHoles(data, width, height) {
  const n = width * height;
  const borderEmpty = Buffer.alloc(n);
  const queue = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const i = y * width + x;
    if (borderEmpty[i]) return;
    if (!isEmpty(data, i * 4)) return;
    borderEmpty[i] = 1;
    queue.push(i);
  };
  for (let x = 0; x < width; x += 1) {
    push(x, 0);
    push(x, height - 1);
  }
  for (let y = 0; y < height; y += 1) {
    push(0, y);
    push(width - 1, y);
  }
  while (queue.length) {
    const i = queue.pop();
    const x = i % width;
    const y = (i / width) | 0;
    push(x + 1, y);
    push(x - 1, y);
    push(x, y + 1);
    push(x, y - 1);
  }

  const seen = Buffer.alloc(n);
  let filled = 0;
  for (let i = 0; i < n; i += 1) {
    if (seen[i] || borderEmpty[i] || !isEmpty(data, i * 4)) continue;
    const cells = [];
    const stack = [i];
    seen[i] = 1;
    while (stack.length) {
      const cur = stack.pop();
      cells.push(cur);
      const x = cur % width;
      const y = (cur / width) | 0;
      const neigh = [
        [x + 1, y],
        [x - 1, y],
        [x, y + 1],
        [x, y - 1],
      ];
      for (const [nx, ny] of neigh) {
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
        const ni = ny * width + nx;
        if (seen[ni] || borderEmpty[ni] || !isEmpty(data, ni * 4)) continue;
        seen[ni] = 1;
        stack.push(ni);
      }
    }
    if (cells.length === 0 || cells.length > 420) continue;
    let iceN = 0;
    let iceR = 0;
    let iceG = 0;
    let iceB = 0;
    let iceA = 0;
    for (const cur of cells) {
      const x = cur % width;
      const y = (cur / width) | 0;
      for (let dy = -2; dy <= 2; dy += 1) {
        for (let dx = -2; dx <= 2; dx += 1) {
          const nx = x + dx;
          const ny = y + dy;
          if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
          const no = (ny * width + nx) * 4;
          if (!isIce(data, no)) continue;
          iceN += 1;
          iceR += data[no];
          iceG += data[no + 1];
          iceB += data[no + 2];
          iceA += data[no + 3];
        }
      }
    }
    if (iceN < 8) continue;
    const r = Math.round(iceR / iceN);
    const g = Math.round(iceG / iceN);
    const b = Math.round(iceB / iceN);
    const a = Math.round(iceA / iceN);
    for (const cur of cells) {
      const o = cur * 4;
      data[o] = r;
      data[o + 1] = g;
      data[o + 2] = b;
      data[o + 3] = a;
      filled += 1;
    }
  }
  return filled;
}

function fillDarkIcePocks(data, width, height) {
  const n = width * height;
  const seen = Buffer.alloc(n);
  let filled = 0;
  for (let i = 0; i < n; i += 1) {
    if (seen[i]) continue;
    const o = i * 4;
    if (data[o + 3] < 40) continue;
    const L0 = lum(data[o], data[o + 1], data[o + 2]);
    const C0 = chroma(data[o], data[o + 1], data[o + 2]);
    if (L0 > 68 || C0 > 55) continue;
    const cells = [];
    const stack = [i];
    seen[i] = 1;
    while (stack.length) {
      const cur = stack.pop();
      cells.push(cur);
      const x = cur % width;
      const y = (cur / width) | 0;
      const neigh = [
        [x + 1, y],
        [x - 1, y],
        [x, y + 1],
        [x, y - 1],
      ];
      for (const [nx, ny] of neigh) {
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
        const ni = ny * width + nx;
        if (seen[ni]) continue;
        const no = ni * 4;
        if (data[no + 3] < 40) continue;
        const L = lum(data[no], data[no + 1], data[no + 2]);
        const C = chroma(data[no], data[no + 1], data[no + 2]);
        if (L > 68 || C > 55) continue;
        seen[ni] = 1;
        stack.push(ni);
      }
    }
    if (cells.length === 0 || cells.length > 90) continue;
    let iceN = 0;
    let iceR = 0;
    let iceG = 0;
    let iceB = 0;
    let iceA = 0;
    for (const cur of cells) {
      const x = cur % width;
      const y = (cur / width) | 0;
      for (let dy = -3; dy <= 3; dy += 1) {
        for (let dx = -3; dx <= 3; dx += 1) {
          const nx = x + dx;
          const ny = y + dy;
          if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
          const no = (ny * width + nx) * 4;
          if (!isIce(data, no)) continue;
          iceN += 1;
          iceR += data[no];
          iceG += data[no + 1];
          iceB += data[no + 2];
          iceA += data[no + 3];
        }
      }
    }
    if (iceN < 14) continue;
    const r = Math.round(iceR / iceN);
    const g = Math.round(iceG / iceN);
    const b = Math.round(iceB / iceN);
    const a = Math.round(iceA / iceN);
    for (const cur of cells) {
      const o = cur * 4;
      data[o] = r;
      data[o + 1] = g;
      data[o + 2] = b;
      data[o + 3] = a;
      filled += 1;
    }
  }
  return filled;
}

async function toPng(rgba, width, height) {
  return sharp(rgba, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

function ensureMeta(absPng) {
  const metaPath = absPng + ".meta";
  if (fs.existsSync(metaPath)) return;
  const template = fs.readFileSync(path.join(ART, "09_p0.png.meta"), "utf8");
  const guid = crypto.randomBytes(16).toString("hex");
  const spriteId = guid.slice(0, 16) + "8000000000000000";
  fs.writeFileSync(
    metaPath,
    template.replace(/guid: [a-f0-9]+/, `guid: ${guid}`).replace(/spriteID: [a-f0-9]+/, `spriteID: ${spriteId}`),
  );
}

function components(pixels, width, height) {
  const n = width * height;
  const seen = Buffer.alloc(n);
  const groups = [];
  const stack = [];
  for (let i = 0; i < n; i += 1) {
    if (seen[i] || pixels[i * 4 + 3] < 20) continue;
    stack.length = 0;
    stack.push(i);
    seen[i] = 1;
    const cells = [];
    while (stack.length) {
      const cur = stack.pop();
      cells.push(cur);
      const x = cur % width;
      const y = (cur / width) | 0;
      const neigh = [cur + 1, cur - 1, cur + width, cur - width];
      for (const ni of neigh) {
        if (ni < 0 || ni >= n || seen[ni]) continue;
        const nx = ni % width;
        const ny = (ni / width) | 0;
        if (Math.abs(nx - x) + Math.abs(ny - y) !== 1) continue;
        if (pixels[ni * 4 + 3] < 20) continue;
        seen[ni] = 1;
        stack.push(ni);
      }
    }
    if (cells.length < 160 || cells.length > 9000) continue;
    let minX = width;
    let minY = height;
    let maxX = 0;
    let maxY = 0;
    for (const c of cells) {
      const x = c % width;
      const y = (c / width) | 0;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
    const bw = maxX - minX + 1;
    const bh = maxY - minY + 1;
    const fill = cells.length / (bw * bh);
    if (bw > 86 || bh > 86) continue;
    if (fill < 0.37) continue;
    groups.push({ cells, minX, minY, maxX, maxY, bw, bh, area: cells.length, fill });
  }
  groups.sort((a, b) => a.area - b.area);
  return groups;
}

async function fixHover() {
  const { data, info } = await sharp(fs.readFileSync(path.join(ART, "state_hover.png")))
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  const holes = fillIceHoles(pixels, info.width, info.height);
  const pocks = fillDarkIcePocks(pixels, info.width, info.height);
  writeBoth("state_hover.png", await toPng(pixels, info.width, info.height));
  console.log("hover holes", holes, "pocks", pocks, `${info.width}x${info.height}`);
}

async function sliceShards() {
  const { data, info } = await sharp(fs.readFileSync(path.join(SRC, "resonance_dive_shards_v1.png")))
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  keyBackdropOnly(pixels, info.width, info.height);
  const found = components(pixels, info.width, info.height);
  console.log("shards", found.length);
  if (found.length < SHARD_COUNT) throw new Error(`only ${found.length} shards`);
  const picked = found.slice(0, SHARD_COUNT);
  for (let i = 0; i < SHARD_COUNT; i += 1) {
    const g = picked[i];
    const pad = 4;
    const left = Math.max(0, g.minX - pad);
    const top = Math.max(0, g.minY - pad);
    const w = Math.min(info.width - left, g.bw + pad * 2);
    const h = Math.min(info.height - top, g.bh + pad * 2);
    const raw = Buffer.alloc(w * h * 4);
    for (let y = 0; y < h; y += 1) {
      for (let x = 0; x < w; x += 1) {
        const so = ((top + y) * info.width + (left + x)) * 4;
        const o = (y * w + x) * 4;
        raw[o] = pixels[so];
        raw[o + 1] = pixels[so + 1];
        raw[o + 2] = pixels[so + 2];
        raw[o + 3] = pixels[so + 3];
      }
    }
    const side = Math.max(w, h, 32);
    const png = await sharp(raw, { raw: { width: w, height: h, channels: 4 } })
      .extend({
        top: Math.floor((side - h) / 2),
        bottom: Math.ceil((side - h) / 2),
        left: Math.floor((side - w) / 2),
        right: Math.ceil((side - w) / 2),
        background: { r: 0, g: 0, b: 0, alpha: 0 },
      })
      .png()
      .toBuffer();
    const name = `09_p${i}.png`;
    writeBoth(name, png);
    ensureMeta(path.join(ART, name));
    ensureMeta(path.join(RES, name));
    console.log("shard", name, `${w}x${h}`, "fill", g.fill.toFixed(2));
  }
}

async function main() {
  await fixHover();
  await sliceShards();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
