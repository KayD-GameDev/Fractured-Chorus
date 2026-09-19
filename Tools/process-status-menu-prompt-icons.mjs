import fs from "fs";
import { createRequire } from "module";

const require = createRequire("C:/Users/Asus/AppData/Local/Temp/package.json");
const Jimp = require("jimp");

const SRC_CONFIRM =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/statusmenu_prompt_mouse_confirm_gen.png";
const SRC_BACK =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/statusmenu_prompt_esc_back_gen.png";

const OUTS = [
  {
    src: SRC_CONFIRM,
    art: "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_prompt_confirm.png",
    res: "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/StatusMenu/statusmenu_prompt_confirm.png",
  },
  {
    src: SRC_BACK,
    art: "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_prompt_close.png",
    res: "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/StatusMenu/statusmenu_prompt_close.png",
  },
];

function keyToAlpha(im) {
  im.scan(0, 0, im.bitmap.width, im.bitmap.height, (x, y, idx) => {
    const r = im.bitmap.data[idx];
    const g = im.bitmap.data[idx + 1];
    const b = im.bitmap.data[idx + 2];
    const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
    if (lum < 28) {
      im.bitmap.data[idx + 3] = 0;
      return;
    }
    im.bitmap.data[idx] = 255;
    im.bitmap.data[idx + 1] = 255;
    im.bitmap.data[idx + 2] = 255;
    im.bitmap.data[idx + 3] = Math.min(255, Math.round((lum - 24) * 3.2));
  });
  return im;
}

for (const spec of OUTS) {
  const im = keyToAlpha(await Jimp.read(spec.src));
  await im.writeAsync(spec.art);
  await im.writeAsync(spec.res);
  console.log("wrote", spec.art);
}
