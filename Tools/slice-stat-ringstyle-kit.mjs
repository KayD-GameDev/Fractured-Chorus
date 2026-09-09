import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import sharp from "sharp";

const SRC_BARWAVE =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/ui_stat_ringstyle_barwave_v1.png";
const SRC_CHIPS =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/ui_stat_ringstyle_chips_lock_v1.png";
const ROOT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu";
const REF = path.join(ROOT, "_ref");
const HUD = path.join(ROOT, "Decor/Hud");
const KIT = path.join(ROOT, "Kit");
const ICONS = path.join(ROOT, "Icons");
const WAVE_FRAMES = 24;

function guid() {
  return crypto.randomBytes(16).toString("hex");
}

function spriteId() {
  return crypto.randomBytes(16).toString("hex");
}

async function writeSpriteMeta(pngPath) {
  if (fs.existsSync(pngPath + ".meta")) return;
  const sid = spriteId();
  fs.writeFileSync(
    pngPath + ".meta",
    `fileFormatVersion: 2
guid: ${guid()}
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
  spriteMeshType: 0
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 0
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
    textureCompression: 0
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
    spriteID: ${sid}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`
  );
}

function toAlpha(px) {
  for (let i = 0; i < px.length; i += 4) {
    const m = Math.max(px[i], px[i + 1], px[i + 2]);
    const a = Math.max(0, Math.min(255, (m - 14) * 6));
    px[i + 3] = a;
    if (a < 10) {
      px[i] = 0;
      px[i + 1] = 0;
      px[i + 2] = 0;
      px[i + 3] = 0;
    }
  }
}

function extract(px, w, h, left, top, width, height) {
  left = Math.max(0, left);
  top = Math.max(0, top);
  width = Math.min(w - left, width);
  height = Math.min(h - top, height);
  const out = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y++) {
    px.copy(out, y * width * 4, ((top + y) * w + left) * 4, ((top + y) * w + left + width) * 4);
  }
  return { buf: out, width, height };
}

function padSquare(buf, width, height, size) {
  const canvas = Buffer.alloc(size * size * 4);
  const ox = Math.max(0, Math.floor((size - width) / 2));
  const oy = Math.max(0, Math.floor((size - height) / 2));
  const copyW = Math.min(width, size);
  const copyH = Math.min(height, size);
  for (let y = 0; y < copyH; y++) {
    buf.copy(canvas, ((oy + y) * size + ox) * 4, y * width * 4, y * width * 4 + copyW * 4);
  }
  return canvas;
}

function blobs(px, w, h, minArea) {
  const seen = Buffer.alloc(w * h);
  const out = [];
  const thresh = 12;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const start = y * w + x;
      if (seen[start] || px[start * 4 + 3] < thresh) continue;
      let minX = x;
      let maxX = x;
      let minY = y;
      let maxY = y;
      let area = 0;
      const q = [start];
      seen[start] = 1;
      while (q.length) {
        const id = q.pop();
        area++;
        const cx = id % w;
        const cy = (id / w) | 0;
        if (cx < minX) minX = cx;
        if (cx > maxX) maxX = cx;
        if (cy < minY) minY = cy;
        if (cy > maxY) maxY = cy;
        const nbs = [id - 1, id + 1, id - w, id + w];
        for (const n of nbs) {
          if (n < 0 || n >= w * h || seen[n]) continue;
          const nx = n % w;
          const ny = (n / w) | 0;
          if (Math.abs(nx - cx) + Math.abs(ny - cy) !== 1) continue;
          if (px[n * 4 + 3] < thresh) continue;
          seen[n] = 1;
          q.push(n);
        }
      }
      if (area < minArea) continue;
      out.push({ minX, maxX, minY, maxY, area });
    }
  }
  out.sort((a, b) => a.minX - b.minX);
  return out;
}

function sampleBilinear(src, w, h, x, y) {
  const x0 = Math.max(0, Math.min(w - 1, Math.floor(x)));
  const y0 = Math.max(0, Math.min(h - 1, Math.floor(y)));
  const x1 = Math.min(w - 1, x0 + 1);
  const y1 = Math.min(h - 1, y0 + 1);
  const tx = x - x0;
  const ty = y - y0;
  const i00 = (y0 * w + x0) * 4;
  const i10 = (y0 * w + x1) * 4;
  const i01 = (y1 * w + x0) * 4;
  const i11 = (y1 * w + x1) * 4;
  const out = [0, 0, 0, 0];
  for (let c = 0; c < 4; c++) {
    const a = src[i00 + c] * (1 - tx) + src[i10 + c] * tx;
    const b = src[i01 + c] * (1 - tx) + src[i11 + c] * tx;
    out[c] = a * (1 - ty) + b * ty;
  }
  return out;
}

function waveformFrames(src, w, h, count) {
  const cols = new Array(w);
  for (let x = 0; x < w; x++) {
    let bot = -1;
    let barTop = -1;
    for (let y = h - 1; y >= 0; y--) {
      if (src[(y * w + x) * 4 + 3] < 16) {
        if (bot >= 0) break;
        continue;
      }
      if (bot < 0) bot = y;
      barTop = y;
    }
    cols[x] = bot < 0 ? null : { top: barTop, bot };
  }

  const frames = [];
  for (let f = 0; f < count; f++) {
    const out = Buffer.alloc(w * h * 4);
    const phase = (f / count) * Math.PI * 2;
    for (let x = 0; x < w; x++) {
      const col = cols[x];
      if (!col) continue;
      const baseH = col.bot - col.top + 1;
      const nx = x / Math.max(1, w - 1);
      const wave =
        1 +
        0.09 * Math.sin(nx * 8.4 + phase) +
        0.05 * Math.sin(nx * 19.2 - phase * 1.15) +
        0.025 * Math.sin(nx * 5.1 + phase * 0.55);
      const scale = Math.max(0.88, Math.min(1.1, wave));
      const keep = Math.min(5, baseH);
      const barH = Math.max(keep + 1, baseH * scale);
      const destTop = col.bot - barH + 1;
      for (let y = Math.max(0, Math.floor(destTop)); y <= col.bot; y++) {
        const fromBot = col.bot - y;
        let srcY;
        if (fromBot < keep) {
          srcY = col.bot - fromBot;
        } else {
          const t = (fromBot - keep) / Math.max(1, barH - keep);
          srcY = col.bot - keep - t * (baseH - keep);
        }
        const samp = sampleBilinear(src, w, h, x, srcY);
        const o = (y * w + x) * 4;
        out[o] = samp[0];
        out[o + 1] = samp[1];
        out[o + 2] = samp[2];
        out[o + 3] = samp[3];
      }
    }
    frames.push(out);
  }
  return frames;
}

async function saveRaw(buf, width, height, dest) {
  await sharp(buf, { raw: { width, height, channels: 4 } }).png().toFile(dest);
  await writeSpriteMeta(dest);
}

async function load(p) {
  const { data, info } = await sharp(p).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const px = Buffer.from(data);
  toAlpha(px);
  return { px, w: info.width, h: info.height };
}

async function main() {
  fs.mkdirSync(REF, { recursive: true });
  fs.copyFileSync(SRC_BARWAVE, path.join(REF, "_ref_ringstyle_barwave_v1.png"));
  fs.copyFileSync(SRC_CHIPS, path.join(REF, "_ref_ringstyle_chips_lock_v1.png"));

  const barwave = await load(SRC_BARWAVE);
  const bwBlobs = blobs(barwave.px, barwave.w, barwave.h, 800);
  console.log(
    "barwave blobs",
    bwBlobs.map((b) => `${b.minX},${b.minY}-${b.maxX},${b.maxY} a${b.area}`)
  );
  if (bwBlobs.length < 2) {
    throw new Error("expected wave + bar");
  }
  const waveBox = bwBlobs[0].maxY - bwBlobs[0].minY > bwBlobs[1].maxY - bwBlobs[1].minY ? bwBlobs[0] : bwBlobs[1];
  const barBox = waveBox === bwBlobs[0] ? bwBlobs[1] : bwBlobs[0];
  const pad = 8;
  const wave = extract(
    barwave.px,
    barwave.w,
    barwave.h,
    waveBox.minX - pad,
    waveBox.minY - pad,
    waveBox.maxX - waveBox.minX + 1 + pad * 2,
    waveBox.maxY - waveBox.minY + 1 + pad * 2
  );
  const bar = extract(
    barwave.px,
    barwave.w,
    barwave.h,
    barBox.minX - pad,
    barBox.minY - pad,
    barBox.maxX - barBox.minX + 1 + pad * 2,
    barBox.maxY - barBox.minY + 1 + pad * 2
  );
  await saveRaw(wave.buf, wave.width, wave.height, path.join(HUD, "ui_stat_hud_wave_base_v1.png"));
  await saveRaw(bar.buf, bar.width, bar.height, path.join(HUD, "ui_stat_hud_bar_v1.png"));
  const frames = waveformFrames(wave.buf, wave.width, wave.height, WAVE_FRAMES);
  for (let i = 0; i < frames.length; i++) {
    await saveRaw(
      frames[i],
      wave.width,
      wave.height,
      path.join(HUD, `ui_stat_hud_wave_${String(i).padStart(2, "0")}.png`)
    );
  }

  const chips = await load(SRC_CHIPS);
  const chipBlobs = blobs(chips.px, chips.w, chips.h, 1200)
    .sort((a, b) => b.area - a.area)
    .slice(0, 3)
    .sort((a, b) => a.minX - b.minX);
  console.log(
    "chip blobs",
    chipBlobs.map((b) => `${b.minX},${b.minY}-${b.maxX},${b.maxY} a${b.area}`)
  );
  if (chipBlobs.length < 3) {
    throw new Error("expected 3 chip/lock blobs");
  }
  const targets = [
    path.join(KIT, "ui_stat_slot_portrait_v1.png"),
    path.join(KIT, "ui_stat_slot_portrait_locked_v1.png"),
    path.join(ICONS, "ui_stat_icon_lock_v1.png")
  ];
  const size = 320;
  for (let i = 0; i < 3; i++) {
    const b = chipBlobs[i];
    const p = 12;
    const piece = extract(
      chips.px,
      chips.w,
      chips.h,
      b.minX - p,
      b.minY - p,
      b.maxX - b.minX + 1 + p * 2,
      b.maxY - b.minY + 1 + p * 2
    );
    const square = padSquare(piece.buf, piece.width, piece.height, size);
    await saveRaw(square, size, size, targets[i]);
  }

  console.log("wrote wave", wave.width + "x" + wave.height, "bar", bar.width + "x" + bar.height, "chips 320");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
