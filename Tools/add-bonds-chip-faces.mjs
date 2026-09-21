import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/Bonds.unity";

function faceYaml({ go, xf, cr, img, father, guid }) {
  return `--- !u!1 &${go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${xf}}
  - component: {fileID: ${cr}}
  - component: {fileID: ${img}}
  m_Layer: 0
  m_Name: Face
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${xf}
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
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 5.4, y: 0.9}
  m_SizeDelta: {x: 153.1, y: 142.29}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &${cr}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_CullTransparentMesh: 1
--- !u!114 &${img}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
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
  m_Sprite: {fileID: 21300000, guid: ${guid}, type: 3}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`;
}

const faces = [
  {
    go: 932200301,
    xf: 932200302,
    cr: 932200303,
    img: 932200304,
    father: 931000309,
    guid: "ad4f5b6c7e809123ad4f5b6c7e809123",
    frame: "146310505",
    nameLabel: "931000325",
  },
  {
    go: 932200401,
    xf: 932200402,
    cr: 932200403,
    img: 932200404,
    father: 659314520,
    guid: "cf617d8e0a021345cf617d8e0a021345",
    frame: "1211555371",
    nameLabel: "2119466388",
  },
  {
    go: 932200501,
    xf: 932200502,
    cr: 932200503,
    img: 932200504,
    father: 738828451,
    guid: "be506c7d8f910234be506c7d8f910234",
    frame: "1420852487",
    nameLabel: "1629402924",
  },
];

let scene = fs.readFileSync(SCENE, "utf8");
if (scene.includes("&932200301")) {
  throw new Error("Face blocks already exist");
}

for (const face of faces) {
  const childNeedle = `  - {fileID: ${face.frame}}\n`;
  if (!scene.includes(childNeedle)) {
    throw new Error(`Missing child ${face.frame}`);
  }
  scene = scene.replace(childNeedle, `${childNeedle}  - {fileID: ${face.xf}}\n`);

  const wireNeedle = `  nameLabel: {fileID: ${face.nameLabel}}\n  roleLabel:`;
  const wired = `  face: {fileID: ${face.img}}\n  lockIcon: {fileID: 0}\n  nameLabel: {fileID: ${face.nameLabel}}\n  roleLabel:`;
  if (!scene.includes(wireNeedle)) {
    throw new Error(`Missing nameLabel ${face.nameLabel}`);
  }
  scene = scene.replace(
    `  face: {fileID: 0}\n  lockIcon: {fileID: 0}\n  nameLabel: {fileID: ${face.nameLabel}}\n  roleLabel:`,
    wired,
  );
}

scene += faces.map((face) => faceYaml(face)).join("");
fs.writeFileSync(SCENE, scene);
console.log(JSON.stringify({ added: faces.map((face) => face.guid) }, null, 2));
