import fs from "fs";

const scenes = [
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuild.unity",
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/CharacterBuildLayoutSandbox.unity",
];

function patchAllocControls(s) {
  const re = /--- !u!224 &(\d+)\nRectTransform:[\s\S]*?m_GameObject: \{fileID: (\d+)\}[\s\S]*?m_Father: \{fileID: (\d+)\}[\s\S]*?m_AnchorMin: \{x: [^}]+\}[\s\S]*?m_AnchorMax: \{x: [^}]+\}[\s\S]*?m_Pivot: \{x: 0\.5, y: 0\.5\}/g;
  return s.replace(re, (block) => {
    const goId = block.match(/m_GameObject: \{fileID: (\d+)\}/)?.[1];
    if (!goId) return block;
    const goBlock = s.match(new RegExp(`--- !u!1 &${goId}\\nGameObject:[\\s\\S]*?m_Name: ([^\\n]+)`));
    if (!goBlock || goBlock[1].trim() !== "AllocControls") return block;
    return block
      .replace(/m_AnchorMin: \{x: [^}]+\}/, "m_AnchorMin: {x: 0.82, y: 0.06}")
      .replace(/m_AnchorMax: \{x: [^}]+\}/, "m_AnchorMax: {x: 0.99, y: 0.94}");
  });
}

function patchStatValueLabels(s) {
  const statRowIds = new Set();
  for (const m of s.matchAll(/m_Name: StatRow_(?:Strength|Magic|Endurance|HeartBeat|Luck)[\s\S]*?--- !u!224 &(\d+)/g)) {
    statRowIds.add(m[1]);
  }
  for (const m of s.matchAll(/--- !u!224 &(\d+)\nRectTransform:[\s\S]*?m_Father: \{fileID: (\d+)\}/g)) {
    const rtId = m[1];
    const fatherId = m[2];
    if (!statRowIds.has(fatherId)) continue;
    const blockStart = s.indexOf(`--- !u!224 &${rtId}`);
    if (blockStart < 0) continue;
    const blockEnd = s.indexOf("--- !u!", blockStart + 10);
    const block = s.slice(blockStart, blockEnd < 0 ? undefined : blockEnd);
    const goId = block.match(/m_GameObject: \{fileID: (\d+)\}/)?.[1];
    if (!goId) continue;
    const goName = s.match(new RegExp(`--- !u!1 &${goId}[\\s\\S]*?m_Name: ([^\\n]+)`))?.[1]?.trim();
    if (goName !== "ValueLabel") continue;
    const patched = block
      .replace(/m_AnchorMin: \{x: [^}]+\}/, "m_AnchorMin: {x: 0.68, y: 0.42}")
      .replace(/m_AnchorMax: \{x: [^}]+\}/, "m_AnchorMax: {x: 0.80, y: 0.96}");
    s = s.slice(0, blockStart) + patched + s.slice(blockEnd < 0 ? s.length : blockEnd);
  }
  return s;
}

function patchNoteCircleRaycast(s) {
  return s.replace(
    /(m_Name: NoteCircle[\s\S]{0,1200}?m_RaycastTarget: )1/mg,
    "$10",
  );
}

for (const path of scenes) {
  if (!fs.existsSync(path)) {
    console.log("skip", path);
    continue;
  }
  let s = fs.readFileSync(path, "utf8");
  s = patchAllocControls(s);
  s = patchStatValueLabels(s);
  s = patchNoteCircleRaycast(s);
  fs.writeFileSync(path, s);
  console.log("patched", path);
}
