import fs from "fs";
import path from "path";
import sharp from "sharp";

const ROOT = "D:/Fractured-Chorus1";
const PACK = path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/Pack");
const SCENE = path.join(ROOT, "Assets/FracturedChorus/Scenes/Bonds.unity");
const GUID_SELECTED_CARD = "e80e07b265bcf16810f278af0b5d2fe8";

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function isChecker(r, g, b, a) {
  if (a < 8) return false;
  const y = luma(r, g, b);
  const c = chroma(r, g, b);
  if (c > 28) return false;
  return (y >= 88 && y <= 178) || (y >= 210 && y <= 236 && c < 18);
}

function isLeftoverWhite(r, g, b, a) {
  if (a < 8) return false;
  return luma(r, g, b) >= 244 && chroma(r, g, b) <= 14;
}

function floodPunch(data, width, height, predicate) {
  const n = width * height;
  const cand = new Uint8Array(n);
  for (let idx = 0; idx < n; idx++) {
    const i = idx * 4;
    if (predicate(data[i], data[i + 1], data[i + 2], data[i + 3], idx)) cand[idx] = 1;
  }
  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let punched = 0;
  for (let start = 0; start < n; start++) {
    if (!cand[start] || seen[start]) continue;
    let head = 0;
    let tail = 0;
    queue[tail++] = start;
    seen[start] = 1;
    const first = 0;
    while (head < tail) {
      const idx = queue[head++];
      const x = idx % width;
      const y = (idx / width) | 0;
      const tryPush = (nx, ny) => {
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;
        const nidx = ny * width + nx;
        if (seen[nidx] || !cand[nidx]) return;
        seen[nidx] = 1;
        queue[tail++] = nidx;
      };
      tryPush(x - 1, y);
      tryPush(x + 1, y);
      tryPush(x, y - 1);
      tryPush(x, y + 1);
    }
    if (tail - first < 40) continue;
    for (let q = 0; q < tail; q++) {
      const i = queue[q] * 4;
      data[i] = 0;
      data[i + 1] = 0;
      data[i + 2] = 0;
      data[i + 3] = 0;
      punched += 1;
    }
  }
  return punched;
}

function opaqueBounds(data, width, height, pad = 6) {
  let minX = width;
  let minY = height;
  let maxX = -1;
  let maxY = -1;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      if (data[(y * width + x) * 4 + 3] < 12) continue;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }
  if (maxX < 0) return null;
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(width - 1, maxX + pad);
  maxY = Math.min(height - 1, maxY + pad);
  return { left: minX, top: minY, width: maxX - minX + 1, height: maxY - minY + 1 };
}

async function loadRaw(filePath) {
  const { data, info } = await sharp(filePath).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { pixels: Buffer.from(data), width: info.width, height: info.height };
}

async function writeRaw(filePath, pixels, width, height) {
  await sharp(pixels, { raw: { width, height, channels: 4 } }).png().toFile(filePath);
}

function largestOpaqueBox(data, width, height, pad = 8) {
  const n = width * height;
  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let best = null;
  for (let start = 0; start < n; start++) {
    if (seen[start] || data[start * 4 + 3] < 40) continue;
    let head = 0;
    let tail = 0;
    queue[tail++] = start;
    seen[start] = 1;
    let minX = width;
    let minY = height;
    let maxX = -1;
    let maxY = -1;
    while (head < tail) {
      const idx = queue[head++];
      const x = idx % width;
      const y = (idx / width) | 0;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
      const tryPush = (nx, ny) => {
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;
        const nidx = ny * width + nx;
        if (seen[nidx] || data[nidx * 4 + 3] < 40) return;
        seen[nidx] = 1;
        queue[tail++] = nidx;
      };
      tryPush(x - 1, y);
      tryPush(x + 1, y);
      tryPush(x, y - 1);
      tryPush(x, y + 1);
    }
    if (!best || tail > best.size) {
      best = { size: tail, minX, minY, maxX, maxY };
    }
  }
  if (!best) return null;
  const left = Math.max(0, best.minX - pad);
  const top = Math.max(0, best.minY - pad);
  const right = Math.min(width - 1, best.maxX + pad);
  const bottom = Math.min(height - 1, best.maxY + pad);
  return { left, top, width: right - left + 1, height: bottom - top + 1, size: best.size };
}

async function cropOpaque(file, pad) {
  const filePath = path.join(PACK, file);
  const { pixels, width, height } = await loadRaw(filePath);
  const box = largestOpaqueBox(pixels, width, height, pad);
  if (!box) return { file, skipped: true };
  if (box.width === width && box.height === height) return { file, cropped: false, width, height };
  const tmp = `${filePath}.crop.png`;
  await sharp(filePath).extract(box).png().toFile(tmp);
  fs.copyFileSync(tmp, filePath);
  fs.unlinkSync(tmp);
  return { file, cropped: true, from: [width, height], to: [box.width, box.height] };
}

async function punchLeftover(file, mode) {
  const filePath = path.join(PACK, file);
  const { pixels, width, height } = await loadRaw(filePath);
  let punched = 0;
  if (mode === "checker" || mode === "both") {
    punched += floodPunch(pixels, width, height, (r, g, b, a) => isChecker(r, g, b, a));
  }
  if (mode === "white-left" || mode === "both") {
    punched += floodPunch(pixels, width, height, (r, g, b, a, idx) => {
      const x = idx % width;
      if (x > width * 0.28) return false;
      return isLeftoverWhite(r, g, b, a);
    });
  }
  if (mode === "white-window") {
    punched += floodPunch(pixels, width, height, (r, g, b, a, idx) => {
      const y = (idx / width) | 0;
      if (y > height * 0.68) return false;
      return isLeftoverWhite(r, g, b, a);
    });
  }
  await writeRaw(filePath, pixels, width, height);
  return { file, punched, size: [width, height] };
}

const spriteReport = {
  cropped: [],
  punched: [],
};

for (const file of ["02_Menu_Normal.png", "03_Menu_Selected.png", "06_Row_Episode_Normal.png", "07_Row_Episode_Selected.png"]) {
  spriteReport.cropped.push(await cropOpaque(file, 8));
}

spriteReport.punched.push(await punchLeftover("08_Card_Character_Normal.png", "white-window"));
spriteReport.punched.push(await punchLeftover("09_Card_Character_Selected.png", "white-window"));
spriteReport.punched.push(await punchLeftover("10_Panel_CharacterInfo.png", "white-left"));

function parseScene(text) {
  const parts = text.split(/^--- !u!/m);
  const byId = new Map();
  const order = [];
  for (let i = 1; i < parts.length; i++) {
    const match = parts[i].match(/^(\d+) &(\d+)\n([\s\S]*)$/);
    if (!match) continue;
    byId.set(match[2], { type: match[1], id: match[2], body: match[3] });
    order.push(match[2]);
  }
  return { byId, order, header: parts[0] };
}

function goName(block) {
  return block.body.match(/\n  m_Name: (.+)/)?.[1] ?? null;
}

function goComponents(block) {
  return [...block.body.matchAll(/component: \{fileID: (\d+)\}/g)].map((match) => match[1]);
}

function xfOfGo(byId, goId) {
  for (const componentId of goComponents(byId.get(goId))) {
    const component = byId.get(componentId);
    if (component?.type === "224") return component;
  }
  return null;
}

function imageOfGo(byId, goId) {
  for (const componentId of goComponents(byId.get(goId))) {
    const component = byId.get(componentId);
    if (component?.type === "114" && component.body.includes("UnityEngine.UI::UnityEngine.UI.Image")) {
      return component;
    }
  }
  return null;
}

function goOfComponent(byId, componentId) {
  for (const [goId, block] of byId.entries()) {
    if (block.type === "1" && block.body.includes(`component: {fileID: ${componentId}}`)) return goId;
  }
  return null;
}

function getChildren(body) {
  const match = body.match(/\n  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/);
  if (!match) return [];
  return [...match[1].matchAll(/fileID: (\d+)/g)].map((entry) => entry[1]);
}

function setChildren(body, childIds) {
  const list = childIds.map((id) => `  - {fileID: ${id}}\n`).join("");
  if (body.includes("\n  m_Children: []\n")) {
    return body.replace("\n  m_Children: []\n", `\n  m_Children:\n${list}`);
  }
  return body.replace(/\n  m_Children:\n(?:  - \{fileID: \d+\}\n)*/, `\n  m_Children:\n${list}`);
}

function patchRect(body, pos, size) {
  return body
    .replace(/m_AnchoredPosition: \{x: [^}]+\}/, `m_AnchoredPosition: {x: ${pos[0]}, y: ${pos[1]}}`)
    .replace(/m_SizeDelta: \{x: [^}]+\}/, `m_SizeDelta: {x: ${size[0]}, y: ${size[1]}}`);
}

const { byId, order, header } = parseScene(fs.readFileSync(SCENE, "utf8"));
const patched = [];

for (const block of byId.values()) {
  if (block.type !== "114" || !block.body.includes("UnityEngine.UI::UnityEngine.UI.Image")) continue;
  if (!block.body.includes("m_FillCenter: 0")) continue;
  block.body = block.body.replace(/m_FillCenter: 0/, "m_FillCenter: 1");
}

for (const block of byId.values()) {
  if (block.type !== "1") continue;
  const name = goName(block);
  const xf = xfOfGo(byId, block.id);
  if (!xf) continue;

  if (name === "Frame" && xf.body.includes("m_SizeDelta: {x: 192.07, y: 191.99}")) {
    xf.body = patchRect(xf.body, [4, 10], [128, 140]);
    patched.push(`Frame ${block.id}`);
  }

  if (name === "Face") {
    xf.body = patchRect(xf.body, [4, 18], [80, 88]);
    patched.push(`Face ${block.id}`);
  }

  if (name === "Portrait" && xf.body.includes("m_Father: {fileID: 931000401}")) {
    xf.body = patchRect(xf.body, [128, 16], [196, 260]);
    patched.push("DetailCard/Portrait");
  }
}

const chip1Frame = [...byId.values()].find((block) => block.type === "1" && goName(block) === "Frame" && xfOfGo(byId, block.id)?.body.includes("m_Father: {fileID: 931000265}"));
if (chip1Frame) {
  const image = imageOfGo(byId, chip1Frame.id);
  if (image) {
    image.body = image.body.replace(/m_Sprite: \{fileID: [^}]+\}/, `m_Sprite: {fileID: 21300000, guid: ${GUID_SELECTED_CARD}, type: 3}`);
    patched.push("Chip_1/Frame → 09_Selected");
  }
}

const chip0 = [...byId.values()].find((block) => block.type === "1" && goName(block) === "Chip_0");
if (chip0) {
  const xf = xfOfGo(byId, chip0.id);
  const kids = getChildren(xf.body);
  const named = kids.map((id) => ({ id, name: goName(byId.get(goOfComponent(byId, id))) }));
  const face = named.find((entry) => entry.name === "Face");
  const frame = named.find((entry) => entry.name === "Frame");
  if (face && frame && kids[0] === frame.id) {
    const rest = kids.filter((id) => id !== face.id && id !== frame.id);
    xf.body = setChildren(xf.body, [face.id, frame.id, ...rest]);
    patched.push("Chip_0 Face behind Frame");
  }
}

const emitOrder = [
  ...order.filter((id) => byId.get(id).type !== "1660057539"),
  ...order.filter((id) => byId.get(id).type === "1660057539"),
];
const nextScene = `${header}${emitOrder
  .map((id) => {
    const block = byId.get(id);
    return `--- !u!${block.type} &${block.id}\n${block.body}`;
  })
  .join("")}`;
const tmpScene = `${SCENE}.tmp`;
fs.writeFileSync(tmpScene, nextScene);
try {
  fs.copyFileSync(tmpScene, SCENE);
  fs.unlinkSync(tmpScene);
} catch (error) {
  console.error("SCENE_WRITE_FAILED", error.code, tmpScene);
  throw error;
}

console.log(JSON.stringify({ spriteReport, patched }, null, 2));
