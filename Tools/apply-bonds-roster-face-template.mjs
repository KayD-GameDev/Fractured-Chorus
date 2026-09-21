import fs from "fs";

const SCENE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Scenes/Bonds.unity";

const SPRITE = {
  ren: "7a1c2e3f4b5d67890a1c2e3f4b5d6789",
  charlotte: "8b2d3f4a5c6e78901b2d3f4a5c6e7890",
  coda: "9c3e4a5b6d7f89012c3e4a5b6d7f8901",
  hidden: "e1f2a3b4c5d6478901234567890abcde",
  astra: "ad4f5b6c7e809123ad4f5b6c7e809123",
  ryo: "cf617d8e0a021345cf617d8e0a021345",
  meilin: "be506c7d8f910234be506c7d8f910234",
  lock: "65586af2206956775fc6395ace370607",
  promo: "f3a8c2e91b4d4650a7e6d9058c1b2f30",
};

const CHIP_VIEWS = [
  1418816800, 901861930, 1878732199, 214103228, 659314521, 1010747492, 937798697, 147231676,
  623621021, 1307977672,
];

function spriteRef(guid) {
  return `{fileID: 21300000, guid: ${guid}, type: 3}`;
}

function patchComponent(text, id, mutator) {
  const marker = `--- !u!114 &${id}\n`;
  const start = text.indexOf(marker);
  if (start < 0) {
    return { text, ok: false };
  }

  const end = text.indexOf("\n--- !u!", start + marker.length);
  const chunk = end < 0 ? text.slice(start) : text.slice(start, end);
  const updated = mutator(chunk);
  const next =
    end < 0 ? text.slice(0, start) + updated : text.slice(0, start) + updated + text.slice(end);
  return { text: next, ok: true };
}

function setFaceSprite(chunk, guid) {
  return chunk.replace(/m_Sprite: \{fileID: [^}]+\}/, `m_Sprite: ${spriteRef(guid)}`);
}

function setLock(chunk, visible) {
  let out = chunk.replace(/m_Sprite: \{fileID: [^}]+\}/, `m_Sprite: ${spriteRef(SPRITE.lock)}`);
  out = out.replace(
    /m_Color: \{r: [^}]+\}/,
    visible ? "m_Color: {r: 1, g: 1, b: 1, a: 1}" : "m_Color: {r: 1, g: 1, b: 1, a: 0}",
  );
  return out;
}

function setPromoSprite(chunk) {
  return chunk.replace(
    /m_Sprite: \{fileID: [^}]+\}/,
    `m_Sprite: {fileID: 4829103748291038472, guid: ${SPRITE.promo}, type: 3}`,
  );
}

function wireLockIcon(chunk, lockImageId) {
  if (chunk.includes("lockIcon: {fileID: 0}")) {
    return chunk.replace(/lockIcon: \{fileID: 0\}/, `lockIcon: {fileID: ${lockImageId}}`);
  }

  return chunk;
}

const FACE_BY_ID = {
  931000251: SPRITE.ren,
  931000273: SPRITE.charlotte,
  931000295: SPRITE.coda,
  932200304: SPRITE.astra,
  932200404: SPRITE.ryo,
  1874636881: SPRITE.meilin,
  1967549607: SPRITE.hidden,
  1225164988: SPRITE.hidden,
  1186401734: SPRITE.hidden,
  2138832485: SPRITE.hidden,
};

const LOCK_BY_ID = {
  931000255: false,
  931000277: false,
  931000299: false,
  2049509059: false,
  693856021: true,
  1973595722: true,
  102615555: true,
  826884709: true,
  981718343: true,
  714638073: true,
};

const VIEW_LOCK = {
  214103228: 2049509059,
  659314521: 693856021,
  1010747492: 1973595722,
  623621021: 981718343,
  937798697: 102615555,
  147231676: 826884709,
  1307977672: 714638073,
};

let text = fs.readFileSync(SCENE, "utf8");
const applied = [];

for (const [imageId, guid] of Object.entries(FACE_BY_ID)) {
  const result = patchComponent(text, imageId, (chunk) => setFaceSprite(chunk, guid));
  text = result.text;
  if (result.ok) applied.push(`face:${imageId}`);
}

for (const [imageId, visible] of Object.entries(LOCK_BY_ID)) {
  const result = patchComponent(text, imageId, (chunk) => setLock(chunk, visible));
  text = result.text;
  if (result.ok) applied.push(`lock:${imageId}:${visible}`);
}

for (const [viewId, lockImageId] of Object.entries(VIEW_LOCK)) {
  if (!lockImageId) continue;
  const result = patchComponent(text, viewId, (chunk) => wireLockIcon(chunk, lockImageId));
  text = result.text;
  if (result.ok) applied.push(`viewLock:${viewId}`);
}

const promo = patchComponent(text, 931000467, (chunk) => setPromoSprite(chunk));
text = promo.text;
if (promo.ok) applied.push("promo");

if (text.includes("  - {fileID: 659314521}\n  rosterChevron:")) {
  text = text.replace(
    /  chips:\n(?:  - \{fileID: \d+\}\n){1,10}  rosterChevron:/,
    `  chips:\n${CHIP_VIEWS.map((id) => `  - {fileID: ${id}}`).join("\n")}\n  rosterChevron:`,
  );
  applied.push("chipsArray");
}

fs.writeFileSync(SCENE, text);
console.log(JSON.stringify({ applied }, null, 2));
