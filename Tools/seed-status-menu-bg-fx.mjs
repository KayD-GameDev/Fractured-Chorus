import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";
const UI_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc";
const BG_FX_SCRIPT = "47a581888e2a4d9a95862c66f50623cb";
const SPRITE_RAYS = "d3e4f5a6b7c8901234567890abcdef10";
const SPRITE_CLOUDS = "d3e4f5a6b7c8901234567890abcdef11";

const STATUS_MENU_GO = "137282936";
const STATUS_MENU_RT = "137282937";
const BACKGROUND_RT = "1921249757";

const BG_FX = "930050001";
const RAYS = { go: "930050010", rt: "930050011", img: "930050012", cr: "930050013" };
const CLOUD_FAR = { go: "930050020", rt: "930050021", img: "930050022", cr: "930050023" };
const CLOUD_NEAR = { go: "930050030", rt: "930050031", img: "930050032", cr: "930050033" };

const REMOVE_IDS = new Set([
  "930040010", "930040011", "930040012", "930040013",
  "930040020", "930040021", "930040022", "930040023",
]);

function imageLayer({ go, rt, img, cr, name, father, min, max, pos, size, color, sprite, preserve = 0 }) {
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
  m_Color: {r: ${color[0]}, g: ${color[1]}, b: ${color[2]}, a: ${color[3]}}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 21300000, guid: ${sprite}, type: 3}
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

let scene = fs.readFileSync(SCENE, "utf8");

if (!scene.includes("m_Name: MenuBgSunRays")) {
  scene = scene.replace(
    /(  - component: \{fileID: 137282937\}\n  - component: \{fileID: 137282938\})/,
    `  - component: {fileID: 137282937}\n  - component: {fileID: 137282938}\n  - component: {fileID: ${BG_FX}}`,
  );

  scene = scene.replace(
    /(m_Father: \{fileID: 137282937\}[\s\S]*?m_Children:\n(?:  - \{fileID: 1921249757\}\n))(  - \{fileID:)/,
    `$1  - {fileID: ${RAYS.rt}}\n  - {fileID: ${CLOUD_FAR.rt}}\n  - {fileID: ${CLOUD_NEAR.rt}}\n$2`,
  );

  const yaml =
    `--- !u!114 &${BG_FX}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${STATUS_MENU_GO}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${BG_FX_SCRIPT}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.Hub.StatusMenuBgFx
  sunRaysImage: {fileID: ${RAYS.img}}
  cloudNear: {fileID: ${CLOUD_NEAR.rt}}
  cloudFar: {fileID: ${CLOUD_FAR.rt}}
  cloudNearDriftSpeed: 14
  cloudFarDriftSpeed: 6
  sunPulseAmplitude: 0.07
  sunPulseHz: 0.22
` +
    imageLayer({
      ...RAYS,
      name: "MenuBgSunRays",
      father: STATUS_MENU_RT,
      min: [0, 0],
      max: [1, 1],
      pos: [0, 0],
      size: [0, 0],
      color: [1, 1, 1, 0.52],
      sprite: SPRITE_RAYS,
    }) +
    imageLayer({
      ...CLOUD_FAR,
      name: "MenuBgCloudsFar",
      father: STATUS_MENU_RT,
      min: [0, 0.58],
      max: [1, 0.96],
      pos: [0, 0],
      size: [960, 0],
      color: [1, 1, 1, 0.45],
      sprite: SPRITE_CLOUDS,
      preserve: 0,
    }) +
    imageLayer({
      ...CLOUD_NEAR,
      name: "MenuBgCloudsNear",
      father: STATUS_MENU_RT,
      min: [0, 0.72],
      max: [1, 1],
      pos: [0, 0],
      size: [720, 0],
      color: [1, 1, 1, 0.72],
      sprite: SPRITE_CLOUDS,
      preserve: 0,
    });

  const rootsIdx = scene.lastIndexOf("--- !u!1660057539");
  scene = scene.slice(0, rootsIdx) + yaml + scene.slice(rootsIdx);
}

scene = scene.replace(/--- !u![^\n]+\n&930040(?:010|011|012|013|020|021|022|023)[\s\S]*?(?=--- !u!|$)/g, "");
scene = scene.replace(/  - \{fileID: 930040(?:011|021)\}\n/g, "");

scene = scene.replace(/(\n  m_Name: BgSunClouds\n[\s\S]*?\n  m_IsActive: )1/g, "$10");
scene = scene.replace(/(\n  m_Name: BgDark\n[\s\S]*?\n  m_IsActive: )0/g, "$11");
scene = scene.replace(/(\n  m_Name: BgLight\n[\s\S]*?\n  m_IsActive: )0/g, "$11");

fs.writeFileSync(SCENE, scene);
console.log("StatusMenu BG sun rays + drifting clouds seeded; corner HUD BgSunClouds removed.");
