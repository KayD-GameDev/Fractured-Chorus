import fs from "fs";
import path from "path";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/Bonds.unity";
const FRAME_GUID = "3d2cb6841e9d2abcbfe04d4478bfdf74";

const PROMO_FRAME_GO = "931000460";
const PROMO_FRAME_RT = "931000461";
const PROMO_FRAME_IMAGE = "931000463";
const PROMO_IMAGE_RT = "931000465";

const CHROME_GO = "932301001";
const CHROME_RT = "932301002";
const CHROME_CR = "932301003";
const CHROME_IMG = "932301004";

function disableParentFrameImage(text) {
  const block = text.match(
    new RegExp(`--- !u!114 &${PROMO_FRAME_IMAGE}[\\s\\S]*?(?=\\n--- !u!)`),
  );
  if (!block) throw new Error("PromoFrame Image block not found");
  if (block[0].includes("m_Enabled: 0")) return text;
  return text.replace(
    new RegExp(`(--- !u!114 &${PROMO_FRAME_IMAGE}[\\s\\S]*?m_Enabled: )1`),
    "$10",
  );
}

function insertChromeBlocks(text) {
  if (text.includes(`m_Name: PromoFrameChrome`)) {
    return text;
  }

  const chromeYaml = `
--- !u!1 &${CHROME_GO}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${CHROME_RT}}
  - component: {fileID: ${CHROME_CR}}
  - component: {fileID: ${CHROME_IMG}}
  m_Layer: 0
  m_Name: PromoFrameChrome
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${CHROME_RT}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${CHROME_GO}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${PROMO_FRAME_RT}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &${CHROME_CR}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${CHROME_GO}}
  m_CullTransparentMesh: 1
--- !u!114 &${CHROME_IMG}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${CHROME_GO}}
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
  m_Sprite: {fileID: 21300000, guid: ${FRAME_GUID}, type: 3}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`;

  const anchor = `--- !u!1 &${PROMO_FRAME_GO}`;
  return text.replace(anchor, chromeYaml + anchor);
}

function wireChildren(text) {
  const rtBlock = text.match(
    new RegExp(`--- !u!224 &${PROMO_FRAME_RT}[\\s\\S]*?(?=\\n--- !u!)`),
  );
  if (!rtBlock) throw new Error("PromoFrame RectTransform not found");
  const updated = rtBlock[0].replace(
    /m_Children:\n(?:  - \{fileID: \d+\}\n)+/,
    `m_Children:\n  - {fileID: ${PROMO_IMAGE_RT}}\n  - {fileID: ${CHROME_RT}}\n`,
  );
  return text.replace(rtBlock[0], updated);
}

let text = fs.readFileSync(SCENE, "utf8");
text = disableParentFrameImage(text);
text = insertChromeBlocks(text);
text = wireChildren(text);
fs.writeFileSync(SCENE, text);
console.log(JSON.stringify({ scene: SCENE, promoStack: "PromoImage(0) + PromoFrameChrome(1)" }, null, 2));
