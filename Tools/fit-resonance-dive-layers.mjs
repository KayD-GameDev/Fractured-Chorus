import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";
const LOCK = path.join(ART, "state_normal.png");

const JOBS = [
  { src: "resonance_dive_04_gradient_v3.png", out: "04_Gradient.png" },
  { src: "resonance_dive_05_deco_left_v3.png", out: "05_Deco_Left.png" },
  { src: "resonance_dive_06_deco_right_v3.png", out: "06_Deco_Right.png" },
  { src: "resonance_dive_07_text_v3.png", out: "07_Text.png" },
  { src: "resonance_dive_09_particles_v3b.png", out: "09_Particles.png" },
  { src: "resonance_dive_10_wave_v3b.png", out: "10_Wave.png" },
  { src: "resonance_dive_11_scanline_v3b.png", out: "11_Scanline.png" },
];

function keepAlpha(r, g, b, a) {
  const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  const chroma = Math.max(r, g, b) - Math.min(r, g, b);
  let keep = Math.max(0, Math.min(1, (lum - 12) / 18));
  if (chroma > 10) keep = Math.max(keep, Math.min(1, chroma / 36));
  return Math.round(a * keep);
}

function writeBoth(name, buf) {
  fs.writeFileSync(path.join(ART, name), buf);
  fs.writeFileSync(path.join(RES, name), buf);
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

async function toPng(rgba, width, height) {
  return sharp(rgba, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

async function mapToLock(layer, lockW, lockH) {
  const scale = lockW / layer.width;
  const newH = Math.max(1, Math.round(layer.height * scale));
  const resized = await sharp(layer.pixels, {
    raw: { width: layer.width, height: layer.height, channels: 4 },
  })
    .resize(lockW, newH, { fit: "fill" })
    .png()
    .toBuffer();

  if (newH === lockH) return resized;
  if (newH > lockH) {
    const top = Math.round((newH - lockH) / 2);
    return sharp(resized).extract({ left: 0, top, width: lockW, height: lockH }).png().toBuffer();
  }

  const canvas = await sharp({
    create: {
      width: lockW,
      height: lockH,
      channels: 4,
      background: { r: 0, g: 0, b: 0, alpha: 0 },
    },
  })
    .png()
    .toBuffer();
  const top = Math.round((lockH - newH) / 2);
  return sharp(canvas).composite([{ input: resized, left: 0, top }]).png().toBuffer();
}

async function blurGray(gray, width, height, sigma) {
  return sharp(gray, { raw: { width, height, channels: 1 } }).blur(sigma).raw().toBuffer();
}

async function makeShadow(lock) {
  const { width, height, pixels } = lock;
  const n = width * height;
  const mask = Buffer.alloc(n);
  for (let i = 0; i < n; i += 1) {
    mask[i] = pixels[i * 4 + 3] >= 220 ? 255 : 0;
  }
  const shift = 16;
  const dropped = Buffer.alloc(n);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const sy = y - shift;
      const src = sy >= 0 ? mask[sy * width + x] : 0;
      dropped[y * width + x] = Math.max(0, src - mask[y * width + x]);
    }
  }
  const blurred = await blurGray(dropped, width, height, 7);
  const out = Buffer.alloc(n * 4);
  for (let i = 0; i < n; i += 1) {
    out[i * 4] = 8;
    out[i * 4 + 1] = 12;
    out[i * 4 + 2] = 28;
    out[i * 4 + 3] = Math.min(255, Math.round(blurred[i] * 0.55));
  }
  return toPng(out, width, height);
}

async function main() {
  const lock = await loadKeyed(LOCK);
  console.log("lock", lock.width, lock.height);

  for (const job of JOBS) {
    const keyed = await loadKeyed(path.join(SRC, job.src));
    const mapped = await mapToLock(keyed, lock.width, lock.height);
    writeBoth(job.out, mapped);
    console.log("wrote", job.out, keyed.width, keyed.height);
  }

  writeBoth("12_Shadow.png", await makeShadow(lock));
  const glow = fs.readFileSync(path.join(ART, "08_Glow.png"));
  writeBoth("02_Border.png", glow);
  console.log("wrote 12_Shadow.png, 02_Border.png");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
