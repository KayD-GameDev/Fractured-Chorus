import fs from "fs";
import { createRequire } from "module";

const require = createRequire("C:/Users/Asus/AppData/Local/Temp/package.json");
const Jimp = require("jimp");

const REF =
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/HubMenu/ui_hub_menu_icon_bonds.png";
const OUTS = [
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/HubMenu/ui_hub_menu_icon_stats.png",
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/HubMenu/ui_hub_menu_icon_stats.png",
];

function rgbToHsl(r, g, b) {
  r /= 255;
  g /= 255;
  b /= 255;
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  const l = (max + min) / 2;
  if (max === min) {
    return [0, 0, l];
  }
  const d = max - min;
  const s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
  let h;
  switch (max) {
    case r:
      h = (g - b) / d + (g < b ? 6 : 0);
      break;
    case g:
      h = (b - r) / d + 2;
      break;
    default:
      h = (r - g) / d + 4;
  }
  h /= 6;
  return [h, s, l];
}

function hslToRgb(h, s, l) {
  if (s === 0) {
    const v = Math.round(l * 255);
    return [v, v, v];
  }
  const hue2rgb = (p, q, t) => {
    if (t < 0) t += 1;
    if (t > 1) t -= 1;
    if (t < 1 / 6) return p + (q - p) * 6 * t;
    if (t < 1 / 2) return q;
    if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
    return p;
  };
  const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
  const p = 2 * l - q;
  return [
    Math.round(hue2rgb(p, q, h + 1 / 3) * 255),
    Math.round(hue2rgb(p, q, h) * 255),
    Math.round(hue2rgb(p, q, h - 1 / 3) * 255),
  ];
}

function lum(r, g, b) {
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function sat(r, g, b) {
  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  return max === 0 ? 0 : (max - min) / max;
}

function refHueAt(ref, x, y) {
  const w = ref.bitmap.width;
  const h = ref.bitmap.height;
  const sx = Math.min(w - 1, Math.max(0, Math.round((x / w) * (w - 1))));
  const sy = Math.min(h - 1, Math.max(0, Math.round((y / h) * (h - 1))));
  const idx = (sy * w + sx) * 4;
  const a = ref.bitmap.data[idx + 3];
  if (a < 40) {
    return 0.56;
  }
  const [hue] = rgbToHsl(
    ref.bitmap.data[idx],
    ref.bitmap.data[idx + 1],
    ref.bitmap.data[idx + 2],
  );
  return hue;
}

async function recolorStats(srcPath, ref) {
  const im = await Jimp.read(srcPath);
  im.scan(0, 0, im.bitmap.width, im.bitmap.height, (x, y, idx) => {
    const a = im.bitmap.data[idx + 3];
    if (a < 24) {
      return;
    }
    const r = im.bitmap.data[idx];
    const g = im.bitmap.data[idx + 1];
    const b = im.bitmap.data[idx + 2];
    const l = lum(r, g, b);
    const s = sat(r, g, b);

    if (l > 228 && s < 0.12) {
      return;
    }

    const isRed = r > 150 && r > g + 28 && r > b + 28;
    const isPurple = b > g + 8 && r > g + 4 && b > 90;
    const isMagenta = r > 110 && b > 110 && g < Math.min(r, b) - 10;
    if (!isRed && !isPurple && !isMagenta && s < 0.18) {
      return;
    }

    const [h0, s0, l0] = rgbToHsl(r, g, b);
    const targetHue = refHueAt(ref, x, y);
    const targetSat = Math.min(1, s0 * 0.92 + 0.08);
    const targetLight = Math.min(0.92, Math.max(0.08, l0));
    let [nr, ng, nb] = hslToRgb(targetHue, targetSat, targetLight);
    if (isRed) {
      [nr, ng, nb] = hslToRgb(0.56, Math.min(1, targetSat + 0.08), targetLight);
    }
    im.bitmap.data[idx] = nr;
    im.bitmap.data[idx + 1] = ng;
    im.bitmap.data[idx + 2] = nb;
  });
  return im;
}

const ref = await Jimp.read(REF);
for (const out of OUTS) {
  const im = await recolorStats(out, ref);
  await im.writeAsync(out);
  console.log("updated", out);
}
