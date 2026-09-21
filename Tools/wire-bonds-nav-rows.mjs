import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/Bonds.unity";
const NAV_VIEW_GUID = "c42fdfb3011d86c41b00840dd6a12477";
const MENU_NORMAL = "f5e3112d2d11179fbadc7a711a632b94";
const MENU_SELECTED = "8e2b1a39addcffaed01343437f5306a7";
const SPRITE_NORMAL = `{fileID: 21300000, guid: ${MENU_NORMAL}, type: 3}`;
const SPRITE_SELECTED = `{fileID: 21300000, guid: ${MENU_SELECTED}, type: 3}`;

const rows = [
  { name: "Row_SocialStats", go: "931000062", plate: "931000065", button: "931000066", label: "931000074", view: "931000901" },
  { name: "Row_Link", go: "931000075", plate: "931000078", button: "931000079", label: "931000087", view: "931000902" },
  { name: "Row_Conversations", go: "931000088", plate: "931000091", button: "931000092", label: "931000100", view: "931000903" },
  { name: "Row_Memories", go: "931000101", plate: "931000104", button: "931000105", label: "931000113", view: "931000904" },
  { name: "Row_Gallery", go: "931000114", plate: "931000117", button: "931000118", label: "931000126", view: "931000905" },
];

function setButtonTransitionNone(text, buttonId) {
  const re = new RegExp(`(--- !u!114 &${buttonId}\\nMonoBehaviour:[\\s\\S]*?m_Transition: )1`, "m");
  if (!re.test(text)) {
    throw new Error(`Button ${buttonId} transition block missing`);
  }
  return text.replace(re, "$10");
}

function ensureGoComponent(text, goId, componentId) {
  const re = new RegExp(`(--- !u!1 &${goId}\\nGameObject:[\\s\\S]*?m_Component:\\n(?:  - component: \\{fileID: \\d+\\}\\n)+)`, "m");
  const match = text.match(re);
  if (!match) {
    throw new Error(`GameObject ${goId} missing`);
  }
  if (match[0].includes(`{fileID: ${componentId}}`)) {
    return text;
  }
  return text.replace(re, `$1  - component: {fileID: ${componentId}}\n`);
}

function upsertNavView(text, row) {
  const block = `--- !u!114 &${row.view}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${row.go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${NAV_VIEW_GUID}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::FracturedChorus.Hub.BondNavRowView
  plate: {fileID: ${row.plate}}
  label: {fileID: ${row.label}}
  button: {fileID: ${row.button}}
  plateNormal: ${SPRITE_NORMAL}
  plateSelected: ${SPRITE_SELECTED}
`;

  const marker = `--- !u!114 &${row.view}\nMonoBehaviour:`;
  if (text.includes(marker)) {
    return text.replace(
      new RegExp(`--- !u!114 &${row.view}\\nMonoBehaviour:[\\s\\S]*?(?=\\n--- !u!)`, "m"),
      block.trimEnd(),
    );
  }

  const insertAfter = `--- !u!114 &${row.button}\nMonoBehaviour:`;
  const idx = text.indexOf(insertAfter);
  if (idx < 0) {
    throw new Error(`Button block ${row.button} missing for ${row.name}`);
  }
  const next = text.indexOf("\n--- !u!", idx + insertAfter.length);
  return `${text.slice(0, next)}\n${block}${text.slice(next)}`;
}

function wireBondsMenuUi(text) {
  const navLines = rows.map((row) => `  - {fileID: ${row.view}}`).join("\n");
  return text
    .replace(
      /  navRows:\n(?:  - \{fileID: \d+\}\n){5}/,
      `  navRows:\n${navLines}\n`,
    )
    .replace(/  navPlateNormal: \{fileID: 0\}/, `  navPlateNormal: ${SPRITE_NORMAL}`)
    .replace(/  navPlateSelected: \{fileID: 0\}/, `  navPlateSelected: ${SPRITE_SELECTED}`);
}

let text = fs.readFileSync(SCENE, "utf8");

text = text.replace(
  /(m_GameObject: \{fileID: 931000062\}[\s\S]*?m_Sprite: \{fileID: 21300000, guid: )8e2b1a39addcffaed01343437f5306a7/,
  `$1${MENU_NORMAL}`,
);

for (const row of rows) {
  text = setButtonTransitionNone(text, row.button);
  text = ensureGoComponent(text, row.go, row.view);
  text = upsertNavView(text, row);
}

text = wireBondsMenuUi(text);

const tmp = `${SCENE}.tmp`;
fs.writeFileSync(tmp, text);
fs.copyFileSync(tmp, SCENE);
fs.unlinkSync(tmp);

console.log(
  JSON.stringify(
    {
      scene: SCENE.replace("D:/Fractured-Chorus1/", ""),
      navRows: rows.map((row) => row.name),
      menuNormal: MENU_NORMAL,
      menuSelected: MENU_SELECTED,
    },
    null,
    2,
  ),
);
