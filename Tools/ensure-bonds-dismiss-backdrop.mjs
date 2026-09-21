import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/Bonds.unity";
const FX_RECT = 931001851;
const FX_BTN = 931001854;

let text = fs.readFileSync(SCENE, "utf8");

if (text.includes("m_Name: DismissBackdrop")) {
  if (!text.includes("  - {fileID: 931001851}")) {
    text = text.replace(
      "  - {fileID: 931000016}\n  - {fileID: 931000020}",
      "  - {fileID: 931000016}\n  - {fileID: 931001851}\n  - {fileID: 931000020}",
    );
    fs.writeFileSync(SCENE, text);
    console.log(JSON.stringify({ ok: true, linked: true }));
  } else {
    console.log(JSON.stringify({ skipped: true, reason: "DismissBackdrop exists" }));
  }
  process.exit(0);
}

if (!text.includes("  - {fileID: 931000016}\n  - {fileID: 931000020}")) {
  console.error("Canvas child anchor not found");
  process.exit(1);
}

text = text.replace(
  "  - {fileID: 931000016}\n  - {fileID: 931000020}",
  `  - {fileID: 931000016}\n  - {fileID: ${FX_RECT}}\n  - {fileID: 931000020}`,
);

if (!text.includes("dismissBackdrop:")) {
  text = text.replace(
    "  promoImage: {fileID: 931000467}",
    `  promoImage: {fileID: 931000467}\n  dismissBackdrop: {fileID: ${FX_BTN}}`,
  );
}

const block = `
--- !u!1 &931001850
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${FX_RECT}}
  - component: {fileID: 931001852}
  - component: {fileID: 931001853}
  - component: {fileID: ${FX_BTN}}
  m_Layer: 0
  m_Name: DismissBackdrop
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!224 &${FX_RECT}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 931001850}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 931000010}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &931001852
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 931001850}
  m_CullTransparentMesh: 1
--- !u!114 &931001853
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 931001850}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
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
--- !u!114 &${FX_BTN}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 931001850}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}
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
    m_NormalColor: {r: 1, g: 1, b: 1, a: 0}
    m_HighlightedColor: {r: 1, g: 1, b: 1, a: 0}
    m_PressedColor: {r: 1, g: 1, b: 1, a: 0}
    m_SelectedColor: {r: 1, g: 1, b: 1, a: 0}
    m_DisabledColor: {r: 1, g: 1, b: 1, a: 0}
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
  m_TargetGraphic: {fileID: 931001853}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
`;

text += block;

fs.writeFileSync(SCENE, text);
console.log(JSON.stringify({ ok: true, dismissBackdrop: FX_BTN }, null, 2));
