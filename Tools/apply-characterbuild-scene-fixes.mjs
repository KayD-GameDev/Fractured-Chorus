import fs from "fs";

const SCENES = [
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity",
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuildLayoutSandbox.unity",
];

const GUID = {
  note: "3d5a82200cc0672ff0def2d965931088",
  orb: "0daee440cc836f472cadb0b11439eee8",
  plus: "b1cf6fa5eacbadd8a2a75ba84302ddec",
  minus: "857dbc685c7ad85f072bf87db6eb5f06",
  image: "fe87c0e1cc204ed48ad3b37840f39efc",
  button: "4e29b1a8efbd4b44bb3f3716e73f07ff",
  text: "5f7201a12d95ffc409449d95f23cf332",
  statRow: "4f3e8c32141862b4297e2dd9bad0f582",
  skillRow: "c7f500d59c469f24d9987c63d69beb30",
};

const STAT_ROWS = ["StatRow_Strength", "StatRow_Magic", "StatRow_Endurance", "StatRow_HeartBeat"];
const FONT = "{fileID: 12800000, guid: d4e5f6a7b8c94091a2b3c4d5e6f70891, type: 3}";

let nextId = 992001000;
const allocId = () => ++nextId;

function parseScene(text) {
  const parts = text.split(/^--- !u!/m);
  const header = parts[0];
  const blocks = [];
  const byId = new Map();
  for (let i = 1; i < parts.length; i++) {
    const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
    if (!m) continue;
    const block = { type: m[1], id: m[2], body: m[3], raw: `--- !u!${m[1]} &${m[2]}\n${m[3]}` };
    blocks.push(block);
    byId.set(block.id, block);
  }
  return { header, blocks, byId };
}

function serializeScene({ header, blocks }) {
  return header + blocks.map((b) => b.raw).join("");
}

function goName(byId, goId) {
  const b = byId.get(goId);
  if (!b || b.type !== "1") return null;
  return b.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}

function goComponents(byId, goId) {
  const b = byId.get(goId);
  if (!b || b.type !== "1") return [];
  return [...b.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((m) => m[1]);
}

function findGoByName(byId, name) {
  for (const b of byId.values()) {
    if (b.type !== "1") continue;
    if (b.body.match(new RegExp(`\\n  m_Name: ${name}\\s*\\n`))) return b.id;
  }
  return null;
}

function rtOfGo(byId, goId) {
  for (const cid of goComponents(byId, goId)) {
    if (byId.get(cid)?.type === "224") return cid;
  }
  return null;
}

function goOfComponent(byId, compId) {
  for (const [gid, g] of byId.entries()) {
    if (g.type !== "1") continue;
    if (g.body.includes(`component: {fileID: ${compId}}`)) return gid;
  }
  return null;
}

function getChildren(byId, rtId) {
  const body = byId.get(rtId)?.body ?? "";
  const m = body.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!m) return [];
  return [...m[1].matchAll(/fileID: (\d+)/g)].map((x) => x[1]);
}

function setChildren(block, childIds) {
  const lines = childIds.map((id) => `  - {fileID: ${id}}`).join("\n");
  block.body = block.body.replace(/\n  m_Children:\n(?:  - \{fileID: \d+\}\n)*/g, `\n  m_Children:\n${lines}\n`);
  block.raw = `--- !u!${block.type} &${block.id}\n${block.body}`;
}

function patchField(block, field, value) {
  const re = new RegExp(`\\n  ${field}: \\{fileID: [^}]+\\}`);
  if (block.body.match(re)) {
    block.body = block.body.replace(re, `\n  ${field}: {fileID: ${value}}`);
  } else if (field === "noteSlot" || field === "iconFrame") {
    block.body = block.body.replace(/(\n  icon: \{fileID: \d+\})/, `\n  ${field}: {fileID: ${value}}$1`);
  } else {
    block.body = block.body.replace(
      /(\n  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus\.Hub\.CharacterBuild\.CharacterBuildStatRowView\n)/,
      `$1  ${field}: {fileID: ${value}}\n`,
    );
  }
  block.raw = `--- !u!${block.type} &${block.id}\n${block.body}`;
}

function pushBlock(blocks, byId, block) {
  blocks.push(block);
  byId.set(block.id, block);
}

function makeGo(name, compIds) {
  const goId = allocId();
  const comps = compIds.map((id) => `  - component: {fileID: ${id}}`).join("\n");
  const body = `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
${comps}
  m_Layer: 0
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
`;
  return { type: "1", id: String(goId), body, raw: `--- !u!1 &${goId}\n${body}` };
}

function makeRt(goId, fatherId, anchorMin, anchorMax) {
  const rtId = allocId();
  const body = `RectTransform:
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
  m_Father: {fileID: ${fatherId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${anchorMin[0]}, y: ${anchorMin[1]}}
  m_AnchorMax: {x: ${anchorMax[0]}, y: ${anchorMax[1]}}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
`;
  return { type: "224", id: String(rtId), body, raw: `--- !u!224 &${rtId}\n${body}` };
}

function makeCr(goId) {
  const id = allocId();
  const body = `CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
`;
  return { type: "222", id: String(id), body, raw: `--- !u!222 &${id}\n${body}` };
}

function makeImage(goId, spriteGuid, raycast = 0, empty = false) {
  const id = allocId();
  const spriteLine = empty
    ? "  m_Sprite: {fileID: 0}"
    : `  m_Sprite: {fileID: 21300000, guid: ${spriteGuid}, type: 3}`;
  const body = `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID.image}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: ${raycast}
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
${spriteLine}
  m_Type: 0
  m_PreserveAspect: 1
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
`;
  return { type: "114", id: String(id), body, raw: `--- !u!114 &${id}\n${body}` };
}

function makeButton(goId, imageId) {
  const id = allocId();
  const body = `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID.button}, type: 3}
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
  m_TargetGraphic: {fileID: ${imageId}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
`;
  return { type: "114", id: String(id), body, raw: `--- !u!114 &${id}\n${body}` };
}

function makeText(goId) {
  const id = allocId();
  const body = `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${GUID.text}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Text
  m_Material: {fileID: 0}
  m_Color: {r: 0.227, g: 0.259, b: 0.4, a: 1}
  m_RaycastTarget: 0
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: ${FONT}
    m_FontSize: 14
    m_FontStyle: 1
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: 4
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: 0
`;
  return { type: "114", id: String(id), body, raw: `--- !u!114 &${id}\n${body}` };
}

function imageCompOnGo(byId, goId) {
  for (const cid of goComponents(byId, goId)) {
    const c = byId.get(cid);
    if (c?.type === "114" && c.body.includes(GUID.image)) return cid;
  }
  return null;
}

function findChildGoByName(byId, parentRt, name) {
  for (const rt of getChildren(byId, parentRt)) {
    const go = goOfComponent(byId, rt);
    if (goName(byId, go) === name) return go;
  }
  return null;
}

function createAllocControls(scene, rowGo, rowRt) {
  const { blocks, byId } = scene;
  const allocGo = makeGo("AllocControls", []);
  const allocRt = makeRt(allocGo.id, rowRt, [0.82, 0.06], [0.99, 0.94]);
  allocGo.body = allocGo.body.replace("m_Component:\n", `m_Component:\n  - component: {fileID: ${allocRt.id}}\n`);
  allocGo.raw = `--- !u!1 &${allocGo.id}\n${allocGo.body}`;

  const mkBtn = (name, sprite, amin, amax) => {
    const goId = allocId();
    const rt = makeRt(goId, allocRt.id, amin, amax);
    const cr = makeCr(goId);
    const img = makeImage(goId, sprite, 1);
    const btn = makeButton(goId, img.id);
    const go = makeGo(name, [rt.id, cr.id, img.id, btn.id]);
    for (const b of [go, rt, cr, img, btn]) pushBlock(blocks, byId, b);
    return btn.id;
  };

  const minusBtn = mkBtn("MinusBtn", GUID.minus, [0, 0.08], [0.3, 0.92]);
  const spentGoId = allocId();
  const spentRt = makeRt(spentGoId, allocRt.id, [0.32, 0.08], [0.68, 0.92]);
  const spentCr = makeCr(spentGoId);
  const spentTxt = makeText(spentGoId);
  const spentGo = makeGo("SpentLabel", [spentRt.id, spentCr.id, spentTxt.id]);
  const plusBtn = mkBtn("PlusBtn", GUID.plus, [0.7, 0.08], [1, 0.92]);

  for (const b of [allocGo, allocRt, spentGo, spentRt, spentCr, spentTxt]) pushBlock(blocks, byId, b);

  const children = getChildren(byId, rowRt);
  children.push(allocRt.id);
  setChildren(byId.get(rowRt), children);

  for (const cid of goComponents(byId, rowGo)) {
    const c = byId.get(cid);
    if (c?.body.includes(GUID.statRow)) {
      patchField(c, "minusButton", minusBtn);
      patchField(c, "spentLabel", spentTxt.id);
      patchField(c, "plusButton", plusBtn);
      patchField(c, "allocControlsRoot", allocGo.id);
    }
  }
}

function tuneAllocControls(scene) {
  const { byId } = scene;
  for (const rowName of STAT_ROWS) {
    const rowGo = findGoByName(byId, rowName);
    if (!rowGo) continue;
    const rowRt = rtOfGo(byId, rowGo);
    const allocGo = findChildGoByName(byId, rowRt, "AllocControls");
    if (!allocGo) {
      createAllocControls(scene, rowGo, rowRt);
      continue;
    }
    const allocRt = rtOfGo(byId, allocGo);
    const rt = byId.get(allocRt);
    rt.body = rt.body
      .replace(/m_AnchorMin: \{x: [^}]+\}/, "m_AnchorMin: {x: 0.82, y: 0.06}")
      .replace(/m_AnchorMax: \{x: [^}]+\}/, "m_AnchorMax: {x: 0.99, y: 0.94}");
    rt.raw = `--- !u!224 &${rt.id}\n${rt.body}`;

    const children = getChildren(byId, rowRt).filter((id) => id !== allocRt);
    children.push(allocRt);
    setChildren(byId.get(rowRt), children);

    for (const childRt of getChildren(byId, allocRt)) {
      const name = goName(byId, goOfComponent(byId, childRt));
      const c = byId.get(childRt);
      if (name === "PlusBtn") {
        c.body = c.body
          .replace(/m_AnchorMin: \{x: [^}]+\}/, "m_AnchorMin: {x: 0.70, y: 0.08}")
          .replace(/m_AnchorMax: \{x: [^}]+\}/, "m_AnchorMax: {x: 1, y: 0.92}");
      }
      if (name === "MinusBtn") {
        c.body = c.body
          .replace(/m_AnchorMin: \{x: [^}]+\}/, "m_AnchorMin: {x: 0, y: 0.08}")
          .replace(/m_AnchorMax: \{x: [^}]+\}/, "m_AnchorMax: {x: 0.30, y: 0.92}");
      }
      c.raw = `--- !u!224 &${c.id}\n${c.body}`;
    }

    const valueGo = findChildGoByName(byId, rowRt, "ValueLabel");
    if (valueGo) {
      const v = byId.get(rtOfGo(byId, valueGo));
      v.body = v.body
        .replace(/m_AnchorMin: \{x: [^}]+\}/, "m_AnchorMin: {x: 0.68, y: 0.42}")
        .replace(/m_AnchorMax: \{x: [^}]+\}/, "m_AnchorMax: {x: 0.80, y: 0.96}");
      v.raw = `--- !u!224 &${v.id}\n${v.body}`;
    }
  }
}

function ensureSkillOrbs(scene) {
  const { blocks, byId } = scene;
  let count = 0;
  for (const b of byId.values()) {
    if (b.type !== "1" || goName(byId, b.id) !== "NoteCircle") continue;
    const noteGo = b.id;
    const noteRt = rtOfGo(byId, noteGo);
    const noteImgId = imageCompOnGo(byId, noteGo);
    if (noteImgId) {
      const img = byId.get(noteImgId);
      img.body = img.body
        .replace(/m_Sprite: \{fileID: 21300000, guid: [a-f0-9]+, type: 3\}/, `m_Sprite: {fileID: 21300000, guid: ${GUID.note}, type: 3}`)
        .replace(/m_RaycastTarget: 1/, "m_RaycastTarget: 0");
      img.raw = `--- !u!114 &${img.id}\n${img.body}`;
    }

    let iconImgId = null;
    let frameImgId = noteImgId;

    if (!findChildGoByName(byId, noteRt, "SkillIcon")) {
      const iconGoId = allocId();
      const iconRt = makeRt(iconGoId, noteRt, [0.2, 0.2], [0.8, 0.8]);
      const iconCr = makeCr(iconGoId);
      const iconImg = makeImage(iconGoId, GUID.note, 0, true);
      const iconGo = makeGo("SkillIcon", [iconRt.id, iconCr.id, iconImg.id]);
      for (const x of [iconGo, iconRt, iconCr, iconImg]) pushBlock(blocks, byId, x);
      const kids = getChildren(byId, noteRt);
      kids.push(iconRt.id);
      setChildren(byId.get(noteRt), kids);
      iconImgId = iconImg.id;
    } else {
      iconImgId = imageCompOnGo(byId, findChildGoByName(byId, noteRt, "SkillIcon"));
    }

    if (!findChildGoByName(byId, noteRt, "SkillOrbFrame")) {
      const frameGoId = allocId();
      const frameRt = makeRt(frameGoId, noteRt, [0.06, 0.06], [0.94, 0.94]);
      const frameCr = makeCr(frameGoId);
      const frameImg = makeImage(frameGoId, GUID.orb, 0);
      const frameGo = makeGo("SkillOrbFrame", [frameRt.id, frameCr.id, frameImg.id]);
      for (const x of [frameGo, frameRt, frameCr, frameImg]) pushBlock(blocks, byId, x);
      const kids = getChildren(byId, noteRt);
      kids.push(frameRt.id);
      setChildren(byId.get(noteRt), kids);
      frameImgId = frameImg.id;
    } else {
      frameImgId = imageCompOnGo(byId, findChildGoByName(byId, noteRt, "SkillOrbFrame"));
    }

    const fatherRt = byId.get(noteRt).body.match(/m_Father: \{fileID: (\d+)\}/)?.[1];
    const slotGo = fatherRt ? goOfComponent(byId, fatherRt) : null;
    if (slotGo) {
      for (const cid of goComponents(byId, slotGo)) {
        const c = byId.get(cid);
        if (!c?.body.includes(GUID.skillRow)) continue;
        if (noteImgId) patchField(c, "noteSlot", noteImgId);
        if (frameImgId) patchField(c, "iconFrame", frameImgId);
        if (iconImgId) patchField(c, "icon", iconImgId);
      }
    }
    count++;
  }
  return count;
}

for (const path of SCENES) {
  if (!fs.existsSync(path)) continue;
  nextId = path.includes("Sandbox") ? 993001000 : 992001000;
  const scene = parseScene(fs.readFileSync(path, "utf8"));
  tuneAllocControls(scene);
  const orbs = ensureSkillOrbs(scene);
  fs.writeFileSync(path, serializeScene(scene));
  console.log(path.split("/").pop(), "skill-orbs:", orbs);
}
