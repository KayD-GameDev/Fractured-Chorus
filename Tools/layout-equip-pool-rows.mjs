import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";

function poolBox(index) {
  const col = Math.floor(index / 5);
  const row = index % 5;
  const yMax0 = 0.86;
  const rowH = 0.108;
  const gapY = 0.022;
  const yMax = yMax0 - row * (rowH + gapY);
  const yMin = yMax - rowH;
  return col === 0
    ? { xmin: 0.06, xmax: 0.46, ymin: yMin, ymax: yMax }
    : { xmin: 0.54, xmax: 0.94, ymin: yMin, ymax: yMax };
}

const text = fs.readFileSync(SCENE, "utf8");
const parts = text.split(/^--- !u!/m);
const byId = new Map();
for (let i = 1; i < parts.length; i++) {
  const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
  if (!m) continue;
  byId.set(m[2], { type: m[1], id: m[2], body: m[3], idx: i });
}

function goName(id) {
  return byId.get(id)?.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}
function comps(id) {
  return [...(byId.get(id)?.body.matchAll(/- component: \{fileID: (\d+)\}/g) ?? [])].map((x) => x[1]);
}
function goOf(compId) {
  for (const [gid, g] of byId.entries()) {
    if (g.type === "1" && comps(gid).includes(compId)) return gid;
  }
  return null;
}

const poolGo = new Map();
for (const [id, b] of byId.entries()) {
  if (b.type !== "1") continue;
  const n = goName(id);
  const m = n && n.match(/^EquipPool_(\d+)$/);
  if (m) poolGo.set(Number(m[1]), id);
}

if (poolGo.size !== 10) {
  throw new Error(`expected 10 pool slots, found ${poolGo.size}`);
}

for (const [index, goId] of poolGo) {
  const box = poolBox(index);
  for (const cid of comps(goId)) {
    const c = byId.get(cid);
    if (c.type === "224") {
      c.body = c.body
        .replace(/m_AnchorMin: \{x: [^}]+\}/, `m_AnchorMin: {x: ${box.xmin}, y: ${box.ymin}}`)
        .replace(/m_AnchorMax: \{x: [^}]+\}/, `m_AnchorMax: {x: ${box.xmax}, y: ${box.ymax}}`)
        .replace(/m_AnchoredPosition: \{x: [^}]+\}/, "m_AnchoredPosition: {x: 0, y: 0}")
        .replace(/m_SizeDelta: \{x: [^}]+\}/, "m_SizeDelta: {x: 0, y: 0}");

      const childRts = [...c.body.matchAll(/- \{fileID: (\d+)\}/g)].map((x) => x[1]);
      for (const childRt of childRts) {
        const childGo = goOf(childRt);
        const name = goName(childGo);
        if (name === "Label") {
          const lr = byId.get(childRt);
          if (lr?.type === "224") {
            lr.body = lr.body
              .replace(/m_AnchorMin: \{x: [^}]+\}/, "m_AnchorMin: {x: 0.08, y: 0.18}")
              .replace(/m_AnchorMax: \{x: [^}]+\}/, "m_AnchorMax: {x: 0.92, y: 0.82}");
          }
          for (const lid of comps(childGo)) {
            const t = byId.get(lid);
            if (t?.body.includes("UnityEngine.UI.Text")) {
              t.body = t.body
                .replace(/m_FontSize: \d+/, "m_FontSize: 18")
                .replace(/m_FontStyle: \d+/, "m_FontStyle: 0")
                .replace(/m_BestFit: \d+/, "m_BestFit: 1")
                .replace(/m_MinSize: \d+/, "m_MinSize: 12")
                .replace(/m_MaxSize: \d+/, "m_MaxSize: 18")
                .replace(/m_Color: \{[^}]+\}/, "m_Color: {r: 1, g: 1, b: 1, a: 1}");
            }
          }
        }
      }
    }
  }
}

const out =
  parts[0] +
  parts
    .slice(1)
    .map((p) => {
      const m = p.match(/^(\d+) &(\d+)\n([\s\S]*)$/);
      if (!m) return "--- !u!" + p;
      const b = byId.get(m[2]);
      return `--- !u!${b.type} &${b.id}\n${b.body}`;
    })
    .join("");

fs.writeFileSync(SCENE, out);
console.log(
  [...poolGo.keys()]
    .sort((a, b) => a - b)
    .map((i) => `${i} ${JSON.stringify(poolBox(i))}`)
    .join("\n"),
);
