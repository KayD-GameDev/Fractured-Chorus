import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import sharp from "sharp";

const SRC =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_Icon_Kit-bb4838b4-4129-43fe-8e2b-f274a2649386.png";
const ROOT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu";
const REF = path.join(ROOT, "_ref");
const KIT = path.join(ROOT, "Kit");
const OUT = path.join(ROOT, "MockKit");
const CANVAS = 320;

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

function isCanvas(r, g, b) {
  return Math.max(r, g, b) < 28 && Math.abs(r - g) < 12 && Math.abs(g - b) < 12;
}

function floodWhite(px, w, h) {
  const seen = Buffer.alloc(w * h);
  const q = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= w || y >= h) return;
    const id = y * w + x;
    if (seen[id]) return;
    seen[id] = 1;
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
    if (!isCanvas(px[i], px[i + 1], px[i + 2])) continue;
    px[i] = 255;
    px[i + 1] = 255;
    px[i + 2] = 255;
    px[i + 3] = 255;
    const x = id % w;
    const y = (id / w) | 0;
    push(x - 1, y);
    push(x + 1, y);
    push(x, y - 1);
    push(x, y + 1);
  }
}

function fillLargeHoles(px, w, h, minArea) {
  const seen = Buffer.alloc(w * h);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const start = y * w + x;
      if (seen[start]) continue;
      const i = start * 4;
      if (!isCanvas(px[i], px[i + 1], px[i + 2])) {
        seen[start] = 1;
        continue;
      }
      const q = [start];
      seen[start] = 1;
      const cells = [start];
      while (q.length) {
        const id = q.pop();
        const cx = id % w;
        const cy = (id / w) | 0;
        for (const n of [id - 1, id + 1, id - w, id + w]) {
          if (n < 0 || n >= w * h || seen[n]) continue;
          const nx = n % w;
          const ny = (n / w) | 0;
          if (Math.abs(nx - cx) + Math.abs(ny - cy) !== 1) continue;
          const ni = n * 4;
          if (!isCanvas(px[ni], px[ni + 1], px[ni + 2])) continue;
          seen[n] = 1;
          q.push(n);
          cells.push(n);
        }
      }
      if (cells.length < minArea) continue;
      for (const id of cells) {
        const o = id * 4;
        px[o] = 255;
        px[o + 1] = 255;
        px[o + 2] = 255;
      }
    }
  }
}

function extract(px, w, h, rect) {
  const left = Math.max(0, rect.left);
  const top = Math.max(0, rect.top);
  const width = Math.min(w - left, rect.width);
  const height = Math.min(h - top, rect.height);
  const out = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y++) {
    const src = ((top + y) * w + left) * 4;
    px.copy(out, y * width * 4, src, src + width * 4);
  }
  return { buf: out, width, height };
}

function toSquare(buf, width, height, size, fillHole) {
  const px = Buffer.from(buf);
  if (fillHole) fillLargeHoles(px, width, height, 800);
  const canvas = Buffer.alloc(size * size * 4, 255);
  const ox = Math.max(0, Math.floor((size - width) / 2));
  const oy = Math.max(0, Math.floor((size - height) / 2));
  for (let y = 0; y < height; y++) {
    const dest = ((oy + y) * size + ox) * 4;
    px.copy(canvas, dest, y * width * 4, (y + 1) * width * 4);
  }
  return canvas;
}

async function save(buf, dest) {
  await sharp(buf, { raw: { width: CANVAS, height: CANVAS, channels: 4 } }).png().toFile(dest);
  await writeSpriteMeta(dest);
}

async function main() {
  fs.mkdirSync(REF, { recursive: true });
  fs.mkdirSync(KIT, { recursive: true });
  fs.mkdirSync(OUT, { recursive: true });
  fs.copyFileSync(SRC, path.join(REF, "_ref_icon_kit_src.png"));

  const { data, info } = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const px = Buffer.from(data);
  const { width: w, height: h } = info;
  floodWhite(px, w, h);

  const pad = 10;
  const unlocked = extract(px, w, h, { left: 436 - pad, top: 81 - pad, width: 284 + pad * 2, height: 272 + pad * 2 });
  const locked = extract(px, w, h, { left: 731 - pad, top: 81 - pad, width: 285 + pad * 2, height: 277 + pad * 2 });

  const openBuf = toSquare(unlocked.buf, unlocked.width, unlocked.height, CANVAS, true);
  const lockedBuf = toSquare(locked.buf, locked.width, locked.height, CANVAS, false);

  const openKit = path.join(KIT, "ui_stat_slot_portrait_v1.png");
  const lockedKit = path.join(KIT, "ui_stat_slot_portrait_locked_v1.png");
  await save(openBuf, openKit);
  await save(lockedBuf, lockedKit);
  await save(openBuf, path.join(OUT, "ui_stat_slot_portrait_open.png"));
  await save(lockedBuf, path.join(OUT, "ui_stat_slot_portrait_locked.png"));

  const pair = Buffer.alloc(CANVAS * 2 * CANVAS * 4, 255);
  for (let y = 0; y < CANVAS; y++) {
    openBuf.copy(pair, y * CANVAS * 2 * 4, y * CANVAS * 4, (y + 1) * CANVAS * 4);
    lockedBuf.copy(pair, y * CANVAS * 2 * 4 + CANVAS * 4, y * CANVAS * 4, (y + 1) * CANVAS * 4);
  }
  await sharp(pair, { raw: { width: CANVAS * 2, height: CANVAS, channels: 4 } })
    .png()
    .toFile(path.join(REF, "_ref_portrait_chips_open_locked.png"));

  console.log("chips", CANVAS + "x" + CANVAS, "open+locked");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
