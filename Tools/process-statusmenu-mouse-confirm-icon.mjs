import { createRequire } from "module";

const require = createRequire("C:/Users/Asus/AppData/Local/Temp/package.json");
const Jimp = require("jimp");

const SRC =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/statusmenu_prompt_mouse_confirm_ref_v2.png";
const OUTS = [
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_prompt_confirm.png",
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/StatusMenu/statusmenu_prompt_confirm.png",
];

const im = await Jimp.read(SRC);
im.scan(0, 0, im.bitmap.width, im.bitmap.height, (x, y, idx) => {
  const r = im.bitmap.data[idx];
  const g = im.bitmap.data[idx + 1];
  const b = im.bitmap.data[idx + 2];
  const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  if (lum < 30) {
    im.bitmap.data[idx + 3] = 0;
    return;
  }
  im.bitmap.data[idx] = 255;
  im.bitmap.data[idx + 1] = 255;
  im.bitmap.data[idx + 2] = 255;
  im.bitmap.data[idx + 3] = Math.min(255, Math.round((lum - 22) * 3.1));
});

for (const out of OUTS) {
  await im.clone().writeAsync(out);
  console.log("wrote", out);
}
