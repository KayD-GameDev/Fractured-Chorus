import fs from "fs";
import path from "path";

const ROOT = "D:/Fractured-Chorus1";
const SCENE = path.join(ROOT, "Assets/FracturedChorus/Scenes/FlowerShopWork.unity");
const LAYOUT = path.join(
  ROOT,
  "Assets/FracturedChorus/Data/UI/Vn/vn_dialogue_panel_layout_flower_shop_work.json"
);

const PORTRAIT_Y = { left: 141, right: 137 };

function patchRectBlock(body, node) {
  let out = body;
  const set = (key, x, y) => {
    const re = new RegExp(`(\\n  m_${key}: \\{x: )[^,]+(, y: )[^}]+(\\})`);
    out = out.replace(re, `$1${x}$2${y}$3`);
  };
  set("AnchorMin", node.anchorMin.x, node.anchorMin.y);
  set("AnchorMax", node.anchorMax.x, node.anchorMax.y);
  set("AnchoredPosition", node.anchoredPosition.x, node.anchoredPosition.y);
  set("SizeDelta", node.sizeDelta.x, node.sizeDelta.y);
  set("Pivot", node.pivot.x, node.pivot.y);
  return out;
}

function findGoNameBeforeTransform(text, xfId) {
  const goRe = new RegExp(
    `--- !u!1 &(\\d+)\\r?\\nGameObject:[\\s\\S]*?m_Name: ([^\\r\\n]+)[\\s\\S]*?--- !u!224 &${xfId}\\r?\\nRectTransform:`,
    "m"
  );
  const m = text.match(goRe);
  return m ? m[2].trim() : null;
}

function patchScene(text, layout) {
  const byName = new Map(layout.nodes.map((n) => [n.name, n]));
  const xfIds = [...text.matchAll(/--- !u!224 &(\d+)\r?\nRectTransform:/g)].map((m) => m[1]);

  for (const xfId of xfIds) {
    const name = findGoNameBeforeTransform(text, xfId);
    const node = name ? byName.get(name) : null;
    if (!node) continue;

    const blockRe = new RegExp(`(--- !u!224 &${xfId}\\r?\\nRectTransform:[\\s\\S]*?)(--- !u!)`, "m");
    const blockMatch = text.match(blockRe);
    if (!blockMatch) continue;

    const patched = patchRectBlock(blockMatch[1], node);
    text = text.replace(blockMatch[1], patched);

    if (name === "DialogueFrame" && node.imageType >= 0) {
      const goRe = new RegExp(
        `(--- !u!1 &\\d+\\r?\\nGameObject:[\\s\\S]*?m_Name: DialogueFrame[\\s\\S]*?--- !u!114 &(\\d+)\\r?\\nMonoBehaviour:[\\s\\S]*?m_Type: )\\d+`,
        "m"
      );
      text = text.replace(goRe, `$1${node.imageType}`);
      const paRe = /(m_Name: DialogueFrame[\s\S]*?m_PreserveAspect: )\d/;
      text = text.replace(paRe, `$1${node.preserveAspect ? 1 : 0}`);
    }
  }

  text = text.replace(
    /(m_Name: DialoguePortrait_Left[\s\S]*?m_AnchoredPosition: \{x: 28, y: )420(\})/m,
    `$1${PORTRAIT_Y.left}$2`
  );
  text = text.replace(
    /(m_Name: DialoguePortrait_Right[\s\S]*?m_AnchoredPosition: \{x: -28, y: )420(\})/m,
    `$1${PORTRAIT_Y.right}$2`
  );
  text = text.replace(/leftAnchoredPosition: \{x: 28, y: 420\}/g, `leftAnchoredPosition: {x: 28, y: ${PORTRAIT_Y.left}}`);
  text = text.replace(
    /rightAnchoredPosition: \{x: -28, y: 420\}/g,
    `rightAnchoredPosition: {x: -28, y: ${PORTRAIT_Y.right}}`
  );

  return text;
}

const layout = JSON.parse(fs.readFileSync(LAYOUT, "utf8"));
let scene = fs.readFileSync(SCENE, "utf8");
const before = scene;
scene = patchScene(scene, layout);
if (scene === before) {
  console.error("No changes applied — check scene structure.");
  process.exit(1);
}

fs.writeFileSync(SCENE, scene);
console.log("Patched FlowerShopWork.unity dialogue + portrait layout from JSON.");
