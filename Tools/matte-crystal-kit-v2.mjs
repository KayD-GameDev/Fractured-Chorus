import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import sharp from "sharp";

const DIR = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit";
const REF_META = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/_ref/_ref_crystal_character_ui_kit_src.png";

function guid() {
  return crypto.randomBytes(16).toString("hex");
}

function spriteId() {
  return crypto.randomBytes(16).toString("hex");
}

function writeSpriteMeta(pngPath, border = { x: 0, y: 0, z: 0, w: 0 }) {
  if (fs.existsSync(pngPath + ".meta")) return;
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
  spriteBorder: {x: ${border.x}, y: ${border.y}, z: ${border.z}, w: ${border.w}}
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
    spriteID: ${spriteId()}
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
  );
}

function matteBlack(px) {
  for (let i = 0; i < px.length; i += 4) {
    const lum = Math.max(px[i], px[i + 1], px[i + 2]);
    if (lum < 14) {
      px[i] = 0;
      px[i + 1] = 0;
      px[i + 2] = 0;
      px[i + 3] = 0;
      continue;
    }
    const a = Math.max(px[i + 3], Math.min(255, (lum - 10) * 8));
    px[i + 3] = a;
  }
}

function cropInk(px, w, h, pad = 8) {
  let minX = w;
  let minY = h;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[(y * w + x) * 4 + 3] < 12) continue;
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x);
      maxY = Math.max(maxY, y);
    }
  }
  if (maxX < minX) return { buf: px, width: w, height: h };
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(w - 1, maxX + pad);
  maxY = Math.min(h - 1, maxY + pad);
  const nw = maxX - minX + 1;
  const nh = maxY - minY + 1;
  const out = Buffer.alloc(nw * nh * 4);
  for (let y = 0; y < nh; y++) {
    px.copy(out, y * nw * 4, ((minY + y) * w + minX) * 4, ((minY + y) * w + minX + nw) * 4);
  }
  return { buf: out, width: nw, height: nh };
}

const BORDERS = {
  "ui_stat_panel_portrait_v2.png": { x: 64, y: 64, z: 64, w: 64 },
  "ui_stat_header_bar_v2.png": { x: 48, y: 24, z: 48, w: 24 },
  "ui_stat_btn_nav_normal_v2.png": { x: 40, y: 24, z: 40, w: 24 },
  "ui_stat_btn_nav_selected_v2.png": { x: 40, y: 24, z: 40, w: 24 },
  "ui_stat_slot_skill_v2.png": { x: 36, y: 24, z: 36, w: 24 },
  "ui_stat_bar_track_v2.png": { x: 32, y: 16, z: 32, w: 16 },
  "ui_stat_bar_fill_v2.png": { x: 24, y: 12, z: 24, w: 12 },
  "ui_stat_bar_exp_v2.png": { x: 40, y: 18, z: 40, w: 18 },
  "ui_stat_panel_memory_v2.png": { x: 36, y: 36, z: 36, w: 36 },
};

const files = fs.readdirSync(DIR).filter((f) => f.endsWith(".png"));
const manifest = { generated: "2026-08-24", source: "per-component gen (no slice)", items: [] };

for (const name of files) {
  const file = path.join(DIR, name);
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const px = Buffer.from(data);
  matteBlack(px);
  const cropped = cropInk(px, info.width, info.height, 10);
  await sharp(cropped.buf, {
    raw: { width: cropped.width, height: cropped.height, channels: 4 },
  })
    .png()
    .toFile(file);
  writeSpriteMeta(file, BORDERS[name] || { x: 0, y: 0, z: 0, w: 0 });
  manifest.items.push({
    file: name,
    size: [cropped.width, cropped.height],
    border: BORDERS[name] || null,
  });
}

if (!fs.existsSync(DIR + ".meta")) {
  fs.writeFileSync(
    DIR + ".meta",
    `fileFormatVersion: 2
guid: ${guid()}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`,
  );
}

if (fs.existsSync(REF_META) && !fs.existsSync(REF_META + ".meta")) {
  writeSpriteMeta(REF_META);
}

fs.writeFileSync(path.join(DIR, "manifest.json"), JSON.stringify(manifest, null, 2));
if (!fs.existsSync(path.join(DIR, "manifest.json.meta"))) {
  fs.writeFileSync(
    path.join(DIR, "manifest.json.meta"),
    `fileFormatVersion: 2
guid: ${guid()}
TextScriptImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`,
  );
}

console.log(JSON.stringify({ count: files.length, items: manifest.items }, null, 2));
