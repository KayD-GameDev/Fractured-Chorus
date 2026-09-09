import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SANDBOX = `${ROOT}/Assets/FracturedChorus/Scenes/CharacterBuildLayoutSandbox.unity`;
const PRODUCTION = `${ROOT}/Assets/FracturedChorus/Scenes/CharacterBuild.unity`;
const SNAPSHOT_SANDBOX = `${ROOT}/Assets/FracturedChorus/Art/UI/StatMenu/MockKit/sandbox_layout_snapshot.json`;
const SNAPSHOT_PROD = `${ROOT}/Assets/FracturedChorus/Art/UI/StatMenu/MockKit/characterbuild_layout_snapshot.json`;

const GUID = {
  plus: "b1cf6fa5eacbadd8a2a75ba84302ddec",
  minus: "857dbc685c7ad85f072bf87db6eb5f06",
  image: "fe87c0e1cc204ed48ad3b37840f39efc",
  button: "4e29b1a8efbd4b44bb3f3716e73f07ff",
  text: "5f7201a12d95ffc409449d95f23cf332",
  statRow: "4f3e8c32141862b4297e2dd9bad0f582",
  font: "d4e5f6a7b8c94091a2b3c4d5e6f70891",
};

const CRYSTAL_GUIDS = [
  "c95f8a968c3dc9da10235e87bfeba29c",
  "2d93032e1590014acffe431da4114952",
  "c14126736ec286554ba756af726d747f",
  "b6ce273ed5604d524d69905b443bc091",
  "91c3e8a04b2d4f6e8a1c5d7b9e0f2341",
  "92d4f9b15c3e507f9b2d6e8c0f103452",
  "93e50ac26d4f6180ac3e7f9d10214563",
];

const STAT_ROWS = ["StatRow_Strength", "StatRow_Magic", "StatRow_Endurance", "StatRow_HeartBeat"];
const CRYSTAL_SEEDS = [
  { x: -640, y: 220, z: 18, s: 42 },
  { x: 520, y: -180, z: -32, s: 36 },
  { x: -180, y: 410, z: 54, s: 50 },
  { x: 780, y: 90, z: -12, s: 28 },
  { x: -820, y: -260, z: 40, s: 46 },
  { x: 210, y: -390, z: -50, s: 32 },
  { x: -420, y: -80, z: 8, s: 38 },
  { x: 90, y: 310, z: -22, s: 54 },
  { x: 640, y: 360, z: 26, s: 30 },
  { x: -70, y: -220, z: -40, s: 44 },
  { x: 380, y: 40, z: 12, s: 26 },
  { x: -560, y: 140, z: -8, s: 48 },
  { x: 860, y: -320, z: 34, s: 34 },
  { x: -310, y: 480, z: -28, s: 40 },
];

let nextId = 0;
const allocId = () => ++nextId;

function parseScene(text) {
  const parts = text.split(/^--- !u!/m);
  const header = parts[0];
  const blocks = [];
  const byId = new Map();
  for (let i = 1; i < parts.length; i++) {
    const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
    if (!m) continue;
    const block = { type: m[1], id: m[2], body: m[3], raw: `--- !u!${m[1]} &${m[2]}\n${m[3]}` };
    blocks.push(block);
    byId.set(block.id, block);
  }
  return { header, blocks, byId };
}

function serializeScene({ header, blocks }) {
  return header + blocks.map((b) => b.raw).join("");
}

function syncRaw(block) {
  block.raw = `--- !u!${block.type} &${block.id}\n${block.body}`;
}

function goName(byId, goId) {
  const b = byId.get(goId);
  if (!b || b.type !== "1") return null;
  return b.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}

function goComponents(byId, goId) {
  const b = byId.get(goId);
  if (!b || b.type !== "1") return [];
  return [...b.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((m) => m[1]);
}

function findGoByName(byId, name) {
  for (const b of byId.values()) {
    if (b.type !== "1") continue;
    if (b.body.match(new RegExp(`\\n  m_Name: ${name}\\s*\\n`))) return b.id;
  }
  return null;
}

function rtOfGo(byId, goId) {
  for (const cid of goComponents(byId, goId)) {
    if (byId.get(cid)?.type === "224") return cid;
  }
  return null;
}

function goOfComponent(byId, compId) {
  for (const [gid, g] of byId.entries()) {
    if (g.type !== "1") continue;
    if (g.body.includes(`component: {fileID: ${compId}}`)) return gid;
  }
  return null;
}

function getChildren(byId, rtId) {
  const body = byId.get(rtId)?.body ?? "";
  const m = body.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!m) return [];
  return [...m[1].matchAll(/fileID: (\d+)/g)].map((x) => x[1]);
}

function setChildren(block, childIds) {
  const lines = childIds.length ? childIds.map((id) => `  - {fileID: ${id}}`).join("\n") + "\n" : "";
  if (block.body.includes("\n  m_Children: []")) {
    block.body = block.body.replace(
      "\n  m_Children: []\n",
      childIds.length ? `\n  m_Children:\n${lines}` : "\n  m_Children: []\n",
    );
  } else {
    block.body = block.body.replace(
      /\n  m_Children:\n(?:  - \{fileID: \d+\}\n)*/g,
      childIds.length ? `\n  m_Children:\n${lines}` : "\n  m_Children: []\n",
    );
  }
  syncRaw(block);
}

function patchField(block, field, value) {
  const re = new RegExp(`\\n  ${field}: \\{fileID: [^}]+\\}`);
  if (block.body.match(re)) {
    block.body = block.body.replace(re, `\n  ${field}: {fileID: ${value}}`);
  } else {
    block.body = block.body.replace(
      /(\n  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus\.Hub\.CharacterBuild\.CharacterBuildStatRowView\n)/,
      `$1  ${field}: {fileID: ${value}}\n`,
    );
  }
  syncRaw(block);
}

function pushBlock(blocks, byId, block) {
  blocks.push(block);
  byId.set(block.id, block);
}

function vec(body, key) {
  const m = body.match(new RegExp(`${key}: \\{x: ([^,}]+), y: ([^,}]+)(?:, z: ([^}]+))?\\}`));
  if (!m) return { x: 0, y: 0, z: 0 };
  return { x: Number(m[1]), y: Number(m[2]), z: m[3] != null ? Number(m[3]) : 0 };
}

function boolActive(body) {
  return /m_IsActive: 1/.test(body);
}

function loadGuidMap() {
  const map = new Map();
  const walk = (dir) => {
    if (!fs.existsSync(dir)) return;
    for (const name of fs.readdirSync(dir)) {
      const full = path.join(dir, name);
      const st = fs.statSync(full);
      if (st.isDirectory()) walk(full);
      else if (name.endsWith(".meta")) {
        const txt = fs.readFileSync(full, "utf8");
        const guid = txt.match(/^guid: ([a-f0-9]+)/m)?.[1];
        if (guid) map.set(guid, full.replace(/\\/g, "/").replace(/\.meta$/, "").replace(`${ROOT}/`, ""));
      }
    }
  };
  walk(`${ROOT}/Assets/FracturedChorus/Art/UI`);
  return map;
}

function spritePathForGo(byId, goId, guidMap) {
  for (const cid of goComponents(byId, goId)) {
    const c = byId.get(cid);
    if (!c || c.type !== "114" || !c.body.includes(GUID.image)) continue;
    const guid = c.body.match(/m_Sprite: \{fileID: 21300000, guid: ([a-f0-9]+)/)?.[1];
    if (guid && guidMap.has(guid)) return guidMap.get(guid);
  }
  return "";
}

function exportSnapshot(scenePath, outPath, guidMap) {
  const scene = parseScene(fs.readFileSync(scenePath, "utf8"));
  const canvasGo = findGoByName(scene.byId, "BuildCanvas");
  if (!canvasGo) throw new Error(`BuildCanvas missing in ${scenePath}`);
  const canvasRt = rtOfGo(scene.byId, canvasGo);
  const childNames = getChildren(scene.byId, canvasRt).map((rt) => goName(scene.byId, goOfComponent(scene.byId, rt)));
  const nodes = [];

  const walk = (goId, pathStr, siblingIndex) => {
    const rtId = rtOfGo(scene.byId, goId);
    const rt = rtId ? scene.byId.get(rtId) : null;
    const go = scene.byId.get(goId);
    const entry = {
      path: pathStr,
      siblingIndex,
      activeSelf: boolActive(go.body),
      layoutKind: rt ? "RectTransform" : "Transform",
      localScale: rt ? vec(rt.body, "m_LocalScale") : { x: 1, y: 1, z: 1 },
    };
    if (rt) {
      entry.anchorMin = vec(rt.body, "m_AnchorMin");
      entry.anchorMax = vec(rt.body, "m_AnchorMax");
      entry.anchoredPosition = vec(rt.body, "m_AnchoredPosition");
      entry.sizeDelta = vec(rt.body, "m_SizeDelta");
      entry.pivot = vec(rt.body, "m_Pivot");
      entry.localPosition = vec(rt.body, "m_LocalPosition");
    }
    const sp = spritePathForGo(scene.byId, goId, guidMap);
    if (sp) entry.spritePath = sp;
    nodes.push(entry);
    if (!rt) return;
    const kids = getChildren(scene.byId, rtId);
    kids.forEach((childRt, i) => {
      const childGo = goOfComponent(scene.byId, childRt);
      if (!childGo) return;
      walk(childGo, `${pathStr}/${goName(scene.byId, childGo)}`, i);
    });
  };

  getChildren(scene.byId, canvasRt).forEach((rt, i) => {
    const go = goOfComponent(scene.byId, rt);
    if (!go) return;
    walk(go, `BuildCanvas/${goName(scene.byId, go)}`, i);
  });

  const snapshot = {
    scene: scenePath.replace(`${ROOT}/`, "").replace(/\\/g, "/"),
    savedAtUtc: new Date().toISOString(),
    note: "Reference backup only. Layout SoT is the scene file — editor menus must not re-apply these values.",
    buildCanvasChildren: childNames,
    nodes,
  };
  fs.writeFileSync(outPath, JSON.stringify(snapshot, null, 2) + "\n");
  return { nodes: nodes.length, children: childNames };
}

function maxId(scene) {
  let n = 0;
  for (const id of scene.byId.keys()) {
    const v = Number(id);
    if (!Number.isSafeInteger(v) || v >= 2_000_000_000) continue;
    n = Math.max(n, v);
  }
  return n;
}

function makeGo(name, compIds) {
  const goId = allocId();
  const comps = compIds.map((id) => `  - component: {fileID: ${id}}`).join("\n");
  const body = `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
${comps}
  m_Layer: 0
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
`;
  return { type: "1", id: String(goId), body, raw: `--- !u!1 &${goId}\n${body}` };
}

function makeRt(goId, fatherId, opts) {
  const id = allocId();
  const amin = opts.anchorMin;
  const amax = opts.anchorMax;
  const pos = opts.anchoredPosition ?? [0, 0];
  const size = opts.sizeDelta ?? [0, 0];
  const rotZ = opts.rotZ ?? 0;
  const children = opts.children ?? [];
  const childYaml = children.length
    ? "\n" + children.map((c) => `  - {fileID: ${c}}`).join("\n")
    : " []";
  const body = `RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: ${rotZ === 0 ? "0" : rotZ}, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:${childYaml}
  m_Father: {fileID: ${fatherId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: ${rotZ === 0 ? "0" : rotZ}}
  m_AnchorMin: {x: ${amin[0]}, y: ${amin[1]}}
  m_AnchorMax: {x: ${amax[0]}, y: ${amax[1]}}
  m_AnchoredPosition: {x: ${pos[0]}, y: ${pos[1]}}
  m_SizeDelta: {x: ${size[0]}, y: ${size[1]}}
  m_Pivot: {x: 0.5, y: 0.5}
`;
  return { type: "224", id: String(id), body, raw: `--- !u!224 &${id}\n${body}` };
}

function makeCr(goId) {
  const id = allocId();
  const body = `CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
`;
  return { type: "222", id: String(id), body, raw: `--- !u!222 &${id}\n${body}` };
}

function makeImage(goId, spriteGuid, raycast = 1) {
  const id = allocId();
  const body = `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID.image}, type: 3}
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
  m_Sprite: {fileID: 21300000, guid: ${spriteGuid}, type: 3}
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
  return { type: "114", id: String(id), body, raw: `--- !u!114 &${id}\n${body}` };
}

function makeButton(goId, imageId) {
  const id = allocId();
  const body = `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID.button}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
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
  m_TargetGraphic: {fileID: ${imageId}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
`;
  return { type: "114", id: String(id), body, raw: `--- !u!114 &${id}\n${body}` };
}

function makeText(goId) {
  const id = allocId();
  const body = `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID.text}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text
  m_Material: {fileID: 0}
  m_Color: {r: 0.227, g: 0.259, b: 0.4, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: ${GUID.font}, type: 3}
    m_FontSize: 14
    m_FontStyle: 1
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: 4
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: 0
`;
  return { type: "114", id: String(id), body, raw: `--- !u!114 &${id}\n${body}` };
}

function repairComponentOwners(scene) {
  for (const go of scene.blocks.filter((b) => b.type === "1")) {
    for (const cid of goComponents(scene.byId, go.id)) {
      const c = scene.byId.get(cid);
      if (!c) continue;
      c.body = c.body.replace(/m_GameObject: \{fileID: \d+\}/, `m_GameObject: {fileID: ${go.id}}`);
      syncRaw(c);
    }
  }
}

function rebuildRtChildren(scene) {
  const fatherToKids = new Map();
  for (const b of scene.blocks) {
    if (b.type !== "224") continue;
    const father = b.body.match(/m_Father: \{fileID: (\d+)\}/)?.[1];
    if (!father) continue;
    if (!fatherToKids.has(father)) fatherToKids.set(father, []);
    fatherToKids.get(father).push(b.id);
  }
  for (const [father, kids] of fatherToKids) {
    const rt = scene.byId.get(father);
    if (!rt || rt.type !== "224") continue;
    const existing = getChildren(scene.byId, father);
    const merged = [];
    for (const id of existing) {
      if (kids.includes(id) && !merged.includes(id)) merged.push(id);
    }
    for (const id of kids) {
      if (!merged.includes(id)) merged.push(id);
    }
    setChildren(rt, merged);
  }
}

function findChildGoByName(byId, parentRt, name) {
  for (const rt of getChildren(byId, parentRt)) {
    const go = goOfComponent(byId, rt);
    if (goName(byId, go) === name) return go;
  }
  return null;
}

function createIconButton(scene, name, fatherRt, sprite, amin, amax) {
  const { blocks, byId } = scene;
  const goId = allocId();
  const rt = makeRt(goId, fatherRt, { anchorMin: amin, anchorMax: amax });
  const cr = makeCr(goId);
  const img = makeImage(goId, sprite, 1);
  const btn = makeButton(goId, img.id);
  const go = makeGo(name, [rt.id, cr.id, img.id, btn.id]);
  go.id = String(goId);
  go.raw = `--- !u!1 &${goId}\n${go.body}`;
  for (const b of [go, rt, cr, img, btn]) pushBlock(blocks, byId, b);
  return { go, rt, btn };
}

function createAllocControls(scene, rowGo, rowRt) {
  const { blocks, byId } = scene;
  const allocGoId = allocId();
  const allocRt = makeRt(allocGoId, rowRt, { anchorMin: [0.82, 0.06], anchorMax: [0.99, 0.94] });
  const allocGo = makeGo("AllocControls", [allocRt.id]);
  allocGo.id = String(allocGoId);
  allocGo.raw = `--- !u!1 &${allocGoId}\n${allocGo.body}`;
  pushBlock(blocks, byId, allocGo);
  pushBlock(blocks, byId, allocRt);

  const minus = createIconButton(scene, "MinusBtn", allocRt.id, GUID.minus, [0, 0.08], [0.3, 0.92]);
  const spentGoId = allocId();
  const spentRt = makeRt(spentGoId, allocRt.id, { anchorMin: [0.32, 0.08], anchorMax: [0.68, 0.92] });
  const spentCr = makeCr(spentGoId);
  const spentTxt = makeText(spentGoId);
  const spentGo = makeGo("SpentLabel", [spentRt.id, spentCr.id, spentTxt.id]);
  spentGo.id = String(spentGoId);
  spentGo.raw = `--- !u!1 &${spentGoId}\n${spentGo.body}`;
  for (const b of [spentGo, spentRt, spentCr, spentTxt]) pushBlock(blocks, byId, b);
  const plus = createIconButton(scene, "PlusBtn", allocRt.id, GUID.plus, [0.7, 0.08], [1, 0.92]);

  setChildren(allocRt, [minus.rt.id, spentRt.id, plus.rt.id]);
  const rowKids = getChildren(byId, rowRt);
  if (!rowKids.includes(allocRt.id)) rowKids.push(allocRt.id);
  setChildren(byId.get(rowRt), rowKids);

  for (const cid of goComponents(byId, rowGo)) {
    const c = byId.get(cid);
    if (!c?.body.includes(GUID.statRow)) continue;
    patchField(c, "minusButton", minus.btn.id);
    patchField(c, "spentLabel", spentTxt.id);
    patchField(c, "plusButton", plus.btn.id);
    patchField(c, "allocControlsRoot", allocGo.id);
  }
}

function ensureAlloc(scene) {
  repairComponentOwners(scene);
  rebuildRtChildren(scene);
  let created = 0;
  for (const rowName of STAT_ROWS) {
    const rowGo = findGoByName(scene.byId, rowName);
    if (!rowGo) continue;
    const rowRt = rtOfGo(scene.byId, rowGo);
    let allocGo = findChildGoByName(scene.byId, rowRt, "AllocControls");
    if (!allocGo) {
      createAllocControls(scene, rowGo, rowRt);
      created++;
      continue;
    }
    const allocRt = rtOfGo(scene.byId, allocGo);
    scene.byId.get(allocGo).body = scene.byId.get(allocGo).body.replace(/m_IsActive: 0/, "m_IsActive: 1");
    syncRaw(scene.byId.get(allocGo));

    const kids = getChildren(scene.byId, allocRt);
    const names = kids.map((rt) => goName(scene.byId, goOfComponent(scene.byId, rt)));
    if (!names.includes("PlusBtn") || !names.includes("MinusBtn")) {
      const removeIds = new Set([allocGo, allocRt, ...goComponents(scene.byId, allocGo), ...kids]);
      for (const rt of kids) {
        const go = goOfComponent(scene.byId, rt);
        if (go) {
          removeIds.add(go);
          for (const cid of goComponents(scene.byId, go)) removeIds.add(cid);
        }
      }
      scene.blocks = scene.blocks.filter((b) => !removeIds.has(b.id));
      for (const id of removeIds) scene.byId.delete(id);
      const rowKids = getChildren(scene.byId, rowRt).filter((id) => id !== allocRt);
      setChildren(scene.byId.get(rowRt), rowKids);
      createAllocControls(scene, rowGo, rowRt);
      created++;
    }
  }
  rebuildRtChildren(scene);
  return created;
}

function seedCrystals(scene) {
  const fieldGo = findGoByName(scene.byId, "CrystalField");
  if (!fieldGo) return 0;
  const field = scene.byId.get(fieldGo);
  field.body = field.body.replace(/m_IsActive: 0/, "m_IsActive: 1");
  syncRaw(field);
  const fieldRt = rtOfGo(scene.byId, fieldGo);
  const existing = getChildren(scene.byId, fieldRt);
  if (existing.length > 0) return existing.length;

  const childRts = [];
  CRYSTAL_SEEDS.forEach((seed, i) => {
    const goId = allocId();
    const zSin = Math.sin((seed.z * Math.PI) / 180);
    const zCos = Math.cos((seed.z * Math.PI) / 180);
    const rt = makeRt(goId, fieldRt, {
      anchorMin: [0.5, 0.5],
      anchorMax: [0.5, 0.5],
      anchoredPosition: [seed.x, seed.y],
      sizeDelta: [seed.s, seed.s],
    });
    rt.body = rt.body
      .replace(
        /m_LocalRotation: \{x: 0, y: 0, z: 0, w: 1\}/,
        `m_LocalRotation: {x: 0, y: 0, z: ${zSin.toFixed(6)}, w: ${zCos.toFixed(6)}}`,
      )
      .replace(/m_LocalEulerAnglesHint: \{x: 0, y: 0, z: 0\}/, `m_LocalEulerAnglesHint: {x: 0, y: 0, z: ${seed.z}}`);
    syncRaw(rt);
    const cr = makeCr(goId);
    const img = makeImage(goId, CRYSTAL_GUIDS[i % CRYSTAL_GUIDS.length], 0);
    img.body = img.body.replace(/m_Color: \{r: 1, g: 1, b: 1, a: 1\}/, "m_Color: {r: 1, g: 1, b: 1, a: 0.72}");
    syncRaw(img);
    const go = makeGo(`Crystal_${i}`, [rt.id, cr.id, img.id]);
    go.id = String(goId);
    go.raw = `--- !u!1 &${goId}\n${go.body}`;
    for (const b of [go, rt, cr, img]) pushBlock(scene.blocks, scene.byId, b);
    childRts.push(rt.id);
  });
  setChildren(scene.byId.get(fieldRt), childRts);
  return childRts.length;
}

function patchScene(scenePath, idBase) {
  const scene = parseScene(fs.readFileSync(scenePath, "utf8"));
  nextId = Math.max(idBase, maxId(scene) + 1);
  const createdAlloc = ensureAlloc(scene);
  const crystals = seedCrystals(scene);
  fs.writeFileSync(scenePath, serializeScene(scene));
  return { createdAlloc, crystals, nextId };
}

const guidMap = loadGuidMap();
const snapSandbox = exportSnapshot(SANDBOX, SNAPSHOT_SANDBOX, guidMap);
const snapProd = exportSnapshot(PRODUCTION, SNAPSHOT_PROD, guidMap);
console.log("snapshot sandbox", snapSandbox.nodes, "children", snapSandbox.children.join(","));
console.log("snapshot production", snapProd.nodes, "children", snapProd.children.join(","));

const sandbox = patchScene(SANDBOX, 994010000);
const prod = patchScene(PRODUCTION, 2100100000);
console.log("patched sandbox", sandbox);
console.log("patched production", prod);
