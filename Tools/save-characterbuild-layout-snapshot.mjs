import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SANDBOX = `${ROOT}/Assets/FracturedChorus/Scenes/CharacterBuildLayoutSandbox.unity`;
const PRODUCTION = `${ROOT}/Assets/FracturedChorus/Scenes/CharacterBuild.unity`;
const SNAPSHOT_SANDBOX = `${ROOT}/Assets/FracturedChorus/Art/UI/StatMenu/MockKit/sandbox_layout_snapshot.json`;
const SNAPSHOT_PROD = `${ROOT}/Assets/FracturedChorus/Art/UI/StatMenu/MockKit/characterbuild_layout_snapshot.json`;

function parseScene(text) {
  const parts = text.split(/^--- !u!/m);
  const header = parts[0];
  const byId = new Map();
  for (let i = 1; i < parts.length; i++) {
    const m = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
    if (!m) continue;
    byId.set(m[2], { type: m[1], id: m[2], body: m[3] });
  }
  return { header, byId };
}

function goName(byId, goId) {
  const b = byId.get(goId);
  if (!b || b.type !== "1") return null;
  return b.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}

function goComponents(byId, goId) {
  const b = byId.get(goId);
  if (!b || b.type !== "1") return [];
  return [...b.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((m) => m[1]);
}

function findGoByName(byId, name) {
  for (const b of byId.values()) {
    if (b.type === "1" && b.body.match(new RegExp(`\\n  m_Name: ${name}\\s*\\n`))) return b.id;
  }
  return null;
}

function xfOfGo(byId, goId) {
  let transformId = null;
  for (const cid of goComponents(byId, goId)) {
    const c = byId.get(cid);
    if (!c) continue;
    if (c.type === "224") return { id: cid, kind: "RectTransform" };
    if (c.type === "4") transformId = cid;
  }
  return transformId ? { id: transformId, kind: "Transform" } : null;
}

function rtOfGo(byId, goId) {
  const xf = xfOfGo(byId, goId);
  return xf?.kind === "RectTransform" ? xf.id : null;
}

function goOfComponent(byId, compId) {
  for (const [gid, g] of byId.entries()) {
    if (g.type === "1" && g.body.includes(`component: {fileID: ${compId}}`)) return gid;
  }
  return null;
}

function getChildren(byId, rtId) {
  const body = byId.get(rtId)?.body ?? "";
  const m = body.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!m) return [];
  return [...m[1].matchAll(/fileID: (\d+)/g)].map((x) => x[1]);
}

function vec(body, key) {
  const m = body.match(
    new RegExp(`${key}: \\{x: ([^,}]+), y: ([^,}]+)(?:, z: ([^,}]+))?(?:, w: ([^}]+))?\\}`),
  );
  if (!m) return { x: 0, y: 0, z: 0 };
  const out = { x: Number(m[1]), y: Number(m[2]), z: m[3] != null ? Number(m[3]) : 0 };
  if (m[4] != null) out.w = Number(m[4]);
  return out;
}

function loadGuidMap() {
  const map = new Map();
  const walk = (dir) => {
    if (!fs.existsSync(dir)) return;
    for (const name of fs.readdirSync(dir)) {
      const full = path.join(dir, name);
      if (fs.statSync(full).isDirectory()) walk(full);
      else if (name.endsWith(".meta")) {
        const guid = fs.readFileSync(full, "utf8").match(/^guid: ([a-f0-9]+)/m)?.[1];
        if (guid) map.set(guid, full.replace(/\\/g, "/").replace(/\.meta$/, "").replace(`${ROOT}/`, ""));
      }
    }
  };
  walk(`${ROOT}/Assets/FracturedChorus/Art/UI`);
  return map;
}

function spritePathForGo(byId, goId, guidMap) {
  for (const cid of goComponents(byId, goId)) {
    const c = byId.get(cid);
    if (!c || c.type !== "114" || !c.body.includes("UnityEngine.UI.Image")) continue;
    const guid = c.body.match(/m_Sprite: \{fileID: [^,]+, guid: ([a-f0-9]+)/)?.[1];
    if (guid && guidMap.has(guid)) return guidMap.get(guid);
  }
  return "";
}

function sceneRootIds(sceneText) {
  const m = sceneText.match(/SceneRoots:[\s\S]*?\n  m_Roots:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!m) return [];
  return [...m[1].matchAll(/fileID: (\d+)/g)].map((x) => x[1]);
}

function exportSnapshot(scenePath, outPath, guidMap) {
  const sceneText = fs.readFileSync(scenePath, "utf8");
  const scene = parseScene(sceneText);
  const canvasGo = findGoByName(scene.byId, "BuildCanvas");
  if (!canvasGo) throw new Error(`BuildCanvas missing in ${scenePath}`);
  const canvasRt = rtOfGo(scene.byId, canvasGo);
  const childNames = getChildren(scene.byId, canvasRt).map((rt) =>
    goName(scene.byId, goOfComponent(scene.byId, rt)),
  );
  const nodes = [];
  const extraRootNames = [];

  const walk = (goId, pathStr, siblingIndex) => {
    const xf = xfOfGo(scene.byId, goId);
    const xfBlock = xf ? scene.byId.get(xf.id) : null;
    const go = scene.byId.get(goId);
    const entry = {
      path: pathStr,
      goFileId: goId,
      siblingIndex,
      activeSelf: /m_IsActive: 1/.test(go.body),
      layoutKind: xf?.kind ?? "None",
    };
    if (xfBlock) {
      entry.localScale = vec(xfBlock.body, "m_LocalScale");
      entry.localPosition = vec(xfBlock.body, "m_LocalPosition");
      entry.localRotation = vec(xfBlock.body, "m_LocalRotation");
      entry.localEulerAnglesHint = vec(xfBlock.body, "m_LocalEulerAnglesHint");
      if (xf.kind === "RectTransform") {
        entry.anchorMin = vec(xfBlock.body, "m_AnchorMin");
        entry.anchorMax = vec(xfBlock.body, "m_AnchorMax");
        entry.anchoredPosition = vec(xfBlock.body, "m_AnchoredPosition");
        entry.sizeDelta = vec(xfBlock.body, "m_SizeDelta");
        entry.pivot = vec(xfBlock.body, "m_Pivot");
      }
    }
    const sp = spritePathForGo(scene.byId, goId, guidMap);
    if (sp) entry.spritePath = sp;
    nodes.push(entry);
    if (!xf) return;
    getChildren(scene.byId, xf.id).forEach((childXf, i) => {
      const childGo = goOfComponent(scene.byId, childXf);
      if (!childGo) return;
      walk(childGo, `${pathStr}/${goName(scene.byId, childGo)}`, i);
    });
  };

  sceneRootIds(sceneText).forEach((rootCompId, i) => {
    const go = goOfComponent(scene.byId, rootCompId);
    const name = goName(scene.byId, go);
    if (!go || !name) return;
    if (name !== "BuildCanvas") extraRootNames.push(name);
    walk(go, name, i);
  });

  fs.writeFileSync(
    outPath,
    JSON.stringify(
      {
        scene: scenePath.replace(`${ROOT}/`, "").replace(/\\/g, "/"),
        savedAtUtc: new Date().toISOString(),
        note: "Reference backup only. Layout SoT is the scene file — editor menus must not re-apply these values.",
        buildCanvasChildren: childNames,
        extraSceneRoots: extraRootNames,
        nodes,
      },
      null,
      2,
    ) + "\n",
  );
  return { nodes: nodes.length, children: childNames, extraSceneRoots: extraRootNames };
}

const guidMap = loadGuidMap();
console.log("sandbox", exportSnapshot(SANDBOX, SNAPSHOT_SANDBOX, guidMap));
console.log("production", exportSnapshot(PRODUCTION, SNAPSHOT_PROD, guidMap));
