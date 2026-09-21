import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";

const text = fs.readFileSync(SCENE, "utf8");
const parts = text.split(/^--- !u!/m);
const header = parts[0];
const blocks = [];
for (let i = 1; i < parts.length; i++) {
  const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
  if (!m) continue;
  blocks.push({ type: m[1], id: m[2], body: m[3] });
}
const byId = new Map(blocks.map((b) => [b.id, b]));

function goName(id) {
  return byId.get(id)?.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}
function comps(id) {
  return [...(byId.get(id)?.body.matchAll(/component: \{fileID: (\d+)\}/g) || [])].map((x) => x[1]);
}
function goOf(c) {
  for (const [gid, g] of byId) {
    if (g.type === "1" && g.body.includes(`component: {fileID: ${c}}`)) return gid;
  }
  return null;
}
function children(rt) {
  const b = byId.get(rt)?.body ?? "";
  const m = b.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!m) return [];
  return [...m[1].matchAll(/fileID: (\d+)/g)].map((x) => x[1]);
}
function xf(go) {
  for (const c of comps(go)) {
    const t = byId.get(c);
    if (t?.type === "224") return c;
  }
  return null;
}
function findGo(name) {
  for (const [id, b] of byId) {
    if (b.type === "1" && b.body.match(new RegExp(`\\n  m_Name: ${name}\\n`))) return id;
  }
  return null;
}

function getTailRt(btnName, listName) {
  const list = findGo(listName);
  if (!list) return null;
  const lrt = xf(list);
  for (const crt of children(lrt)) {
    const btn = goOf(crt);
    if (goName(btn) !== btnName) continue;
    for (const t of children(crt)) {
      const cgo = goOf(t);
      if (goName(cgo) === "TailEdgeFx") {
        return { tailRt: t, body: byId.get(t).body };
      }
    }
  }
  return null;
}

function extractLayout(body) {
  const keys = [
    "m_LocalRotation",
    "m_LocalPosition",
    "m_LocalScale",
    "m_AnchorMin",
    "m_AnchorMax",
    "m_AnchoredPosition",
    "m_SizeDelta",
    "m_Pivot",
    "m_LocalEulerAnglesHint",
  ];
  const out = {};
  for (const k of keys) {
    const m = body.match(new RegExp(`  ${k}: \\{[^}]+\\}`));
    if (m) out[k] = m[0];
  }
  return out;
}

function applyLayout(body, layout) {
  let next = body;
  for (const [k, line] of Object.entries(layout)) {
    next = next.replace(new RegExp(`  ${k}: \\{[^}]+\\}`), line);
  }
  return next;
}

const map = [
  ["BtnStats", "BtnSave"],
  ["BtnBonds", "BtnLoad"],
  ["BtnCalendar", "BtnConfig"],
  ["BtnSystem", "BtnReturnToTitle"],
];

for (const [srcBtn, dstBtn] of map) {
  const src = getTailRt(srcBtn, "MenuList");
  const dst = getTailRt(dstBtn, "SystemMenuList");
  if (!src || !dst) {
    console.error("missing", srcBtn, "->", dstBtn, { src: !!src, dst: !!dst });
    continue;
  }
  const layout = extractLayout(src.body);
  console.log(
    `${srcBtn} -> ${dstBtn}`,
    layout.m_AnchoredPosition,
    layout.m_SizeDelta,
  );
  byId.get(dst.tailRt).body = applyLayout(dst.body, layout);
}

const out = [header];
for (const b of blocks) {
  const cur = byId.get(b.id);
  out.push(`--- !u!${cur.type} &${cur.id}\n${cur.body}`);
}
fs.writeFileSync(SCENE, out.join(""));
console.log("patched");
