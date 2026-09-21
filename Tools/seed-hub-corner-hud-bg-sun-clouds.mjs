import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";
const SPRITE = "c2d3e4f5a6b7890123456789abcdef02";
const UI_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc";

const HUD_ROOTS = [
  { rt: "930020002", ids: { go: "930040010", rt: "930040011", img: "930040012", cr: "930040013" } },
  { rt: "930030002", ids: { go: "930040020", rt: "930040021", img: "930040022", cr: "930040023" } },
];

function block({ go, rt, img, cr, father }) {
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
  m_Name: BgSunClouds
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
  m_Color: {r: 1, g: 1, b: 1, a: 0.92}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 21300000, guid: ${SPRITE}, type: 3}
  m_Type: 0
  m_PreserveAspect: 0
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

let scene = fs.readFileSync(SCENE, "utf8");
if (scene.includes("m_Name: BgSunClouds")) {
  console.log("BgSunClouds already seeded — skip.");
  process.exit(0);
}

let yaml = "";
for (const hud of HUD_ROOTS) {
  if (!scene.includes(`&${hud.rt}\nRectTransform:`)) {
    console.warn(`Missing HUD root ${hud.rt}`);
    continue;
  }
  yaml += block({ ...hud.ids, father: hud.rt });
  scene = scene.replace(
    new RegExp(`(&${hud.rt}\\b[\\s\\S]*?m_Children:\\n)((?:  - \\{fileID: \\d+\\}\\n)+)`),
    (m, head, kids) => `${head}  - {fileID: ${hud.ids.rt}}\n${kids}`,
  );
}

scene = scene.replace(/(\n  m_Name: BgDark\n[\s\S]*?\n  m_IsActive: )1/g, "$10");
scene = scene.replace(/(\n  m_Name: BgLight\n[\s\S]*?\n  m_IsActive: )1/g, "$10");

const rootsIdx = scene.lastIndexOf("--- !u!1660057539");
scene = scene.slice(0, rootsIdx) + yaml + scene.slice(rootsIdx);
fs.writeFileSync(SCENE, scene);
console.log("Added BgSunClouds to HubCornerInfoHud (TownMap + StatusMenu); BgDark/BgLight hidden.");
