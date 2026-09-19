import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/Bonds.unity";
const SPARK_GUID = "598df4563fb81a16b77c6cc645a4c963";
const SPARK_SCRIPT_GUID = "a7b3c9d1e2f4084958675a4b3c2d1e0f";
const SPARK_SPRITE = `{fileID: 21300000, guid: ${SPARK_GUID}, type: 3}`;

const SPARKS = [
  { ax: 0.08, ay: 0.18, w: 20, h: 20, a: 0.85 },
  { ax: 0.22, ay: 0.32, w: 14, h: 14, a: 0.7 },
  { ax: 0.38, ay: 0.14, w: 18, h: 18, a: 0.9 },
  { ax: 0.52, ay: 0.4, w: 12, h: 12, a: 0.65 },
  { ax: 0.68, ay: 0.24, w: 16, h: 16, a: 0.8 },
  { ax: 0.84, ay: 0.52, w: 22, h: 22, a: 0.75 },
  { ax: 0.14, ay: 0.58, w: 15, h: 15, a: 0.72 },
  { ax: 0.33, ay: 0.74, w: 13, h: 13, a: 0.68 },
  { ax: 0.59, ay: 0.68, w: 17, h: 17, a: 0.82 },
  { ax: 0.77, ay: 0.8, w: 11, h: 11, a: 0.6 },
];

let text = fs.readFileSync(SCENE, "utf8");
if (text.includes("m_Name: SparkFx")) {
  console.log(JSON.stringify({ skipped: true, reason: "SparkFx exists" }));
  process.exit(0);
}

const baseId = 931001700;
const blocks = [];
const childRectIds = [];

for (let i = 0; i < SPARKS.length; i++) {
  const s = SPARKS[i];
  const goId = baseId + 10 + i * 10;
  const rectId = goId + 1;
  const crId = goId + 2;
  const imgId = goId + 3;
  childRectIds.push(rectId);

  blocks.push(`--- !u!1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rectId}}
  - component: {fileID: ${crId}}
  - component: {fileID: ${imgId}}
  m_Layer: 0
  m_Name: Spark_${i}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${rectId}
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
  m_Father: {fileID: ${baseId + 1}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${s.ax}, y: ${s.ay}}
  m_AnchorMax: {x: ${s.ax}, y: ${s.ay}}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: ${s.w}, y: ${s.h}}
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
  m_Color: {r: 1, g: 1, b: 1, a: ${s.a}}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: ${SPARK_SPRITE}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`);
}

const fxGo = baseId;
const fxRect = baseId + 1;
const fxCr = baseId + 2;
const fxScript = baseId + 3;

const sparkFxBlock = `--- !u!1 &${fxGo}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${fxRect}}
  - component: {fileID: ${fxScript}}
  m_Layer: 0
  m_Name: SparkFx
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${fxRect}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${fxGo}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
${childRectIds.map((id) => `  - {fileID: ${id}}`).join("\n")}
  m_Father: {fileID: 931000016}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!114 &${fxScript}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${fxGo}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SPARK_SCRIPT_GUID}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.Hub.BondsMenuSparkDrift
  sparkImages: []
`;

if (!text.includes("  m_Children:\n  []\n  m_Father: {fileID: 931000010}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 1, y: 1}")) {
  text = text.replace(
    /(m_GameObject: \{fileID: 931000015\}[\s\S]*?m_Children:\n)  \[\]/,
    `$1  - {fileID: ${fxRect}}`,
  );
} else {
  console.error("Background children anchor not found");
  process.exit(1);
}

text += `\n${sparkFxBlock}${blocks.join("")}`;

fs.writeFileSync(SCENE, text);
console.log(JSON.stringify({ ok: true, sparks: SPARKS.length }, null, 2));
