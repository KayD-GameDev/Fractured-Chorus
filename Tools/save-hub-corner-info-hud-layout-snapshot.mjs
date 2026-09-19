import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = `${ROOT}/Assets/FracturedChorus/Scenes/CampusHub.unity`;
const OUT = `${ROOT}/Assets/FracturedChorus/Art/UI/TownMap/hub_corner_info_hud_layout_snapshot.json`;

function parseScene(text) {
  const parts = text.split(/^--- !u!/m);
  const byId = new Map();
  for (let i = 1; i < parts.length; i++) {
    const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
    if (!m) continue;
    byId.set(m[2], { type: m[1], id: m[2], body: m[3] });
  }
  return byId;
}

function goName(byId, goId) {
  return byId.get(goId)?.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}

function goComponents(byId, goId) {
  return [...(byId.get(goId)?.body.matchAll(/component: \{fileID: (\d+)\}/g) || [])].map((m) => m[1]);
}

function findGoByName(byId, name) {
  for (const [id, b] of byId) {
    if (b.type === "1" && b.body.match(new RegExp(`\\n  m_Name: ${name}\\s*\\n`))) return id;
  }
  return null;
}

function xfOfGo(byId, goId) {
  for (const cid of goComponents(byId, goId)) {
    const c = byId.get(cid);
    if (c?.type === "224") return { id: cid, kind: "RectTransform" };
  }
  return null;
}

function goOfComponent(byId, compId) {
  for (const [gid, g] of byId) {
    if (g.type === "1" && g.body.includes(`component: {fileID: ${compId}}`)) return gid;
  }
  return null;
}

function getChildren(byId, rtId) {
  const b = byId.get(rtId)?.body ?? "";
  const m = b.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!m) return [];
  return [...m[1].matchAll(/fileID: (\d+)/g)].map((x) => x[1]);
}

function vec(body, key) {
  const m = body.match(
    new RegExp(`${key}: \\{x: ([^,}]+), y: ([^,}]+)(?:, z: ([^,}]+))?(?:, w: ([^}]+))?\\}`),
  );
  if (!m) return { x: 0, y: 0 };
  const out = { x: Number(m[1]), y: Number(m[2]) };
  if (m[3] != null) out.z = Number(m[3]);
  if (m[4] != null) out.w = Number(m[4]);
  return out;
}

function colorFromImage(byId, goId) {
  for (const cid of goComponents(byId, goId)) {
    const c = byId.get(cid);
    if (!c?.body.includes("UnityEngine.UI.Image")) continue;
    const m = c.body.match(/m_Color: \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}/);
    if (m) return { r: Number(m[1]), g: Number(m[2]), b: Number(m[3]), a: Number(m[4]) };
  }
  return null;
}

function textMeta(byId, goId) {
  for (const cid of goComponents(byId, goId)) {
    const c = byId.get(cid);
    if (!c?.body.includes("UnityEngine.UI.Text")) continue;
    return {
      text: c.body.match(/\n  m_Text: (.*)/)?.[1] ?? "",
      fontSize: Number(c.body.match(/m_FontSize: (\d+)/)?.[1] ?? 0),
      fontStyle: Number(c.body.match(/m_FontStyle: (\d+)/)?.[1] ?? 0),
      alignment: Number(c.body.match(/m_Alignment: (\d+)/)?.[1] ?? 0),
      color: (() => {
        const m = c.body.match(/m_Color: \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}/);
        return m
          ? { r: Number(m[1]), g: Number(m[2]), b: Number(m[3]), a: Number(m[4]) }
          : { r: 1, g: 1, b: 1, a: 1 };
      })(),
    };
  }
  return null;
}

const byId = parseScene(fs.readFileSync(SCENE, "utf8"));
const hudGo = findGoByName(byId, "HubCornerInfoHud");
if (!hudGo) throw new Error("HubCornerInfoHud missing in scene");

const nodes = [];
const walk = (goId, pathStr, siblingIndex) => {
  const xf = xfOfGo(byId, goId);
  const xfBlock = xf ? byId.get(xf.id) : null;
  const go = byId.get(goId);
  const entry = {
    path: pathStr,
    siblingIndex,
    activeSelf: /m_IsActive: 1/.test(go.body),
  };
  if (xfBlock) {
    entry.anchorMin = vec(xfBlock.body, "m_AnchorMin");
    entry.anchorMax = vec(xfBlock.body, "m_AnchorMax");
    entry.anchoredPosition = vec(xfBlock.body, "m_AnchoredPosition");
    entry.sizeDelta = vec(xfBlock.body, "m_SizeDelta");
    entry.pivot = vec(xfBlock.body, "m_Pivot");
  }
  const imgColor = colorFromImage(byId, goId);
  if (imgColor) entry.imageColor = imgColor;
  const txt = textMeta(byId, goId);
  if (txt) Object.assign(entry, { textMeta: txt });
  nodes.push(entry);
  if (!xf) return;
  getChildren(byId, xf.id).forEach((childRt, i) => {
    const childGo = goOfComponent(byId, childRt);
    if (childGo) walk(childGo, `${pathStr}/${goName(byId, childGo)}`, i);
  });
};

walk(hudGo, "HubCornerInfoHud", 0);

fs.mkdirSync(path.dirname(OUT), { recursive: true });
fs.writeFileSync(
  OUT,
  JSON.stringify(
    {
      scene: "Assets/FracturedChorus/Scenes/CampusHub.unity",
      savedAtUtc: new Date().toISOString(),
      note: "Reference backup only. Layout SoT is CampusHub.unity — do not re-apply from runtime code.",
      nodes,
    },
    null,
    2,
  ) + "\n",
);

console.log(JSON.stringify({ nodes: nodes.length, out: OUT.replace(`${ROOT}/`, "") }, null, 2));
