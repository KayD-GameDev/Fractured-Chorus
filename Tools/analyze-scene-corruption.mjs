import fs from "fs";
import { execSync } from "child_process";

const scenePath =
  "d:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";
const sandboxPath =
  "d:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity";

function analyze(path) {
  const buf = fs.readFileSync(path);
  const t = buf.toString("utf8");
  const crlf = (t.match(/\r\n/g) || []).length;
  const lf = (t.match(/(?<!\r)\n/g) || []).length;
  const cr = (t.match(/\r(?!\n)/g) || []).length;
  const goLf = (t.match(/^--- !u!1 &/gm) || []).length;
  const goAny = (t.match(/--- !u!1 &/g) || []).length;
  console.log(path);
  console.log({
    size: buf.length,
    startsYaml: t.startsWith("%YAML"),
    starts: JSON.stringify(t.slice(0, 40)),
    crlf,
    lf,
    cr,
    goLf,
    goAny,
    sceneRoots: t.includes("SceneRoots"),
    buildCanvas: t.includes("m_Name: BuildCanvas"),
  });
  return t;
}

function prefixFromGit() {
  const raw = execSync(
    'git show HEAD:"Assets/FracturedChorus/Scenes/CharacterBuild.unity"',
    { cwd: "d:/Fractured-Chorus1", encoding: "utf8", maxBuffer: 20 * 1024 * 1024 }
  );
  const idx = raw.search(/^--- !u!1 &/m);
  if (idx < 0) throw new Error("no first GO in git scene");
  return raw.slice(0, idx);
}

const t = analyze(scenePath);
analyze(sandboxPath);

const prefix = prefixFromGit();
console.log("\nprefix length", prefix.length);
console.log("prefix head", JSON.stringify(prefix.slice(0, 80)));
console.log("prefix tail", JSON.stringify(prefix.slice(-80)));
