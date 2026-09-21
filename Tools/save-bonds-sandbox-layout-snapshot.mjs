import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = `${ROOT}/Assets/FracturedChorus/Scenes/Bonds.unity`;
const OUT = `${ROOT}/Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_layout_snapshot.json`;
const BOOTSTRAP = `${ROOT}/Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_bootstrap_layout.json`;
const ROOT_NAME = "BondsCanvas";

function parseScene(text) {
  const parts = text.split(/^--- !u!/m);
  const byId = new Map();
  for (let i = 1; i < parts.length; i++) {
    const match = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
    if (!match) continue;
    byId.set(match[2], { type: match[1], id: match[2], body: match[3] });
  }
  return byId;
}

function goName(byId, goId) {
  const block = byId.get(goId);
  if (!block || block.type !== "1") return null;
  return block.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}

function goComponents(byId, goId) {
  const block = byId.get(goId);
  if (!block || block.type !== "1") return [];
  return [...block.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((match) => match[1]);
}

function findGoByName(byId, name) {
  for (const block of byId.values()) {
    if (block.type === "1" && block.body.match(new RegExp(`\\n  m_Name: ${name}\\s*\\n`))) {
      return block.id;
    }
  }
  return null;
}

function xfOfGo(byId, goId) {
  let transformId = null;
  for (const componentId of goComponents(byId, goId)) {
    const component = byId.get(componentId);
    if (!component) continue;
    if (component.type === "224") return { id: componentId, kind: "RectTransform" };
    if (component.type === "4") transformId = componentId;
  }
  return transformId ? { id: transformId, kind: "Transform" } : null;
}

function goOfComponent(byId, componentId) {
  for (const [goId, block] of byId.entries()) {
    if (block.type === "1" && block.body.includes(`component: {fileID: ${componentId}}`)) {
      return goId;
    }
  }
  return null;
}

function getChildren(byId, transformId) {
  const body = byId.get(transformId)?.body ?? "";
  const match = body.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!match) return [];
  return [...match[1].matchAll(/fileID: (\d+)/g)].map((entry) => entry[1]);
}

function vec(body, key) {
  const match = body.match(
    new RegExp(`${key}: \\{x: ([^,}]+), y: ([^,}]+)(?:, z: ([^,}]+))?(?:, w: ([^}]+))?\\}`),
  );
  if (!match) return { x: 0, y: 0, z: 0 };
  const value = { x: Number(match[1]), y: Number(match[2]), z: match[3] != null ? Number(match[3]) : 0 };
  if (match[4] != null) value.w = Number(match[4]);
  return value;
}

function loadGuidMap() {
  const map = new Map();
  const walk = (dir) => {
    if (!fs.existsSync(dir)) return;
    for (const name of fs.readdirSync(dir)) {
      const full = path.join(dir, name);
      if (fs.statSync(full).isDirectory()) {
        walk(full);
        continue;
      }

      if (!name.endsWith(".meta")) continue;
      const guid = fs.readFileSync(full, "utf8").match(/^guid: ([a-f0-9]+)/m)?.[1];
      if (!guid) continue;
      map.set(guid, full.replace(/\\/g, "/").replace(/\.meta$/, "").replace(`${ROOT}/`, ""));
    }
  };

  walk(`${ROOT}/Assets/FracturedChorus/Art/UI`);
  walk(`${ROOT}/Assets/FracturedChorus/Resources/UI`);
  walk(`${ROOT}/Assets/FracturedChorus/Art/Characters`);
  return map;
}

function spritePathForGo(byId, goId, guidMap) {
  for (const componentId of goComponents(byId, goId)) {
    const component = byId.get(componentId);
    if (!component || component.type !== "114" || !component.body.includes("UnityEngine.UI::UnityEngine.UI.Image")) {
      continue;
    }

    const guid = component.body.match(/m_Sprite: \{fileID: [^,]+, guid: ([a-f0-9]+)/)?.[1];
    if (guid && guidMap.has(guid)) return guidMap.get(guid);
  }
  return "";
}

function walkNode(byId, guidMap, goId, pathStr, siblingIndex, nodes) {
  const transform = xfOfGo(byId, goId);
  const transformBlock = transform ? byId.get(transform.id) : null;
  const goBlock = byId.get(goId);
  const entry = {
    path: pathStr,
    goFileId: goId,
    siblingIndex,
    activeSelf: /m_IsActive: 1/.test(goBlock.body),
    layoutKind: transform?.kind ?? "None",
  };

  if (transformBlock) {
    entry.localScale = vec(transformBlock.body, "m_LocalScale");
    entry.localPosition = vec(transformBlock.body, "m_LocalPosition");
    entry.localRotation = vec(transformBlock.body, "m_LocalRotation");
    entry.localEulerAnglesHint = vec(transformBlock.body, "m_LocalEulerAnglesHint");
    if (transform.kind === "RectTransform") {
      entry.anchorMin = vec(transformBlock.body, "m_AnchorMin");
      entry.anchorMax = vec(transformBlock.body, "m_AnchorMax");
      entry.anchoredPosition = vec(transformBlock.body, "m_AnchoredPosition");
      entry.sizeDelta = vec(transformBlock.body, "m_SizeDelta");
      entry.pivot = vec(transformBlock.body, "m_Pivot");
    }
  }

  const spritePath = spritePathForGo(byId, goId, guidMap);
  if (spritePath) {
    entry.spritePath = spritePath;
  }

  nodes.push(entry);
  if (!transform) return;

  getChildren(byId, transform.id).forEach((childTransformId, index) => {
    const childGoId = goOfComponent(byId, childTransformId);
    if (!childGoId) return;
    walkNode(byId, guidMap, childGoId, `${pathStr}/${goName(byId, childGoId)}`, index, nodes);
  });
}

function toBootstrapNode(node) {
  if (node.layoutKind !== "RectTransform") return null;
  return {
    path: node.path,
    anchorMin: node.anchorMin,
    anchorMax: node.anchorMax,
    anchoredPosition: node.anchoredPosition,
    sizeDelta: node.sizeDelta,
    pivot: node.pivot,
  };
}

const byId = parseScene(fs.readFileSync(SCENE, "utf8"));
const guidMap = loadGuidMap();
const rootGoId = findGoByName(byId, ROOT_NAME);
if (!rootGoId) {
  throw new Error(`${ROOT_NAME} missing`);
}

const rootTransform = xfOfGo(byId, rootGoId);
const rootChildren = rootTransform
  ? getChildren(byId, rootTransform.id).map((childTransformId) => goName(byId, goOfComponent(byId, childTransformId)))
  : [];
const nodes = [];
walkNode(byId, guidMap, rootGoId, ROOT_NAME, 0, nodes);

const savedAtUtc = new Date().toISOString();
const snapshotDoc = {
  scene: "Assets/FracturedChorus/Scenes/Bonds.unity",
  savedAtUtc,
  note: "Reference backup only. Layout SoT is Bonds.unity - do not re-apply these values from code.",
  rootChildren,
  nodes,
};

fs.mkdirSync(path.dirname(OUT), { recursive: true });
fs.writeFileSync(OUT, JSON.stringify(snapshotDoc, null, 2) + "\n");

const bootstrapNodes = nodes.map(toBootstrapNode).filter(Boolean);
fs.writeFileSync(
  BOOTSTRAP,
  JSON.stringify(
    {
      scene: snapshotDoc.scene,
      note: "Synced from bonds_sandbox_layout_snapshot.json. Layout SoT is the .unity scene after manual align.",
      savedAtUtc,
      nodes: bootstrapNodes,
    },
    null,
    2,
  ) + "\n",
);

console.log(
  JSON.stringify(
    {
      nodes: nodes.length,
      bootstrapNodes: bootstrapNodes.length,
      rootChildren,
      sample: nodes.find((node) => node.path === "BondsCanvas/CenterStats"),
      out: OUT.replace(`${ROOT}/`, ""),
      bootstrap: BOOTSTRAP.replace(`${ROOT}/`, ""),
    },
    null,
    2,
  ),
);
