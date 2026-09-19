import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");
const MAP = path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/bonds_pack_scene_map.json");
const REPORT = path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/Pack/CLEAN_REPORT.json");
const ROOT_NAME = "BondsCanvas";
const APPLY_LAYOUT = process.argv.includes("--apply-layout");
const packMap = JSON.parse(fs.readFileSync(MAP, "utf8"));

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

function imageOfGo(byId, goId) {
  for (const componentId of goComponents(byId, goId)) {
    const component = byId.get(componentId);
    if (component?.type === "114" && component.body.includes("UnityEngine.UI::UnityEngine.UI.Image")) {
      return component;
    }
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

function patchRectBody(body, layout) {
  let next = body;
  next = next.replace(/m_AnchorMin: \{x: [^}]+\}/, `m_AnchorMin: {x: ${fmt(layout.anchorMin.x)}, y: ${fmt(layout.anchorMin.y)}}`);
  next = next.replace(/m_AnchorMax: \{x: [^}]+\}/, `m_AnchorMax: {x: ${fmt(layout.anchorMax.x)}, y: ${fmt(layout.anchorMax.y)}}`);
  next = next.replace(/m_AnchoredPosition: \{x: [^}]+\}/, `m_AnchoredPosition: {x: ${fmt(layout.anchoredPosition.x)}, y: ${fmt(layout.anchoredPosition.y)}}`);
  next = next.replace(/m_SizeDelta: \{x: [^}]+\}/, `m_SizeDelta: {x: ${fmt(layout.sizeDelta.x)}, y: ${fmt(layout.sizeDelta.y)}}`);
  next = next.replace(/m_Pivot: \{x: [^}]+\}/, `m_Pivot: {x: ${fmt(layout.pivot.x)}, y: ${fmt(layout.pivot.y)}}`);
  return next;
}

function assignSprite(imageBlock, guid, simple, file) {
  if (guid) {
    imageBlock.body = imageBlock.body.replace(
      /m_Sprite: \{fileID: [^}]+\}/,
      `m_Sprite: {fileID: 21300000, guid: ${guid}, type: 3}`,
    );
    imageBlock.body = imageBlock.body.replace(
      /m_Color: \{r: [^,]+, g: [^,]+, b: [^,]+, a: 0\}/,
      "m_Color: {r: 1, g: 1, b: 1, a: 1}",
    );
  } else {
    imageBlock.body = imageBlock.body.replace(/m_Sprite: \{fileID: [^}]+\}/, "m_Sprite: {fileID: 0}");
  }
  if (simple) {
    imageBlock.body = imageBlock.body.replace(/m_Type: 1/, "m_Type: 0");
    imageBlock.body = imageBlock.body.replace(/m_FillCenter: 0/, "m_FillCenter: 1");
  }
  const panelFiles = new Set([
    "01_Header_Bonds.png",
    "02_Menu_Normal.png",
    "03_Menu_Selected.png",
    "04_Panel_SocialStats.png",
    "05_Panel_Episodes.png",
    "06_Row_Episode_Normal.png",
    "07_Row_Episode_Selected.png",
    "08_Card_Character_Normal.png",
    "09_Card_Character_Selected.png",
    "10_Panel_CharacterInfo.png",
    "11_Frame_Promo.png",
    "12_Panel_Footer.png",
  ]);
  if (panelFiles.has(file)) {
    imageBlock.body = imageBlock.body.replace(/m_PreserveAspect: 1/, "m_PreserveAspect: 0");
  } else if (simple) {
    imageBlock.body = imageBlock.body.replace(/m_PreserveAspect: 0/, "m_PreserveAspect: 1");
  }
}

function nextFreeId(byId, start) {
  let id = start;
  while (byId.has(String(id))) id += 1;
  return id;
}

function ensureImageChild(byId, order, parentGoId, name) {
  const existing = pathHasChild(byId, parentGoId, name);
  if (existing) return existing;
  const parentXf = xfOfGo(byId, parentGoId);
  if (!parentXf) return null;
  const goId = String(nextFreeId(byId, 931000800));
  const xfId = String(nextFreeId(byId, Number(goId) + 1));
  const crId = String(nextFreeId(byId, Number(xfId) + 1));
  const imgId = String(nextFreeId(byId, Number(crId) + 1));
  byId.set(goId, {
    type: "1",
    id: goId,
    body: `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${xfId}}
  - component: {fileID: ${crId}}
  - component: {fileID: ${imgId}}
  m_Layer: 0
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
`,
  });
  byId.set(xfId, {
    type: "224",
    id: xfId,
    body: `RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${parentXf.id}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0.5}
  m_AnchorMax: {x: 0, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 36, y: 36}
  m_Pivot: {x: 0.5, y: 0.5}
`,
  });
  byId.set(crId, {
    type: "222",
    id: crId,
    body: `CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
`,
  });
  byId.set(imgId, {
    type: "114",
    id: imgId,
    body: `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
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
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 0
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`,
  });
  const parentXfBlock = byId.get(parentXf.id);
  if (parentXfBlock.body.includes("\n  m_Children: []\n")) {
    parentXfBlock.body = parentXfBlock.body.replace(
      "\n  m_Children: []\n",
      `\n  m_Children:\n  - {fileID: ${xfId}}\n`,
    );
  } else {
    parentXfBlock.body = parentXfBlock.body.replace(
      /(\n  m_Children:\n(?:  - \{fileID: \d+\}\n)*)/,
      `$1  - {fileID: ${xfId}}\n`,
    );
  }
  order.push(goId, xfId, crId, imgId);
  return goId;
}

function pathHasChild(byId, parentGoId, name) {
  const transform = xfOfGo(byId, parentGoId);
  if (!transform) return null;
  for (const childTransformId of getChildren(byId, transform.id)) {
    const childGoId = goOfComponent(byId, childTransformId);
    if (childGoId && goName(byId, childGoId) === name) return childGoId;
  }
  return null;
}

const layoutDoc = APPLY_LAYOUT
  ? JSON.parse(fs.readFileSync(path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_bootstrap_layout.json"), "utf8"))
  : null;
const report = JSON.parse(fs.readFileSync(REPORT, "utf8"));
const guidByFile = new Map(report.map((entry) => [entry.file, entry.guid]));
const { byId, order, header } = parseScene(fs.readFileSync(SCENE, "utf8"));
const rootGoId = findGoByName(byId, ROOT_NAME);
if (!rootGoId) throw new Error("BondsCanvas missing");

const footerGoId = [...byId.values()].find((block) => block.type === "1" && block.body.includes("\n  m_Name: Footer\n"))?.id;
if (footerGoId) {
  ensureImageChild(byId, order, footerGoId, "ConfirmIcon");
  ensureImageChild(byId, order, footerGoId, "BackIcon");
}
if (!pathHasChild(byId, rootGoId, "Divider")) {
  ensureImageChild(byId, order, rootGoId, "Divider");
}

const pathMap = new Map();
buildPathMap(byId, rootGoId, ROOT_NAME, pathMap);

const applied = [];
const missing = [];
if (APPLY_LAYOUT && layoutDoc) {
  for (const node of layoutDoc.nodes) {
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
    const block = byId.get(transform.id);
    block.body = patchRectBody(block.body, node);
    applied.push(node.path);
  }
}

const sprites = [];
for (const { path: objectPath, file } of packMap.assignments) {
  const goId = pathMap.get(objectPath);
  const image = goId ? imageOfGo(byId, goId) : null;
  if (!image) {
    missing.push(`${objectPath} sprite`);
    continue;
  }
  assignSprite(image, guidByFile.get(file), true, file);
  sprites.push({ objectPath, file });
}

const promoImage = pathMap.get("BondsCanvas/LinkEpisodes/PromoFrame/PromoImage");
if (promoImage) {
  const image = imageOfGo(byId, promoImage);
  if (image) assignSprite(image, null, true, "");
}

const menu = [...byId.values()].find((block) =>
  block.type === "114" && block.body.includes("Assembly-CSharp::FracturedChorus.Hub.BondsMenuUI"),
);
if (menu) {
  const spriteRef = (file) => `{fileID: 21300000, guid: ${guidByFile.get(file)}, type: 3}`;
  const refs = packMap.refs;
  menu.body = menu.body
    .replace(
      /statIcons:\n(?:  - \{fileID: [^}]+\}\n){5}/,
      `statIcons:\n${refs.statIcons.map((file) => `  - ${spriteRef(file)}\n`).join("")}`,
    )
    .replace(/chipFrameNormal: \{fileID: [^}]+\}/, `chipFrameNormal: ${spriteRef(refs.chipFrameNormal)}`)
    .replace(/chipFrameSelected: \{fileID: [^}]+\}/, `chipFrameSelected: ${spriteRef(refs.chipFrameSelected)}`)
    .replace(/chipFrameLocked: \{fileID: [^}]+\}/, `chipFrameLocked: ${spriteRef(refs.chipFrameLocked)}`)
    .replace(/lockIcon: \{fileID: 21300000, guid: [^}]+\}/, `lockIcon: ${spriteRef(refs.lockIcon)}`);
}

for (const block of byId.values()) {
  if (block.type === "114" && block.body.includes("BondEpisodeRowView")) {
    block.body = block.body
      .replace(/playSprite: \{fileID: [^}]+\}/, `playSprite: {fileID: 21300000, guid: ${guidByFile.get(packMap.refs.playSprite)}, type: 3}`)
      .replace(/lockSprite: \{fileID: [^}]+\}/, `lockSprite: {fileID: 21300000, guid: ${guidByFile.get(packMap.refs.lockSprite)}, type: 3}`);
  }
}


const emitOrder = [
  ...order.filter((id) => byId.get(id).type !== "1660057539"),
  ...order.filter((id) => byId.get(id).type === "1660057539"),
];
fs.writeFileSync(
  SCENE,
  `${header}${emitOrder.map((id) => {
    const block = byId.get(id);
    return `--- !u!${block.type} &${block.id}\n${block.body}`;
  }).join("")}`,
);

console.log(JSON.stringify({ mode: APPLY_LAYOUT ? "sprites+layout" : "sprites-only", applied: applied.length, sprites: sprites.length, missing }, null, 2));
