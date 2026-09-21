import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const PARTICLES = "d:/Fractured-Chorus1/Assets/FracturedChorus/VFX/ButterflyTransition/Particles";

function makeCanvas(width, height) {
  return { data: Buffer.alloc(width * height * 4), width, height };
}

function setPixel(img, x, y, r, g, b, a) {
  if (x < 0 || y < 0 || x >= img.width || y >= img.height || a <= 0) return;
  const o = (y * img.width + x) * 4;
  const da = img.data[o + 3] / 255;
  const sa = a / 255;
  const outA = sa + da * (1 - sa);
  if (outA <= 0) return;
  img.data[o] = Math.round((r * sa + img.data[o] * da * (1 - sa)) / outA);
  img.data[o + 1] = Math.round((g * sa + img.data[o + 1] * da * (1 - sa)) / outA);
  img.data[o + 2] = Math.round((b * sa + img.data[o + 2] * da * (1 - sa)) / outA);
  img.data[o + 3] = Math.round(outA * 255);
}

function softBloom(width, height, rx, ry, peak) {
  const img = makeCanvas(width, height);
  const cx = width * 0.5;
  const cy = height * 0.5;
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const dx = (x - cx) / rx;
      const dy = (y - cy) / ry;
      const d2 = dx * dx + dy * dy;
      const fall = Math.exp(-d2 * 4.2);
      const a = fall * peak;
      if (a <= 1) continue;
      setPixel(img, x, y, 248 + fall * 7, 250 + fall * 5, 255, a);
    }
  }
  return img;
}

async function savePng(img, filePath) {
  await sharp(img.data, { raw: { width: img.width, height: img.height, channels: 4 } })
    .png()
    .toFile(filePath);
  console.log("wrote", path.basename(filePath), img.width, img.height);
}

const rim = softBloom(256, 256, 256 * 0.38, 256 * 0.38, 170);
await savePng(rim, path.join(PARTICLES, "fc_bt_glow_rim.png"));
const hex = softBloom(512, 256, 512 * 0.4, 256 * 0.28, 175);
const hexPath = path.join(PARTICLES, "fc_bt_glow_hex.png");
await savePng(hex, hexPath);
const metaPath = hexPath + ".meta";
if (!fs.existsSync(metaPath)) {
  const metaSrc = fs.readFileSync(path.join(PARTICLES, "fc_bt_glow_rim.png.meta"), "utf8");
  fs.writeFileSync(
    metaPath,
    metaSrc
      .replace("guid: 2ef6f60306fe4a7a916927c096a96aea", "guid: 8c4e1f2a9b0d4e6f8a7c5d3b1e0f2948")
      .replace("spriteID: f1df02990b4742a286e2529b3bfa1df3", "spriteID: 7a3e8c1d4b2f4096ae15c08d9f6b34e2")
  );
}
