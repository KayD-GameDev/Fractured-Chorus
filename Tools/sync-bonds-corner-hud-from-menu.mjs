import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");
const SNAPSHOT = path.join(ROOT, "Assets/FracturedChorus/Art/UI/TownMap/hub_corner_info_hud_layout_snapshot.json");

const UI_IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc";
const UI_TEXT = "5f7201a12d95ffc409449d95f23cf332";
const FONT_DISPLAY = "d4e5f6a7b8c94091a2b3c4d5e6f70891";
const FONT_BODY = "787fda44816c480a9cc3cadfdc29ce24";
const SPRITE_SUN = "01667d25060e423ea7cb957281a474cc";

const CORNER_HUD_RT = "931000020";
const CORNER_HUD_IMG = "931000022";
const PATH_PREFIX = "BondsCanvas/CornerHud";
const MENU_ROOT_POS = { x: 28, y: -20 };
const MENU_ROOT_SIZE = { x: 392, y: 132 };

const NEW = {
  BgDark: { go: "931000881", rt: "931000882", img: "931000883", cr: "931000884" },
  BgLight: { go: "931000885", rt: "931000886", img: "931000887", cr: "931000888" },
  DecorLineV: { go: "931000889", rt: "931000890", img: "931000891", cr: "931000892" },
  DecorLineH: { go: "931000893", rt: "931000894", img: "931000895", cr: "931000896" },
  DecorGlint: { go: "931000897", rt: "931000898", img: "931000899", cr: "931000900" },
};

const CHILD_ORDER = [
  NEW.BgDark.rt,
  NEW.BgLight.rt,
  NEW.DecorLineV.rt,
  NEW.DecorLineH.rt,
  NEW.DecorGlint.rt,
  "931000024",
  "931000026",
  "931000028",
  "931000030",
  "931000032",
];

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

function textOfGo(byId, goId) {
  for (const componentId of goComponents(byId, goId)) {
    const component = byId.get(componentId);
    if (component?.type === "114" && component.body.includes("UnityEngine.UI::UnityEngine.UI.Text")) {
      return { id: componentId };
    }
  }
  return null;
}

function imageOfGo(byId, goId) {
  for (const componentId of goComponents(byId, goId)) {
    const component = byId.get(componentId);
    if (component?.type === "114" && component.body.includes("UnityEngine.UI::UnityEngine.UI.Image")) {
      return { id: componentId };
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

function getChildren(byId, transformId) {
  const body = byId.get(transformId)?.body ?? "";
  const match = body.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!match) return [];
  return [...match[1].matchAll(/fileID: (\d+)/g)].map((entry) => entry[1]);
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
  return next;
}

function patchImageColor(body, color) {
  return body.replace(
    /m_Color: \{r: [^}]+\}/,
    `m_Color: {r: ${fmt(color.r)}, g: ${fmt(color.g)}, b: ${fmt(color.b)}, a: ${fmt(color.a)}}`,
  );
}

function patchImageSprite(body, spriteGuid) {
  if (spriteGuid) {
    return body.replace(
      /m_Sprite: \{fileID: [^}]+\}/,
      `m_Sprite: {fileID: 21300000, guid: ${spriteGuid}, type: 3}`,
    );
  }
  return body.replace(/m_Sprite: \{fileID: [^}]+\}/, "m_Sprite: {fileID: 0}");
}

function patchPreserveAspect(body, value) {
  return body.replace(/m_PreserveAspect: [01]/, `m_PreserveAspect: ${value}`);
}

function patchText(body, meta, fontGuid) {
  let next = body;
  next = next.replace(
    /m_Color: \{r: [^}]+\}/,
    `m_Color: {r: ${fmt(meta.color.r)}, g: ${fmt(meta.color.g)}, b: ${fmt(meta.color.b)}, a: ${fmt(meta.color.a)}}`,
  );
  next = next.replace(/m_FontSize: \d+/, `m_FontSize: ${meta.fontSize}`);
  next = next.replace(/m_MaxSize: \d+/, `m_MaxSize: ${meta.fontSize}`);
  next = next.replace(/m_FontStyle: \d+/, `m_FontStyle: ${meta.fontStyle}`);
  next = next.replace(/m_Alignment: \d+/, `m_Alignment: ${meta.alignment}`);
  next = next.replace(
    /m_Font: \{fileID: 12800000, guid: [^,]+, type: 3\}/,
    `m_Font: {fileID: 12800000, guid: ${fontGuid}, type: 3}`,
  );
  return next;
}

function imageYaml({ go, rt, img, cr, name, father, node, active = 1 }) {
  const color = node.imageColor;
  const pivot = node.pivot ?? { x: 0.5, y: 0.5 };
  return `--- !u!1 &${go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rt}}
  - component: {fileID: ${cr}}
  - component: {fileID: ${img}}
  m_Layer: 0
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: ${active}
--- !u!224 &${rt}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${father}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${fmt(node.anchorMin.x)}, y: ${fmt(node.anchorMin.y)}}
  m_AnchorMax: {x: ${fmt(node.anchorMax.x)}, y: ${fmt(node.anchorMax.y)}}
  m_AnchoredPosition: {x: ${fmt(node.anchoredPosition.x)}, y: ${fmt(node.anchoredPosition.y)}}
  m_SizeDelta: {x: ${fmt(node.sizeDelta.x)}, y: ${fmt(node.sizeDelta.y)}}
  m_Pivot: {x: ${fmt(pivot.x)}, y: ${fmt(pivot.y)}}
--- !u!114 &${img}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${UI_IMAGE}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: ${fmt(color.r)}, g: ${fmt(color.g)}, b: ${fmt(color.b)}, a: ${fmt(color.a)}}
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
--- !u!222 &${cr}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_CullTransparentMesh: 1
`;
}

const snapshot = JSON.parse(fs.readFileSync(SNAPSHOT, "utf8"));
const nodeByTail = new Map();
for (const node of snapshot.nodes) {
  const tail = node.path.replace(/^HubCornerInfoHud\/?/, "");
  if (!tail || tail === "HubCornerInfoHud") {
    nodeByTail.set("", node);
  } else {
    nodeByTail.set(tail, node);
  }
}

const { byId, order, header } = parseScene(fs.readFileSync(SCENE, "utf8"));
const rootGoId = findGoByName(byId, "BondsCanvas");
if (!rootGoId) throw new Error("BondsCanvas missing");

const pathMap = new Map();
buildPathMap(byId, rootGoId, "BondsCanvas", pathMap);

const created = [];
const patched = [];
const skipped = [];

let insertYaml = "";
for (const [name, ids] of Object.entries(NEW)) {
  const targetPath = `${PATH_PREFIX}/${name}`;
  if (pathMap.has(targetPath)) {
    skipped.push(`${name} (exists)`);
    continue;
  }
  const node = nodeByTail.get(name);
  if (!node) {
    skipped.push(`${name} (no snapshot)`);
    continue;
  }
  const active = name === "BgDark" || name === "BgLight" ? 1 : node.activeSelf ? 1 : 0;
  insertYaml += imageYaml({
    ...ids,
    name,
    father: CORNER_HUD_RT,
    node,
    active,
  });
  created.push(name);
}

const cornerRt = byId.get(CORNER_HUD_RT);
if (!cornerRt) throw new Error("CornerHud RectTransform missing");
cornerRt.body = cornerRt.body.replace(
  /\n  m_Children:\n(?:  - \{fileID: \d+\}\n)+/,
  `\n  m_Children:\n${CHILD_ORDER.map((id) => `  - {fileID: ${id}}`).join("\n")}\n`,
);

const rootNode = nodeByTail.get("");
if (rootNode) {
  cornerRt.body = patchRectBody(cornerRt.body, {
    ...rootNode,
    anchoredPosition: MENU_ROOT_POS,
    sizeDelta: MENU_ROOT_SIZE,
  });
  patched.push(`${PATH_PREFIX} (root rect)`);
}

const rootImg = byId.get(CORNER_HUD_IMG);
if (rootImg) {
  rootImg.body = patchImageColor(rootImg.body, { r: 1, g: 1, b: 1, a: 0 });
  rootImg.body = patchImageSprite(rootImg.body, null);
  rootImg.body = rootImg.body.replace(/m_FillCenter: [01]/, "m_FillCenter: 1");
  patched.push(`${PATH_PREFIX} (root image)`);
}

for (const node of snapshot.nodes) {
  const tail = node.path.replace(/^HubCornerInfoHud\/?/, "");
  if (!tail || tail === "HubCornerInfoHud" || tail === "BgSunClouds" || NEW[tail.split("/")[0]]) {
    continue;
  }

  const bondsPath = `${PATH_PREFIX}/${tail}`;
  const goId = pathMap.get(bondsPath);
  if (!goId) {
    skipped.push(`${bondsPath} (missing go)`);
    continue;
  }

  const transform = xfOfGo(byId, goId);
  if (transform) {
    byId.get(transform.id).body = patchRectBody(byId.get(transform.id).body, node);
    patched.push(`${bondsPath} (rect)`);
  }

  if (node.textMeta) {
    const text = textOfGo(byId, goId);
    if (text) {
      const font = tail === "TaglineLabel" ? FONT_BODY : FONT_DISPLAY;
      byId.get(text.id).body = patchText(byId.get(text.id).body, node.textMeta, font);
      patched.push(`${bondsPath} (text)`);
    }
  }

  if (node.imageColor && tail === "PhaseIcon") {
    const image = imageOfGo(byId, goId);
    if (image) {
      byId.get(image.id).body = patchImageColor(byId.get(image.id).body, node.imageColor);
      byId.get(image.id).body = patchImageSprite(byId.get(image.id).body, SPRITE_SUN);
      byId.get(image.id).body = patchPreserveAspect(byId.get(image.id).body, 1);
      patched.push(`${bondsPath} (phase icon)`);
    }
  }
}

const emitOrder = [
  ...order.filter((id) => byId.get(id).type !== "1660057539"),
  ...order.filter((id) => byId.get(id).type === "1660057539"),
];

const rootsIdx = emitOrder.findIndex((id) => byId.get(id).type === "1660057539");
const beforeRoots = emitOrder.slice(0, rootsIdx >= 0 ? rootsIdx : emitOrder.length);
const afterRoots = rootsIdx >= 0 ? emitOrder.slice(rootsIdx) : [];

const body = `${header}${beforeRoots.map((id) => {
  const block = byId.get(id);
  return `--- !u!${block.type} &${block.id}\n${block.body}`;
}).join("")}${insertYaml}${afterRoots.map((id) => {
  const block = byId.get(id);
  return `--- !u!${block.type} &${block.id}\n${block.body}`;
}).join("")}`;

fs.writeFileSync(SCENE, body);

console.log(
  JSON.stringify(
    {
      created,
      patchedCount: patched.length,
      patched,
      skipped,
    },
    null,
    2,
  ),
);
