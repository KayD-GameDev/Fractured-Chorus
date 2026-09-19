import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CampusHub.unity";

let text = fs.readFileSync(SCENE, "utf8");
const sceneRootsBlockIdx = text.lastIndexOf("--- !u!1660057539");
const hubStart = text.indexOf("--- !u!1 &930020001");
if (sceneRootsBlockIdx < 0) {
  console.log("SceneRoots block not found — skip reorder.");
  process.exit(0);
}

if (hubStart < 0) {
  console.log("HubCornerInfoHud blocks not found — skip reorder.");
  process.exit(0);
}

if (hubStart < sceneRootsBlockIdx) {
  console.log("HubCornerInfoHud already before SceneRoots — skip.");
  process.exit(0);
}

const beforeRoots = text.slice(0, sceneRootsBlockIdx);
const rootsSection = text.slice(sceneRootsBlockIdx, hubStart);
const hubSection = text.slice(hubStart);
const fixed = beforeRoots + hubSection + rootsSection;
fs.writeFileSync(SCENE, fixed);
console.log("Moved HubCornerInfoHud YAML blocks before SceneRoots.");
