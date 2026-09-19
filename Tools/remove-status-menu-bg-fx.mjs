import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";
const REMOVE_IDS = new Set([
  "930050001",
  "930050010", "930050011", "930050012", "930050013",
  "930050020", "930050021", "930050022", "930050023",
  "930050030", "930050031", "930050032", "930050033",
]);

let scene = fs.readFileSync(SCENE, "utf8");
const parts = scene.split(/^--- !u!/m);
const header = parts[0];
const kept = [header];

for (let i = 1; i < parts.length; i++) {
  const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
  if (!m) {
    kept.push("--- !u!" + parts[i]);
    continue;
  }
  if (REMOVE_IDS.has(m[2])) {
    continue;
  }
  kept.push("--- !u!" + parts[i]);
}

scene = kept.join("");
scene = scene.replace(/\n  - component: \{fileID: 930050001\}/g, "");
scene = scene.replace(/\n  - \{fileID: 930050011\}/g, "");
scene = scene.replace(/\n  - \{fileID: 930050021\}/g, "");
scene = scene.replace(/\n  - \{fileID: 930050031\}/g, "");

fs.writeFileSync(SCENE, scene);
console.log("Removed StatusMenu BG sun/cloud layers and StatusMenuBgFx from CampusHub.unity");
