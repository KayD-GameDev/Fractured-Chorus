import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const BONDS = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");
const FLOWER = path.join(ROOT, "Assets/FracturedChorus/Scenes/FlowerShopWork.unity");
const CORNER_GO = "931000019";
const CORNER_XF = "931000020";
const HUB_COMPONENT = "931000043";
const FLOWER_CANVAS_XF = "57730263";
const VN_RUNTIME = "1838241919";
const ID_BASE = 1991000000;

function parseScene(text) {
  const parts = text.split(/^--- !u!/m);
  const byId = new Map();
  const order = [];
  for (let i = 1; i < parts.length; i++) {
    const match = parts[i].match(/^(\d+) &(\d+)\r?\n([\s\S]*)$/);
    if (!match) continue;
    byId.set(match[2], { type: match[1], id: match[2], body: match[3] });
    order.push(match[2]);
  }
  return { byId, order, header: parts[0] };
}

function goComponents(byId, goId) {
  const block = byId.get(goId);
  if (!block || block.type !== "1") return [];
  return [...block.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((m) => m[1]);
}

function xfOfGo(byId, goId) {
  for (const componentId of goComponents(byId, goId)) {
    const component = byId.get(componentId);
    if (component?.type === "224") return componentId;
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
  const match = body.match(/\r?\n  m_Children:\r?\n((?:  - \{fileID: \d+\}\r?\n)*)/);
  if (!match) return [];
  return [...match[1].matchAll(/fileID: (\d+)/g)].map((m) => m[1]);
}

function collectSubtree(byId, rootGoId) {
  const ids = new Set();
  const rootXf = xfOfGo(byId, rootGoId);
  if (!rootXf) {
    return ids;
  }

  const stack = [rootXf];
  while (stack.length) {
    const xfId = stack.pop();
    if (!xfId || ids.has(xfId)) {
      continue;
    }

    ids.add(xfId);
    const goId = goOfComponent(byId, xfId);
    if (goId) {
      ids.add(goId);
      for (const c of goComponents(byId, goId)) {
        ids.add(c);
      }
    }

    for (const childXf of getChildren(byId, xfId)) {
      stack.push(childXf);
    }
  }

  return ids;
}

function remapBody(body, idMap) {
  let next = body;
  for (const [oldId, newId] of idMap.entries()) {
    next = next.replaceAll(`{fileID: ${oldId}}`, `{fileID: ${newId}}`);
  }
  return next;
}

function findGoByName(byId, name) {
  for (const block of byId.values()) {
    if (block.type === "1" && block.body.match(new RegExp(`\\n  m_Name: ${name}\\s*\\n`))) {
      return block.id;
    }
  }
  return null;
}

function removeCornerHud(flower) {
  const cornerGo = findGoByName(flower.byId, "CornerHud");
  if (!cornerGo) return null;
  const toRemove = collectSubtree(flower.byId, cornerGo);
  const xf = xfOfGo(flower.byId, cornerGo);
  for (const id of toRemove) {
    flower.byId.delete(id);
    flower.order = flower.order.filter((x) => x !== id);
  }
  const canvasBlock = flower.byId.get(FLOWER_CANVAS_XF);
  if (canvasBlock) {
    canvasBlock.body = canvasBlock.body.replace(
      new RegExp(`  - \\{fileID: ${xf}\\}\\n`, "g"),
      "",
    );
  }
  return null;
}

function serializeScene(header, order, byId) {
  const blocks = order.map((id) => {
    const block = byId.get(id);
    return `--- !u!${block.type} &${block.id}\n${block.body}`;
  });
  return `${header}${blocks.join("")}`;
}

const bonds = parseScene(fs.readFileSync(BONDS, "utf8"));
const flower = parseScene(fs.readFileSync(FLOWER, "utf8"));

removeCornerHud(flower);

const sourceIds = [...collectSubtree(bonds.byId, CORNER_GO)].sort((a, b) => Number(a) - Number(b));
const idMap = new Map();
sourceIds.forEach((oldId, index) => {
  idMap.set(oldId, String(ID_BASE + index));
});

const newHubId = idMap.get(HUB_COMPONENT);
const newRootXf = idMap.get(CORNER_XF);

for (const oldId of sourceIds) {
  const src = bonds.byId.get(oldId);
  if (!src) continue;
  let body = remapBody(src.body, idMap);
  if (oldId === CORNER_XF) {
    body = body.replace(/m_Father: \{fileID: \d+\}/, `m_Father: {fileID: ${FLOWER_CANVAS_XF}}`);
  }
  const newId = idMap.get(oldId);
  flower.byId.set(newId, { type: src.type, id: newId, body });
  flower.order.push(newId);
}

const canvasBlock = flower.byId.get(FLOWER_CANVAS_XF);
  if (canvasBlock && newRootXf) {
    if (!canvasBlock.body.includes(`{fileID: ${newRootXf}}`)) {
      canvasBlock.body = canvasBlock.body.replace(
        /(\r?\n  m_Children:\r?\n)/,
        `$1  - {fileID: ${newRootXf}}\r\n`,
      );
    }
  canvasBlock.body = canvasBlock.body.replace(
    /m_LocalScale: \{x: 0, y: 0, z: 0\}/,
    "m_LocalScale: {x: 1, y: 1, z: 1}",
  );
}

const vnBlock = flower.byId.get(VN_RUNTIME);
if (vnBlock && newHubId) {
  if (vnBlock.body.includes("hubCornerInfoHud:")) {
    vnBlock.body = vnBlock.body.replace(
      /hubCornerInfoHud: \{fileID: \d+\}/,
      `hubCornerInfoHud: {fileID: ${newHubId}}`,
    );
  } else {
    vnBlock.body = vnBlock.body.replace(
      /(dateHud: \{fileID: \d+\})/,
      `$1\n  hubCornerInfoHud: {fileID: ${newHubId}}`,
    );
  }
}

fs.writeFileSync(FLOWER, serializeScene(flower.header, flower.order, flower.byId));

console.log(
  JSON.stringify(
    {
      clonedBlocks: sourceIds.length,
      hubCornerInfoHud: newHubId,
      cornerRoot: newRootXf,
    },
    null,
    2,
  ),
);
