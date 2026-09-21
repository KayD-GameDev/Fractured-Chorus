import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";
const HUD_SCRIPT = "f3e4d5c6b7a8901234567890abcdef01";
const UI_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc";
const UI_TEXT = "5f7201a12d95ffc409449d95f23cf332";
const FONT_DISPLAY = "d4e5f6a7b8c94091a2b3c4d5e6f70891";
const FONT_BODY = "787fda44816c480a9cc3cadfdc29ce24";
const SPRITE_SUN = "01667d25060e423ea7cb957281a474cc";
const SPRITE_MOON = "d9fa06efb3ad418fa101d69635333b04";
const SPRITE_DAWN = "cae5be8b83eb4708b3224b4d18c55966";

const STATUS_MENU_RT = "137282937";
const META_STATUS_MENU = "137282938";
const DATE_CHIP_GO = "100021601";

const ROOT = {
  go: "930030001",
  rt: "930030002",
  img: "930030004",
  cr: "930030005",
  hud: "930030003",
};

function imageYaml({ go, rt, img, cr, name, father, min, max, pos, size, pivot, color, sprite, preserve = 0 }) {
  const px = pivot?.x ?? 0.5;
  const py = pivot?.y ?? 0.5;
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
  m_AnchorMin: {x: ${min[0]}, y: ${min[1]}}
  m_AnchorMax: {x: ${max[0]}, y: ${max[1]}}
  m_AnchoredPosition: {x: ${pos[0]}, y: ${pos[1]}}
  m_SizeDelta: {x: ${size[0]}, y: ${size[1]}}
  m_Pivot: {x: ${px}, y: ${py}}
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
  m_Color: {r: ${color[0]}, g: ${color[1]}, b: ${color[2]}, a: ${color[3]}}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: ${sprite ? `{fileID: 21300000, guid: ${sprite}, type: 3}` : "{fileID: 0}"}
  m_Type: 0
  m_PreserveAspect: ${preserve}
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

function textYaml({ go, rt, txt, cr, name, father, min, max, pos, size, text, sizePx, font, align, color, italic = 0 }) {
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
  - component: {fileID: ${txt}}
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
  m_AnchorMin: {x: ${min[0]}, y: ${min[1]}}
  m_AnchorMax: {x: ${max[0]}, y: ${max[1]}}
  m_AnchoredPosition: {x: ${pos[0]}, y: ${pos[1]}}
  m_SizeDelta: {x: ${size[0]}, y: ${size[1]}}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!114 &${txt}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${UI_TEXT}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text
  m_Material: {fileID: 0}
  m_Color: {r: ${color[0]}, g: ${color[1]}, b: ${color[2]}, a: ${color[3]}}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: ${font}, type: 3}
    m_FontSize: ${sizePx}
    m_FontStyle: ${italic}
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: ${sizePx}
    m_Alignment: ${align}
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: ${text}
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
if (scene.includes("m_Father: {fileID: 137282937}") && scene.includes("m_Name: HubCornerInfoHud")) {
  const statusBlock = scene.match(/m_Father: \{fileID: 137282937\}[\s\S]{0,200}m_Name: HubCornerInfoHud/);
  if (statusBlock) {
    console.log("HubCornerInfoHud already under StatusMenu — wiring only.");
  }
}

if (scene.includes(`&${ROOT.go}\nGameObject:`)) {
  console.log("StatusMenu HubCornerInfoHud blocks exist — skip create.");
} else {
  const childRts = [
    "930030011",
    "930030021",
    "930030031",
    "930030041",
    "930030051",
    "930030061",
    "930030071",
    "930030081",
    "930030091",
    "930030101",
  ];

  const yaml =
    `--- !u!1 &${ROOT.go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${ROOT.rt}}
  - component: {fileID: ${ROOT.cr}}
  - component: {fileID: ${ROOT.img}}
  - component: {fileID: ${ROOT.hud}}
  m_Layer: 0
  m_Name: HubCornerInfoHud
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${ROOT.rt}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ROOT.go}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
${childRts.map((id) => `  - {fileID: ${id}}`).join("\n")}
  m_Father: {fileID: ${STATUS_MENU_RT}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 24, y: -20}
  m_SizeDelta: {x: 392, y: 132}
  m_Pivot: {x: 0, y: 1}
--- !u!114 &${ROOT.img}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ROOT.go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${UI_IMAGE}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 0}
  m_RaycastTarget: 0
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
--- !u!222 &${ROOT.cr}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ROOT.go}}
  m_CullTransparentMesh: 1
--- !u!114 &${ROOT.hud}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${ROOT.go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${HUD_SCRIPT}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.Hub.HubCornerInfoHud
  dateLabel: {fileID: 930030062}
  dayLabel: {fileID: 930030072}
  phaseIcon: {fileID: 930030082}
  locationLabel: {fileID: 930030092}
  taglineLabel: {fileID: 930030102}
  sunSprite: {fileID: 21300000, guid: ${SPRITE_SUN}, type: 3}
  moonSprite: {fileID: 21300000, guid: ${SPRITE_MOON}, type: 3}
  dawnSprite: {fileID: 21300000, guid: ${SPRITE_DAWN}, type: 3}
` +
    imageYaml({
      go: "930030010",
      rt: "930030011",
      img: "930030012",
      cr: "930030013",
      name: "BgDark",
      father: ROOT.rt,
      min: [0, 0],
      max: [0.52, 1],
      pos: [0, 0],
      size: [0, 0],
      color: [0.04, 0.05, 0.14, 0.82],
    }) +
    imageYaml({
      go: "930030020",
      rt: "930030021",
      img: "930030022",
      cr: "930030023",
      name: "BgLight",
      father: ROOT.rt,
      min: [0.52, 0],
      max: [1, 1],
      pos: [0, 0],
      size: [0, 0],
      color: [0.78, 0.74, 0.88, 0.38],
    }) +
    imageYaml({
      go: "930030030",
      rt: "930030031",
      img: "930030032",
      cr: "930030033",
      name: "DecorLineV",
      father: ROOT.rt,
      min: [0, 0.08],
      max: [0, 0.92],
      pos: [18, 0],
      size: [2, 0],
      color: [1, 1, 1, 1],
    }) +
    imageYaml({
      go: "930030040",
      rt: "930030041",
      img: "930030042",
      cr: "930030043",
      name: "DecorLineH",
      father: ROOT.rt,
      min: [0.04, 0.54],
      max: [0.96, 0.54],
      pos: [0, 0],
      size: [0, 2],
      color: [1, 1, 1, 1],
    }) +
    imageYaml({
      go: "930030050",
      rt: "930030051",
      img: "930030052",
      cr: "930030053",
      name: "DecorGlint",
      father: ROOT.rt,
      min: [0, 0.54],
      max: [0, 0.54],
      pos: [14, 0],
      size: [12, 12],
      pivot: { x: 0, y: 0.5 },
      color: [1, 1, 1, 0.95],
    }) +
    textYaml({
      go: "930030060",
      rt: "930030061",
      txt: "930030062",
      cr: "930030063",
      name: "DateLabel",
      father: ROOT.rt,
      min: [0.08, 0.58],
      max: [0.52, 0.98],
      pos: [0, 0],
      size: [0, 0],
      text: "07 / 09",
      sizePx: 34,
      font: FONT_DISPLAY,
      align: 3,
      color: [1, 1, 1, 1],
    }) +
    textYaml({
      go: "930030070",
      rt: "930030071",
      txt: "930030072",
      cr: "930030073",
      name: "DayLabel",
      father: ROOT.rt,
      min: [0.52, 0.58],
      max: [0.72, 0.98],
      pos: [0, 0],
      size: [0, 0],
      text: "Tue",
      sizePx: 34,
      font: FONT_DISPLAY,
      align: 3,
      color: [1, 1, 1, 1],
    }) +
    imageYaml({
      go: "930030080",
      rt: "930030081",
      img: "930030082",
      cr: "930030083",
      name: "PhaseIcon",
      father: ROOT.rt,
      min: [0.84, 0.62],
      max: [0.98, 0.96],
      pos: [0, 0],
      size: [0, 0],
      color: [1, 1, 1, 1],
      sprite: SPRITE_SUN,
      preserve: 1,
    }) +
    textYaml({
      go: "930030090",
      rt: "930030091",
      txt: "930030092",
      cr: "930030093",
      name: "LocationLabel",
      father: ROOT.rt,
      min: [0.08, 0.22],
      max: [0.92, 0.52],
      pos: [0, 0],
      size: [0, 0],
      text: "HIMA CITY",
      sizePx: 22,
      font: FONT_DISPLAY,
      align: 0,
      color: [1, 1, 1, 1],
    }) +
    textYaml({
      go: "930030100",
      rt: "930030101",
      txt: "930030102",
      cr: "930030103",
      name: "TaglineLabel",
      father: ROOT.rt,
      min: [0.08, 0.02],
      max: [0.92, 0.24],
      pos: [0, 0],
      size: [0, 0],
      text: "Music Lives in You",
      sizePx: 13,
      font: FONT_BODY,
      align: 0,
      color: [1, 1, 1, 0.82],
      italic: 2,
    });

  scene = scene.replace(
    /(m_Father: \{fileID: 137282937\}[\s\S]*?m_Children:\n(?:  - \{fileID: \d+\}\n)*)(  - \{fileID: 1921249757\}\n)/,
    `$1$2  - {fileID: ${ROOT.rt}}\n`,
  );

  const rootsIdx = scene.lastIndexOf("--- !u!1660057539");
  scene = scene.slice(0, rootsIdx) + yaml + scene.slice(rootsIdx);
}

if (!scene.includes(`cornerInfoHud: {fileID: ${ROOT.hud}}`)) {
  scene = scene.replace(
    new RegExp(`(&${META_STATUS_MENU}\\b[\\s\\S]*?dateChipLabel: \\{fileID: \\d+\\})`),
    `$1\n  cornerInfoHud: {fileID: ${ROOT.hud}}`,
  );
}

scene = scene.replace(
  new RegExp(`(&${DATE_CHIP_GO}\\b[\\s\\S]*?m_IsActive: )1`),
  "$10",
);

fs.writeFileSync(SCENE, scene);
console.log("Seeded HubCornerInfoHud under StatusMenu; DateChip hidden.");
