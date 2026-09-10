import fs from "fs";
import sharp from "sharp";
import path from "path";

const src =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/ui_stat_slot_skill_locked_v2.png";
const dest =
  "d:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit/ui_stat_slot_skill_locked_v2.png";
const metaDest = dest + ".meta";
const refMeta =
  "d:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu/CrystalKit/ui_stat_slot_skill_locked_v1.png.meta";

const TARGET_W = 1024;
const TARGET_H = 1536;
const NEW_GUID = "a7c4e91f2b8d4065a1e3f0c9d8b7642e";

function floodClearBlack(data, w, h) {
  const visited = new Uint8Array(w * h);
  const stack = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= w || y >= h) return;
    const i = y * w + x;
    if (visited[i]) return;
    visited[i] = 1;
    stack.push(i);
  };

  // Seed from edges
  for (let x = 0; x < w; x++) {
    push(x, 0);
    push(x, h - 1);
  }
  for (let y = 0; y < h; y++) {
    push(0, y);
    push(w - 1, y);
  }

  const isBg = (i) => {
    const o = i * 4;
    const r = data[o];
    const g = data[o + 1];
    const b = data[o + 2];
    const a = data[o + 3];
    if (a < 8) return true;
    // near-black / very dark (outer void), keep cyan glow pixels
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    if (max <= 28) return true;
    if (max <= 42 && max - min <= 12) return true;
    return false;
  };

  while (stack.length) {
    const i = stack.pop();
    if (!isBg(i)) continue;
    const o = i * 4;
    data[o + 3] = 0;
    const x = i % w;
    const y = (i / w) | 0;
    push(x + 1, y);
    push(x - 1, y);
    push(x, y + 1);
    push(x, y - 1);
  }
}

const raw = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
let { data, info } = raw;
data = Buffer.from(data);
floodClearBlack(data, info.width, info.height);

await sharp(data, {
  raw: { width: info.width, height: info.height, channels: 4 },
})
  .resize(TARGET_W, TARGET_H, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
  .png()
  .toFile(dest);

const dims = await sharp(dest).metadata();
console.log("wrote", dest, dims.width, dims.height);

let meta = fs.readFileSync(refMeta, "utf8");
meta = meta.replace(/guid: [a-f0-9]+/, `guid: ${NEW_GUID}`);
meta = meta.replace(/spriteID: [a-f0-9]+/, `spriteID: ${NEW_GUID.replace(/(.{8})(.{4})(.{4})(.{4})(.{12})/, "$1$2$3$4$5").slice(0, 32)}`);
// simpler unique spriteID
meta = meta.replace(/spriteID: [a-f0-9]+/, "spriteID: c91f2b8d4065a1e3f0c9d8b7642ea7c4");
fs.writeFileSync(metaDest, meta);
console.log("meta guid", NEW_GUID);
