import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";
const REMOVE = new Set([
  "930040010", "930040011", "930040012", "930040013",
  "930040020", "930040021", "930040022", "930040023",
]);

let scene = fs.readFileSync(SCENE, "utf8");
const parts = scene.split(/^--- !u!/m);
const kept = [parts[0]];
for (let i = 1; i < parts.length; i++) {
  const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
  if (!m) continue;
  if (REMOVE.has(m[2])) continue;
  kept.push(`--- !u!${m[1]} &${m[2]}\n${m[3]}`);
}
scene = kept.join("");

for (const id of ["930040011", "930040021"]) {
  scene = scene.replace(new RegExp(`  - \\{fileID: ${id}\\}\\n`, "g"), "");
}

scene = scene.replace(/(\n  m_Name: BgSunClouds[\s\S]*?\n  m_IsActive: )1/g, "$10");
scene = scene.replace(/(\n  m_Name: BgDark[\s\S]*?\n  m_IsActive: )0/g, "$11");
scene = scene.replace(/(\n  m_Name: BgLight[\s\S]*?\n  m_IsActive: )0/g, "$11");

fs.writeFileSync(SCENE, scene);
console.log("Removed BgSunClouds blocks; restored BgDark/BgLight.");
