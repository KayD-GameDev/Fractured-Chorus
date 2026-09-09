import fs from "fs";
import sharp from "sharp";
import path from "path";

const outDir =
  "d:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/Skills/Ren";
const assetsDir =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";

const jobs = [
  {
    src: "ren_skill_icon_strike_v8.png",
    dest: "ren_skill_icon_strike_v2.png",
    guid: "e1a2b3c4d5f6478091a2b3c4d5e6f708",
    skillAsset: "d:/Fractured-Chorus1/Assets/FracturedChorus/Resources/Skills/ren_basic.asset",
  },
  {
    src: "ren_skill_icon_crosscut_v8.png",
    dest: "ren_skill_icon_crosscut_v2.png",
    guid: "f2b3c4d5e6a7489012b3c4d5e6f70819",
    skillAsset: "d:/Fractured-Chorus1/Assets/FracturedChorus/Resources/Skills/ren_skill.asset",
  },
  {
    src: "ren_skill_icon_finale_v8.png",
    dest: "ren_skill_icon_finale_v2.png",
    guid: "a3c4d5e6f7b8490123c4d5e6f7081920",
    skillAsset: "d:/Fractured-Chorus1/Assets/FracturedChorus/Resources/Skills/ren_ult.asset",
  },
];

function floodClearBlack(data, w, h) {
  const visited = new Uint8Array(w * h);
  const stack = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= w || y >= h) return;
    const i = y * w + x;
    if (visited[i]) return;
    visited[i] = 1;
    stack.push(i);
  };
  for (let x = 0; x < w; x++) {
    push(x, 0);
    push(x, h - 1);
  }
  for (let y = 0; y < h; y++) {
    push(0, y);
    push(w - 1, y);
  }
  const isBg = (i) => {
    const o = i * 4;
    const r = data[o];
    const g = data[o + 1];
    const b = data[o + 2];
    const a = data[o + 3];
    if (a < 8) return true;
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    if (max <= 22) return true;
    if (max <= 36 && max - min <= 10) return true;
    if (min >= 200 && max - min <= 35) return true;
    if (min >= 175 && max - min <= 28) return true;
    return false;
  };
  while (stack.length) {
    const i = stack.pop();
    if (!isBg(i)) continue;
    data[i * 4 + 3] = 0;
    const x = i % w;
    const y = (i / w) | 0;
    push(x + 1, y);
    push(x - 1, y);
    push(x, y + 1);
    push(x, y - 1);
  }
}

const metaTemplate = (guid, spriteId) => `fileFormatVersion: 2
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
  maxTextureSize: 1024
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
    maxTextureSize: 1024
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
    spriteID: ${spriteId}
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

for (const job of jobs) {
  const srcPath = path.join(assetsDir, job.src);
  const destPath = path.join(outDir, job.dest);
  const raw = await sharp(srcPath)
    .ensureAlpha()
    .resize(1024, 1024, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .raw()
    .toBuffer({ resolveWithObject: true });
  const data = Buffer.from(raw.data);
  floodClearBlack(data, raw.info.width, raw.info.height);
  await sharp(data, {
    raw: { width: raw.info.width, height: raw.info.height, channels: 4 },
  })
    .png()
    .toFile(destPath);

  const spriteId = job.guid.replace(/-/g, "").slice(0, 32);
  fs.writeFileSync(destPath + ".meta", metaTemplate(job.guid, spriteId));

  let asset = fs.readFileSync(job.skillAsset, "utf8");
  asset = asset.replace(
    /icon: \{fileID: [^,]+, guid: [a-f0-9]+, type: 3\}/,
    `icon: {fileID: 21300000, guid: ${job.guid}, type: 3}`
  );
  fs.writeFileSync(job.skillAsset, asset);
  console.log("installed", job.dest, "→", path.basename(job.skillAsset));
}
