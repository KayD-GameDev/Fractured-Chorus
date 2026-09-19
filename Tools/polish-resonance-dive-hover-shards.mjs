import fs from "fs";
import path from "path";
import crypto from "crypto";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";
const OUT_W = 1536;
const OUT_H = 672;
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

function dilate(mask, width, height, radius) {
  const n = width * height;
  const out = Buffer.alloc(n);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      let m = 0;
      for (let dy = -radius; dy <= radius; dy += 1) {
        const ny = y + dy;
        if (ny < 0 || ny >= height) continue;
        for (let dx = -radius; dx <= radius; dx += 1) {
          const nx = x + dx;
          if (nx < 0 || nx >= width) continue;
          const v = mask[ny * width + nx];
          if (v > m) m = v;
        }
      }
      out[y * width + x] = m;
    }
  }
  return out;
}

function stripHoverHalo(pixels, width, height) {
  const n = width * height;
  const core = Buffer.alloc(n);
  for (let i = 0; i < n; i += 1) {
    const o = i * 4;
    if (pixels[o + 3] < 12) continue;
    const L = lum(pixels[o], pixels[o + 1], pixels[o + 2]);
    const C = chroma(pixels[o], pixels[o + 1], pixels[o + 2]);
    if (L > 52 || C > 42) core[i] = 255;
  }
  const keep = dilate(core, width, height, 1);
  for (let i = 0; i < n; i += 1) {
    const o = i * 4;
    const L = lum(pixels[o], pixels[o + 1], pixels[o + 2]);
    const C = chroma(pixels[o], pixels[o + 1], pixels[o + 2]);
    if (!keep[i] || (L < 32 && C < 30)) {
      pixels[o] = 0;
      pixels[o + 1] = 0;
      pixels[o + 2] = 0;
      pixels[o + 3] = 0;
    }
  }
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

async function toPng(rgba, width, height) {
  return sharp(rgba, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

function ensureMeta(absPng) {
  const metaPath = absPng + ".meta";
  if (fs.existsSync(metaPath)) return;
  const template = fs.readFileSync(path.join(ART, "09_p0.png.meta"), "utf8");
  const guid = crypto.randomBytes(16).toString("hex");
  const spriteId = guid.slice(0, 16) + "8000000000000000";
  const body = template
    .replace(/guid: [a-f0-9]+/, `guid: ${guid}`)
    .replace(/spriteID: [a-f0-9]+/, `spriteID: ${spriteId}`);
  fs.writeFileSync(metaPath, body);
}

function components(pixels, width, height) {
  const n = width * height;
  const seen = Buffer.alloc(n);
  const groups = [];
  const stack = [];
  for (let i = 0; i < n; i += 1) {
    if (seen[i] || pixels[i * 4 + 3] < 20) continue;
    const L = lum(pixels[i * 4], pixels[i * 4 + 1], pixels[i * 4 + 2]);
    const C = chroma(pixels[i * 4], pixels[i * 4 + 1], pixels[i * 4 + 2]);
    if (L < 18 && C < 14) continue;
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
    if (cells.length < 180 || cells.length > 12000) continue;
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
    if (bw > width * 0.18 || bh > height * 0.22) continue;
    groups.push({ cells, minX, minY, maxX, maxY, bw, bh, area: cells.length });
  }
  groups.sort((a, b) => a.area - b.area);
  return groups;
}

async function lockHover() {
  const { data, info } = await sharp(fs.readFileSync(path.join(SRC, "resonance_dive_state_hover_v5.png")))
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  keyBackdropOnly(pixels, info.width, info.height);
  stripHoverHalo(pixels, info.width, info.height);
  const box = bbox(pixels, info.width, info.height);
  if (!box) throw new Error("hover empty");
  const pad = 40;
  const left = Math.max(0, box.minX - pad);
  const top = Math.max(0, box.minY - pad);
  const cropW = Math.min(info.width - left, box.w + pad * 2);
  const cropH = Math.min(info.height - top, box.h + pad * 2);
  const cropped = await sharp(pixels, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .extract({ left, top, width: cropW, height: cropH })
    .resize(OUT_W, OUT_H, { fit: "fill" })
    .png()
    .toBuffer();
  writeBoth("state_hover.png", cropped);
  console.log("hover", { box, left, top, cropW, cropH });
}

async function sliceShards() {
  const sources = [
    path.join(SRC, "resonance_dive_shards_v1.png"),
  ];
  const found = [];
  for (const file of sources) {
    if (!fs.existsSync(file)) continue;
    const { data, info } = await sharp(fs.readFileSync(file)).ensureAlpha().raw().toBuffer({
      resolveWithObject: true,
    });
    const pixels = Buffer.from(data);
    keyBackdropOnly(pixels, info.width, info.height);
    const groups = components(pixels, info.width, info.height);
    console.log("shard src", path.basename(file), groups.length);
    for (const g of groups) found.push({ pixels, width: info.width, height: info.height, ...g });
  }
  found.sort((a, b) => a.area - b.area);
  const picked = found.slice(0, SHARD_COUNT);
  if (picked.length < SHARD_COUNT) {
    throw new Error(`only ${picked.length} shards`);
  }
  for (let i = 0; i < SHARD_COUNT; i += 1) {
    const g = picked[i];
    const pad = 4;
    const left = Math.max(0, g.minX - pad);
    const top = Math.max(0, g.minY - pad);
    const w = Math.min(g.width - left, g.bw + pad * 2);
    const h = Math.min(g.height - top, g.bh + pad * 2);
    const raw = Buffer.alloc(w * h * 4);
    for (let y = 0; y < h; y += 1) {
      for (let x = 0; x < w; x += 1) {
        const sx = left + x;
        const sy = top + y;
        const so = (sy * g.width + sx) * 4;
        const o = (y * w + x) * 4;
        raw[o] = g.pixels[so];
        raw[o + 1] = g.pixels[so + 1];
        raw[o + 2] = g.pixels[so + 2];
        raw[o + 3] = g.pixels[so + 3];
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
    console.log("shard", name, `${w}x${h}`);
  }
}

async function main() {
  await lockHover();
  await sliceShards();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
