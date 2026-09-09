import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";
const PANEL = "eb33cb5a4a3526d5a15eefdb48ef2268"; // ui_stat_panel_memory_v2
const ROW = "b3ce58374d87b2d7d18e2bcb1c1e194d"; // ui_stat_slot_skill_v2
const LOCKED = "6b9e2c4d8f1a4703b6d0e5c2a8f3147e"; // ui_stat_slot_skill_locked_v1
const FONT_DISPLAY = "d4e5f6a7b8c94091a2b3c4d5e6f70891";
const FONT_BODY = "787fda44816c480a9cc3cadfdc29ce24";
const TITLE_COLOR = "m_Color: {r: 0.227451, g: 0.258824, b: 0.4, a: 1}";
const LABEL_COLOR = "m_Color: {r: 1, g: 1, b: 1, a: 1}";

const text = fs.readFileSync(SCENE, "utf8");
const header = text.match(/^[\s\S]*?(?=^--- !u!)/m)?.[0] ?? "";
const parts = text.split(/^--- !u!/m);
const blocks = [];
for (let i = 1; i < parts.length; i++) {
  const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
  if (!m) continue;
  blocks.push({ type: m[1], id: m[2], body: m[3] });
}
const byId = new Map(blocks.map((b) => [b.id, b]));

function nextId() {
  let max = 0;
  for (const id of byId.keys()) {
    const n = Number(id);
    if (Number.isFinite(n) && n < 2e9 && n > max) max = n;
  }
  return String(max + 1);
}

function goName(id) {
  return byId.get(id)?.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}
function comps(id) {
  return [...(byId.get(id)?.body.matchAll(/- component: \{fileID: (\d+)\}/g) ?? [])].map((x) => x[1]);
}
function findGo(name) {
  for (const [id, b] of byId) if (b.type === "1" && goName(id) === name) return id;
  return null;
}
function childGos(goId) {
  const rt = comps(goId).find((c) => byId.get(c)?.type === "224");
  const kids = [...(byId.get(rt)?.body.matchAll(/- \{fileID: (\d+)\}/g) ?? [])].map((x) => x[1]);
  const out = [];
  for (const [gid, g] of byId) {
    if (g.type !== "1") continue;
    const crt = comps(gid).find((c) => byId.get(c)?.type === "224");
    if (kids.includes(crt)) out.push({ gid, name: goName(gid), rt: crt });
  }
  return out;
}

function setImageSprite(goId, guid) {
  for (const cid of comps(goId)) {
    const b = byId.get(cid);
    if (!b?.body.includes("UnityEngine.UI.Image")) continue;
    b.body = b.body
      .replace(/m_Sprite: \{fileID: [^}]+\}/, `m_Sprite: {fileID: 21300000, guid: ${guid}, type: 3}`)
      .replace(/m_Type: \d+/, "m_Type: 0")
      .replace(/m_PreserveAspect: \d+/, "m_PreserveAspect: 0")
      .replace(/m_Color: \{[^}]+\}/, "m_Color: {r: 1, g: 1, b: 1, a: 1}");
  }
}

function styleText(goId, { font, color, size, style }) {
  for (const cid of comps(goId)) {
    const b = byId.get(cid);
    if (!b?.body.includes("UnityEngine.UI.Text")) continue;
    b.body = b.body
      .replace(/m_Font: \{fileID: [^}]+\}/, `m_Font: {fileID: 12800000, guid: ${font}, type: 3}`)
      .replace(/m_Color: \{[^}]+\}/, color)
      .replace(/m_FontSize: \d+/, `m_FontSize: ${size}`)
      .replace(/m_FontStyle: \d+/, `m_FontStyle: ${style}`)
      .replace(/m_BestFit: \d+/, "m_BestFit: 1")
      .replace(/m_MinSize: \d+/, "m_MinSize: 8")
      .replace(/m_MaxSize: \d+/, `m_MaxSize: ${size}`);
  }
}

function addBlock(type, id, body) {
  const b = { type, id, body };
  blocks.push(b);
  byId.set(id, b);
  return b;
}

function ensureLabelBg(poolGoId) {
  const kids = childGos(poolGoId);
  let labelBg = kids.find((k) => k.name === "LabelBg");
  const label = kids.find((k) => k.name === "Label");
  const poolRt = comps(poolGoId).find((c) => byId.get(c)?.type === "224");
  if (!labelBg) {
    const idGo = nextId();
    const idRt = nextId();
    const idCr = nextId();
    const idImg = nextId();
    addBlock(
      "1",
      idGo,
      `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${idRt}}
  - component: {fileID: ${idCr}}
  - component: {fileID: ${idImg}}
  m_Layer: 0
  m_Name: LabelBg
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
`,
    );
    addBlock(
      "224",
      idRt,
      `RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${idGo}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${poolRt}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.06, y: 0.18}
  m_AnchorMax: {x: 0.94, y: 0.82}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
`,
    );
    addBlock(
      "222",
      idCr,
      `CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${idGo}}
  m_CullTransparentMesh: 1
`,
    );
    addBlock(
      "114",
      idImg,
      `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${idGo}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name: 
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 0.04, g: 0.06, b: 0.16, a: 0.55}
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
`,
    );

    const poolRtBlock = byId.get(poolRt);
    if (poolRtBlock.body.includes("m_Children: []")) {
      poolRtBlock.body = poolRtBlock.body.replace(
        "m_Children: []",
        `m_Children:\n  - {fileID: ${idRt}}`,
      );
    } else if (label?.rt) {
      poolRtBlock.body = poolRtBlock.body.replace(
        `- {fileID: ${label.rt}}`,
        `- {fileID: ${idRt}}\n  - {fileID: ${label.rt}}`,
      );
    } else {
      poolRtBlock.body = poolRtBlock.body.replace(
        "m_Children:\n",
        `m_Children:\n  - {fileID: ${idRt}}\n`,
      );
    }
    labelBg = { gid: idGo, name: "LabelBg", rt: idRt, img: idImg };
  }

  // wire view.frame + labelBackground
  const viewId = comps(poolGoId).find((c) => byId.get(c)?.body.includes("CharacterBuildEquipSlotView"));
  const frameImg = comps(poolGoId).find((c) => byId.get(c)?.body.includes("UnityEngine.UI.Image"));
  if (viewId) {
    const vb = byId.get(viewId);
    const imgId =
      labelBg.img ||
      comps(labelBg.gid).find((c) => byId.get(c)?.body.includes("UnityEngine.UI.Image"));
    if (frameImg) {
      if (vb.body.includes("frame:")) {
        vb.body = vb.body.replace(/frame: \{fileID: \d+\}/, `frame: {fileID: ${frameImg}}`);
      } else {
        vb.body = vb.body.replace(/(label: \{fileID: \d+\}\n)/, `$1  frame: {fileID: ${frameImg}}\n`);
      }
    }
    if (vb.body.includes("labelBackground:")) {
      vb.body = vb.body.replace(/labelBackground: \{fileID: \d+\}/, `labelBackground: {fileID: ${imgId}}`);
    } else {
      vb.body = vb.body.replace(
        /(frame: \{fileID: \d+\}\n)/,
        `$1  labelBackground: {fileID: ${imgId}}\n`,
      );
    }
  }
  return labelBg;
}

// Overlay panel
const overlay = findGo("SkillEquipOverlay");
setImageSprite(overlay, PANEL);

// Title
const title = childGos(overlay).find((c) => c.name === "Title");
if (title) styleText(title.gid, { font: FONT_DISPLAY, color: TITLE_COLOR, size: 22, style: 0 });

// Close label
const close = childGos(overlay).find((c) => c.name === "CloseButton");
if (close) {
  for (const c of childGos(close.gid)) {
    if (c.name === "Label") {
      styleText(c.gid, {
        font: FONT_DISPLAY,
        color: TITLE_COLOR,
        size: 18,
        style: 0,
      });
    }
  }
}

// Pool cells
const pools = childGos(overlay).filter((c) => /^EquipPool_\d+$/.test(c.name));
for (const p of pools) {
  setImageSprite(p.gid, ROW);
  ensureLabelBg(p.gid);
  for (const c of childGos(p.gid)) {
    if (c.name === "Label") {
      styleText(c.gid, { font: FONT_BODY, color: LABEL_COLOR, size: 12, style: 0 });
      // stretch label a bit
      const rt = byId.get(c.rt);
      if (rt) {
        rt.body = rt.body
          .replace(/m_AnchorMin: \{x: [^}]+\}/, "m_AnchorMin: {x: 0.08, y: 0.2}")
          .replace(/m_AnchorMax: \{x: [^}]+\}/, "m_AnchorMax: {x: 0.92, y: 0.8}");
      }
    }
  }
}

// Wire menu sprites
for (const b of blocks) {
  if (!b.body.includes("skillSlotUnlocked:")) continue;
  b.body = b.body
    .replace(
      /skillSlotUnlocked: \{fileID: [^}]+\}/,
      `skillSlotUnlocked: {fileID: 21300000, guid: ${ROW}, type: 3}`,
    )
    .replace(
      /skillSlotLocked: \{fileID: [^}]+\}/,
      `skillSlotLocked: {fileID: 21300000, guid: ${LOCKED}, type: 3}`,
    );
}

const out =
  header +
  blocks.map((b) => `--- !u!${b.type} &${b.id}\n${b.body}`).join("").replace(/\n*$/, "\n");
fs.writeFileSync(SCENE, out);
console.log(
  JSON.stringify(
    {
      overlay,
      pools: pools.length,
      title: title?.gid,
      blocks: blocks.length,
    },
    null,
    2,
  ),
);
