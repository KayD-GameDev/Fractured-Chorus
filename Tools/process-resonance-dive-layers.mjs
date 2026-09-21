import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";

const FILES = [
  { src: "resonance_dive_01_base_v1.png", out: "01_Base.png", mode: "plate" },
  { src: "resonance_dive_02_border_v1.png", out: "02_Border.png", mode: "line" },
  { src: "resonance_dive_03_glass_v1.png", out: "03_Glass.png", mode: "soft" },
  { src: "resonance_dive_04_gradient_v1.png", out: "04_Gradient.png", mode: "soft" },
  { src: "resonance_dive_05_deco_left_v1.png", out: "05_Deco_Left.png", mode: "line" },
  { src: "resonance_dive_06_deco_right_v1.png", out: "06_Deco_Right.png", mode: "line" },
  { src: "resonance_dive_07_text_v1.png", out: "07_Text.png", mode: "line" },
  { src: "resonance_dive_08_glow_v1.png", out: "08_Glow.png", mode: "glow" },
  { src: "resonance_dive_09_particles_v1.png", out: "09_Particles.png", mode: "line" },
  { src: "resonance_dive_10_wave_v1.png", out: "10_Wave.png", mode: "line" },
  { src: "resonance_dive_11_scanline_v1.png", out: "11_Scanline.png", mode: "line" },
  { src: "resonance_dive_12_shadow_v1.png", out: "12_Shadow.png", mode: "shadow" },
  { src: "resonance_dive_state_normal_v1.png", out: "state_normal.png", mode: "plate" },
  { src: "resonance_dive_state_hover_v1.png", out: "state_hover.png", mode: "plate" },
  { src: "resonance_dive_state_pressed_v1.png", out: "state_pressed.png", mode: "plate" },
];

function keepAlpha(r, g, b, a, mode) {
  const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  const chroma = Math.max(r, g, b) - Math.min(r, g, b);
  let keep = 0;
  if (mode === "shadow") {
    if (lum > 6 && lum < 96) {
      keep = Math.min(1, (lum - 6) / 28);
    }
    if (chroma > 18) {
      keep = Math.max(keep, 0.7);
    }
  } else if (mode === "glow" || mode === "soft") {
    keep = Math.max(0, Math.min(1, (lum - 10) / 22));
    if (chroma > 10) {
      keep = Math.max(keep, Math.min(1, chroma / 40));
    }
  } else if (mode === "line") {
    keep = Math.max(0, Math.min(1, (lum - 16) / 14));
    if (chroma > 12) {
      keep = Math.max(keep, 0.9);
    }
  } else {
    keep = Math.max(0, Math.min(1, (lum - 14) / 16));
    if (chroma > 12) {
      keep = Math.max(keep, 0.92);
    }
  }
  return Math.round(a * keep);
}

function bbox(data, width, height) {
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const a = data[(y * width + x) * 4 + 3];
      if (a > 10) {
        if (x < minX) minX = x;
        if (y < minY) minY = y;
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
      }
    }
  }
  if (maxX < minX) {
    return null;
  }
  return { minX, minY, maxX, maxY };
}

async function loadKeyed(file) {
  const buf = fs.readFileSync(path.join(SRC, file.src));
  const { data, info } = await sharp(buf).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  for (let i = 0; i < pixels.length; i += 4) {
    pixels[i + 3] = keepAlpha(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3], file.mode);
  }
  return { pixels, width: info.width, height: info.height, out: file.out };
}

async function main() {
  fs.mkdirSync(ART, { recursive: true });
  fs.mkdirSync(RES, { recursive: true });

  const keyed = [];
  for (const file of FILES) {
    keyed.push(await loadKeyed(file));
  }

  const plate = keyed.find((k) => k.out === "state_normal.png");
  const box = bbox(plate.pixels, plate.width, plate.height);
  const padX = Math.round(plate.width * 0.03);
  const padY = Math.round(plate.height * 0.08);
  const left = Math.max(0, box.minX - padX);
  const top = Math.max(0, box.minY - padY);
  const right = Math.min(plate.width - 1, box.maxX + padX);
  const bottom = Math.min(plate.height - 1, box.maxY + padY);
  const cropW = right - left + 1;
  const cropH = bottom - top + 1;
  const outW = 1536;
  const outH = Math.max(256, Math.round((cropH / cropW) * outW));

  console.log("crop", { left, top, cropW, cropH, outW, outH });

  for (const layer of keyed) {
    const cropped = await sharp(layer.pixels, {
      raw: { width: layer.width, height: layer.height, channels: 4 },
    })
      .extract({ left, top, width: cropW, height: cropH })
      .resize(outW, outH, { fit: "fill" })
      .png()
      .toBuffer();

    fs.writeFileSync(path.join(ART, layer.out), cropped);
    fs.writeFileSync(path.join(RES, layer.out), cropped);
    console.log("wrote", layer.out);
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
