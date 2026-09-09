import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const DIR = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit";
const FILES = ["ui_stat_slot_skill_square_v1.png", "ui_stat_slot_skill_locked_v1.png"];
const APPLY = process.argv.includes("--apply");
const RIM_ONLY = process.argv.includes("--rim-only");
const COLOR_RIM = process.argv.includes("--color-rim");
const PREVIEW_DIR = "D:/Fractured-Chorus1/Tools/_preview";

function luma(r, g, b) {
  return (r + g + b) / 3;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function isWhiteish(r, g, b, a, lumaMin, chromaMax) {
  if (a < 8) return false;
  return luma(r, g, b) >= lumaMin && chroma(r, g, b) <= chromaMax;
}

function sample(px, i) {
  const o = i * 4;
  return { r: px[o], g: px[o + 1], b: px[o + 2], a: px[o + 3] };
}

function isStrongMatte(r, g, b, a) {
  if (a < 12) return true;
  return luma(r, g, b) >= 247 && chroma(r, g, b) <= 12;
}

function isWeakHalo(r, g, b, a) {
  if (a < 12) return true;
  const L = luma(r, g, b);
  const C = chroma(r, g, b);
  if (C >= 36) return false;
  return L >= 232 && C <= 28;
}

function floodBg(px, w, h) {
  const n = w * h;
  const mask = new Uint8Array(n);
  const dist = new Int16Array(n);
  dist.fill(32767);
  const q = new Int32Array(n);
  let qs = 0;
  let qe = 0;
  const haloPx = 16;

  const seed = (x, y) => {
    const i = y * w + x;
    const { r, g, b, a } = sample(px, i);
    if (mask[i] || !isStrongMatte(r, g, b, a)) return;
    mask[i] = 1;
    dist[i] = 0;
    q[qe++] = i;
  };

  for (let x = 0; x < w; x++) {
    seed(x, 0);
    seed(x, h - 1);
  }
  for (let y = 0; y < h; y++) {
    seed(0, y);
    seed(w - 1, y);
  }

  while (qs < qe) {
    const i = q[qs++];
    const x = i % w;
    const y = (i / w) | 0;
    const d = dist[i];
    const nb = [
      x > 0 ? i - 1 : -1,
      x < w - 1 ? i + 1 : -1,
      y > 0 ? i - w : -1,
      y < h - 1 ? i + w : -1,
    ];
    for (const j of nb) {
      if (j < 0 || mask[j]) continue;
      const { r, g, b, a } = sample(px, j);
      if (isStrongMatte(r, g, b, a)) {
        mask[j] = 1;
        dist[j] = 0;
        q[qe++] = j;
        continue;
      }
      if (d < haloPx && isWeakHalo(r, g, b, a)) {
        mask[j] = 1;
        dist[j] = d + 1;
        q[qe++] = j;
      }
    }
  }
  return mask;
}

function transNeighbors(px, w, h, i, aCut = 10) {
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
      if (px[(ny * w + nx) * 4 + 3] < aCut) n++;
    }
  }
  return n;
}

function unmatteWhite(r, g, b, a) {
  if (a <= 0) return [0, 0, 0, 0];
  const af = a / 255;
  const u = (c) => Math.max(0, Math.min(255, Math.round((c - 255 * (1 - af)) / af)));
  return [u(r), u(g), u(b), a];
}

function distFromTransparent(px, w, h, aCut = 8) {
  const n = w * h;
  const dist = new Int16Array(n);
  dist.fill(32767);
  const q = new Int32Array(n);
  let qs = 0;
  let qe = 0;
  for (let i = 0; i < n; i++) {
    if (px[i * 4 + 3] >= aCut) continue;
    dist[i] = 0;
    q[qe++] = i;
  }
  while (qs < qe) {
    const i = q[qs++];
    const x = i % w;
    const y = (i / w) | 0;
    const d = dist[i];
    const nb = [
      x > 0 ? i - 1 : -1,
      x < w - 1 ? i + 1 : -1,
      y > 0 ? i - w : -1,
      y < h - 1 ? i + w : -1,
    ];
    for (const j of nb) {
      if (j < 0 || dist[j] <= d + 1) continue;
      dist[j] = d + 1;
      q[qe++] = j;
    }
  }
  return dist;
}

function punch(px, o) {
  px[o] = 0;
  px[o + 1] = 0;
  px[o + 2] = 0;
  px[o + 3] = 0;
}

function audit(px, w, h) {
  let opaque = 0;
  let fringe = 0;
  let edgeWhite = 0;
  let borderMaxA = 0;
  const mid = ((h >> 1) * w + (w >> 1)) * 4;
  const center = {
    r: px[mid],
    g: px[mid + 1],
    b: px[mid + 2],
    a: px[mid + 3],
  };
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const i = y * w + x;
      const o = i * 4;
      const a = px[o + 3];
      const onCanvas = x < 2 || y < 2 || x >= w - 2 || y >= h - 2;
      if (onCanvas && a > borderMaxA) borderMaxA = a;
      if (a === 0) continue;
      opaque++;
      const r = px[o];
      const g = px[o + 1];
      const b = px[o + 2];
      const tn = transNeighbors(px, w, h, i);
      if (tn >= 2 && isWhiteish(r, g, b, a, 210, 40)) fringe++;
      if (tn >= 1 && luma(r, g, b) >= 230 && chroma(r, g, b) <= 28) edgeWhite++;
    }
  }
  return { w, h, opaque, fringe, edgeWhite, borderMaxA, center };
}

function contractOuter(px, w, h, radius) {
  const dist = distFromTransparent(px, w, h);
  let n = 0;
  for (let i = 0; i < w * h; i++) {
    if (dist[i] === 0 || dist[i] > radius) continue;
    punch(px, i * 4);
    n++;
  }
  return n;
}

function punchInnerGlass(px, w, h) {
  const start = ((h >> 1) * w + (w >> 1));
  const { r, g, b, a } = sample(px, start);
  if (a < 8 || luma(r, g, b) < 200 || chroma(r, g, b) > 40) return 0;
  const n = w * h;
  const seen = new Uint8Array(n);
  const q = new Int32Array(n);
  let qs = 0;
  let qe = 0;
  seen[start] = 1;
  q[qe++] = start;
  const glass = (i) => {
    const s = sample(px, i);
    if (s.a < 8) return false;
    return luma(s.r, s.g, s.b) >= 190 && chroma(s.r, s.g, s.b) <= 45;
  };
  while (qs < qe) {
    const i = q[qs++];
    const x = i % w;
    const y = (i / w) | 0;
    const nb = [
      x > 0 ? i - 1 : -1,
      x < w - 1 ? i + 1 : -1,
      y > 0 ? i - w : -1,
      y < h - 1 ? i + w : -1,
    ];
    for (const j of nb) {
      if (j < 0 || seen[j] || !glass(j)) continue;
      seen[j] = 1;
      q[qe++] = j;
    }
  }
  let punched = 0;
  for (let i = 0; i < n; i++) {
    if (!seen[i]) continue;
    punch(px, i * 4);
    punched++;
  }
  return punched;
}

function cleanPaleRim(px, w, h, maxDist) {
  const holeDist = distFromTransparent(px, w, h);
  let innerRim = 0;
  for (let pass = 0; pass < 4; pass++) {
    const kill = [];
    for (let i = 0; i < w * h; i++) {
      if (holeDist[i] === 0 || holeDist[i] > maxDist) continue;
      const o = i * 4;
      if (px[o + 3] < 8) continue;
      const L = luma(px[o], px[o + 1], px[o + 2]);
      const C = chroma(px[o], px[o + 1], px[o + 2]);
      if (C < 70 && L >= 170 && transNeighbors(px, w, h, i) >= 1) kill.push(i);
    }
    if (kill.length === 0) break;
    for (const i of kill) {
      punch(px, i * 4);
      innerRim++;
    }
  }
  return innerRim;
}

function colorDefringe(px, w, h, ring = 3) {
  const dist = distFromTransparent(px, w, h);
  const src = Buffer.from(px);
  let n = 0;
  for (let i = 0; i < w * h; i++) {
    if (dist[i] === 0 || dist[i] > ring) continue;
    const o = i * 4;
    if (src[o + 3] < 8) continue;
    const x = i % w;
    const y = (i / w) | 0;
    let sr = 0;
    let sg = 0;
    let sb = 0;
    let c = 0;
    for (let dy = -6; dy <= 6; dy++) {
      for (let dx = -6; dx <= 6; dx++) {
        const nx = x + dx;
        const ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
        const j = ny * w + nx;
        if (dist[j] <= ring) continue;
        const jo = j * 4;
        if (src[jo + 3] < 180) continue;
        if (chroma(src[jo], src[jo + 1], src[jo + 2]) < 35) continue;
        sr += src[jo];
        sg += src[jo + 1];
        sb += src[jo + 2];
        c++;
      }
    }
    if (c === 0) continue;
    px[o] = Math.round(sr / c);
    px[o + 1] = Math.round(sg / c);
    px[o + 2] = Math.round(sb / c);
    n++;
  }
  return n;
}

function defringe(px, w, h, punchInner = false, rimOnly = false) {
  if (rimOnly) {
    const innerRim = COLOR_RIM ? 0 : cleanPaleRim(px, w, h, 4);
    const colored = COLOR_RIM ? colorDefringe(px, w, h, 3) : 0;
    for (let i = 0; i < w * h; i++) {
      if (px[i * 4 + 3] === 0) punch(px, i * 4);
    }
    return { punched: 0, fringe: 0, contracted: 0, inner: 0, innerRim, colored };
  }
  const bg = floodBg(px, w, h);
  let punched = 0;
  for (let i = 0; i < w * h; i++) {
    if (!bg[i]) continue;
    punch(px, i * 4);
    punched++;
  }

  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const a = px[o + 3];
    if (a === 0) continue;
    if (transNeighbors(px, w, h, i) < 1) continue;
    const [r, g, b, na] = unmatteWhite(px[o], px[o + 1], px[o + 2], a);
    px[o] = r;
    px[o + 1] = g;
    px[o + 2] = b;
    px[o + 3] = na;
  }

  let fringe = 0;
  const rimDist = distFromTransparent(px, w, h);
  for (let pass = 0; pass < 8; pass++) {
    const kill = [];
    for (let i = 0; i < w * h; i++) {
      const o = i * 4;
      const a = px[o + 3];
      if (a < 8) continue;
      if (rimDist[i] > 6) continue;
      const r = px[o];
      const g = px[o + 1];
      const b = px[o + 2];
      const tn = transNeighbors(px, w, h, i);
      const L = luma(r, g, b);
      const C = chroma(r, g, b);
      const silhouetteWhite = tn >= 1 && C < 70 && L >= 175;
      const softHalo = tn >= 3 && a < 210 && L >= 165 && C <= 55;
      const leftoverMatte = tn >= 1 && a < 90 && L >= 140 && C <= 55;
      if (silhouetteWhite || softHalo || leftoverMatte) kill.push(i);
    }
    if (kill.length === 0) break;
    for (const i of kill) {
      punch(px, i * 4);
      fringe++;
    }
  }

  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    if (px[o + 3] === 0) {
      punch(px, o);
      continue;
    }
    if (transNeighbors(px, w, h, i) < 1) continue;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    const a = px[o + 3];
    if (luma(r, g, b) < 200 || chroma(r, g, b) > 55) continue;
    const [ur, ug, ub] = unmatteWhite(r, g, b, a);
    px[o] = ur;
    px[o + 1] = ug;
    px[o + 2] = ub;
  }

  const contracted = contractOuter(px, w, h, 2);
  const inner = punchInner ? punchInnerGlass(px, w, h) : 0;
  const innerRim = inner > 0 ? cleanPaleRim(px, w, h, 4) : 0;
  for (let i = 0; i < w * h; i++) {
    if (px[i * 4 + 3] === 0) punch(px, i * 4);
  }
  return { punched, fringe, contracted, inner, innerRim };
}

async function previewOnDark(px, w, h, dest) {
  const bg = Buffer.alloc(w * h * 4);
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const a = px[o + 3] / 255;
    bg[o] = Math.round(px[o] * a + 18 * (1 - a));
    bg[o + 1] = Math.round(px[o + 1] * a + 16 * (1 - a));
    bg[o + 2] = Math.round(px[o + 2] * a + 28 * (1 - a));
    bg[o + 3] = 255;
  }
  await sharp(bg, { raw: { width: w, height: h, channels: 4 } })
    .png()
    .toFile(dest);
}

const reports = [];
for (const name of FILES) {
  const file = path.join(DIR, name);
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const px = Buffer.from(data);
  const before = audit(px, info.width, info.height);
  const stats = APPLY
    ? defringe(px, info.width, info.height, name.includes("square"), RIM_ONLY)
    : { punched: 0, fringe: 0 };
  const after = audit(px, info.width, info.height);
  if (APPLY) {
    fs.mkdirSync(PREVIEW_DIR, { recursive: true });
    const staged = path.join(PREVIEW_DIR, name);
    await sharp(px, { raw: { width: info.width, height: info.height, channels: 4 } })
      .png({ compressionLevel: 9 })
      .toFile(staged);
    try {
      fs.copyFileSync(staged, file);
    } catch {
      const { spawnSync } = await import("node:child_process");
      const copied = spawnSync(
        "powershell",
        ["-NoProfile", "-Command", `Copy-Item -LiteralPath '${staged}' -Destination '${file}' -Force`],
        { encoding: "utf8" },
      );
      if (copied.status !== 0) {
        reports.push({ name, error: copied.stderr || copied.stdout || "copy failed", staged });
        await previewOnDark(px, info.width, info.height, path.join(PREVIEW_DIR, name.replace(".png", "_dark.png")));
        continue;
      }
    }
    await previewOnDark(px, info.width, info.height, path.join(PREVIEW_DIR, name.replace(".png", "_dark.png")));
  }
  reports.push({ name, before, stats, after });
}

console.log(JSON.stringify({ apply: APPLY, rimOnly: RIM_ONLY, reports }, null, 2));
