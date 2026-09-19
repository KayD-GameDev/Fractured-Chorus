import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";

const JOBS = [
  { src: "resonance_dive_state_normal_v4.png", out: "state_normal.png" },
  { src: "resonance_dive_state_hover_v4.png", out: "state_hover.png" },
  { src: "resonance_dive_state_pressed_v4.png", out: "state_pressed.png" },
  { src: "resonance_dive_08_glow_v4.png", out: "08_Glow.png", glow: true },
];

function lum(r, g, b) {
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function keyBackdropOnly(pixels, width, height) {
  const n = width * height;
  const outside = Buffer.alloc(n);
  const queue = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const i = y * width + x;
    if (outside[i]) return;
    const o = i * 4;
    const L = lum(pixels[o], pixels[o + 1], pixels[o + 2]);
    const C = chroma(pixels[o], pixels[o + 1], pixels[o + 2]);
    if (L > 12 || C > 12) return;
    outside[i] = 1;
    queue.push(i);
  };
  for (let x = 0; x < width; x += 1) {
    push(x, 0);
    push(x, height - 1);
  }
  for (let y = 0; y < height; y += 1) {
    push(0, y);
    push(width - 1, y);
  }
  while (queue.length) {
    const i = queue.pop();
    const x = i % width;
    const y = (i / width) | 0;
    push(x + 1, y);
    push(x - 1, y);
    push(x, y + 1);
    push(x, y - 1);
  }
  for (let i = 0; i < n; i += 1) {
    if (!outside[i]) continue;
    const o = i * 4;
    pixels[o] = 0;
    pixels[o + 1] = 0;
    pixels[o + 2] = 0;
    pixels[o + 3] = 0;
  }
}

function keepBrightCore(pixels, width, height, radius) {
  const n = width * height;
  const core = Buffer.alloc(n);
  for (let i = 0; i < n; i += 1) {
    const o = i * 4;
    if (pixels[o + 3] < 10) continue;
    const L = lum(pixels[o], pixels[o + 1], pixels[o + 2]);
    const C = chroma(pixels[o], pixels[o + 1], pixels[o + 2]);
    if (L > 36 || C > 22) core[i] = 255;
  }
  const keep = dilate(core, width, height, radius);
  for (let i = 0; i < n; i += 1) {
    const o = i * 4;
    if (!keep[i]) {
      pixels[o] = 0;
      pixels[o + 1] = 0;
      pixels[o + 2] = 0;
      pixels[o + 3] = 0;
      continue;
    }
    const L = lum(pixels[o], pixels[o + 1], pixels[o + 2]);
    const C = chroma(pixels[o], pixels[o + 1], pixels[o + 2]);
    if (L < 22 && C < 20) {
      pixels[o] = 0;
      pixels[o + 1] = 0;
      pixels[o + 2] = 0;
      pixels[o + 3] = 0;
    }
  }
}

function stripGrayFog(pixels) {
  for (let i = 0; i < pixels.length; i += 4) {
    if (pixels[i + 3] < 8) continue;
    const L = lum(pixels[i], pixels[i + 1], pixels[i + 2]);
    const C = chroma(pixels[i], pixels[i + 1], pixels[i + 2]);
    if (C < 18 && L < 90) {
      pixels[i] = 0;
      pixels[i + 1] = 0;
      pixels[i + 2] = 0;
      pixels[i + 3] = 0;
    }
  }
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

function writeBoth(name, buf) {
  fs.writeFileSync(path.join(ART, name), buf);
  fs.writeFileSync(path.join(RES, name), buf);
}

async function loadKeyed(file, glow) {
  const { data, info } = await sharp(fs.readFileSync(path.join(SRC, file)))
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  keyBackdropOnly(pixels, info.width, info.height);
  if (glow) stripGrayFog(pixels);
  if (file.includes("hover")) keepBrightCore(pixels, info.width, info.height, 2);
  return { pixels, width: info.width, height: info.height };
}

async function toPng(rgba, width, height) {
  return sharp(rgba, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

async function blurGray(gray, width, height, sigma) {
  const rgba = Buffer.alloc(width * height * 4);
  for (let i = 0; i < width * height; i += 1) {
    const v = gray[i];
    rgba[i * 4] = v;
    rgba[i * 4 + 1] = v;
    rgba[i * 4 + 2] = v;
    rgba[i * 4 + 3] = 255;
  }
  const { data } = await sharp(rgba, { raw: { width, height, channels: 4 } })
    .blur(sigma)
    .raw()
    .toBuffer({ resolveWithObject: true });
  const out = Buffer.alloc(width * height);
  for (let i = 0; i < width * height; i += 1) out[i] = data[i * 4];
  return out;
}

function dilate(mask, width, height, radius) {
  const n = width * height;
  const out = Buffer.alloc(n);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      let m = 0;
      for (let dy = -radius; dy <= radius; dy += 1) {
        const ny = y + dy;
        if (ny < 0 || ny >= height) continue;
        for (let dx = -radius; dx <= radius; dx += 1) {
          const nx = x + dx;
          if (nx < 0 || nx >= width) continue;
          const v = mask[ny * width + nx];
          if (v > m) m = v;
        }
      }
      out[y * width + x] = m;
    }
  }
  return out;
}

async function rebuildShadow(plate, width, height) {
  const n = width * height;
  const hard = Buffer.alloc(n);
  for (let i = 0; i < n; i += 1) {
    hard[i] = plate[i * 4 + 3] >= 96 ? 255 : 0;
  }
  const closed = dilate(hard, width, height, 6);
  const shifted = Buffer.alloc(n);
  const shiftX = 18;
  const shiftY = 28;
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const sx = x - shiftX;
      const sy = y - shiftY;
      if (sx < 0 || sy < 0 || sx >= width || sy >= height) continue;
      shifted[y * width + x] = closed[sy * width + sx];
    }
  }
  const shadowBlur = await blurGray(shifted, width, height, 18);
  const shadow = Buffer.alloc(n * 4);
  for (let i = 0; i < n; i += 1) {
    const covered = hard[i] / 255;
    const a = Math.min(255, Math.round(shadowBlur[i] * 0.45 * (1 - covered)));
    if (a < 6) continue;
    shadow[i * 4] = 4;
    shadow[i * 4 + 1] = 6;
    shadow[i * 4 + 2] = 14;
    shadow[i * 4 + 3] = a;
  }
  return shadow;
}

function qa(label, data, width, height) {
  const box = bbox(data, width, height);
  const edge = {
    left: box.minX === 0,
    top: box.minY === 0,
    right: box.maxX === width - 1,
    bottom: box.maxY === height - 1,
  };
  console.log("qa", label, { width, height, box, edgeHit: edge });
  return box;
}

async function main() {
  const loaded = [];
  for (const job of JOBS) {
    const img = await loadKeyed(job.src, job.glow);
    loaded.push({ ...job, ...img });
  }

  let minX = Infinity;
  let minY = Infinity;
  let maxX = 0;
  let maxY = 0;
  for (const img of loaded) {
    const box = bbox(img.pixels, img.width, img.height);
    if (!box) throw new Error(`empty ${img.src}`);
    if (box.minX < minX) minX = box.minX;
    if (box.minY < minY) minY = box.minY;
    if (box.maxX > maxX) maxX = box.maxX;
    if (box.maxY > maxY) maxY = box.maxY;
    console.log("src bbox", img.out, box, `${img.width}x${img.height}`);
  }

  const pad = 56;
  const srcW = loaded[0].width;
  const srcH = loaded[0].height;
  for (const img of loaded) {
    if (img.width !== srcW || img.height !== srcH) {
      throw new Error(`size mismatch ${img.src} ${img.width}x${img.height}`);
    }
  }
  const left = Math.max(0, minX - pad);
  const top = Math.max(0, minY - pad);
  const right = Math.min(srcW - 1, maxX + pad);
  const bottom = Math.min(srcH - 1, maxY + pad);
  const cropW = right - left + 1;
  const cropH = bottom - top + 1;
  const outW = 1536;
  const outH = Math.max(1, Math.round((outW * cropH) / cropW));
  console.log("crop", { left, top, cropW, cropH, outW, outH, pad });

  let plate = null;
  for (const img of loaded) {
    const cropped = await sharp(img.pixels, {
      raw: { width: img.width, height: img.height, channels: 4 },
    })
      .extract({ left, top, width: cropW, height: cropH })
      .resize(outW, outH, { fit: "fill" })
      .raw()
      .toBuffer({ resolveWithObject: true });
    const data = Buffer.from(cropped.data);
    qa(img.out, data, cropped.info.width, cropped.info.height);
    writeBoth(img.out, await toPng(data, cropped.info.width, cropped.info.height));
    if (img.out === "state_normal.png") plate = data;
    console.log("wrote", img.out);
  }

  if (plate) {
    writeBoth("12_Shadow.png", await toPng(await rebuildShadow(plate, outW, outH), outW, outH));
    console.log("wrote 12_Shadow.png");
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
