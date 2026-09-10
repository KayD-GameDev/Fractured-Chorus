import fs from "fs";

const scenePath =
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuildLayoutSandbox.unity";
const outPath =
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/MockKit/sandbox_layout_snapshot.json";

const text = fs.readFileSync(scenePath, "utf8");
const blocks = text.split(/^--- !u!/m).slice(1);

const byId = new Map();
for (const block of blocks) {
  const head = block.match(/^(\d+) &(\d+)/);
  if (!head) continue;
  byId.set(head[2], { type: head[1], body: block, id: head[2] });
}

function gameObjectName(id) {
  const go = byId.get(id);
  if (!go || go.type !== "1") return null;
  const m = go.body.match(/\n  m_Name: (.+)/);
  return m ? m[1] : null;
}

function findComponentGameObject(componentId) {
  for (const [goId, go] of byId.entries()) {
    if (go.type !== "1") continue;
    if (go.body.includes(`component: {fileID: ${componentId}}`)) return goId;
  }
  return null;
}

function findGameObjectByName(name) {
  for (const [id, block] of byId.entries()) {
    if (block.type !== "1") continue;
    if (block.body.match(new RegExp(`\\n  m_Name: ${name}\\s*\\n`))) return id;
  }
  return null;
}

function gameObjectComponents(goId) {
  const go = byId.get(goId);
  if (!go || go.type !== "1") return [];
  const ids = [];
  for (const m of go.body.matchAll(/component: \{fileID: (\d+)\}/g)) ids.push(m[1]);
  return ids;
}

function parseLayout(id) {
  const b = byId.get(id)?.body;
  if (!b) return null;
  const pick = (key) => {
    const m = b.match(new RegExp(`\\n  ${key}: \\{x: ([^,]+), y: ([^}]+)\\}`));
    return m ? [Number(m[1]), Number(m[2])] : null;
  };
  const pick3 = (key) => {
    const m = b.match(new RegExp(`\\n  ${key}: \\{x: ([^,]+), y: ([^,]+), z: ([^}]+)\\}`));
    return m ? [Number(m[1]), Number(m[2]), Number(m[3])] : null;
  };
  const children = [];
  const childBlock = b.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (childBlock) {
    for (const m of childBlock[1].matchAll(/fileID: (\d+)/g)) children.push(m[1]);
  }
  return {
    anchorMin: pick("m_AnchorMin"),
    anchorMax: pick("m_AnchorMax"),
    anchoredPosition: pick("m_AnchoredPosition"),
    sizeDelta: pick("m_SizeDelta"),
    pivot: pick("m_Pivot"),
    localScale: pick3("m_LocalScale"),
    localPosition: pick3("m_LocalPosition"),
    children,
  };
}

function* walkAssets(dir) {
  for (const ent of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = `${dir}/${ent.name}`;
    if (ent.isDirectory()) yield* walkAssets(p);
    else yield p;
  }
}

const guidToPath = new Map();
for (const file of walkAssets("D:/Fractured-Chorus1/Assets")) {
  if (!file.endsWith(".meta")) continue;
  const meta = fs.readFileSync(file, "utf8");
  const guid = meta.match(/^guid: ([a-f0-9]+)/m)?.[1];
  if (guid) guidToPath.set(guid, file.slice(0, -5).replace(/\\/g, "/").replace("D:/Fractured-Chorus1/", ""));
}

function spritePathForGameObject(goId) {
  for (const cid of gameObjectComponents(goId)) {
    const b = byId.get(cid)?.body;
    if (!b) continue;
    const guid = b.match(/\n  m_Sprite: \{fileID: \d+, guid: ([a-f0-9]+), type: 3\}/)?.[1];
    if (guid && guidToPath.has(guid)) return guidToPath.get(guid);
  }
  return null;
}

const buildCanvasGoId = findGameObjectByName("BuildCanvas");
let buildCanvasRtId = null;
if (buildCanvasGoId) {
  for (const cid of gameObjectComponents(buildCanvasGoId)) {
    const t = byId.get(cid)?.type;
    if (t === "224" || t === "4") {
      buildCanvasRtId = cid;
      break;
    }
  }
}

if (!buildCanvasRtId) {
  console.error("BuildCanvas RectTransform not found");
  process.exit(1);
}

const nodes = [];
const buildCanvasChildren = [];

function walkNode(componentId, path, siblingIndex) {
  const block = byId.get(componentId);
  if (!block) return;
  const goId = findComponentGameObject(componentId);
  const name = gameObjectName(goId) ?? "?";
  const fullPath = path ? `${path}/${name}` : name;
  const goBlock = byId.get(goId)?.body ?? "";
  const active = !goBlock.includes("m_IsActive: 0");
  const layout = parseLayout(componentId);

  const entry = {
    path: fullPath,
    siblingIndex,
    activeSelf: active,
    layoutKind: block.type === "224" ? "RectTransform" : block.type === "4" ? "Transform" : block.type,
  };

  if (layout?.anchorMin) {
    entry.anchorMin = { x: layout.anchorMin[0], y: layout.anchorMin[1] };
    entry.anchorMax = { x: layout.anchorMax[0], y: layout.anchorMax[1] };
    entry.anchoredPosition = { x: layout.anchoredPosition[0], y: layout.anchoredPosition[1] };
    entry.sizeDelta = { x: layout.sizeDelta[0], y: layout.sizeDelta[1] };
    entry.pivot = { x: layout.pivot[0], y: layout.pivot[1] };
  }
  if (layout?.localPosition) {
    entry.localPosition = {
      x: layout.localPosition[0],
      y: layout.localPosition[1],
      z: layout.localPosition[2],
    };
  }
  if (layout?.localScale) {
    entry.localScale = {
      x: layout.localScale[0],
      y: layout.localScale[1],
      z: layout.localScale[2],
    };
  }

  const spritePath = goId ? spritePathForGameObject(goId) : null;
  if (spritePath) entry.spritePath = spritePath;

  nodes.push(entry);

  if (!layout?.children) return;
  layout.children.forEach((childId, i) => walkNode(childId, fullPath, i));
}

const canvasLayout = parseLayout(buildCanvasRtId);
for (const [i, childId] of canvasLayout.children.entries()) {
  const goId = findComponentGameObject(childId);
  buildCanvasChildren.push(gameObjectName(goId) ?? childId);
  walkNode(childId, "BuildCanvas", i);
}

const snapshot = {
  scene: "Assets/FracturedChorus/Scenes/CharacterBuildLayoutSandbox.unity",
  savedAtUtc: new Date().toISOString(),
  note:
    "Reference backup only. Layout SoT is CharacterBuildLayoutSandbox.unity — editor menus must not re-apply these values.",
  buildCanvasChildren,
  nodes,
};

fs.writeFileSync(outPath, JSON.stringify(snapshot, null, 2));
console.log("Wrote", outPath, "nodes:", nodes.length);
