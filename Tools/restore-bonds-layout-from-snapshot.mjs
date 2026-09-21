import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");
const SNAPSHOT = path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_layout_snapshot.json");
const ROOT_NAME = "BondsCanvas";

function parseScene(text) {
  const parts = text.split(/^--- !u!/m);
  const byId = new Map();
  const order = [];
  for (let i = 1; i < parts.length; i++) {
    const match = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
    if (!match) continue;
    byId.set(match[2], { type: match[1], id: match[2], body: match[3] });
    order.push(match[2]);
  }
  return { byId, order, header: parts[0] };
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
  for (const componentId of goComponents(byId, goId)) {
    const component = byId.get(componentId);
    if (component?.type === "224") return { id: componentId };
  }
  return null;
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

function buildPathMap(byId, goId, pathStr, map) {
  map.set(pathStr, goId);
  const transform = xfOfGo(byId, goId);
  if (!transform) return;
  for (const childTransformId of getChildren(byId, transform.id)) {
    const childGoId = goOfComponent(byId, childTransformId);
    if (!childGoId) continue;
    const childName = goName(byId, childGoId);
    if (!childName) continue;
    buildPathMap(byId, childGoId, `${pathStr}/${childName}`, map);
  }
}

function fmt(value) {
  return `${value}`;
}

function patchRectBody(body, node) {
  let next = body;
  next = next.replace(/m_AnchorMin: \{x: [^}]+\}/, `m_AnchorMin: {x: ${fmt(node.anchorMin.x)}, y: ${fmt(node.anchorMin.y)}}`);
  next = next.replace(/m_AnchorMax: \{x: [^}]+\}/, `m_AnchorMax: {x: ${fmt(node.anchorMax.x)}, y: ${fmt(node.anchorMax.y)}}`);
  next = next.replace(/m_AnchoredPosition: \{x: [^}]+\}/, `m_AnchoredPosition: {x: ${fmt(node.anchoredPosition.x)}, y: ${fmt(node.anchoredPosition.y)}}`);
  next = next.replace(/m_SizeDelta: \{x: [^}]+\}/, `m_SizeDelta: {x: ${fmt(node.sizeDelta.x)}, y: ${fmt(node.sizeDelta.y)}}`);
  next = next.replace(/m_Pivot: \{x: [^}]+\}/, `m_Pivot: {x: ${fmt(node.pivot.x)}, y: ${fmt(node.pivot.y)}}`);
  if (node.localRotation) {
    next = next.replace(
      /m_LocalRotation: \{x: [^}]+\}/,
      `m_LocalRotation: {x: ${fmt(node.localRotation.x)}, y: ${fmt(node.localRotation.y)}, z: ${fmt(node.localRotation.z)}, w: ${fmt(node.localRotation.w)}}`,
    );
  }
  if (node.localEulerAnglesHint) {
    next = next.replace(
      /m_LocalEulerAnglesHint: \{x: [^}]+\}/,
      `m_LocalEulerAnglesHint: {x: ${fmt(node.localEulerAnglesHint.x)}, y: ${fmt(node.localEulerAnglesHint.y)}, z: ${fmt(node.localEulerAnglesHint.z)}}`,
    );
  }
  return next;
}

const snapshot = JSON.parse(fs.readFileSync(SNAPSHOT, "utf8"));
const { byId, order, header } = parseScene(fs.readFileSync(SCENE, "utf8"));
const rootGoId = findGoByName(byId, ROOT_NAME);
if (!rootGoId) throw new Error("BondsCanvas missing");

const pathMap = new Map();
buildPathMap(byId, rootGoId, ROOT_NAME, pathMap);

const applied = [];
const missing = [];
for (const node of snapshot.nodes) {
  if (node.layoutKind !== "RectTransform") continue;
  const goId = pathMap.get(node.path);
  if (!goId) {
    missing.push(node.path);
    continue;
  }
  const transform = xfOfGo(byId, goId);
  if (!transform) {
    missing.push(`${node.path} (no Rect)`);
    continue;
  }
  byId.get(transform.id).body = patchRectBody(byId.get(transform.id).body, node);
  applied.push(node.path);
}

const emitOrder = [
  ...order.filter((id) => byId.get(id).type !== "1660057539"),
  ...order.filter((id) => byId.get(id).type === "1660057539"),
];
const nextScene = `${header}${emitOrder.map((id) => {
  const block = byId.get(id);
  return `--- !u!${block.type} &${block.id}\n${block.body}`;
}).join("")}`;
const tmpScene = `${SCENE}.tmp`;
fs.writeFileSync(tmpScene, nextScene);
try {
  fs.copyFileSync(tmpScene, SCENE);
  fs.unlinkSync(tmpScene);
} catch (error) {
  console.error("SCENE_WRITE_FAILED", error.code, tmpScene);
  throw error;
}

console.log(
  JSON.stringify(
    {
      savedAtUtc: snapshot.savedAtUtc,
      applied: applied.length,
      missing,
    },
    null,
    2,
  ),
);
