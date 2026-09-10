import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";
const text = fs.readFileSync(SCENE, "utf8");
const header = text.match(/^[\s\S]*?(?=^--- !u!)/m)?.[0] ?? "";
const parts = text.split(/^--- !u!/m);
const blocks = [];
for (let i = 1; i < parts.length; i++) {
  const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
  if (!m) continue;
  blocks.push({ type: m[1], id: m[2], body: m[3], idx: i });
}

const removeIds = new Set();
for (const b of blocks) {
  if (b.type === "1" && /\n  m_Name: LabelBg\n/.test(b.body)) {
    removeIds.add(b.id);
    for (const c of b.body.matchAll(/- component: \{fileID: (\d+)\}/g)) removeIds.add(c[1]);
  }
}
// also remove orphan blocks that reference removed GameObjects
for (const b of blocks) {
  const go = b.body.match(/m_GameObject: \{fileID: (\d+)\}/)?.[1];
  if (go && removeIds.has(go)) removeIds.add(b.id);
}
// remove any block whose own id is in remove set from broken duplicate id range
for (const b of blocks) {
  if (removeIds.has(b.id)) continue;
  if (/\n  m_Name: LabelBg\n/.test(b.body)) removeIds.add(b.id);
}

let kept = blocks.filter((b) => !removeIds.has(b.id));

// strip LabelBg child refs from parents
for (const b of kept) {
  if (b.type !== "224") continue;
  if (!b.body.includes("m_Children:")) continue;
  const lines = b.body.split("\n");
  const next = [];
  let inChildren = false;
  for (const line of lines) {
    if (line.startsWith("  m_Children:")) {
      inChildren = true;
      next.push(line);
      continue;
    }
    if (inChildren) {
      const m = line.match(/^\s+- \{fileID: (\d+)\}$/);
      if (m) {
        if (removeIds.has(m[1])) continue;
        next.push(line);
        continue;
      }
      inChildren = false;
    }
    next.push(line);
  }
  const idx = next.findIndex((l) => l.startsWith("  m_Children:"));
  if (idx >= 0) {
    let j = idx + 1;
    while (j < next.length && /^\s+- \{fileID:/.test(next[j])) j++;
    if (j === idx + 1) next[idx] = "  m_Children: []";
  }
  b.body = next.join("\n");
}

for (const b of kept) {
  if (b.body.includes("labelBackground:")) {
    b.body = b.body.replace(/labelBackground: \{fileID: [^}]+\n/, "labelBackground: {fileID: 0}\n");
  }
}

const idSet = new Map(kept.map((b) => [b.id + ":" + b.type, b]));
function maxId() {
  let max = 0;
  for (const b of kept) {
    const n = Number(b.id);
    if (Number.isFinite(n) && n < 2e9 && n > max) max = n;
  }
  return max;
}

function add(type, id, body) {
  const b = { type, id, body };
  kept.push(b);
  return b;
}

const poolGos = kept
  .filter((b) => b.type === "1" && /^EquipPool_\d+$/.test(b.body.match(/\n  m_Name: (.+)/)?.[1] || ""))
  .sort((a, b) => {
    const na = Number(a.body.match(/\n  m_Name: EquipPool_(\d+)/)?.[1] || 0);
    const nb = Number(b.body.match(/\n  m_Name: EquipPool_(\d+)/)?.[1] || 0);
    return na - nb;
  });

let cursor = maxId();
for (const poolGo of poolGos) {
  const poolRt = [...poolGo.body.matchAll(/- component: \{fileID: (\d+)\}/g)]
    .map((x) => x[1])
    .find((cid) => kept.some((b) => b.id === cid && b.type === "224"));
  const poolRtBlock = kept.find((b) => b.id === poolRt && b.type === "224");
  const existingKids = [
    ...(poolRtBlock.body.matchAll(/- \{fileID: (\d+)\}/g) ?? []),
  ].map((x) => x[1]);

  const idGo = String(++cursor);
  const idRt = String(++cursor);
  const idCr = String(++cursor);
  const idImg = String(++cursor);

  add(
    "1",
    idGo,
    `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${idRt}}
  - component: {fileID: ${idCr}}
  - component: {fileID: ${idImg}}
  m_Layer: 0
  m_Name: LabelBg
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
`,
  );
  add(
    "224",
    idRt,
    `RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${idGo}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${poolRt}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.06, y: 0.18}
  m_AnchorMax: {x: 0.94, y: 0.82}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
`,
  );
  add(
    "222",
    idCr,
    `CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${idGo}}
  m_CullTransparentMesh: 1
`,
  );
  add(
    "114",
    idImg,
    `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${idGo}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 0.04, g: 0.06, b: 0.16, a: 0.55}
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
`,
  );

  const childLines = [`  - {fileID: ${idRt}}`, ...existingKids.map((k) => `  - {fileID: ${k}}`)].join(
    "\n",
  );
  if (poolRtBlock.body.includes("m_Children: []")) {
    poolRtBlock.body = poolRtBlock.body.replace("m_Children: []", `m_Children:\n${childLines}`);
  } else {
    poolRtBlock.body = poolRtBlock.body.replace(
      /m_Children:\n(?:  - \{fileID: \d+\}\n)*/,
      `m_Children:\n${childLines}\n`,
    );
  }

  const view = kept.find(
    (b) =>
      b.type === "114" &&
      b.body.includes(`m_GameObject: {fileID: ${poolGo.id}}`) &&
      b.body.includes("CharacterBuildEquipSlotView"),
  );
  if (view) {
    if (view.body.includes("labelBackground:")) {
      view.body = view.body.replace(
        /labelBackground: \{fileID: [^}]+\}/,
        `labelBackground: {fileID: ${idImg}}`,
      );
    } else {
      view.body = view.body.replace(
        /(frame: \{fileID: \d+\}\n)/,
        `$1  labelBackground: {fileID: ${idImg}}\n`,
      );
    }
  }
}

// ensure SceneRoots last
const roots = kept.filter((b) => b.type === "1660057539");
kept = kept.filter((b) => b.type !== "1660057539").concat(roots);

const out =
  header +
  kept.map((b) => `--- !u!${b.type} &${b.id}\n${b.body}`).join("").replace(/\n*$/, "\n");
fs.writeFileSync(SCENE, out);

const names = (out.match(/m_Name: LabelBg/g) || []).length;
const uniqueGo = kept.filter((b) => b.type === "1" && /\n  m_Name: LabelBg\n/.test(b.body));
let bad = 0;
for (const g of uniqueGo) {
  const cs = [...g.body.matchAll(/- component: \{fileID: (\d+)\}/g)].map((x) => x[1]);
  if (new Set(cs).size !== 3 || cs.includes(g.id)) bad++;
}
console.log(JSON.stringify({ removed: removeIds.size, labelBg: uniqueGo.length, bad, names }));
