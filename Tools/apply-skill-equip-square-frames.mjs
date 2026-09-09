import fs from "fs";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = `${ROOT}/Assets/FracturedChorus/Scenes/CharacterBuild.unity`;
const UNLOCKED = "7e4c1a9b2d6f4830a5c8e0f1b3d7a924";
const LOCKED = "6b9e2c4d8f1a4703b6d0e5c2a8f3147e";

function parseScene(text) {
  const parts = text.split(/^--- !u!/m);
  const header = parts[0];
  const blocks = [];
  const byId = new Map();
  for (let i = 1; i < parts.length; i++) {
    const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
    if (!m) continue;
    const block = { type: m[1], id: m[2], body: m[3] };
    blocks.push(block);
    byId.set(block.id, block);
  }
  return { header, blocks, byId };
}

function sync(block) {
  block.raw = `--- !u!${block.type} &${block.id}\n${block.body}`;
}

function goName(byId, goId) {
  return byId.get(goId)?.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}

function goComponents(byId, goId) {
  const b = byId.get(goId);
  if (!b) return [];
  return [...b.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((m) => m[1]);
}

function findGoByName(byId, name) {
  for (const b of byId.values()) {
    if (b.type === "1" && b.body.match(new RegExp(`\\n  m_Name: ${name}\\s*\\n`))) return b.id;
  }
  return null;
}

function xfOfGo(byId, goId) {
  for (const cid of goComponents(byId, goId)) {
    if (byId.get(cid)?.type === "224") return cid;
  }
  return null;
}

function goOfComponent(byId, compId) {
  for (const [gid, g] of byId.entries()) {
    if (g.type === "1" && g.body.includes(`component: {fileID: ${compId}}`)) return gid;
  }
  return null;
}

function cell(columns, col, yMin, yMax) {
  const inset = 0.07;
  const gap = 0.012;
  const usable = 1 - inset * 2;
  const w = (usable - gap * (columns - 1)) / columns;
  const x = inset + col * (w + gap);
  return { xmin: x, xmax: x + w, ymin: yMin, ymax: yMax };
}

function setRect(rt, box) {
  rt.body = rt.body
    .replace(/m_AnchorMin: \{x: [^}]+\}/, `m_AnchorMin: {x: ${box.xmin}, y: ${box.ymin}}`)
    .replace(/m_AnchorMax: \{x: [^}]+\}/, `m_AnchorMax: {x: ${box.xmax}, y: ${box.ymax}}`)
    .replace(/m_AnchoredPosition: \{x: [^}]+\}/, "m_AnchoredPosition: {x: 0, y: 0}")
    .replace(/m_SizeDelta: \{x: [^}]+\}/, "m_SizeDelta: {x: 0, y: 0}");
  sync(rt);
}

function applyImage(img) {
  img.body = img.body
    .replace(/m_Sprite: \{fileID: [^}]+\}/, `m_Sprite: {fileID: 21300000, guid: ${UNLOCKED}, type: 3}`)
    .replace(/m_Type: \d+/, "m_Type: 0")
    .replace(/m_PreserveAspect: \d+/, "m_PreserveAspect: 1");
  sync(img);
}

function applyLabel(rt, text) {
  if (rt?.type === "224") {
    rt.body = rt.body
      .replace(/m_AnchorMin: \{x: [^}]+\}/, "m_AnchorMin: {x: 0.12, y: 0.18}")
      .replace(/m_AnchorMax: \{x: [^}]+\}/, "m_AnchorMax: {x: 0.88, y: 0.82}")
      .replace(/m_AnchoredPosition: \{x: [^}]+\}/, "m_AnchoredPosition: {x: 0, y: 0}")
      .replace(/m_SizeDelta: \{x: [^}]+\}/, "m_SizeDelta: {x: 0, y: 0}");
    sync(rt);
  }
  if (text) {
    text.body = text.body
      .replace(/m_Color: \{r: [^}]+\}/, "m_Color: {r: 0.12, g: 0.2, b: 0.42, a: 1}")
      .replace(/m_FontSize: \d+/, "m_FontSize: 13")
      .replace(/m_FontStyle: \d+/, "m_FontStyle: 1")
      .replace(/m_BestFit: \d+/, "m_BestFit: 1")
      .replace(/m_MinSize: \d+/, "m_MinSize: 10")
      .replace(/m_MaxSize: \d+/, "m_MaxSize: 16");
    sync(text);
  }
}

function maxId(byId) {
  let n = 2144000000;
  for (const id of byId.keys()) {
    const v = Number(id);
    if (!Number.isSafeInteger(v) || v >= 2_000_000_000) continue;
    n = Math.max(n, v);
  }
  return n;
}

function makePool(scene, index, fatherRt, box, next) {
  const goId = String(next());
  const rtId = String(next());
  const crId = String(next());
  const imgId = String(next());
  const btnId = String(next());
  const viewId = String(next());
  const labelGo = String(next());
  const labelRt = String(next());
  const labelCr = String(next());
  const labelTxt = String(next());

  const go = {
    type: "1",
    id: goId,
    body: `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rtId}}
  - component: {fileID: ${crId}}
  - component: {fileID: ${imgId}}
  - component: {fileID: ${btnId}}
  - component: {fileID: ${viewId}}
  m_Layer: 0
  m_Name: EquipPool_${index}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
`,
  };
  const rt = {
    type: "224",
    id: rtId,
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
  m_Children:
  - {fileID: ${labelRt}}
  m_Father: {fileID: ${fatherRt}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${box.xmin}, y: ${box.ymin}}
  m_AnchorMax: {x: ${box.xmax}, y: ${box.ymax}}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
`,
  };
  const cr = {
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
  };
  const img = {
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
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 21300000, guid: ${UNLOCKED}, type: 3}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`,
  };
  const btn = {
    type: "114",
    id: btnId,
    body: `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}
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
  m_TargetGraphic: {fileID: ${imgId}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
`,
  };
  const view = {
    type: "114",
    id: viewId,
    body: `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 9c2e8a4b71d04f6e8f1c3a5d0b7e92c4, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.Hub.CharacterBuild.CharacterBuildEquipSlotView
  button: {fileID: ${btnId}}
  label: {fileID: ${labelTxt}}
  frame: {fileID: ${imgId}}
`,
  };
  const lgo = {
    type: "1",
    id: labelGo,
    body: `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${labelRt}}
  - component: {fileID: ${labelCr}}
  - component: {fileID: ${labelTxt}}
  m_Layer: 0
  m_Name: Label
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
`,
  };
  const lrt = {
    type: "224",
    id: labelRt,
    body: `RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${labelGo}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${rtId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.12, y: 0.18}
  m_AnchorMax: {x: 0.88, y: 0.82}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
`,
  };
  const lcr = {
    type: "222",
    id: labelCr,
    body: `CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${labelGo}}
  m_CullTransparentMesh: 1
`,
  };
  const ltxt = {
    type: "114",
    id: labelTxt,
    body: `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${labelGo}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text
  m_Material: {fileID: 0}
  m_Color: {r: 0.12, g: 0.2, b: 0.42, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {fileID: 12800000, guid: 787fda44816c480a9cc3cadfdc29ce24, type: 3}
    m_FontSize: 13
    m_FontStyle: 1
    m_BestFit: 1
    m_MinSize: 10
    m_MaxSize: 16
    m_Alignment: 4
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: 
`,
  };

  for (const b of [go, rt, cr, img, btn, view, lgo, lrt, lcr, ltxt]) {
    sync(b);
    scene.blocks.push(b);
    scene.byId.set(b.id, b);
  }
  return { goId, rtId, viewId, imgId };
}

const scene = parseScene(fs.readFileSync(SCENE, "utf8"));
const overlayGo = findGoByName(scene.byId, "SkillEquipOverlay");
const overlayRt = xfOfGo(scene.byId, overlayGo);
if (!overlayGo || !overlayRt) throw new Error("SkillEquipOverlay missing");

let nid = Math.max(2144000100, maxId(scene.byId) + 1);
const next = () => nid++;

const slotViews = [];
const poolViews = [];

for (let i = 0; i < 5; i++) {
  const go = findGoByName(scene.byId, `EquipSlot_${i}`);
  if (!go) throw new Error(`EquipSlot_${i} missing`);
  const rt = scene.byId.get(xfOfGo(scene.byId, go));
  setRect(rt, cell(5, i, 0.74, 0.9));
  for (const cid of goComponents(scene.byId, go)) {
    const c = scene.byId.get(cid);
    if (c?.body.includes("UnityEngine.UI.Image") && !c.body.includes("UnityEngine.UI.Text")) applyImage(c);
    if (c?.body.includes("CharacterBuildEquipSlotView")) {
      slotViews[i] = c.id;
      if (!c.body.includes("\n  frame:")) {
        const imgId = goComponents(scene.byId, go).find((id) =>
          scene.byId.get(id)?.body.includes("UnityEngine.UI.Image"),
        );
        c.body = c.body.replace(/(\n  label: \{fileID: \d+\})/, `$1\n  frame: {fileID: ${imgId}}`);
        sync(c);
      }
    }
  }
  const labelRtId = rt.body.match(/m_Children:\n  - \{fileID: (\d+)\}/)?.[1];
  if (labelRtId) {
    const labelGo = goOfComponent(scene.byId, labelRtId);
    applyLabel(
      scene.byId.get(labelRtId),
      goComponents(scene.byId, labelGo)
        .map((id) => scene.byId.get(id))
        .find((b) => b?.body.includes("UnityEngine.UI.Text")),
    );
  }
}

for (let i = 0; i < 10; i++) {
  const row = i < 5 ? 0 : 1;
  const col = i < 5 ? i : i - 5;
  const yMin = row === 0 ? 0.28 : 0.12;
  const yMax = row === 0 ? 0.44 : 0.28;
  const box = cell(5, col, yMin, yMax);
  let go = findGoByName(scene.byId, `EquipPool_${i}`);
  if (!go) {
    const created = makePool(scene, i, overlayRt, box, next);
    poolViews[i] = created.viewId;
    const overlay = scene.byId.get(overlayRt);
    overlay.body = overlay.body.replace(
      /(\n  m_Children:\n(?:  - \{fileID: \d+\}\n)*)/,
      (m) => `${m}  - {fileID: ${created.rtId}}\n`,
    );
    sync(overlay);
    continue;
  }
  const rt = scene.byId.get(xfOfGo(scene.byId, go));
  setRect(rt, box);
  for (const cid of goComponents(scene.byId, go)) {
    const c = scene.byId.get(cid);
    if (c?.body.includes("UnityEngine.UI.Image") && !c.body.includes("UnityEngine.UI.Text")) applyImage(c);
    if (c?.body.includes("CharacterBuildEquipSlotView")) {
      poolViews[i] = c.id;
      if (!c.body.includes("\n  frame:")) {
        const imgId = goComponents(scene.byId, go).find((id) =>
          scene.byId.get(id)?.body.includes("UnityEngine.UI.Image"),
        );
        c.body = c.body.replace(/(\n  label: \{fileID: \d+\})/, `$1\n  frame: {fileID: ${imgId}}`);
        sync(c);
      }
    }
  }
  const labelRtId = rt.body.match(/m_Children:\n  - \{fileID: (\d+)\}/)?.[1];
  if (labelRtId) {
    const labelGo = goOfComponent(scene.byId, labelRtId);
    applyLabel(
      scene.byId.get(labelRtId),
      goComponents(scene.byId, labelGo)
        .map((id) => scene.byId.get(id))
        .find((b) => b?.body.includes("UnityEngine.UI.Text")),
    );
  }
}

const menu = [...scene.byId.values()].find((b) =>
  b.body.includes("FracturedChorus.Hub.CharacterBuild.CharacterBuildMenuUI"),
);
if (!menu) throw new Error("CharacterBuildMenuUI missing");

const slotYaml = slotViews.map((id) => `  - {fileID: ${id}}`).join("\n");
const poolYaml = poolViews.map((id) => `  - {fileID: ${id}}`).join("\n");
menu.body = menu.body
  .replace(
    /equipSlotViews:\n(?:  - \{fileID: \d+\}\n)*/,
    `equipSlotViews:\n${slotYaml}\n`,
  )
  .replace(
    /equipPoolViews:\n(?:  - \{fileID: \d+\}\n)*/,
    `equipPoolViews:\n${poolYaml}\n`,
  );
if (menu.body.includes("skillSlotUnlocked:")) {
  menu.body = menu.body
    .replace(/skillSlotUnlocked: \{fileID: [^}]+\}/, `skillSlotUnlocked: {fileID: 21300000, guid: ${UNLOCKED}, type: 3}`)
    .replace(/skillSlotLocked: \{fileID: [^}]+\}/, `skillSlotLocked: {fileID: 21300000, guid: ${LOCKED}, type: 3}`);
} else {
  menu.body = menu.body.replace(
    /skillEquipCloseButton: \{fileID: (\d+)\}/,
    `skillEquipCloseButton: {fileID: $1}\n  skillSlotUnlocked: {fileID: 21300000, guid: ${UNLOCKED}, type: 3}\n  skillSlotLocked: {fileID: 21300000, guid: ${LOCKED}, type: 3}`,
  );
}
sync(menu);

const out = scene.header + scene.blocks.map((b) => b.raw ?? `--- !u!${b.type} &${b.id}\n${b.body}`).join("");
fs.writeFileSync(SCENE, out);
console.log("slots", slotViews);
console.log("pool", poolViews);
