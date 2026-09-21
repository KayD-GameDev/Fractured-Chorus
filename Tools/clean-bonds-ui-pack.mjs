import crypto from "crypto";
import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/Downloads/Bond UI Pack/Bonds_UI/Generated_Source";
const OUT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/Bonds/Pack";
const REPORT = path.join(OUT, "CLEAN_REPORT.json");

const FILES = [
  "01_Header_Bonds.png",
  "02_Menu_Normal.png",
  "03_Menu_Selected.png",
  "04_Panel_SocialStats.png",
  "05_Panel_Episodes.png",
  "06_Row_Episode_Normal.png",
  "07_Row_Episode_Selected.png",
  "08_Card_Character_Normal.png",
  "09_Card_Character_Selected.png",
  "10_Panel_CharacterInfo.png",
  "11_Frame_Promo.png",
  "12_Panel_Footer.png",
  "13_Icon_Bonds.png",
  "14_Icon_SocialStats.png",
  "15_Icon_Link.png",
  "16_Icon_Conversations.png",
  "17_Icon_Memories.png",
  "18_Icon_Gallery.png",
  "19_Icon_Pulse.png",
  "20_Icon_Cadence.png",
  "21_Icon_Harmony.png",
  "22_Icon_Resonance.png",
  "23_Icon_Rhythm.png",
  "24_Icon_Lock.png",
  "25_Icon_Play.png",
  "26_Icon_Arrow.png",
  "27_Icon_Sun.png",
  "28_Icon_Mouse.png",
  "29_Icon_Back.png",
  "30_Decor_Spark.png",
  "31_Decor_Divider.png",
];

function dist(a, b) {
  const dr = a[0] - b[0];
  const dg = a[1] - b[1];
  const db = a[2] - b[2];
  return Math.sqrt(dr * dr + dg * dg + db * db);
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
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

function isBackground(r, g, b, seeds, file) {
  const c = chroma(r, g, b);
  const y = luma(r, g, b);
  if (file.startsWith("11_") && y < 18 && c < 16) {
    return true;
  }
  if (c > 22) {
    return false;
  }
  let best = 255;
  for (let i = 0; i < seeds.length; i++) {
    const d = dist([r, g, b], seeds[i]);
    if (d < best) best = d;
  }
  if (file.match(/^(13_|14_|15_|16_|17_|18_|19_|20_|21_|22_|23_|24_|25_|26_|27_|28_|29_|30_)/) && y >= 246) {
    return false;
  }
  return best <= 28;
}

function floodAlpha(data, width, height, seeds, file) {
  const n = width * height;
  const bg = new Uint8Array(n);
  for (let idx = 0; idx < n; idx++) {
    const i = idx * 4;
    if (isBackground(data[i], data[i + 1], data[i + 2], seeds, file)) {
      bg[idx] = 1;
    }
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
  if (file.startsWith("11_")) {
    tryPush(width >> 1, height >> 1);
  }

  while (head < tail) {
    const idx = queue[head++];
    const x = idx % width;
    const y = (idx / width) | 0;
    const i = idx * 4;
    data[i] = 0;
    data[i + 1] = 0;
    data[i + 2] = 0;
    data[i + 3] = 0;
    tryPush(x - 1, y);
    tryPush(x + 1, y);
    tryPush(x, y - 1);
    tryPush(x, y + 1);
  }

  for (let i = 0; i < data.length; i += 4) {
    if (data[i + 3] === 0) {
      data[i] = 0;
      data[i + 1] = 0;
      data[i + 2] = 0;
    }
  }
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
  if (maxX < 0) {
    return { left: 0, top: 0, width, height };
  }
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(width - 1, maxX + pad);
  maxY = Math.min(height - 1, maxY + pad);
  return { left: minX, top: minY, width: maxX - minX + 1, height: maxY - minY + 1 };
}

function spriteMeta(guid) {
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
`;
}

function folderMeta(guid) {
  return `fileFormatVersion: 2
guid: ${guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
}

async function cleanOne(file) {
  const src = path.join(SRC, file);
  const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  const seeds = collectSeeds(pixels, info.width, info.height);
  floodAlpha(pixels, info.width, info.height, seeds, file);

  let opaque = 0;
  for (let i = 3; i < pixels.length; i += 4) {
    if (pixels[i] > 16) opaque += 1;
  }
  const pad = file.match(/^(13_|14_|15_|16_|17_|18_|19_|20_|21_|22_|23_|24_|25_|26_|27_|28_|29_|30_)/) ? 16 : 8;
  const box = contentBox(pixels, info.width, info.height, pad);
  const cropped = await sharp(pixels, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .extract(box)
    .png()
    .toBuffer();

  const outPath = path.join(OUT, file);
  fs.writeFileSync(outPath, cropped);
  const metaPath = `${outPath}.meta`;
  const existingGuid = fs.existsSync(metaPath)
    ? fs.readFileSync(metaPath, "utf8").match(/^guid: ([a-f0-9]+)/m)?.[1]
    : null;
  const guid = existingGuid ?? crypto.randomBytes(16).toString("hex");
  fs.writeFileSync(metaPath, spriteMeta(guid));
  return {
    file,
    guid,
    srcSize: [info.width, info.height],
    outSize: [box.width, box.height],
    opaqueRatio: Number((opaque / (info.width * info.height)).toFixed(4)),
    blocked: opaque / (info.width * info.height) < 0.004,
  };
}

fs.mkdirSync(OUT, { recursive: true });
if (!fs.existsSync(`${OUT}.meta`)) {
  fs.writeFileSync(`${OUT}.meta`, folderMeta(crypto.randomBytes(16).toString("hex")));
}

const report = [];
for (const file of FILES) {
  const started = Date.now();
  const entry = await cleanOne(file);
  entry.ms = Date.now() - started;
  report.push(entry);
  console.log(`${file} opaque=${entry.opaqueRatio} ${entry.outSize.join("x")} ${entry.ms}ms`);
}
fs.writeFileSync(REPORT, JSON.stringify(report, null, 2) + "\n");
console.log(JSON.stringify({ out: OUT, count: report.length, blocked: report.filter((r) => r.blocked).map((r) => r.file) }, null, 2));
