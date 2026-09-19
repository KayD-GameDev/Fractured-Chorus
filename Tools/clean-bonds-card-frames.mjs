import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/Downloads/Bond UI Pack/Bonds_UI/Generated_Source";
const OUT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/Bonds/Pack";

const FILES = [
  { file: "08_Card_Character_Normal.png", kind: "normal" },
  { file: "09_Card_Character_Selected.png", kind: "selected" },
];

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function dist(a, b) {
  const dr = a[0] - b[0];
  const dg = a[1] - b[1];
  const db = a[2] - b[2];
  return Math.sqrt(dr * dr + dg * dg + db * db);
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

function isOuterBackground(r, g, b, seeds) {
  if (chroma(r, g, b) > 22) return false;
  let best = 255;
  for (let i = 0; i < seeds.length; i++) {
    const d = dist([r, g, b], seeds[i]);
    if (d < best) best = d;
  }
  return best <= 28;
}

function floodFromEdges(data, width, height, seeds) {
  const n = width * height;
  const bg = new Uint8Array(n);
  for (let idx = 0; idx < n; idx++) {
    const i = idx * 4;
    if (isOuterBackground(data[i], data[i + 1], data[i + 2], seeds)) bg[idx] = 1;
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
  let punched = 0;
  while (head < tail) {
    const idx = queue[head++];
    const x = idx % width;
    const y = (idx / width) | 0;
    const i = idx * 4;
    data[i] = 0;
    data[i + 1] = 0;
    data[i + 2] = 0;
    data[i + 3] = 0;
    punched += 1;
    tryPush(x - 1, y);
    tryPush(x + 1, y);
    tryPush(x, y - 1);
    tryPush(x, y + 1);
  }
  return punched;
}

function isBezel(r, g, b, a) {
  if (a < 16) return false;
  const c = chroma(r, g, b);
  const y = luma(r, g, b);
  if (c > 48 && b > r + 4) return true;
  if (c > 32 && b > r + 10 && y < 195) return true;
  return false;
}

function isWindowFill(r, g, b, a) {
  if (a < 16) return true;
  if (isBezel(r, g, b, a)) return false;
  const y = luma(r, g, b);
  const c = chroma(r, g, b);
  if (c <= 42 && y >= 70) return true;
  if (y >= 168 && c <= 55 && b >= r) return true;
  return false;
}

function markNameplate(data, width, height, kind) {
  const n = width * height;
  const keep = new Uint8Array(n);
  if (kind === "normal") {
    for (let idx = 0; idx < n; idx++) {
      const y = (idx / width) | 0;
      if (y < height * 0.55) continue;
      const i = idx * 4;
      const a = data[i + 3];
      if (a < 16) continue;
      const r = data[i];
      const g = data[i + 1];
      const b = data[i + 2];
      const yv = luma(r, g, b);
      if (yv < 110 && b >= r && chroma(r, g, b) > 8) keep[idx] = 1;
    }
    return keep;
  }

  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let best = null;
  for (let start = 0; start < n; start++) {
    if (seen[start]) continue;
    const i0 = start * 4;
    if (data[i0 + 3] < 16) continue;
    const y0 = luma(data[i0], data[i0 + 1], data[i0 + 2]);
    const c0 = chroma(data[i0], data[i0 + 1], data[i0 + 2]);
    const row0 = (start / width) | 0;
    if (row0 < height * 0.52 || y0 < 220 || c0 > 22) continue;
    let head = 0;
    let tail = 0;
    queue[tail++] = start;
    seen[start] = 1;
    let minY = height;
    let maxY = -1;
    while (head < tail) {
      const idx = queue[head++];
      const row = (idx / width) | 0;
      if (row < minY) minY = row;
      if (row > maxY) maxY = row;
      const x = idx % width;
      const tryPush = (nx, ny) => {
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;
        const nidx = ny * width + nx;
        if (seen[nidx]) return;
        const i = nidx * 4;
        if (data[i + 3] < 16) return;
        const yv = luma(data[i], data[i + 1], data[i + 2]);
        const c = chroma(data[i], data[i + 1], data[i + 2]);
        if (yv < 220 || c > 22) return;
        seen[nidx] = 1;
        queue[tail++] = nidx;
      };
      tryPush(x - 1, row);
      tryPush(x + 1, row);
      tryPush(x, row - 1);
      tryPush(x, row + 1);
    }
    if (!best || tail > best.size) {
      best = { size: tail, cells: queue.slice(0, tail), minY, maxY };
    }
  }
  if (best && best.size > width * 20) {
    for (let q = 0; q < best.cells.length; q++) keep[best.cells[q]] = 1;
  }
  return keep;
}

function punchWindow(data, width, height, keep) {
  const n = width * height;
  const cand = new Uint8Array(n);
  for (let idx = 0; idx < n; idx++) {
    if (keep[idx]) continue;
    const i = idx * 4;
    if (isWindowFill(data[i], data[i + 1], data[i + 2], data[i + 3])) cand[idx] = 1;
  }

  let minX = width;
  let minY = height;
  let maxX = -1;
  let maxY = -1;
  for (let idx = 0; idx < n; idx++) {
    if (data[idx * 4 + 3] < 16) continue;
    const x = idx % width;
    const y = (idx / width) | 0;
    if (x < minX) minX = x;
    if (y < minY) minY = y;
    if (x > maxX) maxX = x;
    if (y > maxY) maxY = y;
  }
  const seeds = [
    [((minX + maxX) / 2) | 0, ((minY + maxY * 2) / 3) | 0],
    [((minX + maxX) / 2) | 0, ((minY * 2 + maxY) / 3) | 0],
    [((minX * 2 + maxX) / 3) | 0, ((minY + maxY) / 2) | 0],
    [((minX + maxX * 2) / 3) | 0, ((minY + maxY) / 2) | 0],
  ];

  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let head = 0;
  let tail = 0;
  const tryPush = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const idx = y * width + x;
    if (seen[idx] || !cand[idx] || keep[idx]) return;
    seen[idx] = 1;
    queue[tail++] = idx;
  };
  for (const [sx, sy] of seeds) tryPush(sx, sy);

  let punched = 0;
  while (head < tail) {
    const idx = queue[head++];
    const x = idx % width;
    const y = (idx / width) | 0;
    const i = idx * 4;
    if (data[i + 3] > 0) {
      data[i] = 0;
      data[i + 1] = 0;
      data[i + 2] = 0;
      data[i + 3] = 0;
      punched += 1;
    }
    tryPush(x - 1, y);
    tryPush(x + 1, y);
    tryPush(x, y - 1);
    tryPush(x, y + 1);
  }
  return punched;
}

function punchCheckerSpeckles(data, width, height, keep) {
  let punched = 0;
  for (let idx = 0; idx < width * height; idx++) {
    if (keep[idx]) continue;
    const i = idx * 4;
    const a = data[i + 3];
    if (a < 16) continue;
    if (isBezel(data[i], data[i + 1], data[i + 2], a)) continue;
    const y = luma(data[i], data[i + 1], data[i + 2]);
    const c = chroma(data[i], data[i + 1], data[i + 2]);
    if (c <= 32 && y >= 70 && y <= 242) {
      data[i] = 0;
      data[i + 1] = 0;
      data[i + 2] = 0;
      data[i + 3] = 0;
      punched += 1;
    }
  }
  return punched;
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
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(width - 1, maxX + pad);
  maxY = Math.min(height - 1, maxY + pad);
  return { left: minX, top: minY, width: maxX - minX + 1, height: maxY - minY + 1 };
}

async function cleanOne({ file, kind }) {
  const src = path.join(SRC, file);
  const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  const seeds = collectSeeds(pixels, info.width, info.height);
  const outer = floodFromEdges(pixels, info.width, info.height, seeds);
  const keep = markNameplate(pixels, info.width, info.height, kind);
  const windowPunched = punchWindow(pixels, info.width, info.height, keep);
  const speckles = punchCheckerSpeckles(pixels, info.width, info.height, keep);
  let leftoverGlass = 0;
  for (let idx = 0; idx < info.width * info.height; idx++) {
    if (keep[idx]) continue;
    const i = idx * 4;
    if (!isWindowFill(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3])) continue;
    if (isBezel(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3])) continue;
    pixels[i] = 0;
    pixels[i + 1] = 0;
    pixels[i + 2] = 0;
    pixels[i + 3] = 0;
    leftoverGlass += 1;
  }

  for (let i = 0; i < pixels.length; i += 4) {
    if (pixels[i + 3] === 0) {
      pixels[i] = 0;
      pixels[i + 1] = 0;
      pixels[i + 2] = 0;
    }
  }

  const box = contentBox(pixels, info.width, info.height, 8);
  const cropped = await sharp(pixels, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .extract(box)
    .png()
    .toBuffer();

  const outPath = path.join(OUT, file);
  fs.writeFileSync(outPath, cropped);

  let leftoverWhite = 0;
  let leftoverGray = 0;
  const out = await sharp(cropped).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < out.data.length; i += 4) {
    const a = out.data[i + 3];
    if (a < 16) continue;
    const y = luma(out.data[i], out.data[i + 1], out.data[i + 2]);
    const c = chroma(out.data[i], out.data[i + 1], out.data[i + 2]);
    if (c <= 16 && y >= 230) leftoverWhite += 1;
    if (c <= 28 && y >= 80 && y <= 190) leftoverGray += 1;
  }

  return {
    file,
    from: [info.width, info.height],
    to: [box.width, box.height],
    outer,
    windowPunched,
    speckles,
    leftoverGlass,
    leftoverWhite,
    leftoverGray,
  };
}

const report = [];
for (const entry of FILES) {
  report.push(await cleanOne(entry));
}
console.log(JSON.stringify(report, null, 2));
