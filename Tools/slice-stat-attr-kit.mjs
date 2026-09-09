import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ICONS = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/Icons";
const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit";

function luma(r, g, b) {
  return (r + g + b) / 3;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function isMagenta(r, g, b) {
  return r > 170 && b > 170 && g < 130 && r + b - 2 * g > 160;
}

function isWhitePlate(r, g, b) {
  return luma(r, g, b) > 235 && chroma(r, g, b) < 28;
}

function punch(px, o) {
  px[o] = px[o + 1] = px[o + 2] = px[o + 3] = 0;
}

async function loadRgba(file) {
  return sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
}

function mattePixels(px, n) {
  for (let i = 0; i < n; i++) {
    const o = i * 4;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    if (isMagenta(r, g, b) || isWhitePlate(r, g, b) || luma(r, g, b) < 8) {
      punch(px, o);
    } else {
      px[o + 3] = 255;
    }
  }
}

function floodBg(px, w, h) {
  const n = w * h;
  const mask = new Uint8Array(n);
  const q = new Int32Array(n);
  let qs = 0;
  let qe = 0;
  const trySeed = (x, y) => {
    const i = y * w + x;
    if (mask[i]) return;
    const o = i * 4;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    if (!(isMagenta(r, g, b) || isWhitePlate(r, g, b) || luma(r, g, b) < 14)) return;
    mask[i] = 1;
    q[qe++] = i;
  };
  for (let x = 0; x < w; x++) {
    trySeed(x, 0);
    trySeed(x, h - 1);
  }
  for (let y = 0; y < h; y++) {
    trySeed(0, y);
    trySeed(w - 1, y);
  }
  while (qs < qe) {
    const i = q[qs++];
    const x = i % w;
    const y = (i / w) | 0;
    const nb = [x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1];
    for (const j of nb) {
      if (j < 0 || mask[j]) continue;
      const o = j * 4;
      const r = px[o];
      const g = px[o + 1];
      const b = px[o + 2];
      if (isMagenta(r, g, b) || isWhitePlate(r, g, b) || luma(r, g, b) < 18) {
        mask[j] = 1;
        q[qe++] = j;
      }
    }
  }
  for (let i = 0; i < n; i++) {
    if (mask[i]) punch(px, i * 4);
  }
}

function cropAlpha(px, w, h, pad = 8) {
  let minX = w;
  let minY = h;
  let maxX = -1;
  let maxY = -1;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[(y * w + x) * 4 + 3] < 16) continue;
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x);
      maxY = Math.max(maxY, y);
    }
  }
  if (maxX < 0) return { data: Buffer.alloc(4), w: 1, h: 1 };
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(w - 1, maxX + pad);
  maxY = Math.min(h - 1, maxY + pad);
  const cw = maxX - minX + 1;
  const ch = maxY - minY + 1;
  const out = Buffer.alloc(cw * ch * 4);
  for (let y = 0; y < ch; y++) {
    for (let x = 0; x < cw; x++) {
      const s = ((minY + y) * w + (minX + x)) * 4;
      const d = (y * cw + x) * 4;
      out[d] = px[s];
      out[d + 1] = px[s + 1];
      out[d + 2] = px[s + 2];
      out[d + 3] = px[s + 3];
    }
  }
  return { data: out, w: cw, h: ch };
}

async function writePng(img, file) {
  await sharp(img.data, { raw: { width: img.w, height: img.h, channels: 4 } }).png().toFile(file);
}

async function matteFile(src, dest, pad = 10) {
  const { data, info } = await loadRgba(src);
  floodBg(data, info.width, info.height);
  mattePixels(data, info.width * info.height);
  const cropped = cropAlpha(data, info.width, info.height, pad);
  await writePng(cropped, dest);
  return cropped;
}

function splitColumns(px, w, h, count) {
  const colW = Math.floor(w / count);
  const parts = [];
  for (let i = 0; i < count; i++) {
    const x0 = i * colW;
    const x1 = i === count - 1 ? w : (i + 1) * colW;
    const cw = x1 - x0;
    const buf = Buffer.alloc(cw * h * 4);
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < cw; x++) {
        const s = (y * w + (x0 + x)) * 4;
        const d = (y * cw + x) * 4;
        buf[d] = px[s];
        buf[d + 1] = px[s + 1];
        buf[d + 2] = px[s + 2];
        buf[d + 3] = px[s + 3];
      }
    }
    parts.push({ data: buf, w: cw, h });
  }
  return parts;
}

function spriteMeta(guid, border) {
  const b = border || { x: 0, y: 0, z: 0, w: 0 };
  return `fileFormatVersion: 2
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
  spriteMeshType: 0
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: ${b.x}, y: ${b.y}, z: ${b.z}, w: ${b.w}}
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
    spriteID: ${guid.slice(0, 32)}
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
`;
}

function ensureMeta(file, guid, border) {
  const meta = file + ".meta";
  if (fs.existsSync(meta)) return;
  fs.writeFileSync(meta, spriteMeta(guid, border));
}

const iconNames = [
  "ui_stat_icon_strength_v2.png",
  "ui_stat_icon_magic_v2.png",
  "ui_stat_icon_endurance_v2.png",
  "ui_stat_icon_heartbeat_v2.png",
  "ui_stat_icon_luck_v2.png",
];
const labelNames = [
  "ui_stat_label_str_v2.png",
  "ui_stat_label_ma_v2.png",
  "ui_stat_label_en_v2.png",
  "ui_stat_label_hb_v2.png",
  "ui_stat_label_luck_v2.png",
];

const icons = await loadRgba(path.join(SRC, "ui_stat_attr_icons_sheet_v2.png"));
floodBg(icons.data, icons.info.width, icons.info.height);
mattePixels(icons.data, icons.info.width * icons.info.height);
const iconParts = splitColumns(icons.data, icons.info.width, icons.info.height, 5);
for (let i = 0; i < 5; i++) {
  const cropped = cropAlpha(iconParts[i].data, iconParts[i].w, iconParts[i].h, 6);
  const dest = path.join(ICONS, iconNames[i]);
  await writePng(cropped, dest);
  ensureMeta(dest, `b1a1000${i}c2d34e5f678901234567890ab`, null);
}

const labels = await loadRgba(path.join(SRC, "ui_stat_attr_labels_sheet_v3.png"));
floodBg(labels.data, labels.info.width, labels.info.height);
mattePixels(labels.data, labels.info.width * labels.info.height);
const labelParts = splitColumns(labels.data, labels.info.width, labels.info.height, 5);
for (let i = 0; i < 5; i++) {
  const cropped = cropAlpha(labelParts[i].data, labelParts[i].w, labelParts[i].h, 4);
  const dest = path.join(ICONS, labelNames[i]);
  await writePng(cropped, dest);
  ensureMeta(dest, `b1a2000${i}c2d34e5f678901234567890ab`, null);
}

const track = await matteFile(path.join(SRC, "ui_stat_attr_slider_track_v2.png"), path.join(KIT, "ui_stat_attr_slider_track_v2.png"), 4);
const fill = await matteFile(path.join(SRC, "ui_stat_attr_slider_fill_v2.png"), path.join(KIT, "ui_stat_attr_slider_fill_v2.png"), 4);
const handle = await matteFile(path.join(SRC, "ui_stat_attr_slider_handle_v2.png"), path.join(KIT, "ui_stat_attr_slider_handle_v2.png"), 6);

ensureMeta(path.join(KIT, "ui_stat_attr_slider_track_v2.png"), "c3d40111e2f34a5b9c0d1e2f3a4b5c6d", { x: 48, y: 12, z: 48, w: 12 });
ensureMeta(path.join(KIT, "ui_stat_attr_slider_fill_v2.png"), "c3d40222e2f34a5b9c0d1e2f3a4b5c6d", { x: 40, y: 10, z: 40, w: 10 });
ensureMeta(path.join(KIT, "ui_stat_attr_slider_handle_v2.png"), "c3d40333e2f34a5b9c0d1e2f3a4b5c6d", null);

const manifest = {
  generated: "2026-08-26",
  icons: iconNames,
  labels: labelNames,
  slider: [
    "ui_stat_attr_slider_track_v2.png",
    "ui_stat_attr_slider_fill_v2.png",
    "ui_stat_attr_slider_handle_v2.png",
  ],
  sizes: {
    track: [track.w, track.h],
    fill: [fill.w, fill.h],
    handle: [handle.w, handle.h],
  },
};
fs.writeFileSync(path.join(KIT, "stat_attr_kit_manifest.json"), JSON.stringify(manifest, null, 2));
console.log(JSON.stringify(manifest));
