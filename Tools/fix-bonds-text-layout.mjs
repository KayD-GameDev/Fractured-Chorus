import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");

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

function patchTextBody(body, { fontSize, color, horizontalOverflow, verticalOverflow, fontRef }) {
  let next = body;
  if (fontSize != null) {
    next = next.replace(/m_FontSize: \d+/, `m_FontSize: ${fontSize}`);
  }
  if (fontRef != null) {
    next = next.replace(/m_Font: \{fileID: [^}]+\}/, `m_Font: ${fontRef}`);
  }
  if (color != null) {
    next = next.replace(
      /m_Color: \{r: [^}]+\}/,
      `m_Color: {r: ${color.r}, g: ${color.g}, b: ${color.b}, a: ${color.a}}`,
    );
  }
  if (horizontalOverflow != null) {
    next = next.replace(/m_HorizontalOverflow: \d+/, `m_HorizontalOverflow: ${horizontalOverflow}`);
  }
  if (verticalOverflow != null) {
    next = next.replace(/m_VerticalOverflow: \d+/, `m_VerticalOverflow: ${verticalOverflow}`);
  }
  return next;
}

function textComponentOfGo(byId, goId) {
  for (const componentId of goComponents(byId, goId)) {
    const component = byId.get(componentId);
    if (component?.type === "114" && component.body.includes("UnityEngine.UI::UnityEngine.UI.Text")) {
      return componentId;
    }
  }
  return null;
}

function applyText(byId, pathMap, nodePath, spec) {
  const goId = pathMap.get(nodePath);
  if (!goId) return false;
  const textId = textComponentOfGo(byId, goId);
  if (!textId) return false;
  byId.get(textId).body = patchTextBody(byId.get(textId).body, spec);
  return true;
}

const BODY_FONT = `{fileID: 12800000, guid: 787fda44816c480a9cc3cadfdc29ce24, type: 3}`;
const OVERFLOW = { horizontalOverflow: 1, verticalOverflow: 1 };

const statNodeNames = ["Node_Resonance", "Node_Cadence", "Node_Pulse", "Node_Harmony", "Node_Rhythm"];
const statTextTargets = [
  { suffix: "Name", spec: { fontSize: 14, ...OVERFLOW } },
  { suffix: "Rank", spec: { fontSize: 13, fontRef: BODY_FONT, ...OVERFLOW } },
  { suffix: "Flavor", spec: { fontSize: 11, ...OVERFLOW } },
];

const { byId, order, header } = parseScene(fs.readFileSync(SCENE, "utf8"));
const rootGoId = findGoByName(byId, "BondsCanvas");
if (!rootGoId) throw new Error("BondsCanvas missing");

const pathMap = new Map();
buildPathMap(byId, rootGoId, "BondsCanvas", pathMap);

let patched = 0;
for (const nodeName of statNodeNames) {
  for (const target of statTextTargets) {
    if (applyText(byId, pathMap, `BondsCanvas/CenterStats/${nodeName}/${target.suffix}`, target.spec)) {
      patched += 1;
    }
  }
}

for (let i = 1; i <= 5; i++) {
  const base = `BondsCanvas/LinkEpisodes/Row_${String(i).padStart(2, "0")}`;
  for (const child of ["Index", "Label"]) {
    if (applyText(byId, pathMap, `${base}/${child}`, { fontRef: BODY_FONT, ...OVERFLOW })) {
      patched += 1;
    }
  }
}

if (applyText(byId, pathMap, "BondsCanvas/LinkEpisodes/Title", { fontSize: 24, ...OVERFLOW })) {
  patched += 1;
}
if (applyText(byId, pathMap, "BondsCanvas/LinkEpisodes/Hint", OVERFLOW)) {
  patched += 1;
}

const emitOrder = [
  ...order.filter((id) => byId.get(id).type !== "1660057539"),
  ...order.filter((id) => byId.get(id).type === "1660057539"),
];
const nextScene = `${header}${emitOrder
  .map((id) => {
    const block = byId.get(id);
    return `--- !u!${block.type} &${block.id}\n${block.body}`;
  })
  .join("")}`;
const tmpScene = `${SCENE}.tmp`;
fs.writeFileSync(tmpScene, nextScene);
fs.copyFileSync(tmpScene, SCENE);
fs.unlinkSync(tmpScene);

console.log(
  JSON.stringify(
    {
      scene: SCENE.replace(`${ROOT}/`, ""),
      mode: "text-only",
      patched,
      note: "RectTransform untouched. Save layout with save-bonds-sandbox-layout-snapshot.mjs",
    },
    null,
    2,
  ),
);
