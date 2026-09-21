import fs from "node:fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";
const CANVAS_RT = 777585278;

const GUID = {
  panelPortrait: "65b38bcfadf3ec000b1ddadc35e7218a",
  headerBar: "fa13a212984f897b047dcdee68ee9f4a",
  navNormal: "31c6a9e5e0833c2acf73fca1f893a7e2",
  navSelected: "f7717c5916c9afb8be03f47495a79128",
  memory: "eb33cb5a4a3526d5a15eefdb48ef2268",
  skillSlot: "b3ce58374d87b2d7d18e2bcb1c1e194d",
  close: "b4a235cf97f7f56d73752d17678ec675",
  note: "3d5a82200cc0672ff0def2d965931088",
  orb: "0daee440cc836f472cadb0b11439eee8",
  shards: "4c57420fe715ff33207b3b91d687a3aa",
  divider: "1a7948506ce41953e9da778be76f3b64",
  barTrack: "ecf396f1eee27009cca7e590d3af2549",
  barFill: "8f644d969735cb1aeceac8f8686e3e24",
  barExp: "271de6ce388c4d05e97c85c48af46fa3",
};

let nextId = 950000001;
function id() {
  return nextId++;
}

function imageBlock({ name, father, amin, amax, sprite, type = 1, preserve = 0, raycast = 0, children = [] }) {
  const go = id();
  const rt = id();
  const img = id();
  const cr = id();
  return {
    rt,
    yaml: `--- !u!1 &${go}
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
  m_Children:${children.length ? "\n" + children.map((c) => `  - {fileID: ${c}}`).join("\n") : " []"}
  m_Father: {fileID: ${father}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${amin[0]}, y: ${amin[1]}}
  m_AnchorMax: {x: ${amax[0]}, y: ${amax[1]}}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
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
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: ${raycast}
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 21300000, guid: ${sprite}, type: 3}
  m_Type: ${type}
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
`,
  };
}

function panelBlock(name, father, amin, amax, childRts = []) {
  const go = id();
  const rt = id();
  return {
    rt,
    yaml: `--- !u!1 &${go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rt}}
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
  m_Children:${childRts.length ? "\n" + childRts.map((c) => `  - {fileID: ${c}}`).join("\n") : " []"}
  m_Father: {fileID: ${father}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${amin[0]}, y: ${amin[1]}}
  m_AnchorMax: {x: ${amax[0]}, y: ${amax[1]}}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
`,
  };
}

let text = fs.readFileSync(SCENE, "utf8");
if (text.includes("m_Name: PortraitPanel")) {
  console.log("PortraitPanel already present — skip");
  process.exit(0);
}

const chunks = [];
const rootChildren = [];

const portrait = imageBlock({
  name: "PortraitPanel",
  father: CANVAS_RT,
  amin: [0.15, 0.1],
  amax: [0.4, 0.78],
  sprite: GUID.panelPortrait,
  type: 1,
  preserve: 0,
});
chunks.push(portrait.yaml);
rootChildren.push(portrait.rt);

const header = imageBlock({
  name: "HeaderBar",
  father: CANVAS_RT,
  amin: [0.42, 0.78],
  amax: [0.9, 0.86],
  sprite: GUID.headerBar,
  type: 1,
});
chunks.push(header.yaml);
rootChildren.push(header.rt);

const navBtns = [];
const labels = ["STAT", "BONDS", "CALENDAR", "SYSTEM"];
for (let i = 0; i < 4; i++) {
  const selected = i === 0;
  const y0 = 0.76 - i * 0.16;
  const btn = imageBlock({
    name: `Nav_${labels[i]}`,
    father: 0,
    amin: [0, y0],
    amax: [1, y0 + 0.14],
    sprite: selected ? GUID.navSelected : GUID.navNormal,
    type: 1,
    raycast: 1,
  });
  navBtns.push(btn);
}
const nav = panelBlock(
  "NavColumn",
  CANVAS_RT,
  [0.02, 0.18],
  [0.145, 0.76],
  navBtns.map((b) => b.rt),
);
for (const b of navBtns) {
  b.yaml = b.yaml.replace(`m_Father: {fileID: 0}`, `m_Father: {fileID: ${nav.rt}}`);
  chunks.push(b.yaml);
}
chunks.push(nav.yaml);
rootChildren.push(nav.rt);

const memory = imageBlock({
  name: "MemoryPanel",
  father: CANVAS_RT,
  amin: [0.15, 0.02],
  amax: [0.34, 0.16],
  sprite: GUID.memory,
  type: 1,
});
chunks.push(memory.yaml);
rootChildren.push(memory.rt);

const skillSlots = [];
for (let i = 0; i < 3; i++) {
  const y0 = 0.72 - i * 0.22;
  const slot = imageBlock({
    name: `SkillSlot_${i}`,
    father: 0,
    amin: [0.06, y0],
    amax: [0.94, y0 + 0.18],
    sprite: GUID.skillSlot,
    type: 1,
    raycast: 1,
  });
  skillSlots.push(slot);
}
const skills = panelBlock(
  "SkillsPanel",
  CANVAS_RT,
  [0.72, 0.1],
  [0.97, 0.76],
  skillSlots.map((s) => s.rt),
);
for (const s of skillSlots) {
  s.yaml = s.yaml.replace(`m_Father: {fileID: 0}`, `m_Father: {fileID: ${skills.rt}}`);
  chunks.push(s.yaml);
}
chunks.push(skills.yaml);
rootChildren.push(skills.rt);

const expTrack = imageBlock({
  name: "ExpTrack",
  father: 0,
  amin: [0.05, 0.55],
  amax: [0.95, 0.72],
  sprite: GUID.barTrack,
  type: 1,
});
const expFill = imageBlock({
  name: "ExpFill",
  father: 0,
  amin: [0.05, 0.55],
  amax: [0.55, 0.72],
  sprite: GUID.barFill,
  type: 1,
  preserve: 0,
});
const expCombo = imageBlock({
  name: "ExpBar",
  father: 0,
  amin: [0.05, 0.3],
  amax: [0.95, 0.48],
  sprite: GUID.barExp,
  type: 1,
});
const divider = imageBlock({
  name: "Divider",
  father: 0,
  amin: [0.08, 0.2],
  amax: [0.92, 0.28],
  sprite: GUID.divider,
  type: 0,
  preserve: 1,
});
const stats = panelBlock(
  "StatsPanel",
  CANVAS_RT,
  [0.42, 0.18],
  [0.7, 0.76],
  [expTrack.rt, expFill.rt, expCombo.rt, divider.rt],
);
for (const b of [expTrack, expFill, expCombo, divider]) {
  b.yaml = b.yaml.replace(`m_Father: {fileID: 0}`, `m_Father: {fileID: ${stats.rt}}`);
  chunks.push(b.yaml);
}
chunks.push(stats.yaml);
rootChildren.push(stats.rt);

const close = imageBlock({
  name: "CloseBtn",
  father: CANVAS_RT,
  amin: [0.93, 0.9],
  amax: [0.985, 0.98],
  sprite: GUID.close,
  type: 0,
  preserve: 1,
  raycast: 1,
});
chunks.push(close.yaml);
rootChildren.push(close.rt);

const note = imageBlock({
  name: "NoteCircle",
  father: CANVAS_RT,
  amin: [0.43, 0.04],
  amax: [0.5, 0.14],
  sprite: GUID.note,
  type: 0,
  preserve: 1,
  raycast: 1,
});
chunks.push(note.yaml);
rootChildren.push(note.rt);

const orb = imageBlock({
  name: "OrbFilled",
  father: CANVAS_RT,
  amin: [0.52, 0.04],
  amax: [0.59, 0.14],
  sprite: GUID.orb,
  type: 0,
  preserve: 1,
  raycast: 1,
});
chunks.push(orb.yaml);
rootChildren.push(orb.rt);

const shards = imageBlock({
  name: "CrystalShards",
  father: CANVAS_RT,
  amin: [0.88, 0.02],
  amax: [0.98, 0.16],
  sprite: GUID.shards,
  type: 0,
  preserve: 1,
});
chunks.push(shards.yaml);
rootChildren.push(shards.rt);

const childAppend = rootChildren.map((c) => `  - {fileID: ${c}}`).join("\n");
text = text.replace(
  `  m_Children:
  - {fileID: 32497166}
  - {fileID: 576334294}
  - {fileID: 1704021110}
  m_Father: {fileID: 0}`,
  `  m_Children:
  - {fileID: 32497166}
  - {fileID: 576334294}
  - {fileID: 1704021110}
${childAppend}
  m_Father: {fileID: 0}`,
);

fs.writeFileSync(SCENE, text.trimEnd() + "\n" + chunks.join("") + "\n");
console.log(JSON.stringify({ added: rootChildren.length, names: ["PortraitPanel","HeaderBar","NavColumn","MemoryPanel","SkillsPanel","StatsPanel","CloseBtn","NoteCircle","OrbFilled","CrystalShards"] }));
