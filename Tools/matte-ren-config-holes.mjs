import sharp from "sharp";

const src = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/Characters/Ren/School/ren_config_pose_v1.png";

function luma(data, i) {
  const o = i * 4;
  return (data[o] + data[o + 1] + data[o + 2]) / 3;
}

function sat(data, i) {
  const o = i * 4;
  return Math.max(data[o], data[o + 1], data[o + 2]) - Math.min(data[o], data[o + 1], data[o + 2]);
}

function isGrey(data, i) {
  return data[i * 4 + 3] >= 8 && sat(data, i) <= 18 && luma(data, i) >= 120;
}

const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const w = info.width;
const h = info.height;
const seen = Buffer.alloc(w * h);
const out = Buffer.from(data);
let removed = 0;

for (let y = 0; y < h; y++) {
  for (let x = 0; x < w; x++) {
    const id = y * w + x;
    if (seen[id] || !isGrey(data, id)) continue;
    const q = [id];
    seen[id] = 1;
    const pix = [];
    let nNavy = 0;
    let nEdge = 0;
    let sum = 0;
    let sum2 = 0;
    while (q.length) {
      const cur = q.pop();
      pix.push(cur);
      const L = luma(data, cur);
      sum += L;
      sum2 += L * L;
      const cx = cur % w;
      const cy = (cur / w) | 0;
      const nbs = [
        [1, 0],
        [-1, 0],
        [0, 1],
        [0, -1],
      ];
      for (const [dx, dy] of nbs) {
        const nx = cx + dx;
        const ny = cy + dy;
        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
        const nid = ny * w + nx;
        if (isGrey(data, nid)) {
          if (!seen[nid]) {
            seen[nid] = 1;
            q.push(nid);
          }
          continue;
        }
        nEdge++;
        const o = nid * 4;
        if (data[o + 3] >= 8 && luma(data, nid) < 90) nNavy++;
      }
    }

    const n = pix.length;
    if (n < 40) continue;
    const mean = sum / n;
    const std = Math.sqrt(Math.max(0, sum2 / n - mean * mean));
    const navyPct = nNavy / Math.max(1, nEdge);
    const leftover =
      (mean >= 232 && std <= 16) ||
      (mean >= 225 && std <= 25 && navyPct >= 0.7) ||
      (n < 3500 && mean >= 228 && navyPct >= 0.55);

    if (!leftover) continue;

    for (const p of pix) {
      const o = p * 4;
      out[o] = 0;
      out[o + 1] = 0;
      out[o + 2] = 0;
      out[o + 3] = 0;
      removed++;
    }
  }
}

function isFringe(i) {
  const o = i * 4;
  if (out[o + 3] < 8) return false;
  const r = out[o];
  const g = out[o + 1];
  const b = out[o + 2];
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const avg = (r + g + b) / 3;
  const s = max - min;
  return s <= 14 && avg >= 155;
}

function transNeighborCount(i) {
  const x = i % w;
  const y = (i / w) | 0;
  let n = 0;
  for (let dy = -1; dy <= 1; dy++) {
    for (let dx = -1; dx <= 1; dx++) {
      if (dx === 0 && dy === 0) continue;
      const nx = x + dx;
      const ny = y + dy;
      if (nx < 0 || ny < 0 || nx >= w || ny >= h) {
        n++;
        continue;
      }
      if (out[(ny * w + nx) * 4 + 3] < 8) n++;
    }
  }
  return n;
}

let fringe = 0;
for (let pass = 0; pass < 3; pass++) {
  const kill = [];
  for (let i = 0; i < w * h; i++) {
    if (isFringe(i) && transNeighborCount(i) >= 4) kill.push(i);
  }
  if (kill.length === 0) break;
  for (const i of kill) {
    const o = i * 4;
    out[o] = 0;
    out[o + 1] = 0;
    out[o + 2] = 0;
    out[o + 3] = 0;
    fringe++;
  }
}

for (let i = 0; i < w * h; i++) {
  if (out[i * 4 + 3] === 0) {
    out[i * 4] = 0;
    out[i * 4 + 1] = 0;
    out[i * 4 + 2] = 0;
  }
}

await sharp(out, { raw: { width: w, height: h, channels: 4 } })
  .png({ compressionLevel: 9 })
  .toFile(src);

console.log(JSON.stringify({ removed, fringe, size: `${w}x${h}` }));
