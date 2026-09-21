import fs from "fs";
import path from "path";

const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";
const HUB = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";
const MENU = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/MainMenuStartGame.unity";

const PNGS = [
  "01_Base.png",
  "02_Border.png",
  "03_Glass.png",
  "04_Gradient.png",
  "05_Deco_Left.png",
  "06_Deco_Right.png",
  "07_Text.png",
  "08_Glow.png",
  "09_Particles.png",
  "10_Wave.png",
  "11_Scanline.png",
  "12_Shadow.png",
  "state_normal.png",
  "state_hover.png",
  "state_pressed.png",
];

const ART_GUID = {
  "01_Base.png": "c4d5e6f7a8b90a010000000000000001",
  "02_Border.png": "c4d5e6f7a8b90a010000000000000002",
  "03_Glass.png": "c4d5e6f7a8b90a010000000000000003",
  "04_Gradient.png": "c4d5e6f7a8b90a010000000000000004",
  "05_Deco_Left.png": "c4d5e6f7a8b90a010000000000000005",
  "06_Deco_Right.png": "c4d5e6f7a8b90a010000000000000006",
  "07_Text.png": "c4d5e6f7a8b90a010000000000000007",
  "08_Glow.png": "c4d5e6f7a8b90a010000000000000008",
  "09_Particles.png": "c4d5e6f7a8b90a010000000000000009",
  "10_Wave.png": "c4d5e6f7a8b90a01000000000000000a",
  "11_Scanline.png": "c4d5e6f7a8b90a01000000000000000b",
  "12_Shadow.png": "c4d5e6f7a8b90a01000000000000000c",
  "state_normal.png": "c4d5e6f7a8b90a01000000000000000d",
  "state_hover.png": "c4d5e6f7a8b90a01000000000000000e",
  "state_pressed.png": "c4d5e6f7a8b90a01000000000000000f",
};

const RES_GUID = {
  "01_Base.png": "c4d5e6f7a8b90b010000000000000001",
  "02_Border.png": "c4d5e6f7a8b90b010000000000000002",
  "03_Glass.png": "c4d5e6f7a8b90b010000000000000003",
  "04_Gradient.png": "c4d5e6f7a8b90b010000000000000004",
  "05_Deco_Left.png": "c4d5e6f7a8b90b010000000000000005",
  "06_Deco_Right.png": "c4d5e6f7a8b90b010000000000000006",
  "07_Text.png": "c4d5e6f7a8b90b010000000000000007",
  "08_Glow.png": "c4d5e6f7a8b90b010000000000000008",
  "09_Particles.png": "c4d5e6f7a8b90b010000000000000009",
  "10_Wave.png": "c4d5e6f7a8b90b01000000000000000a",
  "11_Scanline.png": "c4d5e6f7a8b90b01000000000000000b",
  "12_Shadow.png": "c4d5e6f7a8b90b01000000000000000c",
  "state_normal.png": "c4d5e6f7a8b90b01000000000000000d",
  "state_hover.png": "c4d5e6f7a8b90b01000000000000000e",
  "state_pressed.png": "c4d5e6f7a8b90b01000000000000000f",
};

const SCRIPT = "c4d5e6f7a8b90c010000000000000001";
const UI_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc";
const UI_BUTTON = "4e29b1a8efbd4b44bb3f3716e73f07ff";

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

function pngMeta(guid, spriteId) {
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
}

function layerYaml({ go, rt, img, cr, name, father, sprite, alpha = 1 }) {
  return `--- !u!1 &${go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rt}}
  - component: {fileID: ${cr}}
  - component: {fileID: ${img}}
  m_Layer: 0
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${rt}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${father}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!114 &${img}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${UI_IMAGE}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: ${alpha}}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 21300000, guid: ${sprite}, type: 3}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!222 &${cr}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_CullTransparentMesh: 1
`;
}

function buttonYaml({ ids, father, pos, size }) {
  const g = ART_GUID;
  const layers = [
    { key: "shadow", name: "Layer_Shadow", sprite: g["12_Shadow.png"], alpha: 0.55 },
    { key: "plate", name: "Layer_Plate", sprite: g["state_normal.png"], alpha: 1 },
    { key: "glow", name: "Layer_Glow", sprite: g["08_Glow.png"], alpha: 0.35 },
    { key: "particles", name: "Layer_Particles", sprite: g["09_Particles.png"], alpha: 0.55 },
    { key: "wave", name: "Layer_Wave", sprite: g["10_Wave.png"], alpha: 0.4 },
    { key: "scan", name: "Layer_Scanline", sprite: g["11_Scanline.png"], alpha: 0.4 },
  ];

  let children = "";
  for (const layer of layers) {
    children += layerYaml({
      go: ids[layer.key].go,
      rt: ids[layer.key].rt,
      img: ids[layer.key].img,
      cr: ids[layer.key].cr,
      name: layer.name,
      father: ids.root.rt,
      sprite: layer.sprite,
      alpha: layer.alpha,
    });
  }

  return `--- !u!1 &${ids.root.go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${ids.root.rt}}
  - component: {fileID: ${ids.root.cr}}
  - component: {fileID: ${ids.root.hit}}
  - component: {fileID: ${ids.root.btn}}
  - component: {fileID: ${ids.root.fx}}
  - component: {fileID: ${ids.root.cg}}
  m_Layer: 0
  m_Name: ResonanceDiveButton
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${ids.root.rt}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ids.root.go}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: ${ids.shadow.rt}}
  - {fileID: ${ids.plate.rt}}
  - {fileID: ${ids.glow.rt}}
  - {fileID: ${ids.particles.rt}}
  - {fileID: ${ids.wave.rt}}
  - {fileID: ${ids.scan.rt}}
  m_Father: {fileID: ${father}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: ${pos[0]}, y: ${pos[1]}}
  m_SizeDelta: {x: ${size[0]}, y: ${size[1]}}
  m_Pivot: {x: 0, y: 0}
--- !u!222 &${ids.root.cr}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ids.root.go}}
  m_CullTransparentMesh: 1
--- !u!114 &${ids.root.hit}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ids.root.go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${UI_IMAGE}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 0}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!114 &${ids.root.btn}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ids.root.go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${UI_BUTTON}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 0
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 1, g: 1, b: 1, a: 1}
    m_PressedColor: {r: 1, g: 1, b: 1, a: 1}
    m_SelectedColor: {r: 1, g: 1, b: 1, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {fileID: ${ids.root.hit}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
--- !u!114 &${ids.root.fx}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ids.root.go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.ResonanceDiveButton
  button: {fileID: ${ids.root.btn}}
  plate: {fileID: ${ids.plate.img}}
  shadow: {fileID: ${ids.shadow.img}}
  glow: {fileID: ${ids.glow.img}}
  particles: {fileID: ${ids.particles.img}}
  wave: {fileID: ${ids.wave.img}}
  scanline: {fileID: ${ids.scan.img}}
  normalSprite: {fileID: 21300000, guid: ${g["state_normal.png"]}, type: 3}
  hoverSprite: {fileID: 21300000, guid: ${g["state_hover.png"]}, type: 3}
  pressedSprite: {fileID: 21300000, guid: ${g["state_pressed.png"]}, type: 3}
  reducedMotion: 0
  glowIdleMin: 0.22
  glowIdleMax: 0.55
  glowHoverMin: 0.55
  glowHoverMax: 1
  pulseSpeed: 2.2
  particleDrift: 5
  scanAmplitude: 10
--- !u!225 &${ids.root.cg}
CanvasGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ids.root.go}}
  m_Enabled: 1
  m_Alpha: 1
  m_Interactable: 1
  m_BlocksRaycasts: 1
  m_IgnoreParentGroups: 0
${children}`;
}

function makeIds(base) {
  const keys = ["root", "shadow", "plate", "glow", "particles", "wave", "scan"];
  const ids = {};
  keys.forEach((key, i) => {
    const n = base + i * 10;
    ids[key] = {
      go: n,
      rt: n + 1,
      cr: n + 2,
      img: n + 3,
      hit: n + 3,
      btn: n + 4,
      fx: n + 5,
      cg: n + 6,
    };
  });
  return ids;
}

function injectHub(text) {
  if (text.includes("m_Name: ResonanceDiveButton")) {
    console.log("CampusHub already has ResonanceDiveButton");
    return text;
  }

  const ids = makeIds(940030001);
  const yaml = buttonYaml({
    ids,
    father: 1341905658,
    pos: [40, 40],
    size: [640, 288],
  });

  if (!text.includes("runMapHotkey: {fileID: 0}")) {
    throw new Error("CampusHub TownMapView runMapHotkey marker missing");
  }
  text = text.replace(
    "runMapHotkey: {fileID: 0}",
    `runMapHotkey: {fileID: 0}\n  diveButton: {fileID: ${ids.root.fx}}`,
  );

  const childrenNeedle = `  - {fileID: 1507891236}\n  - {fileID: 1004106883}`;
  if (!text.includes(childrenNeedle)) {
    throw new Error("CampusHub TownMap children marker missing");
  }
  text = text.replace(
    childrenNeedle,
    `  - {fileID: 1507891236}\n  - {fileID: ${ids.root.rt}}\n  - {fileID: 1004106883}`,
  );

  return text + "\n" + yaml;
}

function injectMenu(text) {
  if (text.includes("m_Name: ResonanceDiveButton")) {
    console.log("MainMenu already has ResonanceDiveButton");
    return text;
  }

  const ids = makeIds(940040001);
  const yaml = buttonYaml({
    ids,
    father: 671449013,
    pos: [40, 40],
    size: [640, 288],
  });

  if (!text.includes("  editorPreview: 2\n")) {
    throw new Error("MainMenu editorPreview marker missing");
  }
  text = text.replace(
    "  editorPreview: 2\n",
    `  editorPreview: 2\n  diveButton: {fileID: ${ids.root.fx}}\n`,
  );

  const childrenNeedle = `  - {fileID: 1197262871}\n  - {fileID: 859736071}`;
  if (!text.includes(childrenNeedle)) {
    throw new Error("MainMenu canvas children marker missing");
  }
  text = text.replace(
    childrenNeedle,
    `  - {fileID: 1197262871}\n  - {fileID: ${ids.root.rt}}\n  - {fileID: 859736071}`,
  );

  return text + "\n" + yaml;
}

fs.writeFileSync(`${ART}.meta`, folderMeta("c4d5e6f7a8b90a0100000000000000f0"));
fs.writeFileSync(`${RES}.meta`, folderMeta("c4d5e6f7a8b90b0100000000000000f0"));

for (const name of PNGS) {
  const artGuid = ART_GUID[name];
  const resGuid = RES_GUID[name];
  const spriteArt = artGuid.slice(0, 16) + "8000000000000000";
  const spriteRes = resGuid.slice(0, 16) + "8000000000000000";
  fs.writeFileSync(path.join(ART, `${name}.meta`), pngMeta(artGuid, spriteArt));
  fs.writeFileSync(path.join(RES, `${name}.meta`), pngMeta(resGuid, spriteRes));
}

let hub = fs.readFileSync(HUB, "utf8");
hub = injectHub(hub);
fs.writeFileSync(HUB, hub);

let menu = fs.readFileSync(MENU, "utf8");
menu = injectMenu(menu);
fs.writeFileSync(MENU, menu);

console.log("seeded metas + scenes");
