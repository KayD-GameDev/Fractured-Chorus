import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import sharp from "sharp";

const SRC =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_Stats_Mock_Kit-b8adc265-de4c-4a3a-8535-5c845bad1690.png";
const ROOT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu";
const REF = path.join(ROOT, "_ref");
const OUT = path.join(ROOT, "MockKit");
const KIT = path.join(ROOT, "Kit");
const ICONS = path.join(ROOT, "Icons");
const DECOR = path.join(ROOT, "Decor");
const SHEET = path.join(ROOT, "ui_stat_mock_kit_white_v1.png");

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

async function writeSpriteMeta(pngPath, border = [0, 0, 0, 0]) {
  if (fs.existsSync(pngPath + ".meta")) return;
  const [l, b, r, t] = border;
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

function clampRect(r, w, h) {
  const left = Math.max(0, Math.floor(r.left));
  const top = Math.max(0, Math.floor(r.top));
  const width = Math.max(1, Math.min(w - left, Math.floor(r.width)));
  const height = Math.max(1, Math.min(h - top, Math.floor(r.height)));
  return { left, top, width, height };
}

function padRect(r, pad, w, h) {
  return clampRect(
    { left: r.left - pad, top: r.top - pad, width: r.width + pad * 2, height: r.height + pad * 2 },
    w,
    h
  );
}

function extractRaw(px, w, h, rect) {
  const r = clampRect(rect, w, h);
  const out = Buffer.alloc(r.width * r.height * 4);
  for (let y = 0; y < r.height; y++) {
    const src = ((r.top + y) * w + r.left) * 4;
    px.copy(out, y * r.width * 4, src, src + r.width * 4);
  }
  return { buf: out, ...r };
}

async function savePng(buf, width, height, dest, border, holeFill) {
  const px = Buffer.from(buf);
  if (holeFill) fillLargeHoles(px, width, height, holeFill);
  await sharp(px, { raw: { width, height, channels: 4 } }).png().toFile(dest);
  await writeSpriteMeta(dest, border);
}

function isGlyphNavy(r, g, b) {
  return r < 70 && g < 80 && r + g < 130 && b > 70;
}

function sampleFill(px, width, height) {
  let r = 0;
  let g = 0;
  let b = 0;
  let n = 0;
  for (let i = 0; i < width * height; i++) {
    const o = i * 4;
    const pr = px[o];
    const pg = px[o + 1];
    const pb = px[o + 2];
    if (isGlyphNavy(pr, pg, pb)) continue;
    if (pr > 248 && pg > 248 && pb > 248) continue;
    r += pr;
    g += pg;
    b += pb;
    n++;
  }
  if (!n) return [220, 228, 245];
  return [(r / n) | 0, (g / n) | 0, (b / n) | 0];
}

function clearGlyph(buf, width, height) {
  const px = Buffer.from(buf);
  const fill = sampleFill(px, width, height);
  const navy = [];
  for (let i = 0; i < width * height; i++) {
    const o = i * 4;
    if (!isGlyphNavy(px[o], px[o + 1], px[o + 2])) continue;
    navy.push(i);
    px[o] = fill[0];
    px[o + 1] = fill[1];
    px[o + 2] = fill[2];
  }
  return { px, navy };
}

function paintDiamondFromPlus(buf, width, height) {
  return clearGlyph(buf, width, height).px;
}

function paintMinusFromPlus(buf, width, height) {
  const { px, navy } = clearGlyph(buf, width, height);
  if (!navy.length) return px;
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  for (const id of navy) {
    const x = id % width;
    const y = (id / width) | 0;
    if (x < minX) minX = x;
    if (y < minY) minY = y;
    if (x > maxX) maxX = x;
    if (y > maxY) maxY = y;
  }
  const cy = ((minY + maxY) / 2) | 0;
  const x0 = minX + 1;
  const x1 = maxX - 1;
  const t = Math.max(1, Math.round((maxY - minY) * 0.14));
  for (let y = cy - t; y <= cy + t; y++) {
    for (let x = x0; x <= x1; x++) {
      if (y < 0 || y >= height || x < 0 || x >= width) continue;
      const o = (y * width + x) * 4;
      px[o] = 22;
      px[o + 1] = 28;
      px[o + 2] = 78;
    }
  }
  return px;
}

async function copyFile(src, dest, border) {
  fs.copyFileSync(src, dest);
  await writeSpriteMeta(dest, border || [0, 0, 0, 0]);
}

async function main() {
  fs.mkdirSync(REF, { recursive: true });
  fs.mkdirSync(OUT, { recursive: true });
  fs.mkdirSync(KIT, { recursive: true });
  fs.mkdirSync(ICONS, { recursive: true });
  fs.mkdirSync(DECOR, { recursive: true });
  folderMeta(OUT);
  folderMeta(KIT);
  folderMeta(ICONS);
  folderMeta(DECOR);

  for (const f of fs.readdirSync(OUT)) {
    if (f.endsWith(".png") || f.endsWith(".json")) fs.unlinkSync(path.join(OUT, f));
  }

  fs.copyFileSync(SRC, path.join(REF, "_ref_stats_mock_kit_src.png"));

  const { data, info } = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const px = Buffer.from(data);
  const { width: w, height: h } = info;
  floodWhite(px, w, h);

  await sharp(px, { raw: { width: w, height: h, channels: 4 } }).png().toFile(SHEET);
  await writeSpriteMeta(SHEET);
  fs.copyFileSync(SHEET, path.join(REF, "_ref_stats_mock_kit_white.png"));

  const P = (l, t) => [l, t, l, t];
  const named = [
    { name: "header_hud", left: 8, top: 2, width: 258, height: 92, border: P(18), pad: 4 },
    { name: "panel_portrait", left: 13, top: 96, width: 276, height: 327, border: P(28), pad: 6 },
    { name: "panel_stats", left: 298, top: 86, width: 294, height: 342, border: P(22), pad: 6 },
    { name: "panel_skills", left: 608, top: 86, width: 403, height: 322, border: P(22), pad: 6 },
    { name: "slot_portrait_01", left: 262, top: 13, width: 74, height: 65, pad: 4 },
    { name: "slot_portrait_02", left: 337, top: 14, width: 75, height: 64, pad: 4 },
    { name: "slot_portrait_03", left: 414, top: 14, width: 76, height: 64, pad: 4 },
    { name: "slot_portrait_04", left: 490, top: 14, width: 76, height: 64, pad: 4 },
    { name: "slot_portrait_05", left: 569, top: 14, width: 76, height: 64, pad: 4 },
    { name: "slot_portrait_06", left: 649, top: 14, width: 76, height: 64, pad: 4 },
    { name: "slot_portrait_07", left: 727, top: 14, width: 75, height: 64, pad: 4 },
    { name: "slot_portrait_08", left: 807, top: 14, width: 76, height: 64, pad: 4 },
    { name: "btn_close", left: 940, top: 15, width: 66, height: 59, pad: 4 },
    { name: "btn_nav_stat", left: 14, top: 430, width: 194, height: 34, border: P(14), pad: 4 },
    { name: "btn_nav_bonds", left: 14, top: 467, width: 194, height: 34, border: P(14), pad: 4 },
    { name: "btn_nav_calendar", left: 14, top: 503, width: 194, height: 37, border: P(14), pad: 4 },
    { name: "btn_nav_system", left: 14, top: 541, width: 194, height: 36, border: P(14), pad: 4 },
    { name: "btn_empty_sm_01", left: 16, top: 582, width: 129, height: 45, border: P(16), pad: 4 },
    { name: "btn_empty_sm_02", left: 157, top: 584, width: 139, height: 43, border: P(16), pad: 4 },
    { name: "btn_empty_wide", left: 14, top: 629, width: 277, height: 42, border: P(16), pad: 4 },
    { name: "bar_progress", left: 222, top: 433, width: 223, height: 33, border: P(12), pad: 4 },
    { name: "bar_fill", left: 236, top: 442, width: 140, height: 16, border: [8, 6, 8, 6], pad: 2 },
    { name: "data_strip_01", left: 224, top: 472, width: 192, height: 62, border: P(16), pad: 4 },
    { name: "data_strip_02", left: 424, top: 471, width: 196, height: 64, border: P(16), pad: 4 },
    { name: "bar_elements", left: 223, top: 540, width: 280, height: 39, border: P(12), pad: 4 },
    { name: "btn_tag", left: 507, top: 539, width: 113, height: 27, border: P(10), pad: 3 },
    { name: "slider_01", left: 310, top: 609, width: 116, height: 21, pad: 3 },
    { name: "slider_02", left: 310, top: 631, width: 117, height: 22, pad: 3 },
    { name: "slider_03", left: 310, top: 653, width: 123, height: 22, pad: 3 },
    { name: "waveform_line", left: 440, top: 601, width: 141, height: 26, pad: 3 },
    { name: "node_line", left: 452, top: 634, width: 127, height: 11, pad: 3 },
    { name: "node_line_02", left: 452, top: 650, width: 127, height: 24, pad: 3 },
    { name: "icon_user", left: 640, top: 419, width: 52, height: 45, pad: 3 },
    { name: "icon_party", left: 701, top: 420, width: 51, height: 44, pad: 3 },
    { name: "icon_calendar", left: 759, top: 420, width: 51, height: 44, pad: 3 },
    { name: "icon_system", left: 817, top: 420, width: 51, height: 44, pad: 3 },
    { name: "icon_search", left: 876, top: 424, width: 75, height: 34, pad: 3 },
    { name: "icon_close", left: 958, top: 419, width: 50, height: 45, pad: 3 },
    { name: "bar_thin", left: 632, top: 476, width: 185, height: 22, border: P(8), pad: 3 },
    { name: "icon_ff", left: 816, top: 483, width: 42, height: 33, pad: 3 },
    { name: "icon_play", left: 856, top: 476, width: 39, height: 35, pad: 3 },
    { name: "hud_ring", left: 900, top: 472, width: 104, height: 103, pad: 6, holeFill: 900 },
    { name: "crystal_01", left: 697, top: 509, width: 58, height: 85, pad: 4 },
    { name: "crystal_02", left: 752, top: 507, width: 40, height: 82, pad: 4 },
    { name: "crystal_03", left: 768, top: 521, width: 81, height: 82, pad: 4 },
    { name: "crystal_04", left: 800, top: 552, width: 52, height: 76, pad: 4 },
    { name: "crystal_05", left: 864, top: 517, width: 72, height: 147, pad: 4 },
    { name: "sparkle_01", left: 971, top: 573, width: 41, height: 41, pad: 4, holeFill: 200 },
    { name: "sparkle_02", left: 926, top: 593, width: 57, height: 61, pad: 4, holeFill: 200 },
    { name: "nav_chevrons", left: 603, top: 588, width: 180, height: 40, border: P(12), pad: 4 },
    { name: "bar_gradient", left: 603, top: 635, width: 245, height: 34, border: P(12), pad: 4 },
    { name: "icon_note", left: 258, top: 128, width: 22, height: 22, pad: 1 },
    { name: "icon_waveform", left: 248, top: 375, width: 32, height: 32, pad: 1 },
    { name: "icon_plus", left: 648, top: 232, width: 24, height: 24, pad: 1 }
  ];

  const manifest = [];
  const files = new Map();

  for (const item of named) {
    const rect = padRect(item, item.pad ?? 4, w, h);
    const cut = extractRaw(px, w, h, rect);
    const dest = path.join(OUT, `ui_stat_${item.name}.png`);
    await savePng(cut.buf, cut.width, cut.height, dest, item.border || [0, 0, 0, 0], item.holeFill || 0);
    files.set(item.name, dest);
    manifest.push({ name: item.name, width: cut.width, height: cut.height, left: cut.left, top: cut.top });
  }

  for (let row = 0; row < 2; row++) {
    for (let col = 0; col < 5; col++) {
      const idx = row * 5 + col + 1;
      const rect = padRect(
        { left: 618 + col * 78, top: 132 + row * 132, width: 74, height: 126 },
        3,
        w,
        h
      );
      const cut = extractRaw(px, w, h, rect);
      const name = `slot_skill_${String(idx).padStart(2, "0")}`;
      const dest = path.join(OUT, `ui_stat_${name}.png`);
      await savePng(cut.buf, cut.width, cut.height, dest, P(14), 0);
      files.set(name, dest);
      manifest.push({ name, width: cut.width, height: cut.height, left: cut.left, top: cut.top });
    }
  }

  const elements = ["fire", "ice", "lightning", "wind", "sun", "void"];
  for (let i = 0; i < 6; i++) {
    const rect = padRect({ left: 230 + i * 46, top: 544, width: 38, height: 32 }, 1, w, h);
    const cut = extractRaw(px, w, h, rect);
    const name = `icon_element_${elements[i]}`;
    const dest = path.join(OUT, `ui_stat_${name}.png`);
    await savePng(cut.buf, cut.width, cut.height, dest, [0, 0, 0, 0], 0);
    files.set(name, dest);
    manifest.push({ name, width: cut.width, height: cut.height, left: cut.left, top: cut.top });
  }

  const plusSrc = files.get("icon_plus");
  if (plusSrc) {
    const { data: plusData, info: plusInfo } = await sharp(plusSrc).ensureAlpha().raw().toBuffer({
      resolveWithObject: true
    });
    const diamondBuf = paintDiamondFromPlus(plusData, plusInfo.width, plusInfo.height);
    const diamondDest = path.join(OUT, "ui_stat_icon_diamond.png");
    await sharp(diamondBuf, { raw: { width: plusInfo.width, height: plusInfo.height, channels: 4 } })
      .png()
      .toFile(diamondDest);
    await writeSpriteMeta(diamondDest);
    files.set("icon_diamond", diamondDest);
    manifest.push({ name: "icon_diamond", width: plusInfo.width, height: plusInfo.height });

    const minusBuf = paintMinusFromPlus(plusData, plusInfo.width, plusInfo.height);
    const minusDest = path.join(OUT, "ui_stat_icon_minus.png");
    await sharp(minusBuf, { raw: { width: plusInfo.width, height: plusInfo.height, channels: 4 } })
      .png()
      .toFile(minusDest);
    await writeSpriteMeta(minusDest);
    files.set("icon_minus", minusDest);
    manifest.push({ name: "icon_minus", width: plusInfo.width, height: plusInfo.height });
  }

  const kitMap = {
    panel_portrait: ["ui_stat_panel_v1.png", "ui_stat_panel_portrait_v1.png"],
    panel_stats: ["ui_stat_panel_stats_v1.png"],
    panel_skills: ["ui_stat_panel_skills_v1.png"],
    header_hud: ["ui_stat_header_v1.png"],
    slot_portrait_01: ["ui_stat_slot_portrait_v1.png"],
    btn_close: ["ui_stat_btn_close_v1.png"],
    btn_nav_stat: ["ui_stat_btn_nav_v1.png", "ui_stat_btn_nav_selected_v1.png"],
    btn_nav_bonds: ["ui_stat_btn_nav_bonds_v1.png"],
    btn_nav_calendar: ["ui_stat_btn_nav_calendar_v1.png"],
    btn_nav_system: ["ui_stat_btn_nav_system_v1.png"],
    slot_skill_01: ["ui_stat_slot_skill_v1.png", "ui_stat_slot_skill_01.png"],
    slot_skill_02: ["ui_stat_slot_skill_02.png"],
    slot_skill_03: ["ui_stat_slot_skill_03.png"],
    slot_skill_04: ["ui_stat_slot_skill_04.png"],
    slot_skill_05: ["ui_stat_slot_skill_05.png"],
    bar_progress: ["ui_stat_row_v1.png", "ui_stat_bar_track_v1.png"],
    bar_fill: ["ui_stat_bar_fill_v1.png"]
  };
  for (const [key, names] of Object.entries(kitMap)) {
    const src = files.get(key);
    if (!src) continue;
    for (const name of names) {
      await copyFile(src, path.join(KIT, name), P(16));
    }
  }

  const iconMap = {
    icon_user: ["ui_stat_icon_user_v1.png"],
    icon_party: ["ui_stat_icon_party_v1.png"],
    icon_calendar: ["ui_stat_icon_calendar_v1.png"],
    icon_system: ["ui_stat_icon_system_v1.png"],
    icon_close: ["ui_stat_icon_close_v1.png"],
    icon_plus: ["ui_stat_icon_plus_v1.png"],
    icon_minus: ["ui_stat_icon_minus_v1.png"],
    icon_diamond: [
      "ui_stat_icon_diamond_v1.png",
      "ui_stat_icon_strength_v1.png",
      "ui_stat_icon_magic_v1.png",
      "ui_stat_icon_endurance_v1.png",
      "ui_stat_icon_luck_v1.png"
    ],
    icon_waveform: ["ui_stat_icon_heartbeat_v1.png"],
    icon_note: ["ui_stat_icon_note_v1.png"],
    icon_element_void: ["ui_stat_icon_harmony_v1.png"],
    icon_element_fire: ["ui_stat_icon_element_fire_v1.png"],
    icon_element_ice: ["ui_stat_icon_element_ice_v1.png"],
    icon_element_lightning: ["ui_stat_icon_element_lightning_v1.png"],
    icon_element_wind: ["ui_stat_icon_element_wind_v1.png"],
    icon_element_sun: ["ui_stat_icon_element_sun_v1.png"],
    icon_search: ["ui_stat_icon_search_v1.png"]
  };
  for (const [key, names] of Object.entries(iconMap)) {
    const src = files.get(key);
    if (!src) continue;
    for (const name of names) {
      await copyFile(src, path.join(ICONS, name));
    }
  }

  const decorMap = {
    hud_ring: ["ui_stat_hud_ring_v1.png"],
    crystal_03: ["ui_stat_crystal_v1.png"],
    crystal_01: ["ui_stat_crystal_01.png"],
    crystal_02: ["ui_stat_crystal_02.png"],
    crystal_04: ["ui_stat_crystal_04.png"],
    crystal_05: ["ui_stat_crystal_05.png"],
    sparkle_01: ["ui_stat_sparkle_v1.png"],
    sparkle_02: ["ui_stat_sparkle_02.png"]
  };
  for (const [key, names] of Object.entries(decorMap)) {
    const src = files.get(key);
    if (!src) continue;
    for (const name of names) {
      await copyFile(src, path.join(DECOR, name));
    }
  }

  fs.writeFileSync(path.join(OUT, "manifest.json"), JSON.stringify(manifest, null, 2));
  console.log("sheet", w + "x" + h);
  console.log("widgets", manifest.length);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
