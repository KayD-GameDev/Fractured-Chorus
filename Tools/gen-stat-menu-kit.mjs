import fs from "node:fs";
import path from "node:path";
import crypto from "node:crypto";
import sharp from "sharp";

const ROOT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu";
const KIT = path.join(ROOT, "Kit");
const ICONS = path.join(ROOT, "Icons");
const DECOR = path.join(ROOT, "Decor");
const SHEET = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/ui_stat_kit_sheet_v1.png";
const ICON_SHEET = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/ui_stat_icon_sheet_v1.png";

function ensureDir(dir) {
  fs.mkdirSync(dir, { recursive: true });
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
  const meta = await sharp(pngPath).metadata();
  const name = path.basename(pngPath, ".png");
  const g = guid();
  const sid = spriteId();
  const iid = internalId();
  const [l, b, r, t] = border;
  fs.writeFileSync(
    pngPath + ".meta",
    `fileFormatVersion: 2
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

function rgba(w, h) {
  return { px: Buffer.alloc(w * h * 4), w, h };
}

function setPx(buf, w, x, y, r, g, b, a) {
  if (x < 0 || y < 0 || x >= w || y >= buf.length / 4 / w) return;
  const i = (y * w + x) * 4;
  buf[i] = r;
  buf[i + 1] = g;
  buf[i + 2] = b;
  buf[i + 3] = a;
}

function chamferOutside(x, y, w, h, c) {
  return x + y < c || w - 1 - x + y < c || x + h - 1 - y < c || w - 1 - x + h - 1 - y < c;
}

function edgeDist(x, y, w, h, c) {
  const d = [x, y, w - 1 - x, h - 1 - y, x + y - c, w - 1 - x + y - c, x + h - 1 - y - c, w - 1 - x + h - 1 - y - c];
  return Math.min(...d);
}

function drawGlassPanel(w, h, chamfer, borderW) {
  const { px } = rgba(w, h);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (chamferOutside(x, y, w, h, chamfer)) continue;
      const d = edgeDist(x, y, w, h, chamfer);
      const nx = x / (w - 1);
      const ny = y / (h - 1);
      const fillA = 48 + Math.round(18 * (1 - ny) + 10 * nx);
      let r = 228;
      let g = 224;
      let b = 245;
      let a = fillA;
      if (d < borderW + 6) {
        const glow = 1 - d / (borderW + 6);
        r = Math.round(210 + 45 * glow);
        g = Math.round(200 + 50 * glow);
        b = 255;
        a = Math.round(90 + 140 * glow);
      }
      if (d < borderW) {
        r = 245;
        g = 250;
        b = 255;
        a = 230;
      }
      setPx(px, w, x, y, r, g, b, a);
    }
  }
  return px;
}

function drawCapsule(w, h, fill) {
  const { px } = rgba(w, h);
  const rad = h / 2 - 1;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      let dx = 0;
      if (x < rad) dx = x - rad;
      else if (x > w - 1 - rad) dx = x - (w - 1 - rad);
      const dy = y - (h / 2 - 0.5);
      const dist = Math.hypot(dx, dy);
      if (dist > rad) continue;
      const edge = rad - dist;
      if (fill) {
        const t = x / (w - 1);
        const r = Math.round(210 + 20 * t);
        const g = Math.round(160 + 60 * t);
        const b = Math.round(230 + 20 * (1 - t));
        const a = edge < 2 ? 255 : 235;
        setPx(px, w, x, y, r, g, b, a);
      } else {
        const a = edge < 2.2 ? 210 : 70;
        setPx(px, w, x, y, 186, 190, 230, a);
      }
    }
  }
  return px;
}

async function saveRaw(px, w, h, dest, border) {
  await sharp(px, { raw: { width: w, height: h, channels: 4 } }).png().toFile(dest);
  await writeSpriteMeta(dest, border);
  console.log("kit", path.basename(dest), w + "x" + h);
}

function blend(px, w, h, x, y, r, g, b, a) {
  x = Math.round(x);
  y = Math.round(y);
  if (x < 0 || y < 0 || x >= w || y >= h) return;
  const i = (y * w + x) * 4;
  const oa = px[i + 3] / 255;
  const na = a / 255;
  const outA = na + oa * (1 - na);
  if (outA <= 0) return;
  px[i] = Math.round((r * na + px[i] * oa * (1 - na)) / outA);
  px[i + 1] = Math.round((g * na + px[i + 1] * oa * (1 - na)) / outA);
  px[i + 2] = Math.round((b * na + px[i + 2] * oa * (1 - na)) / outA);
  px[i + 3] = Math.round(outA * 255);
}

function disc(px, w, h, cx, cy, rad, r, g, b, a) {
  const rr = rad * rad;
  for (let y = Math.floor(cy - rad); y <= cy + rad; y++) {
    for (let x = Math.floor(cx - rad); x <= cx + rad; x++) {
      const d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
      if (d2 > rr) continue;
      const edge = 1 - Math.sqrt(d2) / rad;
      blend(px, w, h, x, y, r, g, b, Math.round(a * Math.min(1, edge * 4)));
    }
  }
}

function ring(px, w, h, cx, cy, rad, thick, r, g, b, a) {
  const outer = rad + thick * 0.5;
  const inner = Math.max(0, rad - thick * 0.5);
  for (let y = Math.floor(cy - outer); y <= cy + outer; y++) {
    for (let x = Math.floor(cx - outer); x <= cx + outer; x++) {
      const d = Math.hypot(x - cx, y - cy);
      if (d > outer || d < inner) continue;
      const t = 1 - Math.abs(d - rad) / (thick * 0.5);
      blend(px, w, h, x, y, r, g, b, Math.round(a * Math.max(0, t)));
    }
  }
}

function line(px, w, h, x0, y0, x1, y1, thick, r, g, b, a) {
  const n = Math.max(1, Math.hypot(x1 - x0, y1 - y0));
  for (let i = 0; i <= n; i++) {
    const t = i / n;
    disc(px, w, h, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, thick * 0.5, r, g, b, a);
  }
}

function glowIcon(draw) {
  const s = 96;
  const { px, w, h } = rgba(s, s);
  draw(px, w, h, 48, 48, 18, 210, 200, 255, 90);
  draw(px, w, h, 48, 48, 10, 245, 250, 255, 230);
  return { px, w, h };
}

async function saveIcon(name, draw) {
  const { px, w, h } = glowIcon(draw);
  const dest = path.join(ICONS, name + ".png");
  await saveRaw(px, w, h, dest, [0, 0, 0, 0]);
}

async function main() {
  ensureDir(KIT);
  ensureDir(ICONS);
  ensureDir(DECOR);
  folderMeta(ROOT);
  folderMeta(KIT);
  folderMeta(ICONS);
  folderMeta(DECOR);
  folderMeta(path.join(ROOT, "_ref"));

  await saveRaw(drawGlassPanel(256, 256, 36, 3), 256, 256, path.join(KIT, "ui_stat_panel_v1.png"), [40, 40, 40, 40]);
  await saveRaw(drawGlassPanel(512, 96, 22, 3), 512, 96, path.join(KIT, "ui_stat_header_v1.png"), [28, 28, 28, 28]);
  await saveRaw(drawGlassPanel(280, 64, 16, 2), 280, 64, path.join(KIT, "ui_stat_btn_nav_v1.png"), [20, 20, 20, 20]);
  await saveRaw(drawGlassPanel(280, 64, 16, 3), 280, 64, path.join(KIT, "ui_stat_btn_nav_selected_v1.png"), [20, 20, 20, 20]);
  await saveRaw(drawGlassPanel(200, 240, 28, 3), 200, 240, path.join(KIT, "ui_stat_slot_skill_v1.png"), [32, 32, 32, 32]);
  await saveRaw(drawGlassPanel(96, 96, 18, 3), 96, 96, path.join(KIT, "ui_stat_slot_portrait_v1.png"), [22, 22, 22, 22]);
  await saveRaw(drawGlassPanel(72, 72, 14, 3), 72, 72, path.join(KIT, "ui_stat_btn_close_v1.png"), [16, 16, 16, 16]);
  await saveRaw(drawGlassPanel(320, 56, 14, 2), 320, 56, path.join(KIT, "ui_stat_row_v1.png"), [18, 18, 18, 18]);
  await saveRaw(drawCapsule(256, 36, false), 256, 36, path.join(KIT, "ui_stat_bar_track_v1.png"), [18, 16, 18, 16]);
  await saveRaw(drawCapsule(256, 36, true), 256, 36, path.join(KIT, "ui_stat_bar_fill_v1.png"), [18, 16, 18, 16]);

  await saveIcon("ui_stat_icon_plus_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    line(px, w, h, cx - 16, cy, cx + 16, cy, t, r, g, b, a);
    line(px, w, h, cx, cy - 16, cx, cy + 16, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_minus_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    line(px, w, h, cx - 16, cy, cx + 16, cy, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_close_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    line(px, w, h, cx - 14, cy - 14, cx + 14, cy + 14, t, r, g, b, a);
    line(px, w, h, cx + 14, cy - 14, cx - 14, cy + 14, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_diamond_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    line(px, w, h, cx, cy - 18, cx + 14, cy, t, r, g, b, a);
    line(px, w, h, cx + 14, cy, cx, cy + 18, t, r, g, b, a);
    line(px, w, h, cx, cy + 18, cx - 14, cy, t, r, g, b, a);
    line(px, w, h, cx - 14, cy, cx, cy - 18, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_user_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    ring(px, w, h, cx, cy - 10, 9, t, r, g, b, a);
    line(px, w, h, cx - 16, cy + 18, cx + 16, cy + 18, t, r, g, b, a);
    line(px, w, h, cx - 16, cy + 18, cx - 12, cy + 4, t, r, g, b, a);
    line(px, w, h, cx + 16, cy + 18, cx + 12, cy + 4, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_party_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    ring(px, w, h, cx - 8, cy - 8, 8, t, r, g, b, a);
    ring(px, w, h, cx + 10, cy - 4, 8, t, r, g, b, a);
    line(px, w, h, cx - 22, cy + 18, cx + 4, cy + 18, t, r, g, b, a);
    line(px, w, h, cx - 4, cy + 18, cx + 22, cy + 18, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_calendar_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    line(px, w, h, cx - 16, cy - 12, cx + 16, cy - 12, t, r, g, b, a);
    line(px, w, h, cx - 16, cy + 16, cx + 16, cy + 16, t, r, g, b, a);
    line(px, w, h, cx - 16, cy - 12, cx - 16, cy + 16, t, r, g, b, a);
    line(px, w, h, cx + 16, cy - 12, cx + 16, cy + 16, t, r, g, b, a);
    line(px, w, h, cx - 16, cy - 4, cx + 16, cy - 4, t, r, g, b, a);
    line(px, w, h, cx - 6, cy - 18, cx - 6, cy - 8, t, r, g, b, a);
    line(px, w, h, cx + 6, cy - 18, cx + 6, cy - 8, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_system_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    ring(px, w, h, cx, cy, 8, t, r, g, b, a);
    ring(px, w, h, cx, cy, 16, t, r, g, b, a);
    for (let i = 0; i < 6; i++) {
      const ang = (i / 6) * Math.PI * 2;
      line(px, w, h, cx + Math.cos(ang) * 12, cy + Math.sin(ang) * 12, cx + Math.cos(ang) * 20, cy + Math.sin(ang) * 20, t, r, g, b, a);
    }
  });
  await saveIcon("ui_stat_icon_lock_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    ring(px, w, h, cx, cy - 10, 8, t, r, g, b, a);
    line(px, w, h, cx - 12, cy - 2, cx + 12, cy - 2, t, r, g, b, a);
    line(px, w, h, cx - 12, cy + 16, cx + 12, cy + 16, t, r, g, b, a);
    line(px, w, h, cx - 12, cy - 2, cx - 12, cy + 16, t, r, g, b, a);
    line(px, w, h, cx + 12, cy - 2, cx + 12, cy + 16, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_note_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    disc(px, w, h, cx - 8, cy + 12, 6, r, g, b, a);
    line(px, w, h, cx - 2, cy + 12, cx - 2, cy - 16, t, r, g, b, a);
    line(px, w, h, cx - 2, cy - 16, cx + 12, cy - 12, t, r, g, b, a);
    line(px, w, h, cx + 12, cy - 12, cx + 12, cy + 8, t, r, g, b, a);
    disc(px, w, h, cx + 6, cy + 8, 6, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_heartbeat_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    line(px, w, h, cx - 22, cy, cx - 10, cy, t, r, g, b, a);
    line(px, w, h, cx - 10, cy, cx - 4, cy - 14, t, r, g, b, a);
    line(px, w, h, cx - 4, cy - 14, cx + 4, cy + 14, t, r, g, b, a);
    line(px, w, h, cx + 4, cy + 14, cx + 10, cy, t, r, g, b, a);
    line(px, w, h, cx + 10, cy, cx + 22, cy, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_magic_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    ring(px, w, h, cx, cy, 16, t, r, g, b, a);
    ring(px, w, h, cx + 4, cy - 2, 8, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_harmony_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    line(px, w, h, cx, cy - 18, cx + 16, cy + 14, t, r, g, b, a);
    line(px, w, h, cx + 16, cy + 14, cx - 16, cy + 14, t, r, g, b, a);
    line(px, w, h, cx - 16, cy + 14, cx, cy - 18, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_strength_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    line(px, w, h, cx, cy - 18, cx + 14, cy, t, r, g, b, a);
    line(px, w, h, cx + 14, cy, cx, cy + 18, t, r, g, b, a);
    line(px, w, h, cx, cy + 18, cx - 14, cy, t, r, g, b, a);
    line(px, w, h, cx - 14, cy, cx, cy - 18, t, r, g, b, a);
    line(px, w, h, cx - 8, cy, cx + 8, cy, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_endurance_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    ring(px, w, h, cx, cy, 16, t, r, g, b, a);
    line(px, w, h, cx - 8, cy + 4, cx, cy + 12, t, r, g, b, a);
    line(px, w, h, cx, cy + 12, cx + 10, cy - 8, t, r, g, b, a);
  });
  await saveIcon("ui_stat_icon_luck_v1", (px, w, h, cx, cy, t, r, g, b, a) => {
    for (let i = 0; i < 4; i++) {
      const ang = (i / 4) * Math.PI * 2 - Math.PI / 2;
      line(px, w, h, cx, cy, cx + Math.cos(ang) * 18, cy + Math.sin(ang) * 18, t, r, g, b, a);
    }
    disc(px, w, h, cx, cy, 4, r, g, b, a);
  });
  const ringBuf = rgba(160, 160);
  ring(ringBuf.px, 160, 160, 80, 80, 62, 6, 210, 200, 255, 90);
  ring(ringBuf.px, 160, 160, 80, 80, 62, 3, 245, 250, 255, 230);
  ring(ringBuf.px, 160, 160, 80, 80, 48, 2, 200, 220, 255, 160);
  await saveRaw(ringBuf.px, 160, 160, path.join(DECOR, "ui_stat_hud_ring_v1.png"), [0, 0, 0, 0]);

  const shard = rgba(96, 128);
  line(shard.px, 96, 128, 48, 8, 80, 64, 8, 220, 230, 255, 200);
  line(shard.px, 96, 128, 80, 64, 48, 120, 8, 220, 230, 255, 200);
  line(shard.px, 96, 128, 48, 120, 16, 64, 8, 220, 230, 255, 200);
  line(shard.px, 96, 128, 16, 64, 48, 8, 8, 220, 230, 255, 200);
  line(shard.px, 96, 128, 48, 8, 48, 120, 4, 245, 250, 255, 180);
  await saveRaw(shard.px, 96, 128, path.join(DECOR, "ui_stat_crystal_v1.png"), [0, 0, 0, 0]);

  const genDir = path.join(ROOT, "_gen");
  ensureDir(genDir);
  folderMeta(genDir);
  fs.copyFileSync(SHEET, path.join(genDir, "ui_stat_kit_sheet_v1.png"));
  fs.copyFileSync(ICON_SHEET, path.join(genDir, "ui_stat_icon_sheet_v1.png"));

  console.log("done");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
