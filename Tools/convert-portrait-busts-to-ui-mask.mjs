import fs from "fs";

function imageYaml(id, goId, sprite) {
  return `--- !u!114 &${id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: ${sprite}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`;
}

function canvasRendererYaml(id, goId) {
  return `--- !u!222 &${id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
`;
}

function maskYaml(id, goId) {
  return `--- !u!114 &${id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 31a19414c41e5ae4aae2af33fee712f6, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Mask
  m_ShowMaskGraphic: 1
`;
}

function convertPortrait(text, p) {
  const tfRe = new RegExp(`--- !u!4 &${p.tfId}\\nTransform:\\n[\\s\\S]*?m_LocalEulerAnglesHint: \\{x: 0, y: 0, z: 0\\}\\n`);
  const tf = text.match(tfRe);
  if (!tf) throw new Error(`transform missing ${p.name} ${p.tfId}`);
  const pos = tf[0].match(/m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/);
  const sc = tf[0].match(/m_LocalScale: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/);
  const rot = tf[0].match(/m_LocalRotation: \{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}/);
  const father = tf[0].match(/m_Father: \{fileID: (\d+)\}/)[1];
  const sx = Number(sc[1]);
  const sy = Number(sc[2]);
  const w = +(p.spriteW * sx).toFixed(5);
  const h = +(p.spriteH * sy).toFixed(5);

  const rt = `--- !u!224 &${p.tfId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${p.goId}}
  m_LocalRotation: {x: ${rot[1]}, y: ${rot[2]}, z: ${rot[3]}, w: ${rot[4]}}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${father}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: ${pos[1]}, y: ${pos[2]}}
  m_SizeDelta: {x: ${w}, y: ${h}}
  m_Pivot: {x: 0.5, y: 0.5}
`;
  text = text.replace(tfRe, rt);

  const srRe = new RegExp(`--- !u!212 &${p.srId}\\nSpriteRenderer:[\\s\\S]*?m_SpriteSortPoint: 0\\n`);
  if (!srRe.test(text)) throw new Error(`spriterenderer missing ${p.name}`);
  text = text.replace(srRe, canvasRendererYaml(p.crId, p.goId) + imageYaml(p.srId, p.goId, p.sprite));

  const goRe = new RegExp(
    `(m_Name: ${p.name}\\n[\\s\\S]*?m_Component:\\n  - component: \\{fileID: ${p.tfId}\\}\\n  - component: \\{fileID: ${p.srId}\\})`,
  );
  if (!goRe.test(text)) {
    const alt = new RegExp(
      `(m_Component:\\n  - component: \\{fileID: ${p.tfId}\\}\\n  - component: \\{fileID: ${p.srId}\\}\\n  m_Layer: 0\\n  m_Name: ${p.name})`,
    );
    if (!alt.test(text)) throw new Error(`go components missing ${p.name}`);
    text = text.replace(
      alt,
      `m_Component:\n  - component: {fileID: ${p.tfId}}\n  - component: {fileID: ${p.crId}}\n  - component: {fileID: ${p.srId}}\n  m_Layer: 0\n  m_Name: ${p.name}`,
    );
  } else {
    text = text.replace(
      goRe,
      (m) => m.replace(
        `  - component: {fileID: ${p.tfId}}\n  - component: {fileID: ${p.srId}}`,
        `  - component: {fileID: ${p.tfId}}\n  - component: {fileID: ${p.crId}}\n  - component: {fileID: ${p.srId}}`,
      ),
    );
  }
  return text;
}

function addMask(text, panelGoId, maskId, afterCompId) {
  const insert = `  - component: {fileID: ${afterCompId}}\n  - component: {fileID: ${maskId}}`;
  const needle = `  - component: {fileID: ${afterCompId}}`;
  const goBlock = text.split(`--- !u!1 &${panelGoId}`)[1];
  if (!goBlock) throw new Error(`panel go ${panelGoId} missing`);
  const head = goBlock.slice(0, 400);
  if (head.includes(`fileID: ${maskId}`)) return text;
  const idx = text.indexOf(`--- !u!1 &${panelGoId}`);
  const slice = text.slice(idx, idx + 800);
  if (!slice.includes(needle)) throw new Error(`panel component ${afterCompId} missing`);
  text = text.slice(0, idx) + slice.replace(needle, insert) + text.slice(idx + slice.length);
  if (!text.includes(`--- !u!114 &${maskId}`)) {
    text += maskYaml(maskId, panelGoId);
  }
  return text;
}

const prodPath = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";
const sandPath = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";

let prod = fs.readFileSync(prodPath, "utf8");
prod = convertPortrait(prod, {
  name: "Ren_Portrait",
  goId: "2090439961",
  tfId: "2090439962",
  srId: "2090439963",
  crId: "996020001",
  sprite: "{fileID: 21300000, guid: e0d8faa873464347871ea54b0e137c5e, type: 3}",
  spriteW: 10.24,
  spriteH: 15.36,
});
prod = convertPortrait(prod, {
  name: "Charlotte_Portrait",
  goId: "1387315138",
  tfId: "1387315139",
  srId: "1387315140",
  crId: "996020002",
  sprite: "{fileID: 21300000, guid: f7a2c8e14b3d4f6a9e0b1c2d3e4f5a6b, type: 3}",
  spriteW: 10.24,
  spriteH: 15.36,
});
prod = convertPortrait(prod, {
  name: "Coda_Portrait",
  goId: "145208468",
  tfId: "145208470",
  srId: "145208469",
  crId: "996020003",
  sprite: "{fileID: 21300000, guid: e8b3d9f25c4e5a7b0f1c2d3e4f5a6b7c, type: 3}",
  spriteW: 10.24,
  spriteH: 15.36,
});
prod = addMask(prod, "978793064", "996020010", "978793065");
fs.writeFileSync(prodPath, prod);

let sand = fs.readFileSync(sandPath, "utf8");
sand = convertPortrait(sand, {
  name: "Ren_Portrait",
  goId: "529507557",
  tfId: "529507559",
  srId: "529507558",
  crId: "996021001",
  sprite: "{fileID: 21300000, guid: e0d8faa873464347871ea54b0e137c5e, type: 3}",
  spriteW: 10.24,
  spriteH: 15.36,
});
sand = convertPortrait(sand, {
  name: "Charlotte_Portrait",
  goId: "880030001",
  tfId: "880030002",
  srId: "880030003",
  crId: "996021002",
  sprite: "{fileID: 21300000, guid: f7a2c8e14b3d4f6a9e0b1c2d3e4f5a6b, type: 3}",
  spriteW: 10.24,
  spriteH: 15.36,
});
sand = convertPortrait(sand, {
  name: "Coda_Portrait",
  goId: "880040001",
  tfId: "880040002",
  srId: "880040003",
  crId: "996021003",
  sprite: "{fileID: 21300000, guid: e8b3d9f25c4e5a7b0f1c2d3e4f5a6b7c, type: 3}",
  spriteW: 10.24,
  spriteH: 15.36,
});
sand = addMask(sand, "950000001", "996021010", "950000003");
fs.writeFileSync(sandPath, sand);

console.log("converted portraits + mask");
