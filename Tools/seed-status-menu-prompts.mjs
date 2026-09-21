import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";
const UI_TEXT = "5f7201a12d95ffc409449d95f23cf332";
const FONT = "787fda44816c480a9cc3cadfdc29ce24";

const PROMPTS_RT = "1881873895";
const SEP = {
  go: "940060010",
  rt: "940060011",
  txt: "940060012",
  cr: "940060013",
};

function textYaml({ go, rt, txt, cr, name, father, min, max, text, size, align }) {
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
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: ${FONT}, type: 3}
    m_FontSize: ${size}
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
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

if (!scene.includes("m_Name: PromptSeparator")) {
  const yaml = textYaml({
    go: SEP.go,
    rt: SEP.rt,
    txt: SEP.txt,
    cr: SEP.cr,
    name: "PromptSeparator",
    father: PROMPTS_RT,
    min: [0.41, 0],
    max: [0.45, 1],
    text: "|",
    size: 16,
    align: 4,
  });
  const rootsIdx = scene.lastIndexOf("--- !u!1660057539");
  scene = scene.slice(0, rootsIdx) + yaml + scene.slice(rootsIdx);
  scene = scene.replace(
    /(m_Father: \{fileID: 1881873895\}[\s\S]*?m_Children:\n(?:  - \{fileID: \d+\}\n)*)(  - \{fileID: 1235451124\})/,
    `$1  - {fileID: ${SEP.rt}}\n$2`,
  );
}

scene = scene.replace(
  /(m_GameObject: \{fileID: 1851032672\}[\s\S]*?m_AnchorMin: \{x: )0(, y: 0\.1\})/,
  "$10$2",
);
scene = scene.replace(
  /(m_GameObject: \{fileID: 1851032672\}[\s\S]*?m_AnchorMax: \{x: )0\.22(, y: 0\.9\})/,
  "$10.14$2",
);
scene = scene.replace(
  /(m_GameObject: \{fileID: 480427950\}[\s\S]*?m_AnchorMin: \{x: )0\.22(, y: 0\})/,
  "$10.14$2",
);
scene = scene.replace(
  /(m_GameObject: \{fileID: 480427950\}[\s\S]*?m_AnchorMax: \{x: )0\.5(, y: 1\})/,
  "$10.38$2",
);
scene = scene.replace(
  /(m_GameObject: \{fileID: 1235451123\}[\s\S]*?m_AnchorMin: \{x: )0\.52(, y: 0\.1\})/,
  "$10.48$2",
);
scene = scene.replace(
  /(m_GameObject: \{fileID: 1235451123\}[\s\S]*?m_AnchorMax: \{x: )0\.74(, y: 0\.9\})/,
  "$10.62$2",
);
scene = scene.replace(
  /(m_GameObject: \{fileID: 1602759977\}[\s\S]*?m_AnchorMin: \{x: )0\.74(, y: 0\})/,
  "$10.62$2",
);
scene = scene.replace(
  /(m_GameObject: \{fileID: 1602759977\}[\s\S]*?m_AnchorMax: \{x: )1(, y: 1\})/,
  "$10.86$2",
);

scene = scene.replace(
  /(m_GameObject: \{fileID: 1235451123\}[\s\S]*?m_Name: )CloseIcon/m,
  "$1BackIcon",
);
scene = scene.replace(
  /(m_GameObject: \{fileID: 1602759977\}[\s\S]*?m_Name: )CloseText/m,
  "$1BackText",
);
scene = scene.replace(
  /(m_GameObject: \{fileID: 1602759977\}[\s\S]*?m_Text: )Close/m,
  "$1Back",
);

fs.writeFileSync(SCENE, scene);
console.log("StatusMenu Prompts updated: mouse confirm + ESC back + separator");
