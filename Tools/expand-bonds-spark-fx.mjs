import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/Bonds.unity";
const SPARK_GUID = "598df4563fb81a16b77c6cc645a4c963";
const SPARK_SPRITE = `{fileID: 21300000, guid: ${SPARK_GUID}, type: 3}`;
const FX_RECT = 931001701;
const BASE_GO = 931003010;

const EXTRA = [];
for (let n = 0; n < 15; n++) {
  const t = n / 15;
  const ax = 0.05 + ((n * 17 + 7) % 89) / 100;
  const ay = 0.08 + ((n * 23 + 11) % 82) / 100;
  const w = 10 + ((n * 5) % 13);
  const h = w;
  const a = 0.55 + (t * 0.35);
  EXTRA.push({ name: `Spark_${10 + n}`, ax, ay, w, h, a });
}

let text = fs.readFileSync(SCENE, "utf8");
if (text.includes("m_Name: Spark_10")) {
  console.log(JSON.stringify({ skipped: true, reason: "Spark_10 exists" }));
  process.exit(0);
}

const childRectIds = [];
const blocks = [];

for (let i = 0; i < EXTRA.length; i++) {
  const s = EXTRA[i];
  const goId = BASE_GO + i * 10;
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
  m_Name: ${s.name}
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
  m_Father: {fileID: ${FX_RECT}}
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

const childNeedle = `  - {fileID: 931001801}\n  m_Father: {fileID: 931000016}`;
const childInsert = `  - {fileID: 931001801}\n${childRectIds.map((id) => `  - {fileID: ${id}}`).join("\n")}\n  m_Father: {fileID: 931000016}`;

if (!text.includes(childNeedle)) {
  console.error("SparkFx children anchor not found");
  process.exit(1);
}

text = text.replace(childNeedle, childInsert);
text += `\n${blocks.join("")}`;

fs.writeFileSync(SCENE, text);
console.log(JSON.stringify({ ok: true, added: EXTRA.length, total: 10 + EXTRA.length }, null, 2));
