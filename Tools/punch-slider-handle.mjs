import sharp from "sharp";

const HANDLE = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit/ui_config_slider_handle_v1.png";
const { data, info } = await sharp(HANDLE).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const w = info.width;
const h = info.height;
const cx = (w - 1) * 0.5;
const cy = (h - 1) * 0.5;
const keepR = 26;
const fadeR = 32;

for (let y = 0; y < h; y++) {
  for (let x = 0; x < w; x++) {
    const o = (y * w + x) * 4;
    const dist = Math.hypot(x - cx, y - cy);
    if (dist <= keepR) continue;
    if (dist >= fadeR) {
      data[o] = 0;
      data[o + 1] = 0;
      data[o + 2] = 0;
      data[o + 3] = 0;
      continue;
    }
    const t = 1 - (dist - keepR) / (fadeR - keepR);
    data[o + 3] = Math.round(data[o + 3] * t);
    if (data[o + 3] < 8) {
      data[o] = 0;
      data[o + 1] = 0;
      data[o + 2] = 0;
      data[o + 3] = 0;
    }
  }
}

await sharp(data, { raw: { width: w, height: h, channels: 4 } }).png().toFile(HANDLE);
console.log("handle circular", w, h);
