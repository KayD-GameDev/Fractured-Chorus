import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";
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

function goName(id) {
  return byId.get(id)?.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}

function comps(id) {
  return [...(byId.get(id)?.body.matchAll(/- component: \{fileID: (\d+)\}/g) ?? [])].map((x) => x[1]);
}

function childrenOfRt(rtId) {
  const body = byId.get(rtId)?.body ?? "";
  return [...body.matchAll(/- \{fileID: (\d+)\}/g)].map((x) => x[1]);
}

function collectSubtree(goId, out) {
  if (!goId || out.has(goId)) return;
  out.add(goId);
  for (const cid of comps(goId)) {
    out.add(cid);
    if (byId.get(cid)?.type === "224") {
      for (const childRt of childrenOfRt(cid)) {
        for (const [gid, g] of byId) {
          if (g.type === "1" && comps(gid).includes(childRt)) {
            collectSubtree(gid, out);
          }
        }
      }
    }
  }
}

const remove = new Set();
const equipSlotRtIds = [];
for (const b of blocks) {
  if (b.type !== "1") continue;
  const name = goName(b.id);
  if (!name || !/^EquipSlot_\d+$/.test(name)) continue;
  const rt = comps(b.id).find((cid) => byId.get(cid)?.type === "224");
  if (rt) equipSlotRtIds.push(rt);
  collectSubtree(b.id, remove);
}

if (equipSlotRtIds.length === 0) {
  console.log(JSON.stringify({ removed: 0, note: "no EquipSlot_* found" }));
  process.exit(0);
}

for (const b of blocks) {
  if (b.type !== "224" || !b.body.includes("m_Children:")) continue;
  const lines = b.body.split("\n");
  const next = [];
  let inChildren = false;
  for (const line of lines) {
    if (line.startsWith("  m_Children:")) {
      inChildren = true;
      next.push(line);
      continue;
    }
    if (inChildren) {
      const m = line.match(/^\s+- \{fileID: (\d+)\}$/);
      if (m) {
        if (equipSlotRtIds.includes(m[1]) || remove.has(m[1])) continue;
        next.push(line);
        continue;
      }
      inChildren = false;
    }
    next.push(line);
  }
  if (next.join("\n") !== b.body) {
    const childLines = next.filter((l, i, arr) => {
      const prev = arr[i - 1];
      return prev?.startsWith("  m_Children:") || (prev && /^\s+- \{fileID:/.test(prev) && /^\s+- \{fileID:/.test(l));
    });
    // normalize empty children
    const idx = next.findIndex((l) => l.startsWith("  m_Children:"));
    if (idx >= 0) {
      let j = idx + 1;
      while (j < next.length && /^\s+- \{fileID:/.test(next[j])) j++;
      const kids = next.slice(idx + 1, j);
      if (kids.length === 0) {
        next[idx] = "  m_Children: []";
      }
    }
    b.body = next.join("\n");
  }
}

for (const b of blocks) {
  if (!b.body.includes("equipSlotViews:")) continue;
  b.body = b.body.replace(
    /equipSlotViews:\n(?:  - \{fileID: \d+\}\n)*/,
    "equipSlotViews: []\n",
  );
}

const kept = blocks.filter((b) => !remove.has(b.id));
const out =
  header +
  kept.map((b) => `--- !u!${b.type} &${b.id}\n${b.body}`).join("").replace(/\n*$/, "\n");
fs.writeFileSync(SCENE, out);
console.log(
  JSON.stringify({
    removedIds: [...remove].length,
    equipSlots: equipSlotRtIds.length,
    blocksBefore: blocks.length,
    blocksAfter: kept.length,
  }),
);
