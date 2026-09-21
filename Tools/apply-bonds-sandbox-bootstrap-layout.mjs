import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");
const LAYOUT = path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_bootstrap_layout.json");
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
    if (!component) continue;
    if (component.type === "224") return { id: componentId, kind: "RectTransform" };
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
  return Number.isInteger(value) ? `${value}` : `${value}`;
}

function patchRectBody(body, layout) {
  let next = body;
  next = next.replace(
    /m_AnchorMin: \{x: [^}]+\}/,
    `m_AnchorMin: {x: ${fmt(layout.anchorMin.x)}, y: ${fmt(layout.anchorMin.y)}}`,
  );
  next = next.replace(
    /m_AnchorMax: \{x: [^}]+\}/,
    `m_AnchorMax: {x: ${fmt(layout.anchorMax.x)}, y: ${fmt(layout.anchorMax.y)}}`,
  );
  next = next.replace(
    /m_AnchoredPosition: \{x: [^}]+\}/,
    `m_AnchoredPosition: {x: ${fmt(layout.anchoredPosition.x)}, y: ${fmt(layout.anchoredPosition.y)}}`,
  );
  next = next.replace(
    /m_SizeDelta: \{x: [^}]+\}/,
    `m_SizeDelta: {x: ${fmt(layout.sizeDelta.x)}, y: ${fmt(layout.sizeDelta.y)}}`,
  );
  next = next.replace(
    /m_Pivot: \{x: [^}]+\}/,
    `m_Pivot: {x: ${fmt(layout.pivot.x)}, y: ${fmt(layout.pivot.y)}}`,
  );
  return next;
}

function fixGlassFillCenter(byId) {
  const hollowFrameGuids = [
    "5cc6c195c8ed412d90ce5721e2b13cb2",
    "313e540b200d468bb1c08ae54fe24f7a",
  ];
  let fixed = 0;
  for (const block of byId.values()) {
    if (block.type !== "114" || !block.body.includes("UnityEngine.UI::UnityEngine.UI.Image")) {
      continue;
    }
    if (!hollowFrameGuids.some((guid) => block.body.includes(`guid: ${guid}`))) {
      continue;
    }
    if (block.body.includes("m_FillCenter: 0")) {
      continue;
    }
    block.body = block.body.replace(/m_FillCenter: 1/, "m_FillCenter: 0");
    fixed += 1;
  }
  return fixed;
}

function disableMockGuide(byId) {
  for (const block of byId.values()) {
    if (block.type !== "1" || !block.body.includes("\n  m_Name: MockGuide\n")) {
      continue;
    }
    block.body = block.body.replace(/m_IsActive: 1/, "m_IsActive: 0");
    return true;
  }
  return false;
}

function serializeScene(header, order, byId) {
  const blocks = order.map((id) => {
    const block = byId.get(id);
    return `--- !u!${block.type} &${block.id}\n${block.body}`;
  });
  return `${header}${blocks.join("")}`;
}

const layoutDoc = JSON.parse(fs.readFileSync(LAYOUT, "utf8"));
const sceneText = fs.readFileSync(SCENE, "utf8");
const { byId, order, header } = parseScene(sceneText);
const rootGoId = findGoByName(byId, ROOT_NAME);
if (!rootGoId) {
  throw new Error(`${ROOT_NAME} missing`);
}

const pathMap = new Map();
buildPathMap(byId, rootGoId, ROOT_NAME, pathMap);

const applied = [];
const missing = [];
for (const node of layoutDoc.nodes) {
  const goId = pathMap.get(node.path);
  if (!goId) {
    missing.push(node.path);
    continue;
  }

  const transform = xfOfGo(byId, goId);
  if (!transform) {
    missing.push(`${node.path} (no RectTransform)`);
    continue;
  }

  const block = byId.get(transform.id);
  block.body = patchRectBody(block.body, node);
  applied.push(node.path);
}

const glassFixed = fixGlassFillCenter(byId);
const mockGuideOff = disableMockGuide(byId);

fs.writeFileSync(SCENE, serializeScene(header, order, byId));

console.log(
  JSON.stringify(
    {
      applied: applied.length,
      glassFillCenterFixed: glassFixed,
      mockGuideOff,
      missing,
      sample: applied.slice(0, 5),
    },
    null,
    2,
  ),
);
