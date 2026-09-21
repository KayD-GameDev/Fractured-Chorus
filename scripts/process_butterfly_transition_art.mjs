import fs from "node:fs";
import path from "node:path";
import { randomUUID } from "node:crypto";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const OUT = "d:/Fractured-Chorus1/Assets/FracturedChorus/VFX/ButterflyTransition";
const SPRITES = path.join(OUT, "Sprites");
const PARTICLES = path.join(OUT, "Particles");
const SOURCE = path.join(SPRITES, "_source");

const WINGS = [
  ["bt_wing_01_closed_v2.png", "fc_bt_wing_01_closed.png"],
  ["bt_wing_02_quarter.png", "fc_bt_wing_02_quarter.png"],
  ["bt_wing_03_half.png", "fc_bt_wing_03_half.png"],
  ["bt_wing_04_open_v2.png", "fc_bt_wing_04_open.png"],
];

function writeMeta(filePath) {
  const guid = randomUUID().replaceAll("-", "");
  const spriteId = randomUUID().replaceAll("-", "");
  fs.writeFileSync(
    filePath + ".meta",
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
    customData: 
    physicsShape: []
    bones: []
    spriteID: ${spriteId}
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

async function savePng(buf, width, height, filePath) {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  await sharp(buf, { raw: { width, height, channels: 4 } })
    .png()
    .toFile(filePath);
  if (!fs.existsSync(filePath + ".meta")) writeMeta(filePath);
  console.log("wrote", filePath, width, height);
}

function keyButterfly(data, width, height) {
  const seedR = data[0];
  const seedG = data[1];
  const seedB = data[2];
  const bg = new Uint8Array(width * height);
  for (let i = 0; i < width * height; i++) {
    const o = i * 4;
    const dist = Math.hypot(data[o] - seedR, data[o + 1] - seedG, data[o + 2] - seedB);
    bg[i] = dist < 42 ? 1 : 0;
  }

  const visited = new Uint8Array(width * height);
  const qx = [];
  const qy = [];
  const push = (x, y) => {
    const i = y * width + x;
    if (visited[i] || !bg[i]) return;
    visited[i] = 1;
    qx.push(x);
    qy.push(y);
  };
  for (let x = 0; x < width; x++) {
    push(x, 0);
    push(x, height - 1);
  }
  for (let y = 0; y < height; y++) {
    push(0, y);
    push(width - 1, y);
  }
  const dirs = [
    [-1, 0],
    [1, 0],
    [0, -1],
    [0, 1],
  ];
  for (let q = 0; q < qx.length; q++) {
    const x = qx[q];
    const y = qy[q];
    for (const [dx, dy] of dirs) {
      const nx = x + dx;
      const ny = y + dy;
      if (nx >= 0 && ny >= 0 && nx < width && ny < height) push(nx, ny);
    }
  }

  const out = Buffer.from(data);
  for (let i = 0; i < width * height; i++) {
    const o = i * 4;
    if (visited[i]) {
      out[o] = 0;
      out[o + 1] = 0;
      out[o + 2] = 0;
      out[o + 3] = 0;
      continue;
    }
    let r = out[o];
    let g = out[o + 1];
    let b = out[o + 2];
    const mag = Math.max(0, Math.min(1, ((r + b) * 0.5 - g) / 255));
    r = r - mag * Math.max(r - g, 0) * 0.45;
    g = g + mag * 8;
    out[o] = Math.max(0, Math.min(255, r));
    out[o + 1] = Math.max(0, Math.min(255, g));
    out[o + 2] = b;
    out[o + 3] = 255;
  }
  for (let y = 1; y < height - 1; y++) {
    for (let x = 1; x < width - 1; x++) {
      const i = y * width + x;
      if (visited[i]) continue;
      if (visited[i - 1] || visited[i + 1] || visited[i - width] || visited[i + width]) {
        const o = i * 4;
        out[o + 3] = 140;
      }
    }
  }
  return out;
}

function cropAlpha(data, width, height, pad = 22) {
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (data[(y * width + x) * 4 + 3] > 10) {
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
    const src = ((minY + y) * width + minX) * 4;
    data.copy(out, y * w * 4, src, src + w * 4);
  }
  return { data: out, width: w, height: h };
}

async function processWings() {
  fs.mkdirSync(SOURCE, { recursive: true });
  for (const [srcName, dstName] of WINGS) {
    const src = path.join(SRC, srcName);
    fs.copyFileSync(src, path.join(SOURCE, srcName));
    const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    const keyed = keyButterfly(data, info.width, info.height);
    const cropped = cropAlpha(keyed, info.width, info.height, 22);
    await savePng(cropped.data, cropped.width, cropped.height, path.join(SPRITES, dstName));
  }
}

async function processParticles() {
  const { spawnSync } = await import("node:child_process");
  const result = spawnSync(process.execPath, [path.join("d:/Fractured-Chorus1/scripts/process_butterfly_trail_particles.mjs")], {
    stdio: "inherit",
  });
  if (result.status !== 0) throw new Error("trail particle process failed");
}

await processWings();
await processParticles();
fs.mkdirSync(path.join(OUT, "Animations"), { recursive: true });
fs.mkdirSync(path.join(OUT, "Prefabs"), { recursive: true });
fs.mkdirSync(path.join(OUT, "Materials"), { recursive: true });
console.log("butterfly transition art ready");
