import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC_DIR = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";
const LOCK = path.join(ART, "state_normal.png");

const JOBS = [
  { src: "resonance_dive_state_hover_v2.png", out: "state_hover.png" },
  { src: "resonance_dive_state_pressed_v2.png", out: "state_pressed.png" },
];

function keepAlpha(r, g, b, a) {
  const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  const chroma = Math.max(r, g, b) - Math.min(r, g, b);
  let keep = Math.max(0, Math.min(1, (lum - 14) / 16));
  if (chroma > 12) keep = Math.max(keep, 0.92);
  return Math.round(a * keep);
}

function bbox(data, width, height) {
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      if (data[(y * width + x) * 4 + 3] <= 10) continue;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }
  if (maxX < minX) return null;
  return { minX, minY, maxX, maxY, w: maxX - minX + 1, h: maxY - minY + 1 };
}

async function loadKeyed(file) {
  const { data, info } = await sharp(fs.readFileSync(file)).ensureAlpha().raw().toBuffer({
    resolveWithObject: true,
  });
  const pixels = Buffer.from(data);
  for (let i = 0; i < pixels.length; i += 4) {
    pixels[i + 3] = keepAlpha(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
  }
  return { pixels, width: info.width, height: info.height };
}

async function main() {
  const lock = await loadKeyed(LOCK);
  const lockBox = bbox(lock.pixels, lock.width, lock.height);
  console.log("lock", { w: lock.width, h: lock.height, lockBox });

  for (const job of JOBS) {
    const keyed = await loadKeyed(path.join(SRC_DIR, job.src));
    const box = bbox(keyed.pixels, keyed.width, keyed.height);
    const extracted = await sharp(keyed.pixels, {
      raw: { width: keyed.width, height: keyed.height, channels: 4 },
    })
      .extract({ left: box.minX, top: box.minY, width: box.w, height: box.h })
      .resize(lockBox.w, lockBox.h, { fit: "fill" })
      .png()
      .toBuffer();

    const canvas = await sharp({
      create: { width: lock.width, height: lock.height, channels: 4, background: { r: 0, g: 0, b: 0, alpha: 0 } },
    })
      .png()
      .toBuffer();

    const placed = await sharp(canvas)
      .composite([{ input: extracted, left: lockBox.minX, top: lockBox.minY }])
      .png()
      .toBuffer();

    fs.writeFileSync(path.join(ART, job.out), placed);
    fs.writeFileSync(path.join(RES, job.out), placed);
    console.log("wrote", job.out, { srcBox: box, placedAt: lockBox });
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
