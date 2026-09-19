import fs from "fs";
import { execSync } from "child_process";

const prefix = (() => {
  const raw = execSync(
    'git show HEAD:"Assets/FracturedChorus/Scenes/CharacterBuild.unity"',
    {
      cwd: "d:/Fractured-Chorus1",
      encoding: "utf8",
      maxBuffer: 20 * 1024 * 1024,
    }
  );
  const idx = raw.search(/^--- !u!1 &/m);
  if (idx < 0) throw new Error("no first GO in git scene");
  return raw.slice(0, idx);
})();

const scenes = [
  "d:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity",
  "d:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity",
];

for (const path of scenes) {
  let body = fs.readFileSync(path, "utf8");
  if (body.startsWith("%YAML")) {
    console.log("already ok", path);
    continue;
  }
  if (!body.startsWith("--- !u!1 &")) {
    throw new Error(`unexpected start: ${path} -> ${JSON.stringify(body.slice(0, 40))}`);
  }
  fs.writeFileSync(path, prefix + body);
  const check = fs.readFileSync(path, "utf8");
  console.log("repaired", path, {
    size: check.length,
    startsYaml: check.startsWith("%YAML"),
    go: (check.match(/^--- !u!1 &/gm) || []).length,
    sceneRoots: check.includes("SceneRoots"),
  });
}
