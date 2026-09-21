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

function harden(im) {
  im.scan(0, 0, im.bitmap.width, im.bitmap.height, (x, y, idx) => {
    const r = im.bitmap.data[idx];
    const g = im.bitmap.data[idx + 1];
    const b = im.bitmap.data[idx + 2];
    const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
    if (lum < 96) {
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

let im = harden(await Jimp.read(SRC));
im = im.resize(1536, 1536, Jimp.RESIZE_NEAREST_NEIGHBOR);
im = harden(im);

for (const out of OUTS) {
  await im.clone().writeAsync(out);
  console.log("wrote", out, im.bitmap.width, "x", im.bitmap.height);
}

for (const metaPath of METAS) {
  if (!fs.existsSync(metaPath)) continue;
  let meta = fs.readFileSync(metaPath, "utf8");
  meta = meta.replace(/filterMode: 1/g, "filterMode: 0");
  meta = meta.replace(/textureCompression: 1/g, "textureCompression: 0");
  meta = meta.replace(/width: \d+\n        height: \d+/g, "width: 1536\n        height: 1536");
  fs.writeFileSync(metaPath, meta);
  console.log("meta", metaPath.replace(/.*FracturedChorus\//, ""));
}
