import fs from "fs";
import { createRequire } from "module";

const require = createRequire("C:/Users/Asus/AppData/Local/Temp/package.json");
const Jimp = require("jimp");

const SRC =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/statusmenu_prompt_mouse_confirm_ref_v2.png";
const OUTS = [
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_prompt_confirm.png",
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/StatusMenu/statusmenu_prompt_confirm.png",
];
const METAS = [
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_prompt_confirm.png.meta",
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/StatusMenu/statusmenu_prompt_confirm.png.meta",
];
const OUT_SIZE = 128;

function binarize(im, cutoff = 88) {
  im.scan(0, 0, im.bitmap.width, im.bitmap.height, (x, y, idx) => {
    const r = im.bitmap.data[idx];
    const g = im.bitmap.data[idx + 1];
    const b = im.bitmap.data[idx + 2];
    const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
    if (lum < cutoff) {
      im.bitmap.data[idx + 3] = 0;
      return;
    }
    im.bitmap.data[idx] = 255;
    im.bitmap.data[idx + 1] = 255;
    im.bitmap.data[idx + 2] = 255;
    im.bitmap.data[idx + 3] = 255;
  });
  return im;
}

let im = binarize(await Jimp.read(SRC));
im = im.resize(OUT_SIZE, OUT_SIZE, Jimp.RESIZE_NEAREST_NEIGHBOR);
im = binarize(im, 128);

for (const out of OUTS) {
  await im.clone().writeAsync(out);
  console.log("wrote", out, OUT_SIZE, "x", OUT_SIZE);
}

for (const metaPath of METAS) {
  if (!fs.existsSync(metaPath)) continue;
  let meta = fs.readFileSync(metaPath, "utf8");
  meta = meta.replace(/filterMode: 1/g, "filterMode: 0");
  meta = meta.replace(/spriteExtrude: 1/g, "spriteExtrude: 0");
  meta = meta.replace(/textureCompression: 1/g, "textureCompression: 0");
  meta = meta.replace(/width: \d+\n        height: \d+/g, `width: ${OUT_SIZE}\n        height: ${OUT_SIZE}`);
  meta = meta.replace(/spritePivot: \{x: 0, y: 0\}/g, "spritePivot: {x: 0.5, y: 0.5}");
  meta = meta.replace(/pivot: \{x: 0, y: 0\}/g, "pivot: {x: 0.5, y: 0.5}");
  meta = meta.replace(/spritePixelsToUnits: \d+/g, "spritePixelsToUnits: 256");
  fs.writeFileSync(metaPath, meta);
}
