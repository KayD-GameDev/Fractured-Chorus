import sharp from "sharp";

const files = [
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit/ui_config_slider_fill_v1.png",
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit/ui_config_slider_track_v1.png",
];

for (const file of files) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width;
  const h = info.height;
  const rows = [];
  for (let y = 0; y < h; y++) {
    let sumL = 0;
    let sumB = 0;
    let sumC = 0;
    let a0 = 0;
    for (let x = 0; x < w; x++) {
      const o = (y * w + x) * 4;
      const r = data[o];
      const g = data[o + 1];
      const b = data[o + 2];
      const a = data[o + 3];
      if (a < 8) a0++;
      sumL += (r + g + b) / 3;
      sumB += (r + b) * 0.5 - g;
      sumC += Math.max(r, g, b) - Math.min(r, g, b);
    }
    rows.push({
      y,
      L: +(sumL / w).toFixed(1),
      B: +(sumB / w).toFixed(1),
      C: +(sumC / w).toFixed(1),
      a0pct: +((a0 / w) * 100).toFixed(0),
    });
  }
  console.log(file.split("/").pop());
  console.log(JSON.stringify(rows));
}
