import fs from "fs";

const HUB = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";
const MENU = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/MainMenuStartGame.unity";

const UI_TEXT = "5f7201a12d95ffc409449d95f23cf332";
const FONT_DISPLAY = "d4e5f6a7b8c94091a2b3c4d5e6f70891";
const FONT_BODY = "787fda44816c480a9cc3cadfdc29ce24";

const TEXT_PRIMARY = { r: 0.9176471, g: 0.9843138, b: 1, a: 1 };
const CYAN_SOFT = { r: 0.2, g: 0.75, b: 1, a: 0.88 };

function textRootYaml({ go, rt, father }) {
  return `--- !u!1 &${go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rt}}
  m_Layer: 0
  m_Name: TextRoot
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
`;
}

function labelYaml({ go, rt, txt, cr, name, father, min, max, text, size, font, color, align }) {
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
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
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
  m_Color: {r: ${color.r}, g: ${color.g}, b: ${color.b}, a: ${color.a}}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: ${font}, type: 3}
    m_FontSize: ${size}
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: ${align}
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 1
    m_VerticalOverflow: 1
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

function patchScene(scenePath, config) {
  let scene = fs.readFileSync(scenePath, "utf8");
  if (scene.includes(`m_Father: {fileID: ${config.rootRt}}\n  m_LocalEulerAnglesHint`) &&
      scene.includes("m_Name: TextRoot")) {
    const underButton = scene.includes(
      `m_GameObject: {fileID: ${config.buttonGo}}\n  m_LocalRotation`,
    );
    if (scene.includes(`m_Name: TitleLabel`) && underButton) {
      console.log(`${scenePath}: TextRoot already seeded`);
      return;
    }
  }

  if (!scene.includes(`m_Name: ResonanceDiveButton`)) {
    throw new Error(`${scenePath}: ResonanceDiveButton missing`);
  }

  const yaml =
    textRootYaml({ go: config.textRootGo, rt: config.textRootRt, father: config.rootRt }) +
    labelYaml({
      go: config.titleGo,
      rt: config.titleRt,
      txt: config.titleTxt,
      cr: config.titleCr,
      name: "TitleLabel",
      father: config.textRootRt,
      min: [0.38, 0.42],
      max: [0.88, 0.82],
      text: "Resonance Dive",
      size: config.titleSize,
      font: FONT_DISPLAY,
      color: TEXT_PRIMARY,
      align: 3,
    }) +
    labelYaml({
      go: config.subGo,
      rt: config.subRt,
      txt: config.subTxt,
      cr: config.subCr,
      name: "SubtitleLabel",
      father: config.textRootRt,
      min: [0.38, 0.2],
      max: [0.88, 0.42],
      text: "ENTER THE OTHER SIDE",
      size: config.subSize,
      font: FONT_BODY,
      color: CYAN_SOFT,
      align: 0,
    });

  const rootsIdx = scene.lastIndexOf("--- !u!1660057539");
  scene = scene.slice(0, rootsIdx) + yaml + scene.slice(rootsIdx);

  scene = scene.replace(
    new RegExp(
      `(m_GameObject: \\{fileID: ${config.buttonGo}\\}[\\s\\S]*?m_Children:\\n(?:  - \\{fileID: \\d+\\}\\n)*)`,
    ),
    `$1  - {fileID: ${config.textRootRt}}\n`,
  );

  scene = scene.replace(
    new RegExp(
      `(m_GameObject: \\{fileID: ${config.textRootGo}\\}[\\s\\S]*?m_Children:\\n)(?:  - \\{fileID: \\d+\\}\\n)*  m_Father:`,
    ),
    `$1  - {fileID: ${config.titleRt}}\n  - {fileID: ${config.subRt}}\n  m_Father:`,
  );

  const fxBlock = `  button: {fileID: ${config.buttonRef}}
  shadow: {fileID: ${config.shadowRef}}
  baseLayer: {fileID: 0}
  gradient: {fileID: 0}
  glass: {fileID: 0}
  border: {fileID: 0}
  decoLeft: {fileID: 0}
  decoRight: {fileID: 0}
  textLayer: {fileID: 0}
  titleLabel: {fileID: ${config.titleTxt}}
  subtitleLabel: {fileID: ${config.subTxt}}
  plate: {fileID: ${config.plateRef}}
  glow: {fileID: ${config.glowRef}}
  glowIdleMin: 0.55
  glowIdleMax: 1
  glowHoverMin: 0.8
  glowHoverMax: 1
  pulseSpeed: 2.2
  particleOrbitSpeed: 0.7`;

  scene = scene.replace(
    new RegExp(
      `button: \\{fileID: ${config.buttonRef}\\}[\\s\\S]*?pulseSpeed: [\\d.]+(?:\\n  particleDrift:[\\s\\S]*?scanAmplitude: [\\d.]+)?`,
    ),
    fxBlock.trimEnd(),
  );

  fs.writeFileSync(scenePath, scene);
  console.log(`patched ${scenePath}`);
}

patchScene(HUB, {
  buttonGo: "940030001",
  rootRt: "940030002",
  buttonRef: "940030005",
  shadowRef: "940030014",
  plateRef: "940030024",
  glowRef: "940030034",
  textRootGo: "940030071",
  textRootRt: "940030072",
  titleGo: "940030081",
  titleRt: "940030082",
  titleTxt: "940030083",
  titleCr: "940030084",
  subGo: "940030091",
  subRt: "940030092",
  subTxt: "940030093",
  subCr: "940030094",
  titleSize: 28,
  subSize: 11,
});

patchScene(MENU, {
  buttonGo: "940040001",
  rootRt: "940040002",
  buttonRef: "940040005",
  shadowRef: "940040014",
  plateRef: "940040024",
  glowRef: "940040034",
  textRootGo: "940040071",
  textRootRt: "940040072",
  titleGo: "940040081",
  titleRt: "940040082",
  titleTxt: "940040083",
  titleCr: "940040084",
  subGo: "940040091",
  subRt: "940040092",
  subTxt: "940040093",
  subCr: "940040094",
  titleSize: 32,
  subSize: 12,
});
