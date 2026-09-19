import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const SRC =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_Yes_No_Button-46e65af0-eb27-4a28-b6f8-2fa45e654094.jpg";
const OUT_DIR = "d:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/Narrative";
const REF_DIR = path.join(OUT_DIR, "_ref");

const BUTTONS = [
  {
    name: "prologue_choice_yes_normal_v1.png",
    col: 0,
    row: 0,
    guid: "4c8e1a2b9d704f6a8e3c5b1d7f0a92e4",
    fill: [236, 242, 250],
    fillHi: [248, 252, 255],
    stroke: [36, 48, 72],
    accent: [28, 40, 64],
    glow: [180, 220, 255],
    glowRadius: 0,
  },
  {
    name: "prologue_choice_no_normal_v1.png",
    col: 1,
    row: 0,
    guid: "1f6a0c8d4e274b9a8c5d2e0f1b7a3648",
    fill: [48, 56, 74],
    fillHi: [62, 72, 92],
    stroke: [88, 102, 128],
    accent: [230, 236, 248],
    glow: [170, 180, 230],
    glowRadius: 0,
  },
  {
    name: "prologue_choice_yes_selected_v1.png",
    col: 0,
    row: 0,
    guid: "7b3d9e6c1a254890b2f4c8d0e6a1b573",
    fill: [244, 250, 255],
    fillHi: [255, 255, 255],
    stroke: [70, 180, 255],
    accent: [40, 150, 230],
    glow: [90, 200, 255],
    glowRadius: 18,
  },
  {
    name: "prologue_choice_no_selected_v1.png",
    col: 1,
    row: 0,
    guid: "9e2c5a7b0d184f3e8a6c1b4d5f7e9021",
    fill: [32, 36, 58],
    fillHi: [44, 48, 78],
    stroke: [170, 140, 255],
    accent: [210, 200, 255],
    glow: [160, 130, 255],
    glowRadius: 18,
  },
];

function writeSpriteMeta(filePath, guid) {
  const metaPath = filePath + ".meta";
  if (fs.existsSync(metaPath)) {
    return;
  }

  fs.writeFileSync(
    metaPath,
    `fileFormatVersion: 2
guid: ${guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: ${guid}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`,
    "utf8"
  );
}

function luma(r, g, b) {
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function isCheckerLike(r, g, b) {
  const l = luma(r, g, b);
  const c = chroma(r, g, b);
  const bluePush = b - r;
  if (c > 28) return false;
  if (bluePush > 18) return false;
  return l >= 112 && l <= 210;
}

function floodBackground(data, width, height) {
  const n = width * height;
  const bg = new Uint8Array(n);
  const q = new Int32Array(n);
  let head = 0;
  let tail = 0;

  const tryPush = (i) => {
    const o = i * 4;
    if (bg[i]) return;
    if (!isCheckerLike(data[o], data[o + 1], data[o + 2])) return;
    bg[i] = 1;
    q[tail++] = i;
  };

  for (let x = 0; x < width; x++) {
    tryPush(x);
    tryPush((height - 1) * width + x);
  }
  for (let y = 0; y < height; y++) {
    tryPush(y * width);
    tryPush(y * width + width - 1);
  }

  const dirs = [-1, 1, -width, width, -width - 1, -width + 1, width - 1, width + 1];
  while (head < tail) {
    const i = q[head++];
    const x = i % width;
    const y = (i / width) | 0;
    for (const d of dirs) {
      const ni = i + d;
      if (ni < 0 || ni >= n) continue;
      const nx = ni % width;
      const ny = (ni / width) | 0;
      if (Math.abs(nx - x) > 1 || Math.abs(ny - y) > 1) continue;
      tryPush(ni);
    }
  }

  return bg;
}

function fillHoles(mask, width, height) {
  const n = width * height;
  const outside = new Uint8Array(n);
  const q = new Int32Array(n);
  let head = 0;
  let tail = 0;
  const push = (i) => {
    if (outside[i] || mask[i]) return;
    outside[i] = 1;
    q[tail++] = i;
  };
  for (let x = 0; x < width; x++) {
    push(x);
    push((height - 1) * width + x);
  }
  for (let y = 0; y < height; y++) {
    push(y * width);
    push(y * width + width - 1);
  }
  while (head < tail) {
    const i = q[head++];
    const x = i % width;
    const neigh = [i - 1, i + 1, i - width, i + width];
    for (const ni of neigh) {
      if (ni < 0 || ni >= n) continue;
      const nx = ni % width;
      if (Math.abs(nx - x) > 1) continue;
      push(ni);
    }
  }
  const filled = Buffer.from(mask);
  for (let i = 0; i < n; i++) {
    if (!outside[i]) filled[i] = 1;
  }
  return filled;
}

function erode(mask, width, height, radius) {
  const out = Buffer.alloc(mask.length);
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      let ok = 1;
      const x0 = Math.max(0, x - radius);
      const x1 = Math.min(width - 1, x + radius);
      const y0 = Math.max(0, y - radius);
      const y1 = Math.min(height - 1, y + radius);
      for (let yy = y0; yy <= y1 && ok; yy++) {
        for (let xx = x0; xx <= x1; xx++) {
          if (!mask[yy * width + xx]) {
            ok = 0;
            break;
          }
        }
      }
      out[y * width + x] = ok;
    }
  }
  return out;
}

function dilate(mask, width, height, radius) {
  const out = Buffer.alloc(mask.length);
  const n = width * height;
  for (let i = 0; i < n; i++) {
    if (!mask[i]) continue;
    const x = i % width;
    const y = (i / width) | 0;
    const x0 = Math.max(0, x - radius);
    const x1 = Math.min(width - 1, x + radius);
    const y0 = Math.max(0, y - radius);
    const y1 = Math.min(height - 1, y + radius);
    for (let yy = y0; yy <= y1; yy++) {
      for (let xx = x0; xx <= x1; xx++) {
        out[yy * width + xx] = 1;
      }
    }
  }
  return out;
}

function largestComponent(mask, width, height) {
  const n = width * height;
  const seen = new Uint8Array(n);
  const best = new Uint8Array(n);
  let bestCount = 0;
  const q = new Int32Array(n);

  for (let start = 0; start < n; start++) {
    if (!mask[start] || seen[start]) continue;
    let head = 0;
    let tail = 0;
    q[tail++] = start;
    seen[start] = 1;
    const cells = [start];
    while (head < tail) {
      const i = q[head++];
      const x = i % width;
      const y = (i / width) | 0;
      const neigh = [i - 1, i + 1, i - width, i + width];
      for (const ni of neigh) {
        if (ni < 0 || ni >= n || seen[ni] || !mask[ni]) continue;
        const nx = ni % width;
        const ny = (ni / width) | 0;
        if (Math.abs(nx - x) + Math.abs(ny - y) !== 1) continue;
        seen[ni] = 1;
        q[tail++] = ni;
        cells.push(ni);
      }
    }
    if (cells.length > bestCount) {
      bestCount = cells.length;
      best.fill(0);
      for (const i of cells) best[i] = 1;
    }
  }

  return best;
}

function cropCell(src, srcW, x0, y0, cw, ch) {
  const out = Buffer.alloc(cw * ch * 4);
  for (let y = 0; y < ch; y++) {
    src.copy(out, y * cw * 4, ((y0 + y) * srcW + x0) * 4, ((y0 + y) * srcW + x0 + cw) * 4);
  }
  return out;
}

function cropAlpha(data, width, height, pad) {
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (data[(y * width + x) * 4 + 3] > 18) {
        if (x < minX) minX = x;
        if (y < minY) minY = y;
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
      }
    }
  }
  if (maxX < minX) return { data, width, height };
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(width - 1, maxX + pad);
  maxY = Math.min(height - 1, maxY + pad);
  const w = maxX - minX + 1;
  const h = maxY - minY + 1;
  const out = Buffer.alloc(w * h * 4);
  for (let y = 0; y < h; y++) {
    data.copy(out, y * w * 4, ((minY + y) * width + minX) * 4, ((minY + y) * width + minX + w) * 4);
  }
  return { data: out, width: w, height: h };
}

function sampleCheckerTiles(data, width, height, bg, period) {
  const sums = [
    [0, 0, 0, 0],
    [0, 0, 0, 0],
  ];
  const x1 = Math.min(width, 96);
  const y1 = Math.min(height, 64);
  for (let y = 0; y < y1; y++) {
    for (let x = 0; x < x1; x++) {
      const i = y * width + x;
      if (!bg[i]) continue;
      const o = i * 4;
      const r = data[o];
      const g = data[o + 1];
      const b = data[o + 2];
      if (chroma(r, g, b) > 12) continue;
      const tile = (Math.floor(x / period) + Math.floor(y / period)) & 1;
      sums[tile][0] += r;
      sums[tile][1] += g;
      sums[tile][2] += b;
      sums[tile][3] += 1;
    }
  }
  const tiles = sums.map((s) => (s[3] > 8 ? [s[0] / s[3], s[1] / s[3], s[2] / s[3]] : null));
  if (!tiles[0] || !tiles[1] || Math.hypot(tiles[0][0] - tiles[1][0], tiles[0][1] - tiles[1][1], tiles[0][2] - tiles[1][2]) < 12) {
    return [
      [201, 201, 201],
      [145, 145, 145],
    ];
  }
  return tiles;
}

function checkerAt(x, y, period, tiles) {
  const tile = (Math.floor(x / period) + Math.floor(y / period)) & 1;
  return tiles[tile];
}

function processCell(src, srcW, x0, y0, cw, ch, sheetBg, button) {
  const n = cw * ch;
  const keep = new Uint8Array(n);
  for (let i = 0; i < n; i++) {
    const sy = ((i / cw) | 0) + y0;
    const sx = (i % cw) + x0;
    if (!sheetBg[sy * srcW + sx]) keep[i] = 1;
  }

  const captionCut = Math.floor(ch * (y0 > 0 ? 0.80 : 0.86));
  for (let y = captionCut; y < ch; y++) {
    keep.fill(0, y * cw, (y + 1) * cw);
  }

  const blob = largestComponent(keep, cw, ch);
  const core = Buffer.alloc(n);
  let minY = ch;
  let maxY = -1;
  for (let y = 0; y < ch; y++) {
    let minX = cw;
    let maxX = -1;
    const row = y * cw;
    for (let x = 0; x < cw; x++) {
      if (!blob[row + x]) continue;
      if (x < minX) minX = x;
      if (x > maxX) maxX = x;
    }
    if (maxX < minX) continue;
    if (y < minY) minY = y;
    if (y > maxY) maxY = y;
    core.fill(1, row + minX, row + maxX + 1);
  }

  const inner = erode(core, cw, ch, 3);
  const stroke = Buffer.alloc(n);
  for (let i = 0; i < n; i++) {
    if (core[i] && !inner[i]) stroke[i] = 1;
  }

  const glowR = button.glowRadius;
  const glowMask = glowR > 0 ? dilate(core, cw, ch, glowR) : core;
  const dist = distanceFrom(core, cw, ch);
  const out = Buffer.alloc(n * 4);

  for (let i = 0; i < n; i++) {
    const o = i * 4;
    const x = i % cw;
    const y = (i / cw) | 0;
    if (!glowMask[i] && !core[i]) continue;

    if (!core[i]) {
      const d = dist[i];
      const a = Math.max(0, Math.min(170, Math.round(170 * Math.exp(-d / (glowR * 0.38)))));
      if (a <= 8) continue;
      out[o] = button.glow[0];
      out[o + 1] = button.glow[1];
      out[o + 2] = button.glow[2];
      out[o + 3] = a;
      continue;
    }

    const t = minY === maxY ? 0.5 : (y - minY) / (maxY - minY);
    const shine = Math.max(0, 1 - Math.abs(t - 0.32) * 2.4);
    out[o] = Math.round(button.fill[0] * (1 - shine) + button.fillHi[0] * shine);
    out[o + 1] = Math.round(button.fill[1] * (1 - shine) + button.fillHi[1] * shine);
    out[o + 2] = Math.round(button.fill[2] * (1 - shine) + button.fillHi[2] * shine);
    out[o + 3] = 255;
    if (stroke[i]) {
      out[o] = button.stroke[0];
      out[o + 1] = button.stroke[1];
      out[o + 2] = button.stroke[2];
    }
  }

  const body = bbox(core, cw, ch);
  if (body) {
    const cx = body.x0 + Math.round(body.w * 0.16);
    const cy = body.y0 + Math.round(body.h * 0.5);
    drawStar(out, cw, ch, core, cx, cy, Math.max(7, Math.round(body.h * 0.18)), button.accent);
    drawHairline(out, cw, ch, core, cx + Math.round(body.h * 0.16), cy, body.x0 + Math.round(body.w * 0.28), button.accent);
    if (button.glowRadius > 0 || button.col === 1) {
      drawWave(
        out,
        cw,
        ch,
        core,
        body.x0 + Math.round(body.w * 0.72),
        body.x0 + Math.round(body.w * 0.88),
        cy,
        button.accent,
        button.glowRadius > 0 ? 1 : 0.35
      );
    }
  }

  return cropAlpha(out, cw, ch, 10);
}

function bbox(mask, width, height) {
  let x0 = width;
  let y0 = height;
  let x1 = -1;
  let y1 = -1;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (!mask[y * width + x]) continue;
      if (x < x0) x0 = x;
      if (x > x1) x1 = x;
      if (y < y0) y0 = y;
      if (y > y1) y1 = y;
    }
  }
  if (x1 < x0) return null;
  return { x0, y0, x1, y1, w: x1 - x0 + 1, h: y1 - y0 + 1 };
}

function distanceFrom(mask, width, height) {
  const n = width * height;
  const dist = new Float32Array(n);
  dist.fill(1e6);
  const q = new Int32Array(n);
  let head = 0;
  let tail = 0;
  for (let i = 0; i < n; i++) {
    if (!mask[i]) continue;
    dist[i] = 0;
    q[tail++] = i;
  }
  while (head < tail) {
    const i = q[head++];
    const x = i % width;
    const neigh = [i - 1, i + 1, i - width, i + width];
    for (const ni of neigh) {
      if (ni < 0 || ni >= n) continue;
      const nx = ni % width;
      if (Math.abs(nx - x) > 1) continue;
      const nd = dist[i] + 1;
      if (nd >= dist[ni]) continue;
      dist[ni] = nd;
      q[tail++] = ni;
    }
  }
  return dist;
}

function blendPixel(out, width, height, mask, x, y, r, g, b, a) {
  if (x < 0 || y < 0 || x >= width || y >= height || a <= 0) return;
  if (mask && !mask[y * width + x]) return;
  const o = (y * width + x) * 4;
  const da = out[o + 3] / 255;
  const sa = a / 255;
  const oa = sa + da * (1 - sa);
  if (oa <= 0) return;
  out[o] = Math.round((r * sa + out[o] * da * (1 - sa)) / oa);
  out[o + 1] = Math.round((g * sa + out[o + 1] * da * (1 - sa)) / oa);
  out[o + 2] = Math.round((b * sa + out[o + 2] * da * (1 - sa)) / oa);
  out[o + 3] = Math.round(oa * 255);
}

function drawStar(out, width, height, mask, cx, cy, radius, color) {
  for (let y = -radius; y <= radius; y++) {
    for (let x = -radius; x <= radius; x++) {
      const ax = Math.abs(x);
      const ay = Math.abs(y);
      const cross = Math.min(ax, ay * 4.2) + Math.min(ay, ax * 4.2);
      const diag = Math.abs(ax - ay) * 1.8 + Math.min(ax, ay) * 0.35;
      const d = Math.min(cross, diag);
      const t = 1 - d / (radius * 0.85);
      if (t <= 0) continue;
      const a = Math.round(255 * Math.pow(Math.min(1, t), 1.35));
      blendPixel(out, width, height, mask, cx + x, cy + y, color[0], color[1], color[2], a);
    }
  }
}

function drawHairline(out, width, height, mask, x0, y, x1, color) {
  const xa = Math.min(x0, x1);
  const xb = Math.max(x0, x1);
  for (let x = xa; x <= xb; x++) {
    blendPixel(out, width, height, mask, x, y, color[0], color[1], color[2], 210);
    blendPixel(out, width, height, mask, x, y - 1, color[0], color[1], color[2], 80);
  }
}

function drawWave(out, width, height, mask, x0, x1, cy, color, strength) {
  for (let x = x0; x <= x1; x++) {
    const u = (x - x0) / Math.max(1, x1 - x0);
    const amp = (1 - Math.abs(u - 0.5) * 1.6) * 7 * strength;
    const yy = cy + Math.round(Math.sin(u * Math.PI * 6.5) * amp);
    const a = Math.round(210 * strength);
    blendPixel(out, width, height, mask, x, yy, color[0], color[1], color[2], a);
    blendPixel(out, width, height, mask, x, yy - 1, color[0], color[1], color[2], Math.round(a * 0.45));
  }
}

const { data, info } = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const { width, height } = info;
const bg = floodBackground(data, width, height);

fs.mkdirSync(REF_DIR, { recursive: true });
fs.copyFileSync(SRC, path.join(REF_DIR, "yes_no_button_sheet_v1.jpg"));

const midX = Math.floor(width / 2);
const midY = Math.floor(height / 2);

for (const button of BUTTONS) {
  const x0 = button.col === 0 ? 0 : midX;
  const y0 = button.row === 0 ? 0 : midY;
  const cw = button.col === 0 ? midX : width - midX;
  const ch = button.row === 0 ? midY : height - midY;
  const processed = processCell(data, width, x0, y0, cw, ch, bg, button);
  const dest = path.join(OUT_DIR, button.name);
  await sharp(processed.data, { raw: { width: processed.width, height: processed.height, channels: 4 } })
    .png()
    .toFile(dest);
  writeSpriteMeta(dest, button.guid);
  console.log("wrote", button.name, processed.width, processed.height);
}

console.log("prologue yes/no buttons rebuilt");
