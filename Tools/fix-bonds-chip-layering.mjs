import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");
const MAP = path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/bonds_pack_scene_map.json");
const PACK = path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/Pack");
const CHIP_CHILD_ORDER = ["Frame", "Face", "Lock", "Name", "Role"];

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

function imageComponentOfGo(byId, goId) {
  for (const componentId of goComponents(byId, goId)) {
    const component = byId.get(componentId);
    if (component?.type === "114" && component.body.includes("UnityEngine.UI::UnityEngine.UI.Image")) {
      return componentId;
    }
  }
  return null;
}

function loadGuidForFile(file) {
  const meta = path.join(PACK, `${file}.meta`);
  return fs.readFileSync(meta, "utf8").match(/^guid: ([a-f0-9]+)/m)?.[1] ?? null;
}

function patchImage(body, { spriteGuid, preserveAspect, raycastTarget }) {
  let next = body;
  if (spriteGuid) {
    next = next.replace(
      /m_Sprite: \{fileID: [^}]+\}/,
      `m_Sprite: {fileID: 21300000, guid: ${spriteGuid}, type: 3}`,
    );
  }
  if (preserveAspect != null) {
    next = next.replace(/m_PreserveAspect: [01]/, `m_PreserveAspect: ${preserveAspect ? 1 : 0}`);
  }
  if (raycastTarget != null) {
    next = next.replace(/m_RaycastTarget: [01]/, `m_RaycastTarget: ${raycastTarget ? 1 : 0}`);
  }
  return next;
}

function reorderChildren(byId, parentPath, pathMap, childPaths) {
  const parentGoId = pathMap.get(parentPath);
  const parentTransform = xfOfGo(byId, parentGoId);
  const childIds = childPaths.map((childPath) => {
    const goId = pathMap.get(childPath);
    const transform = xfOfGo(byId, goId);
    if (!transform) throw new Error(`Missing transform for ${childPath}`);
    return transform.id;
  });
  const body = byId.get(parentTransform.id).body;
  const nextChildren = childIds.map((id) => `  - {fileID: ${id}}\n`).join("");
  byId.get(parentTransform.id).body = body.replace(
    /\n  m_Children:\n(?:  - \{fileID: \d+\}\n)*/,
    `\n  m_Children:\n${nextChildren}`,
  );
}

const mapDoc = JSON.parse(fs.readFileSync(MAP, "utf8"));
const frameSpriteByPath = new Map(
  mapDoc.assignments
    .filter((entry) => entry.path.includes("/Roster/Chip_") && entry.path.endsWith("/Frame"))
    .map((entry) => [entry.path, loadGuidForFile(entry.file)]),
);

const { byId, order, header } = parseScene(fs.readFileSync(SCENE, "utf8"));
const rootGoId = findGoByName(byId, "BondsCanvas");
if (!rootGoId) throw new Error("BondsCanvas missing");

const pathMap = new Map();
buildPathMap(byId, rootGoId, "BondsCanvas", pathMap);

const reordered = [];
const sprites = [];
for (let i = 0; i < 7; i++) {
  const chipPath = `BondsCanvas/CenterStats/Roster/Chip_${i}`;
  if (!pathMap.has(chipPath)) continue;

  const childPaths = CHIP_CHILD_ORDER.filter((name) => pathMap.has(`${chipPath}/${name}`));
  if (childPaths.length === 0) continue;
  reorderChildren(
    byId,
    chipPath,
    pathMap,
    childPaths.map((name) => `${chipPath}/${name}`),
  );
  reordered.push(chipPath);

  const framePath = `${chipPath}/Frame`;
  const facePath = `${chipPath}/Face`;
  const frameGoId = pathMap.get(framePath);
  const faceGoId = pathMap.get(facePath);
  const frameImageId = frameGoId ? imageComponentOfGo(byId, frameGoId) : null;
  const faceImageId = faceGoId ? imageComponentOfGo(byId, faceGoId) : null;

  if (frameImageId) {
    const spriteGuid = frameSpriteByPath.get(framePath);
    byId.get(frameImageId).body = patchImage(byId.get(frameImageId).body, {
      spriteGuid,
      preserveAspect: true,
      raycastTarget: true,
    });
    sprites.push(framePath);
  }

  if (faceImageId) {
    byId.get(faceImageId).body = patchImage(byId.get(faceImageId).body, {
      preserveAspect: true,
      raycastTarget: false,
    });
  }
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
      reordered,
      frameSprites: sprites,
      childOrder: CHIP_CHILD_ORDER,
      note: "Face renders above Frame. RectTransform untouched.",
    },
    null,
    2,
  ),
);
