import fs from "fs";
import path from "path";
import sharp from "sharp";

const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";
const STATES = ["state_normal.png", "state_hover.png", "state_pressed.png"];

async function load(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data: Buffer.from(data), width: info.width, height: info.height };
}

async function toPng(rgba, width, height) {
  return sharp(rgba, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

function writeBoth(name, buf) {
  fs.writeFileSync(path.join(ART, name), buf);
  fs.writeFileSync(path.join(RES, name), buf);
}

async function main() {
  const grad = await load(path.join(ART, "04_Gradient.png"));
  for (const name of STATES) {
    const plate = await load(path.join(ART, name));
    const { width, height, data } = plate;
    const out = Buffer.from(data);
    for (let y = 0; y < height; y += 1) {
      for (let x = 0; x < width; x += 1) {
        const i = y * width + x;
        const o = i * 4;
        const plateA = data[o + 3];
        const gA = grad.data[o + 3];
        if (plateA >= 56) continue;
        if (gA < 48) continue;
        const t = x / Math.max(1, width - 1);
        const glassR = Math.round(8 + t * 22);
        const glassG = Math.round(14 + t * 28);
        const glassB = Math.round(28 + t * 42);
        const fillA = 242;
        const srcA = plateA / 255;
        const dstA = (1 - srcA) * (fillA / 255);
        const a = Math.min(255, Math.round((srcA + dstA) * 255));
        out[o] = Math.round(data[o] * srcA + glassR * dstA);
        out[o + 1] = Math.round(data[o + 1] * srcA + glassG * dstA);
        out[o + 2] = Math.round(data[o + 2] * srcA + glassB * dstA);
        out[o + 3] = a;
      }
    }
    writeBoth(name, await toPng(out, width, height));
    console.log("filled", name);
  }

  const previewPlate = await load(path.join(ART, "state_normal.png"));
  const sky = Buffer.alloc(previewPlate.width * previewPlate.height * 4);
  for (let i = 0; i < previewPlate.width * previewPlate.height; i += 1) {
    sky[i * 4] = 126;
    sky[i * 4 + 1] = 182;
    sky[i * 4 + 2] = 217;
    sky[i * 4 + 3] = 255;
    const a = previewPlate.data[i * 4 + 3] / 255;
    sky[i * 4] = Math.round(sky[i * 4] * (1 - a) + previewPlate.data[i * 4] * a);
    sky[i * 4 + 1] = Math.round(sky[i * 4 + 1] * (1 - a) + previewPlate.data[i * 4 + 1] * a);
    sky[i * 4 + 2] = Math.round(sky[i * 4 + 2] * (1 - a) + previewPlate.data[i * 4 + 2] * a);
  }
  fs.writeFileSync(
    "D:/Fractured-Chorus1/Tools/_preview_plate_on_sky.png",
    await toPng(sky, previewPlate.width, previewPlate.height),
  );
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
