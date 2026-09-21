import fs from "node:fs";

const SCENE = "d:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/PrologueVN.unity";

const SPR = {
  star: "bae0590139c64b3e8d7dcc7dfa3da4a5",
  glitter: "94c2083478484257986423eb1726f91c",
  triLg: "82d4ce1e103f42dc9da45fc63ce6d143",
};

function particle(goId, rtId, crId, imgId, cgId, name, parentRt, spriteGuid, size) {
  return `--- !u!1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rtId}}
  - component: {fileID: ${crId}}
  - component: {fileID: ${imgId}}
  - component: {fileID: ${cgId}}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!224 &${rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${parentRt}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: ${size}, y: ${size}}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &${crId}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
--- !u!114 &${imgId}
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
  m_Sprite: {fileID: 21300000, guid: ${spriteGuid}, type: 3}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!225 &${cgId}
CanvasGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_Alpha: 1
  m_Interactable: 0
  m_BlocksRaycasts: 0
  m_IgnoreParentGroups: 0
`;
}

function folder(goId, rtId, name, parentRt) {
  return `--- !u!1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rtId}}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
PLACEHOLDER_CHILDREN
  m_Father: {fileID: ${parentRt}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
`;
}

function pack(prefix, count, startId, parentRt, spriteGuid, size) {
  const blocks = [];
  const childIds = [];
  for (let i = 0; i < count; i++) {
    const base = startId + i * 10;
    const pad = String(i).padStart(2, "0");
    childIds.push(base + 1);
    blocks.push(particle(base, base + 1, base + 2, base + 3, base + 4, `${prefix}_${pad}`, parentRt, spriteGuid, size));
  }
  return { blocks: blocks.join(""), childIds };
}

let yaml = fs.readFileSync(SCENE, "utf8");
if (yaml.includes("m_Name: Star_00")) {
  console.log("Trail hierarchy already present.");
  process.exit(0);
}

const stars = pack("Star", 24, 121000, 100411, SPR.star, 20);
const prisms = pack("Prism", 12, 122000, 100431, SPR.triLg, 28);
const music = pack("Music", 6, 123000, 100441, SPR.star, 22);
const far = pack("AmbientFar", 8, 124000, 120101, SPR.star, 12);
const near = pack("AmbientNear", 6, 125000, 120201, SPR.glitter, 32);

function childList(ids) {
  return ids.map((id) => `  - {fileID: ${id}}`).join("\n");
}

const farFolder = folder(120100, 120101, "AmbientFar", 100401).replace(
  "PLACEHOLDER_CHILDREN",
  childList(far.childIds),
);
const nearFolder = folder(120200, 120201, "AmbientNear", 100401).replace(
  "PLACEHOLDER_CHILDREN",
  childList(near.childIds),
);

yaml = yaml.replace(
  `  m_Children:
  - {fileID: 100411}
  - {fileID: 100421}
  - {fileID: 100431}
  - {fileID: 100441}
  - {fileID: 100451}
  m_Father: {fileID: 100101}`,
  `  m_Children:
  - {fileID: 100411}
  - {fileID: 100421}
  - {fileID: 100431}
  - {fileID: 100441}
  - {fileID: 100451}
  - {fileID: 120101}
  - {fileID: 120201}
  m_Father: {fileID: 100101}`,
);

yaml = yaml.replace(
  `  m_Name: StarDust
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &100411
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100410}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 100401}`,
  `  m_Name: StarDust
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &100411
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100410}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
${childList(stars.childIds)}
  m_Father: {fileID: 100401}`,
);

yaml = yaml.replace(
  `  m_Name: PrismFragments
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &100431
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100430}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 100401}`,
  `  m_Name: PrismFragments
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &100431
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100430}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
${childList(prisms.childIds)}
  m_Father: {fileID: 100401}`,
);

yaml = yaml.replace(
  `  m_Name: MusicFragments
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &100441
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100440}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 100401}`,
  `  m_Name: MusicFragments
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &100441
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100440}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
${childList(music.childIds)}
  m_Father: {fileID: 100401}`,
);

yaml = yaml.replace(
  `  waveformRoot: {fileID: 100451}`,
  `  waveformRoot: {fileID: 100451}
  ambientFarRoot: {fileID: 120101}
  ambientNearRoot: {fileID: 120201}`,
);

yaml = yaml.replace(
  "--- !u!1660057539 &9223372036854775807",
  `${farFolder}${nearFolder}${stars.blocks}${prisms.blocks}${music.blocks}${far.blocks}${near.blocks}--- !u!1660057539 &9223372036854775807`,
);

fs.writeFileSync(SCENE, yaml);
console.log("Patched trail hierarchy into PrologueVN.unity");
