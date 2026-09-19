import fs from "fs";
import path from "path";
import sharp from "sharp";

const ROOT = "D:/Fractured-Chorus1";
const src =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_1211bc28-4956-48e9-955a-fb49c9e908c7-8c5b4c65-e718-456f-9f08-3a2ebd8361e4.jpg";
const dest = path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/Cards/bond_card_story_hidden_v1.png");
const guid = "e1f2a3b4c5d6478901234567890abcde";

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function punchCorners(data, width, height) {
  const n = width * height;
  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let head = 0;
  let tail = 0;
  const tryPush = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const idx = y * width + x;
    if (seen[idx]) return;
    const i = idx * 4;
    const yv = luma(data[i], data[i + 1], data[i + 2]);
    const c = chroma(data[i], data[i + 1], data[i + 2]);
    if (yv < 246 || c > 10) return;
    seen[idx] = 1;
    queue[tail++] = idx;
  };
  for (const [x, y] of [
    [0, 0],
    [width - 1, 0],
    [0, height - 1],
    [width - 1, height - 1],
  ]) {
    tryPush(x, y);
  }
  while (head < tail) {
    const idx = queue[head++];
    const x = idx % width;
    const y = (idx / width) | 0;
    const i = idx * 4;
    data[i] = 0;
    data[i + 1] = 0;
    data[i + 2] = 0;
    data[i + 3] = 0;
    tryPush(x - 1, y);
    tryPush(x + 1, y);
    tryPush(x, y - 1);
    tryPush(x, y + 1);
  }
}

const meta = fs.readFileSync(
  path.join(ROOT, "Assets/FracturedChorus/Art/UI/Bonds/Cards/bond_card_ren.png.meta"),
  "utf8",
);
fs.writeFileSync(`${dest}.meta`, meta.replace(/^guid: .+$/m, `guid: ${guid}`));

const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const pixels = Buffer.from(data);
punchCorners(pixels, info.width, info.height);
await sharp(pixels, { raw: { width: info.width, height: info.height, channels: 4 } })
  .resize(1024, 1024, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } })
  .png()
  .toFile(dest);

const m = await sharp(dest).metadata();
console.log(JSON.stringify({ dest, guid, size: [m.width, m.height] }));
