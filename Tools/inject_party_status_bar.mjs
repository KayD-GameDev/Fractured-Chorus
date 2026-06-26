import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SCENE = path.join(__dirname, "../Assets/FracturedChorus/Scenes/CombatPrototype.unity");

const SCRIPT_PARTY_BAR = "8d9ae2e87cdbd43439668cba80ee80d0";
const SCRIPT_PARTY_CARD = "49500af8a17d81c49ab6d91fb9e02c06";
const SCRIPT_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc";
const SCRIPT_HLG = "30649d3a9faa99c48a7b1166b86bf2a0";
const SCRIPT_LAYOUT = "306cc8c2b49d7114eaa3623786fc2126";
const CANVAS_RECT = 706293081;
const CARD_W = 78;
const CARD_H = 98;
const SPACING = 8;
const ROOT_W = CARD_W * 3 + SPACING * 2 + 16;
const ROOT_H = CARD_H;

const uid = (n) => 1810000000 + n;

function goBlock(goId, name, components) {
  const compLines = components.map((c) => `  - component: {fileID: ${c}}`).join("\n");
  return `--- !u!1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
${compLines}
  m_Layer: 0
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
`;
}

function rectBlock(rectId, goId, father, children, anchorMin, anchorMax, pivot, anchored, size) {
  const childLines = children.map((c) => `  - {fileID: ${c}}`).join("\n");
  return `--- !u!224 &${rectId}
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
${childLines}
  m_Father: {fileID: ${father}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${anchorMin[0]}, y: ${anchorMin[1]}}
  m_AnchorMax: {x: ${anchorMax[0]}, y: ${anchorMax[1]}}
  m_AnchoredPosition: {x: ${anchored[0]}, y: ${anchored[1]}}
  m_SizeDelta: {x: ${size[0]}, y: ${size[1]}}
  m_Pivot: {x: ${pivot[0]}, y: ${pivot[1]}}
`;
}

function canvasRendererBlock(crId, goId) {
  return `--- !u!222 &${crId}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
`;
}

function imageBlock(imgId, goId, color, opts = {}) {
  const {
    raycast = 0,
    preserveAspect = 0,
    enabled = 1,
    filled = false,
    fillAmount = 1,
  } = opts;
  const imgType = filled ? 3 : 0;
  const fillMethod = filled ? 0 : 4;
  return `--- !u!114 &${imgId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: ${enabled}
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT_IMAGE}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: ${color[0]}, g: ${color[1]}, b: ${color[2]}, a: ${color[3]}}
  m_RaycastTarget: ${raycast}
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: ${imgType}
  m_PreserveAspect: ${preserveAspect}
  m_FillCenter: 1
  m_FillMethod: ${fillMethod}
  m_FillAmount: ${fillAmount}
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`;
}

function buildCard(index, fatherRect) {
  const base = 100 + index * 100;
  const go = uid(base + 1);
  const rect = uid(base + 2);
  const layout = uid(base + 3);
  const cardView = uid(base + 4);
  const frameGo = uid(base + 11);
  const frameRect = uid(base + 12);
  const frameImg = uid(base + 13);
  const frameCr = uid(base + 14);
  const avRootGo = uid(base + 21);
  const avRootRect = uid(base + 22);
  const avGo = uid(base + 31);
  const avRect = uid(base + 32);
  const avImg = uid(base + 33);
  const avCr = uid(base + 34);
  const elGo = uid(base + 41);
  const elRect = uid(base + 42);
  const elImg = uid(base + 43);
  const elCr = uid(base + 44);
  const hpGo = uid(base + 51);
  const hpRect = uid(base + 52);
  const hpImg = uid(base + 53);
  const hpCr = uid(base + 54);
  const fillGo = uid(base + 61);
  const fillRect = uid(base + 62);
  const fillImg = uid(base + 63);
  const fillCr = uid(base + 64);

  const parts = [
    goBlock(go, `PartyCard_${index}`, [rect, layout, cardView]),
    rectBlock(rect, go, fatherRect, [frameRect, avRootRect, hpRect], [0, 1], [0, 1], [0.5, 1], [0, 0], [CARD_W, CARD_H]),
    `--- !u!114 &${layout}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT_LAYOUT}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.LayoutElement
  m_IgnoreLayout: 0
  m_MinWidth: -1
  m_MinHeight: -1
  m_PreferredWidth: ${CARD_W}
  m_PreferredHeight: ${CARD_H}
  m_FlexibleWidth: 0
  m_FlexibleHeight: 0
  m_LayoutPriority: 1
`,
    `--- !u!114 &${cardView}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT_PARTY_CARD}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.PartyUnitCardView
  frame: {fileID: ${frameImg}}
  avatar: {fileID: ${avImg}}
  elementIcon: {fileID: ${elImg}}
  hpFill: {fileID: ${fillImg}}
  hpBackground: {fileID: ${hpImg}}
  preserveSceneImages: 1
`,
    goBlock(frameGo, "Frame", [frameRect, frameImg, frameCr]),
    rectBlock(frameRect, frameGo, rect, [], [0, 0], [1, 1], [0.5, 0.5], [0, 0], [0, 0]),
    imageBlock(frameImg, frameGo, [0.08, 0.12, 0.28, 0.94]),
    canvasRendererBlock(frameCr, frameGo),
    goBlock(avRootGo, "AvatarRoot", [avRootRect]),
    rectBlock(avRootRect, avRootGo, rect, [avRect, elRect], [0.5, 1], [0.5, 1], [0.5, 1], [0, -6], [68, 68]),
    goBlock(avGo, "Avatar", [avRect, avImg, avCr]),
    rectBlock(avRect, avGo, avRootRect, [], [0, 0], [1, 1], [0.5, 0.5], [0, 0], [0, 0]),
    imageBlock(avImg, avGo, [0.25, 0.28, 0.35, 1], { preserveAspect: 1 }),
    canvasRendererBlock(avCr, avGo),
    goBlock(elGo, "ElementIcon", [elRect, elImg, elCr]),
    rectBlock(elRect, elGo, avRootRect, [], [1, 1], [1, 1], [1, 1], [4, 4], [24, 24]),
    imageBlock(elImg, elGo, [0.95, 0.82, 0.25, 1], { enabled: 0 }),
    canvasRendererBlock(elCr, elGo),
    goBlock(hpGo, "HpBar", [hpRect, hpImg, hpCr]),
    rectBlock(hpRect, hpGo, rect, [fillRect], [0, 0], [1, 0], [0.5, 0], [0, 6], [-12, 10]),
    imageBlock(hpImg, hpGo, [0.04, 0.06, 0.1, 0.95]),
    canvasRendererBlock(hpCr, hpGo),
    goBlock(fillGo, "Fill", [fillRect, fillImg, fillCr]),
    rectBlock(fillRect, fillGo, hpRect, [], [0, 0], [1, 1], [0.5, 0.5], [0, 0], [0, 0]),
    imageBlock(fillImg, fillGo, [0.25, 0.88, 0.35, 1], { filled: true }),
    canvasRendererBlock(fillCr, fillGo),
  ];

  return [parts.join("\n"), rect, cardView];
}

function buildAll() {
  const barGo = uid(1);
  const barRect = uid(2);
  const barView = uid(3);
  const rowGo = uid(11);
  const rowRect = uid(12);
  const rowHlg = uid(13);
  const cardRects = [];
  const cardViews = [];
  const cardParts = [];
  for (let i = 0; i < 3; i++) {
    const [yaml, cardRect, cardView] = buildCard(i, rowRect);
    cardParts.push(yaml);
    cardRects.push(cardRect);
    cardViews.push(cardView);
  }

  const cardLines = cardRects.map((r) => `  - {fileID: ${r}}`).join("\n");
  const yaml = [
    goBlock(barGo, "PartyStatusBarUI", [barRect, barView]),
    `--- !u!224 &${barRect}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${barGo}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: ${rowRect}}
  m_Father: {fileID: ${CANVAS_RECT}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 16, y: -16}
  m_SizeDelta: {x: ${ROOT_W}, y: ${ROOT_H}}
  m_Pivot: {x: 0, y: 1}
`,
    `--- !u!114 &${barView}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${barGo}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT_PARTY_BAR}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.UI.PartyStatusBarUIView
  cardsRow: {fileID: ${rowRect}}
  cardTemplate: {fileID: ${cardViews[0]}}
  sceneCards:
  - {fileID: ${cardViews[0]}}
  - {fileID: ${cardViews[1]}}
  - {fileID: ${cardViews[2]}}
  preserveSceneLayout: 1
  useSceneHierarchyOnly: 1
`,
    goBlock(rowGo, "CardsRow", [rowRect, rowHlg]),
    `--- !u!224 &${rowRect}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${rowGo}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
${cardLines}
  m_Father: {fileID: ${barRect}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
`,
    `--- !u!114 &${rowHlg}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${rowGo}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${SCRIPT_HLG}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 0
  m_Spacing: ${SPACING}
  m_ChildForceExpandWidth: 0
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 0
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
`,
    ...cardParts,
  ].join("\n");

  return { yaml, barView, barRect };
}

let text = fs.readFileSync(SCENE, "utf8");
if (text.includes("PartyStatusBarUI")) {
  text = text.replace(/--- !u!1 &1810000001\nGameObject:[\s\S]*?(?=--- !u!1660057539)/, "");
  text = text.replace(`  - {fileID: ${uid(2)}}\n`, "");
}

const { yaml, barView, barRect } = buildAll();
const canvasChildren = `  m_Children:
  - {fileID: 1236853533}
  - {fileID: 463385829}
  - {fileID: 2120607401}`;
const canvasChildrenNew = `${canvasChildren}\n  - {fileID: ${barRect}}`;
if (!text.includes(`{fileID: ${barRect}}`)) {
  text = text.replace(canvasChildren, canvasChildrenNew);
}
text = text.replace("  partyStatusBar: {fileID: 0}", `  partyStatusBar: {fileID: ${barView}}`);
const marker = "--- !u!1660057539 &9223372036854775807";
if (!text.includes("m_Name: PartyStatusBarUI")) {
  text = text.replace(marker, `${yaml}\n${marker}`);
}
fs.writeFileSync(SCENE, text, "utf8");
console.log(`Injected PartyStatusBarUI -> bootstrap ${barView}, canvas child ${barRect}`);
