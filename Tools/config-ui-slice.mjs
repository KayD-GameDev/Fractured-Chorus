import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import sharp from "sharp";

const ROOT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu";
const KIT = path.join(ROOT, "Kit");
const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";

function chromaScore(r, g, b) {
  return Math.hypot(r - 255, g - 0, b - 255);
}

function floodChroma(px, w, h, threshold = 118) {
  const visited = Buffer.alloc(w * h);
  const q = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= w || y >= h) return;
    const id = y * w + x;
    if (visited[id]) return;
    visited[id] = 1;
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
    if (chromaScore(px[i], px[i + 1], px[i + 2]) >= threshold) continue;
    px[i + 3] = 0;
    const x = id % w;
    const y = (id / w) | 0;
    push(x - 1, y);
    push(x + 1, y);
    push(x, y - 1);
    push(x, y + 1);
  }
}

function lumaMatte(px, w, h, floor = 48, knee = 0.35) {
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    const max = Math.max(r, g, b);
    if (max < floor) {
      px[o] = 0;
      px[o + 1] = 0;
      px[o + 2] = 0;
      px[o + 3] = 0;
      continue;
    }
    const luma = max / 255;
    const a = Math.min(1, luma / knee);
    const a8 = Math.round(a * 255);
    if (a8 < 8) {
      px[o] = 0;
      px[o + 1] = 0;
      px[o + 2] = 0;
      px[o + 3] = 0;
      continue;
    }
    px[o + 3] = a8;
  }
}

function boundsOf(px, w, h, pad = 8) {
  let minX = w;
  let minY = h;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[(y * w + x) * 4 + 3] < 8) continue;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }
  if (maxX < minX) return null;
  const left = Math.max(0, minX - pad);
  const top = Math.max(0, minY - pad);
  return {
    left,
    top,
    width: Math.min(w - left, maxX - minX + 1 + pad * 2),
    height: Math.min(h - top, maxY - minY + 1 + pad * 2),
  };
}

function components(px, w, h, minArea = 80) {
  const seen = Buffer.alloc(w * h);
  const out = [];
  const isInk = (id) => px[id * 4 + 3] >= 10;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const start = y * w + x;
      if (seen[start] || !isInk(start)) continue;
      const q = [start];
      seen[start] = 1;
      let minX = x;
      let minY = y;
      let maxX = x;
      let maxY = y;
      let area = 0;
      while (q.length) {
        const id = q.pop();
        area++;
        const cx = id % w;
        const cy = (id / w) | 0;
        if (cx < minX) minX = cx;
        if (cy < minY) minY = cy;
        if (cx > maxX) maxX = cx;
        if (cy > maxY) maxY = cy;
        const nbs = [id - 1, id + 1, id - w, id + w];
        for (const n of nbs) {
          if (n < 0 || n >= w * h || seen[n] || !isInk(n)) continue;
          const nx = n % w;
          const ny = (n / w) | 0;
          if (Math.abs(nx - cx) + Math.abs(ny - cy) !== 1) continue;
          seen[n] = 1;
          q.push(n);
        }
      }
      const bw = maxX - minX + 1;
      const bh = maxY - minY + 1;
      if (area < minArea || bw < 60 || bh < 50) continue;
      out.push({ minX, minY, maxX, maxY, area, cx: (minX + maxX) / 2, cy: (minY + maxY) / 2 });
    }
  }
  return out;
}

function clusterRows(boxes, rowGap = 48) {
  const sorted = [...boxes].sort((a, b) => a.cy - b.cy || a.cx - b.cx);
  const rows = [];
  for (const box of sorted) {
    const last = rows[rows.length - 1];
    if (!last || box.cy - last[0].cy > rowGap) {
      rows.push([box]);
    } else {
      last.push(box);
    }
  }
  for (const row of rows) row.sort((a, b) => a.cx - b.cx);
  return rows;
}

async function saveCrop(px, w, h, box, pad, dest) {
  const left = Math.max(0, box.minX - pad);
  const top = Math.max(0, box.minY - pad);
  const width = Math.min(w - left, box.maxX - box.minX + 1 + pad * 2);
  const height = Math.min(h - top, box.maxY - box.minY + 1 + pad * 2);
  await sharp(Buffer.from(px), { raw: { width: w, height: h, channels: 4 } })
    .extract({ left, top, width, height })
    .png()
    .toFile(dest);
  const meta = await sharp(dest).metadata();
  console.log("slice", path.basename(dest), meta.width + "x" + meta.height);
}

async function readRgba(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data, w: info.width, h: info.height };
}

async function writePng(px, w, h, dest, cropPad = 10) {
  const b = boundsOf(px, w, h, cropPad);
  let img = sharp(Buffer.from(px), { raw: { width: w, height: h, channels: 4 } });
  if (b) img = img.extract(b);
  await img.png().toFile(dest);
  const meta = await sharp(dest).metadata();
  console.log("wrote", path.basename(dest), meta.width + "x" + meta.height, "alpha", meta.hasAlpha);
}

fs.mkdirSync(KIT, { recursive: true });

const panelSrc = path.join(SRC, "ui_config_panel_chroma_v1.png");
const panel = await readRgba(panelSrc);
floodChroma(panel.data, panel.w, panel.h, 110);
await writePng(panel.data, panel.w, panel.h, path.join(KIT, "ui_config_panel_v1.png"), 12);

const widgetsSrc = path.join(SRC, "ui_config_widgets_v1.png");
const widgets = await readRgba(widgetsSrc);
lumaMatte(widgets.data, widgets.w, widgets.h);
await writePng(widgets.data, widgets.w, widgets.h, path.join(KIT, "ui_config_widgets_v1.png"), 8);

const kitSrc = path.join(KIT, "ui_config_kit_v1.png");
if (fs.existsSync(kitSrc)) {
  const kit = await readRgba(kitSrc);
  lumaMatte(kit.data, kit.w, kit.h);
  await writePng(kit.data, kit.w, kit.h, path.join(KIT, "ui_config_kit_v1.png"), 4);
}

const sliced = await readRgba(path.join(KIT, "ui_config_widgets_v1.png"));
const boxes = components(sliced.data, sliced.w, sliced.h, 2500);
const rows = clusterRows(boxes, 56);
console.log("widget rows", rows.map((r) => r.length).join(","));

const namesByRow = [
  ["ui_config_speaker_min_v1.png", "ui_config_slider_v1.png", "ui_config_speaker_max_v1.png", "ui_config_btn_minus_v1.png", "ui_config_btn_plus_v1.png"],
  ["ui_config_toggle_on_v1.png", "ui_config_toggle_off_v1.png"],
  ["ui_config_chip_normal_v1.png", "ui_config_chip_selected_v1.png"],
  ["ui_config_icon_note_v1.png", "ui_config_icon_brightness_v1.png", "ui_config_icon_skip_v1.png", "ui_config_icon_difficulty_v1.png"],
];

for (let i = 0; i < rows.length; i++) {
  const names = namesByRow[i] ?? rows[i].map((_, j) => `ui_config_row${i}_${j}.png`);
  for (let j = 0; j < rows[i].length; j++) {
    const name = names[j] ?? `ui_config_row${i}_${j}.png`;
    await saveCrop(sliced.data, sliced.w, sliced.h, rows[i][j], 10, path.join(KIT, name));
  }
}

function guid() {
  return crypto.randomBytes(16).toString("hex");
}

function spriteId() {
  return crypto.randomBytes(16).toString("hex");
}

function internalId() {
  const buf = crypto.randomBytes(8);
  buf[7] &= 0x7f;
  return buf.readBigUInt64LE(0).toString();
}

function writeSpriteMeta(pngPath, { border = "0,0,0,0", meshType = 0 } = {}) {
  const img = fs.readFileSync(pngPath);
  return sharp(img).metadata().then((meta) => {
    const name = path.basename(pngPath, ".png");
    const g = guid();
    const sid = spriteId();
    const iid = internalId();
    const [l, b, r, t] = border.split(",").map((n) => n.trim());
    const contents = `fileFormatVersion: 2
guid: ${g}
TextureImporter:
  internalIDToNameTable:
  - first:
      213: ${iid}
    second: ${name}_0
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
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: ${meshType}
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: ${l}, y: ${b}, z: ${r}, w: ${t}}
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
    sprites:
    - serializedVersion: 2
      name: ${name}_0
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: ${meta.width}
        height: ${meta.height}
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: ${l}, y: ${b}, z: ${r}, w: ${t}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: ${sid}
      internalID: ${iid}
      vertices: []
      indices: 
      edges: []
      weights: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
      ${name}_0: ${iid}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`;
    fs.writeFileSync(pngPath + ".meta", contents);
  });
}

const panelMetaPath = path.join(KIT, "ui_config_panel_v1.png");
const panelMeta = await sharp(panelMetaPath).metadata();
const sliceX = Math.max(48, Math.round(panelMeta.width * 0.18));
const sliceY = Math.max(36, Math.round(panelMeta.height * 0.42));

await writeSpriteMeta(panelMetaPath, {
  border: `${sliceX},${sliceY},${sliceX},${sliceY}`,
  meshType: 0,
});

const pngs = fs.readdirSync(KIT).filter((f) => f.endsWith(".png") && f !== "ui_config_panel_v1.png");
for (const f of pngs) {
  await writeSpriteMeta(path.join(KIT, f), { meshType: 0 });
}

if (!fs.existsSync(path.join(KIT, "Kit.meta")) && !fs.existsSync(path.join(ROOT, "Kit.meta"))) {
  fs.writeFileSync(
    path.join(ROOT, "Kit.meta"),
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

console.log("done");
