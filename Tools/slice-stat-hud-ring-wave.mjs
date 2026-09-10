import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import sharp from "sharp";

const SRC =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_RingHUD_and_WaveForm-d7994479-42b2-4b8c-b595-abd990116d10.png";
const ROOT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu";
const REF = path.join(ROOT, "_ref");
const DECOR = path.join(ROOT, "Decor");
const HUD = path.join(DECOR, "Hud");
const FRAME_COUNT = 24;

function guid() {
  return crypto.randomBytes(16).toString("hex");
}

function spriteId() {
  return crypto.randomBytes(16).toString("hex");
}

function folderMeta(dir) {
  const p = dir + ".meta";
  if (fs.existsSync(p)) return;
  fs.writeFileSync(
    p,
    `fileFormatVersion: 2
guid: ${guid()}
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`
  );
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

function luma(px, o) {
  return Math.max(px[o], px[o + 1], px[o + 2]);
}

function toAlpha(px) {
  for (let i = 0; i < px.length; i += 4) {
    const m = Math.max(px[i], px[i + 1], px[i + 2]);
    const a = Math.max(0, Math.min(255, (m - 12) * 5));
    px[i + 3] = a;
    if (a < 8) {
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
  return { buf: out, width, height, left, top };
}

function splitRing(src, w, h, cx, cy) {
  const outer = Buffer.alloc(w * h * 4);
  const inner = Buffer.alloc(w * h * 4);
  const rSplit = 108;
  const rHole = 68;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const o = (y * w + x) * 4;
      const dx = x - cx;
      const dy = y - cy;
      const r = Math.sqrt(dx * dx + dy * dy);
      if (src[o + 3] < 8) continue;
      if (r <= rHole) continue;
      if (dx > rSplit * 0.52 && Math.abs(dy) < rSplit * 0.28 && r > rSplit * 0.58) continue;
      const dest = r > rSplit ? outer : inner;
      src.copy(dest, o, o, o + 4);
    }
  }
  return { outer, inner };
}

function waveformFrames(src, w, h) {
  const cols = new Array(w);
  for (let x = 0; x < w; x++) {
    let top = -1;
    let bot = -1;
    for (let y = 0; y < h; y++) {
      if (src[(y * w + x) * 4 + 3] < 16) continue;
      if (top < 0) top = y;
      bot = y;
    }
    cols[x] = top < 0 ? null : { top, bot };
  }

  const frames = [];
  for (let f = 0; f < FRAME_COUNT; f++) {
    const out = Buffer.alloc(w * h * 4);
    const phase = (f / FRAME_COUNT) * Math.PI * 2;
    for (let x = 0; x < w; x++) {
      const col = cols[x];
      if (!col) continue;
      const baseH = col.bot - col.top + 1;
      const nx = x / Math.max(1, w - 1);
      const wave =
        0.62 +
        0.38 *
          (0.55 * Math.sin(nx * 14 + phase) +
            0.28 * Math.sin(nx * 31 - phase * 1.4) +
            0.17 * Math.sin(nx * 7 + phase * 0.6));
      const newH = Math.max(2, Math.round(baseH * wave));
      const destBot = col.bot;
      const destTop = Math.max(0, destBot - newH + 1);
      for (let y = destTop; y <= destBot; y++) {
        const srcY = col.top + Math.round(((y - destTop) / Math.max(1, newH - 1)) * (baseH - 1));
        const so = (srcY * w + x) * 4;
        const doff = (y * w + x) * 4;
        src.copy(out, doff, so, so + 4);
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

async function main() {
  fs.mkdirSync(REF, { recursive: true });
  fs.mkdirSync(HUD, { recursive: true });
  folderMeta(HUD);
  fs.copyFileSync(SRC, path.join(REF, "_ref_ringhud_waveform_src.png"));

  const { data, info } = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const px = Buffer.from(data);
  const { width: w, height: h } = info;
  toAlpha(px);

  const ring = extract(px, w, h, 8, 20, 292, 300);
  toAlpha(ring.buf);
  const layers = splitRing(ring.buf, ring.width, ring.height, 144, 154);
  await saveRaw(ring.buf, ring.width, ring.height, path.join(HUD, "ui_stat_hud_ring_full_v1.png"));
  await saveRaw(layers.outer, ring.width, ring.height, path.join(HUD, "ui_stat_hud_ring_outer_v1.png"));
  await saveRaw(layers.inner, ring.width, ring.height, path.join(HUD, "ui_stat_hud_ring_inner_v1.png"));
  fs.copyFileSync(path.join(HUD, "ui_stat_hud_ring_full_v1.png"), path.join(DECOR, "ui_stat_hud_ring_v1.png"));

  const bar = extract(px, w, h, 280, 198, 720, 44);
  toAlpha(bar.buf);
  await saveRaw(bar.buf, bar.width, bar.height, path.join(HUD, "ui_stat_hud_bar_v1.png"));

  const wave = extract(px, w, h, 575, 78, 430, 112);
  toAlpha(wave.buf);
  await saveRaw(wave.buf, wave.width, wave.height, path.join(HUD, "ui_stat_hud_wave_base_v1.png"));
  const frames = waveformFrames(wave.buf, wave.width, wave.height);
  for (let i = 0; i < frames.length; i++) {
    const name = `ui_stat_hud_wave_${String(i).padStart(2, "0")}.png`;
    await saveRaw(frames[i], wave.width, wave.height, path.join(HUD, name));
  }

  const sheetW = wave.width * 4;
  const sheetH = wave.height * 4;
  const sheet = Buffer.alloc(sheetW * sheetH * 4);
  for (let i = 0; i < frames.length; i++) {
    const col = i % 4;
    const row = (i / 4) | 0;
    for (let y = 0; y < wave.height; y++) {
      const dest = ((row * wave.height + y) * sheetW + col * wave.width) * 4;
      frames[i].copy(sheet, dest, y * wave.width * 4, (y + 1) * wave.width * 4);
    }
  }
  await saveRaw(sheet, sheetW, sheetH, path.join(REF, "_ref_hud_wave_sheet.png"));
  console.log("ring", ring.width + "x" + ring.height, "wave", wave.width + "x" + wave.height, "frames", FRAME_COUNT);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
