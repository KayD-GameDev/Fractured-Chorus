import crypto from "crypto";
import fs from "fs";
import path from "path";
import sharp from "sharp";

const ROOT = "D:/Fractured-Chorus1";
const CARDS = path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/Cards");
const SCENE = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");
const GEN = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";

const FILES = [
  { name: "bond_card_ren.png", guid: "7a1c2e3f4b5d67890a1c2e3f4b5d6789" },
  { name: "bond_card_charlotte.png", guid: "8b2d3f4a5c6e78901b2d3f4a5c6e7890" },
  { name: "bond_card_coda.png", guid: "9c3e4a5b6d7f89012c3e4a5b6d7f8901" },
  { name: "bond_card_astra.png", guid: "ad4f5b6c7e809123ad4f5b6c7e809123" },
  { name: "bond_card_ryo.png", guid: "cf617d8e0a021345cf617d8e0a021345" },
  { name: "bond_card_meilin.png", guid: "be506c7d8f910234be506c7d8f910234" },
];

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

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function punchCorners(data, width, height) {
  const n = width * height;
  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let head = 0;
  let tail = 0;
  const tryPush = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const idx = y * width + x;
    if (seen[idx]) return;
    const i = idx * 4;
    const yv = luma(data[i], data[i + 1], data[i + 2]);
    const c = chroma(data[i], data[i + 1], data[i + 2]);
    if (yv < 246 || c > 10) return;
    seen[idx] = 1;
    queue[tail++] = idx;
  };
  tryPush(0, 0);
  tryPush(width - 1, 0);
  tryPush(0, height - 1);
  tryPush(width - 1, height - 1);
  let punched = 0;
  while (head < tail) {
    const idx = queue[head++];
    const x = idx % width;
    const y = (idx / width) | 0;
    const i = idx * 4;
    data[i] = 0;
    data[i + 1] = 0;
    data[i + 2] = 0;
    data[i + 3] = 0;
    punched += 1;
    tryPush(x - 1, y);
    tryPush(x + 1, y);
    tryPush(x, y - 1);
    tryPush(x, y + 1);
  }
  return punched;
}

async function importCard({ name, guid }) {
  const src = path.join(GEN, name);
  const dest = path.join(CARDS, name);
  const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  const punched = punchCorners(pixels, info.width, info.height);
  await sharp(pixels, { raw: { width: info.width, height: info.height, channels: 4 } })
    .png()
    .toFile(dest);
  fs.writeFileSync(`${dest}.meta`, spriteMeta(guid));
  return { name, guid, punched, size: [info.width, info.height] };
}

const imported = [];
for (const file of FILES) {
  imported.push(await importCard(file));
}

let scene = fs.readFileSync(SCENE, "utf8");
scene = scene.replace(
  `  portraitSprites:
  - {fileID: 21300000, guid: 953c91274e2e403a8a8b556cfb1e2038, type: 3}
  - {fileID: 21300000, guid: f7a2c8e14b3d4f6a9e0b1c2d3e4f5a6b, type: 3}
  - {fileID: 21300000, guid: e8b3d9f25c4e5a7b0f1c2d3e4f5a6b7c, type: 3}
  - {fileID: -7773200447062792678, guid: ac0327a8d7b5ae04da49dbfd0a69df2c, type: 3}
  - {fileID: 21300000, guid: 46a2f120e5a9437d9f9f8b2444c4c10b, type: 3}
  - {fileID: 21300000, guid: 46a2f120e5a9437d9f9f8b2444c4c10b, type: 3}`,
  `  portraitSprites:
  - {fileID: 21300000, guid: 7a1c2e3f4b5d67890a1c2e3f4b5d6789, type: 3}
  - {fileID: 21300000, guid: 8b2d3f4a5c6e78901b2d3f4a5c6e7890, type: 3}
  - {fileID: 21300000, guid: 9c3e4a5b6d7f89012c3e4a5b6d7f8901, type: 3}
  - {fileID: 21300000, guid: ad4f5b6c7e809123ad4f5b6c7e809123, type: 3}
  - {fileID: 21300000, guid: cf617d8e0a021345cf617d8e0a021345, type: 3}
  - {fileID: 21300000, guid: be506c7d8f910234be506c7d8f910234, type: 3}`,
);
scene = scene.replace(
  /m_Name: Background\n  m_TagString: Untagged\n  m_Icon: \{fileID: 0\}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 0/,
  "m_Name: Background\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1",
);
scene = scene.replace(
  "m_Sprite: {fileID: 21300000, guid: a7c3e91f4b2d6840b1e5f809c3d47c11, type: 3}",
  "m_Sprite: {fileID: 21300000, guid: c4b8a1d0e2f3456789ab0c1d2e3f4051, type: 3}",
);
scene = scene.replace(
  "m_Sprite: {fileID: 21300000, guid: 953c91274e2e403a8a8b556cfb1e2038, type: 3}",
  "m_Sprite: {fileID: 21300000, guid: 7a1c2e3f4b5d67890a1c2e3f4b5d6789, type: 3}",
);
scene = scene.replaceAll(
  "m_Sprite: {fileID: 21300000, guid: f7a2c8e14b3d4f6a9e0b1c2d3e4f5a6b, type: 3}",
  "m_Sprite: {fileID: 21300000, guid: 8b2d3f4a5c6e78901b2d3f4a5c6e7890, type: 3}",
);
scene = scene.replace(
  "m_Sprite: {fileID: 21300000, guid: e8b3d9f25c4e5a7b0f1c2d3e4f5a6b7c, type: 3}",
  "m_Sprite: {fileID: 21300000, guid: 9c3e4a5b6d7f89012c3e4a5b6d7f8901, type: 3}",
);

fs.writeFileSync(SCENE, scene);
console.log(JSON.stringify({ imported, bgGuid: "c4b8a1d0e2f3456789ab0c1d2e3f4051" }, null, 2));
