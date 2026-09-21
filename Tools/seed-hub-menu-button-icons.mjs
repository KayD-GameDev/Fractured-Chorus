import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";

const ICONS = [
  { btn: "BtnStats", guid: "c1a10001a2b34c5d8e9f0123456789b1", base: 920010010 },
  { btn: "BtnBonds", guid: "c1a10002a2b34c5d8e9f0123456789b2", base: 920010020 },
  { btn: "BtnCalendar", guid: "c1a10003a2b34c5d8e9f0123456789b3", base: 920010030 },
  { btn: "BtnSystem", guid: "c1a10004a2b34c5d8e9f0123456789b4", base: 920010040 },
  { btn: "BtnSave", guid: "c1a10005a2b34c5d8e9f0123456789b5", base: 920010050 },
  { btn: "BtnLoad", guid: "c1a10006a2b34c5d8e9f0123456789b6", base: 920010060 },
  { btn: "BtnConfig", guid: "c1a10007a2b34c5d8e9f0123456789b7", base: 920010070 },
  { btn: "BtnReturnToTitle", guid: "c1a10008a2b34c5d8e9f0123456789b8", base: 920010080 },
];

function iconYaml({ go, rt, img, cr, btnRt, guid }) {
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
  m_Name: Icon
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
  m_Father: {fileID: ${btnRt}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.02, y: 0.1}
  m_AnchorMax: {x: 0.24, y: 0.9}
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

const text = fs.readFileSync(SCENE, "utf8");
const parts = text.split(/^--- !u!/m);
const header = parts[0];
const blocks = [];
for (let i = 1; i < parts.length; i++) {
  const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
  if (!m) continue;
  blocks.push({ type: m[1], id: m[2], body: m[3] });
}
const byId = new Map(blocks.map((b) => [b.id, b]));

function goName(id) {
  return byId.get(id)?.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}
function comps(id) {
  return [...(byId.get(id)?.body.matchAll(/component: \{fileID: (\d+)\}/g) || [])].map((x) => x[1]);
}
function goOf(c) {
  for (const [gid, g] of byId) {
    if (g.type === "1" && g.body.includes(`component: {fileID: ${c}}`)) return gid;
  }
  return null;
}
function children(rt) {
  const b = byId.get(rt)?.body ?? "";
  const m = b.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!m) return [];
  return [...m[1].matchAll(/fileID: (\d+)/g)].map((x) => x[1]);
}
function xf(go) {
  for (const c of comps(go)) {
    const t = byId.get(c);
    if (t?.type === "224") return c;
  }
  return null;
}
function findGo(name) {
  for (const [id, b] of byId) {
    if (b.type === "1" && b.body.match(new RegExp(`\\n  m_Name: ${name}\\n`))) return id;
  }
  return null;
}

const extras = [];
for (const spec of ICONS) {
  const btn = findGo(spec.btn);
  if (!btn) {
    console.error("missing button", spec.btn);
    continue;
  }
  const btnRt = xf(btn);
  const kids = children(btnRt);
  const hasIcon = kids.some((rt) => goName(goOf(rt)) === "Icon");
  if (hasIcon) {
    console.log("skip existing Icon", spec.btn);
    continue;
  }

  const go = spec.base;
  const rt = spec.base + 1;
  const img = spec.base + 2;
  const cr = spec.base + 3;
  extras.push(iconYaml({ go, rt, img, cr, btnRt, guid: spec.guid }));

  const rtBlock = byId.get(btnRt);
  if (rtBlock.body.includes("m_Children: []")) {
    rtBlock.body = rtBlock.body.replace(
      "m_Children: []",
      `m_Children:\n  - {fileID: ${rt}}`,
    );
  } else {
    rtBlock.body = rtBlock.body.replace(
      /(\n  m_Children:\n(?:  - \{fileID: \d+\}\n)*)/,
      `$1  - {fileID: ${rt}}\n`,
    );
  }
  console.log("seeded Icon", spec.btn, "rt", rt);
}

const out = [header];
for (const b of blocks) {
  const cur = byId.get(b.id);
  out.push(`--- !u!${cur.type} &${cur.id}\n${cur.body}`);
}
if (extras.length) {
  out.push(extras.join(""));
}
fs.writeFileSync(SCENE, out.join(""));
console.log("wrote", extras.length, "icons");
