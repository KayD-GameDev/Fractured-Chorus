import path from "node:path";
import sharp from "sharp";

const FILE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit/ui_config_slider_fill_v1.png";
const { data, info } = await sharp(FILE).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const w = info.width;
const h = info.height;
const px = data;
const cap = 48;

for (let y = 0; y < h; y++) {
  for (let x = 0; x < cap; x++) {
    const dst = (y * w + (w - 1 - x)) * 4;
    px[dst] = 0;
    px[dst + 1] = 0;
    px[dst + 2] = 0;
    px[dst + 3] = 0;
  }
}

for (let y = 0; y < h; y++) {
  for (let x = 0; x < cap; x++) {
    const src = (y * w + x) * 4;
    const dst = (y * w + (w - 1 - x)) * 4;
    px[dst] = px[src];
    px[dst + 1] = px[src + 1];
    px[dst + 2] = px[src + 2];
    px[dst + 3] = px[src + 3];
  }
}

await sharp(px, { raw: { width: w, height: h, channels: 4 } }).png().toFile(FILE);
console.log("capped", w, h, cap);
