import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import sharp from "sharp";

const HUD = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/Decor/Hud";
const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/Kit";
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

async function loadRaw(pngPath) {
  const { data, info } = await sharp(pngPath).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { px: Buffer.from(data), w: info.width, h: info.height };
}

async function saveRaw(px, w, h, dest) {
  await sharp(px, { raw: { width: w, height: h, channels: 4 } }).png().toFile(dest);
  await writeSpriteMeta(dest);
}

async function splitRingStub() {
  const fullPath = path.join(HUD, "ui_stat_hud_ring_full_v1.png");
  const outerPath = path.join(HUD, "ui_stat_hud_ring_outer_v1.png");
  const { px, w, h } = await loadRaw(fullPath);
  let sx = 0;
  let sy = 0;
  let n = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const a = px[(y * w + x) * 4 + 3];
      if (a < 12) continue;
      sx += x;
      sy += y;
      n++;
    }
  }
  const cx = n ? sx / n : w * 0.5;
  const cy = n ? sy / n : h * 0.5;
  let maxR = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[(y * w + x) * 4 + 3] < 12) continue;
      if (x > w * 0.78) continue;
      maxR = Math.max(maxR, Math.hypot(x - cx, y - cy));
    }
  }

  const rSplit = maxR * 0.78;
  const rHole = maxR * 0.46;
  const outer = Buffer.alloc(w * h * 4);
  const stub = Buffer.alloc(w * h * 4);
  let stubCount = 0;
  let outerCount = 0;
  const stubX = w * 0.84;
  const stubHalfH = h * 0.13;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const o = (y * w + x) * 4;
      if (px[o + 3] < 8) continue;
      const dx = x - cx;
      const dy = y - cy;
      const r = Math.hypot(dx, dy);
      if (r <= rHole) continue;
      const stubHit = x >= stubX && Math.abs(dy) <= stubHalfH;
      if (stubHit) {
        px.copy(stub, o, o, o + 4);
        stubCount++;
        continue;
      }
      if (r <= rSplit) continue;
      px.copy(outer, o, o, o + 4);
      outerCount++;
    }
  }

  await saveRaw(outer, w, h, outerPath);
  await saveRaw(stub, w, h, path.join(HUD, "ui_stat_hud_ring_stub_v1.png"));
  console.log("ring stub", {
    w,
    h,
    cx: +cx.toFixed(1),
    cy: +cy.toFixed(1),
    maxR: +maxR.toFixed(1),
    rSplit: +rSplit.toFixed(1),
    stubCount,
    outerCount
  });
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

async function regenWave() {
  const basePath = path.join(HUD, "ui_stat_hud_wave_base_v1.png");
  const { px, w, h } = await loadRaw(basePath);
  const frames = waveformFrames(px, w, h, WAVE_FRAMES);
  for (let i = 0; i < frames.length; i++) {
    const name = `ui_stat_hud_wave_${String(i).padStart(2, "0")}.png`;
    await saveRaw(frames[i], w, h, path.join(HUD, name));
  }
  console.log("wave frames", WAVE_FRAMES, w + "x" + h);
}

function luma(px, o) {
  return Math.max(px[o], px[o + 1], px[o + 2]);
}

function sdfBox(px, py, hw, hh, r) {
  const ax = Math.abs(px) - hw + r;
  const ay = Math.abs(py) - hh + r;
  const ox = Math.max(ax, 0);
  const oy = Math.max(ay, 0);
  return Math.hypot(ox, oy) + Math.min(Math.max(ax, ay), 0) - r;
}

function sdfRing(px, py, rOuter, rInner) {
  const r = Math.hypot(px, py);
  return Math.max(r - rOuter, rInner - r);
}

function smooth(a) {
  const t = Math.max(0, Math.min(1, 0.5 - a));
  return t * t * (3 - 2 * t);
}

function lockDistance(lx, ly) {
  const body = sdfBox(lx, ly - 10, 20, 16, 5);
  let shackle = sdfRing(lx, ly + 16, 17, 10.4);
  if (ly > 4) shackle = 40;
  const key = Math.hypot(lx, ly - 7) - 3.6;
  let d = Math.min(body, shackle);
  if (body < 1.2) {
    d = Math.max(d, -key);
  }
  return d;
}

function mix(px, o, r, g, b, a) {
  const ia = 1 - a;
  px[o] = px[o] * ia + r * a;
  px[o + 1] = px[o + 1] * ia + g * a;
  px[o + 2] = px[o + 2] * ia + b * a;
  px[o + 3] = Math.min(255, px[o + 3] * ia + 255 * a);
}

async function cleanLock() {
  const dest = path.join(KIT, "ui_stat_slot_portrait_locked_v1.png");
  const src = path.join(KIT, "../MockKit/ui_stat_slot_portrait_locked.png");
  fs.copyFileSync(src, dest);
  const { px, w, h } = await loadRaw(dest);
  const ox = (w - 1) * 0.5;
  const oy = (h - 1) * 0.5 + 6;

  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const lx = x - ox;
      const ly = y - oy;
      const d = lockDistance(lx, ly);
      const backing = 1 - Math.max(0, Math.min(1, (d + 7) / 3.2));
      if (backing > 0.02) {
        mix(px, (y * w + x) * 4, 168, 186, 230, backing * 0.82);
      }
      const fill = 1 - Math.max(0, Math.min(1, (d + 0.7) / 1.5));
      if (fill > 0.02) {
        mix(px, (y * w + x) * 4, 252, 252, 255, fill);
      }
    }
  }

  await saveRaw(px, w, h, dest);
  console.log("lock cleaned", { ox: +ox.toFixed(1), oy: +oy.toFixed(1) });
}

async function main() {
  await splitRingStub();
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
