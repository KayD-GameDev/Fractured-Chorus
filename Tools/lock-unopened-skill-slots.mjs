import fs from "fs";

const scenePath =
  process.argv[2] ||
  "d:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";
const lockedGuid = "6b9e2c4d8f1a4703b6d0e5c2a8f3147e";
const unlockedGuid = "b3ce58374d87b2d7d18e2bcb1c1e194d";
const includePool = !process.argv.includes("--skills-only");

const targets = new Set([
  "SkillSlot_4",
  "SkillSlot_5",
  "SkillSlot_6",
  "SkillSlot_7",
  "SkillSlot_8",
  "SkillSlot_9",
  "SkillSlot_10",
  ...(includePool
    ? [
        "EquipPool_3",
        "EquipPool_4",
        "EquipPool_5",
        "EquipPool_6",
        "EquipPool_7",
        "EquipPool_8",
        "EquipPool_9",
      ]
    : []),
]);

const text = fs.readFileSync(scenePath, "utf8");
if (!text.startsWith("%YAML")) {
  throw new Error(`Refuse to patch corrupt scene (missing YAML header): ${scenePath}`);
}

const unlockedSprite = `m_Sprite: {fileID: 21300000, guid: ${unlockedGuid}, type: 3}`;
const lockedSprite = `m_Sprite: {fileID: 21300000, guid: ${lockedGuid}, type: 3}`;

const idxs = [];
const re = /^--- !u!1 &/gm;
let m;
while ((m = re.exec(text))) {
  idxs.push(m.index);
}

const out = [];
let changed = 0;

if (idxs.length === 0) {
  throw new Error("no GameObjects found");
}

out.push(text.slice(0, idxs[0]));

for (let k = 0; k < idxs.length; k++) {
  const start = idxs[k];
  const end = k + 1 < idxs.length ? idxs[k + 1] : text.length;
  let block = text.slice(start, end);
  const nameM = block.match(/m_Name: ([^\r\n]+)/);
  const name = nameM ? nameM[1].trim() : "";
  if (targets.has(name)) {
    const before = block;
    block = block.split(unlockedSprite).join(lockedSprite);
    if (block !== before) {
      changed += 1;
      console.log("locked", name);
    } else {
      console.log("unchanged", name);
    }
  }
  out.push(block);
}

const result = out.join("");
if (!result.startsWith("%YAML")) {
  throw new Error("internal error: lost YAML header");
}
if ((result.match(/^--- !u!1 &/gm) || []).length !== idxs.length) {
  throw new Error("internal error: GO count mismatch");
}

fs.writeFileSync(scenePath, result);
console.log("total", changed, "size", result.length);
