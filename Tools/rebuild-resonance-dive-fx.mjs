import fs from "fs";
import path from "path";
import sharp from "sharp";

const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";
const SRC = path.join(ART, "state_normal.png");

function writeBoth(name, buf) {
  fs.writeFileSync(path.join(ART, name), buf);
  fs.writeFileSync(path.join(RES, name), buf);
}

async function loadRgba(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data: Buffer.from(data), width: info.width, height: info.height };
}

async function toPng(rgba, width, height) {
  return sharp(rgba, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

async function blurGray(gray, width, height, sigma) {
  return sharp(gray, { raw: { width, height, channels: 1 } })
    .blur(sigma)
    .raw()
    .toBuffer();
}

function tightMask(data, width, height) {
  const n = width * height;
  const mask = Buffer.alloc(n);
  let solid = 0;
  for (let i = 0; i < n; i += 1) {
    if (data[i * 4 + 3] < 220) continue;
    mask[i] = 255;
    solid += 1;
  }
  return { mask, solid, ratio: solid / n };
}

function cleanPlateFog(data) {
  for (let i = 0; i < data.length; i += 4) {
    const a = data[i + 3];
    const lum = 0.2126 * data[i] + 0.7152 * data[i + 1] + 0.0722 * data[i + 2];
    const chroma = Math.max(data[i], data[i + 1], data[i + 2]) - Math.min(data[i], data[i + 1], data[i + 2]);
    if (a < 72 && lum < 48 && chroma < 22) {
      data[i + 3] = 0;
    }
  }
}

function stats(mask, width, height) {
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  let count = 0;
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      if (mask[y * width + x] < 128) continue;
      count += 1;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }
  return { count, bbox: count ? { minX, minY, maxX, maxY, w: maxX - minX + 1, h: maxY - minY + 1 } : null };
}

async function main() {
  for (const name of ["state_normal.png", "state_hover.png", "state_pressed.png"]) {
    const plate = await loadRgba(path.join(ART, name));
    cleanPlateFog(plate.data);
    writeBoth(name, await toPng(plate.data, plate.width, plate.height));
  }

  const src = await loadRgba(SRC);
  const { width, height, data } = src;
  const { mask, solid, ratio } = tightMask(data, width, height);
  const maskStats = stats(mask, width, height);
  console.log({ width, height, solid, ratio: Number(ratio.toFixed(4)), bbox: maskStats.bbox });

  const glowBlur = await blurGray(mask, width, height, 8);
  const glow = Buffer.alloc(width * height * 4);
  for (let i = 0; i < width * height; i += 1) {
    const ring = Math.max(0, glowBlur[i] - mask[i] * 0.92);
    const a = Math.min(255, Math.round(ring * 1.65));
    glow[i * 4] = 159;
    glow[i * 4 + 1] = 214;
    glow[i * 4 + 2] = 255;
    glow[i * 4 + 3] = a;
  }
  writeBoth("08_Glow.png", await toPng(glow, width, height));

  const shadowBlur = await blurGray(mask, width, height, 18);
  const shift = Math.round(height * 0.028);
  const shadow = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      const sy = y - shift;
      const srcA = sy >= 0 && sy < height ? shadowBlur[sy * width + x] : 0;
      const under = mask[i] > 128 ? 0.18 : 1;
      const a = Math.min(255, Math.round(srcA * 0.38 * under));
      const o = i * 4;
      shadow[o] = 8;
      shadow[o + 1] = 12;
      shadow[o + 2] = 28;
      shadow[o + 3] = a;
    }
  }
  writeBoth("12_Shadow.png", await toPng(shadow, width, height));

  const dilate = await blurGray(mask, width, height, 1.15);
  const border = Buffer.alloc(width * height * 4);
  for (let i = 0; i < width * height; i += 1) {
    const edge = Math.max(0, dilate[i] - mask[i] * 0.7);
    const a = Math.min(255, Math.round(edge * 2.4));
    border[i * 4] = 248;
    border[i * 4 + 1] = 251;
    border[i * 4 + 2] = 255;
    border[i * 4 + 3] = a;
  }
  writeBoth("02_Border.png", await toPng(border, width, height));

  const bandY = Math.round(height * 0.52);
  const scan = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y += 1) {
    const dy = (y - bandY) / 3.2;
    const band = Math.exp(-dy * dy);
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      const a = mask[i] > 128 ? Math.min(255, Math.round(band * 210)) : 0;
      const o = i * 4;
      scan[o] = 200;
      scan[o + 1] = 230;
      scan[o + 2] = 255;
      scan[o + 3] = a;
    }
  }
  writeBoth("11_Scanline.png", await toPng(scan, width, height));

  const preview = Buffer.from(data);
  const overlay = (srcLayer, strength) => {
    for (let i = 0; i < width * height; i += 1) {
      const sa = (srcLayer[i * 4 + 3] / 255) * strength;
      if (sa <= 0) continue;
      const o = i * 4;
      preview[o] = Math.round(preview[o] * (1 - sa) + srcLayer[o] * sa);
      preview[o + 1] = Math.round(preview[o + 1] * (1 - sa) + srcLayer[o + 1] * sa);
      preview[o + 2] = Math.round(preview[o + 2] * (1 - sa) + srcLayer[o + 2] * sa);
      preview[o + 3] = Math.max(preview[o + 3], srcLayer[o + 3]);
    }
  };
  overlay(shadow, 1);
  overlay(glow, 0.85);
  overlay(scan, 0.35);
  const previewPath = "D:/Fractured-Chorus1/Tools/_preview_stack.png";
  fs.writeFileSync(previewPath, await toPng(preview, width, height));
  const artPreview = path.join(ART, "_preview_stack.png");
  if (fs.existsSync(artPreview)) fs.unlinkSync(artPreview);
  const artPreviewMeta = `${artPreview}.meta`;
  if (fs.existsSync(artPreviewMeta)) fs.unlinkSync(artPreviewMeta);
  console.log("preview", previewPath);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
