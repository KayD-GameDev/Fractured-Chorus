import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";

function writeBoth(name, buf) {
  fs.writeFileSync(path.join(ART, name), buf);
  fs.writeFileSync(path.join(RES, name), buf);
}

async function loadRgba(file) {
  const { data, info } = await sharp(fs.readFileSync(file)).ensureAlpha().raw().toBuffer({
    resolveWithObject: true,
  });
  return { data: Buffer.from(data), width: info.width, height: info.height };
}

async function toPng(rgba, width, height) {
  return sharp(rgba, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

async function blurGray(gray, width, height, sigma) {
  const rgba = Buffer.alloc(width * height * 4);
  for (let i = 0; i < width * height; i += 1) {
    const v = gray[i];
    const o = i * 4;
    rgba[o] = v;
    rgba[o + 1] = v;
    rgba[o + 2] = v;
    rgba[o + 3] = 255;
  }
  const { data, info } = await sharp(rgba, { raw: { width, height, channels: 4 } })
    .blur(sigma)
    .raw()
    .toBuffer({ resolveWithObject: true });
  if (info.width !== width || info.height !== height) {
    throw new Error(`blur size ${info.width}x${info.height}`);
  }
  const out = Buffer.alloc(width * height);
  for (let i = 0; i < width * height; i += 1) {
    out[i] = data[i * 4];
  }
  return out;
}

function dilateMax(src, width, height, radius) {
  const out = Buffer.alloc(width * height);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      let m = 0;
      for (let dy = -radius; dy <= radius; dy += 1) {
        const ny = y + dy;
        if (ny < 0 || ny >= height) continue;
        for (let dx = -radius; dx <= radius; dx += 1) {
          const nx = x + dx;
          if (nx < 0 || nx >= width) continue;
          const v = src[ny * width + nx];
          if (v > m) m = v;
        }
      }
      out[y * width + x] = m;
    }
  }
  return out;
}

function keepCrystalAlpha(r, g, b, a) {
  const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  const chroma = Math.max(r, g, b) - Math.min(r, g, b);
  let keep = Math.max(0, Math.min(1, (lum - 18) / 22));
  if (chroma > 14) keep = Math.max(keep, Math.min(1, chroma / 40));
  return Math.round(a * keep);
}

function largestComponent(img, minA) {
  const { data, width, height } = img;
  const n = width * height;
  const seen = Buffer.alloc(n);
  let best = null;
  const dirs = [
    [1, 0],
    [-1, 0],
    [0, 1],
    [0, -1],
  ];
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      if (seen[i] || data[i * 4 + 3] < minA) continue;
      const q = [[x, y]];
      seen[i] = 1;
      const cells = [];
      let minX = x;
      let minY = y;
      let maxX = x;
      let maxY = y;
      while (q.length) {
        const [cx, cy] = q.pop();
        cells.push([cx, cy]);
        minX = Math.min(minX, cx);
        minY = Math.min(minY, cy);
        maxX = Math.max(maxX, cx);
        maxY = Math.max(maxY, cy);
        for (const [dx, dy] of dirs) {
          const nx = cx + dx;
          const ny = cy + dy;
          if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
          const ni = ny * width + nx;
          if (seen[ni] || data[ni * 4 + 3] < minA) continue;
          seen[ni] = 1;
          q.push([nx, ny]);
        }
      }
      if (!best || cells.length > best.cells.length) {
        best = { cells, minX, minY, maxX, maxY };
      }
    }
  }
  return best;
}

function cropComponent(img, piece, pad) {
  const pw = piece.maxX - piece.minX + 1;
  const ph = piece.maxY - piece.minY + 1;
  const tw = pw + pad * 2;
  const th = ph + pad * 2;
  const buf = Buffer.alloc(tw * th * 4);
  for (const [cx, cy] of piece.cells) {
    const s = (cy * img.width + cx) * 4;
    const dx = cx - piece.minX + pad;
    const dy = cy - piece.minY + pad;
    const d = (dy * tw + dx) * 4;
    buf[d] = img.data[s];
    buf[d + 1] = img.data[s + 1];
    buf[d + 2] = img.data[s + 2];
    buf[d + 3] = img.data[s + 3];
  }
  return { data: buf, width: tw, height: th };
}

async function isolateCrystal(file, minA = 40) {
  const img = await loadRgba(file);
  for (let i = 0; i < img.data.length; i += 4) {
    img.data[i + 3] = keepCrystalAlpha(img.data[i], img.data[i + 1], img.data[i + 2], img.data[i + 3]);
  }
  const piece = largestComponent(img, minA);
  if (!piece || piece.cells.length < 80) {
    return null;
  }
  const cropped = cropComponent(img, piece, 8);
  const maxSide = 96;
  const scale = Math.min(1, maxSide / Math.max(cropped.width, cropped.height));
  const tw = Math.max(16, Math.round(cropped.width * scale));
  const th = Math.max(16, Math.round(cropped.height * scale));
  const { data, info } = await sharp(cropped.data, {
    raw: { width: cropped.width, height: cropped.height, channels: 4 },
  })
    .resize(tw, th, { fit: "fill", kernel: "lanczos3" })
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  return { data: Buffer.from(data), width: info.width, height: info.height };
}

function stamp(dest, destW, destH, src, x, y) {
  const ox = Math.round(x - src.width / 2);
  const oy = Math.round(y - src.height / 2);
  for (let sy = 0; sy < src.height; sy += 1) {
    const dy = oy + sy;
    if (dy < 0 || dy >= destH) continue;
    for (let sx = 0; sx < src.width; sx += 1) {
      const dx = ox + sx;
      if (dx < 0 || dx >= destW) continue;
      const si = (sy * src.width + sx) * 4;
      const sa = src.data[si + 3] / 255;
      if (sa < 0.02) continue;
      const di = (dy * destW + dx) * 4;
      const da = dest[di + 3] / 255;
      const outA = sa + da * (1 - sa);
      if (outA < 0.001) continue;
      dest[di] = Math.round((src.data[si] * sa + dest[di] * da * (1 - sa)) / outA);
      dest[di + 1] = Math.round((src.data[si + 1] * sa + dest[di + 1] * da * (1 - sa)) / outA);
      dest[di + 2] = Math.round((src.data[si + 2] * sa + dest[di + 2] * da * (1 - sa)) / outA);
      dest[di + 3] = Math.round(outA * 255);
    }
  }
}

async function main() {
  const plate = await loadRgba(path.join(ART, "04_Gradient.png"));
  const decoL = await loadRgba(path.join(ART, "05_Deco_Left.png"));
  const decoR = await loadRgba(path.join(ART, "06_Deco_Right.png"));
  const { width, height, data: plateData } = plate;
  const n = width * height;
  const mask = Buffer.alloc(n);
  const union = Buffer.alloc(n);
  let minX = width;
  let minY = height;
  let maxX = 0;
  let maxY = 0;
  for (let i = 0; i < n; i += 1) {
    const a = plateData[i * 4 + 3];
    mask[i] = a;
    const ua = Math.max(a, decoL.data[i * 4 + 3], decoR.data[i * 4 + 3]);
    union[i] = ua;
    if (a < 24) continue;
    const x = i % width;
    const y = Math.floor(i / width);
    minX = Math.min(minX, x);
    minY = Math.min(minY, y);
    maxX = Math.max(maxX, x);
    maxY = Math.max(maxY, y);
  }
  console.log("plate bbox", minX, minY, maxX, maxY, "size", width, height);

  const base = Buffer.alloc(n * 4);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      const a = mask[i];
      if (a < 2) continue;
      const t = (x - minX) / Math.max(1, maxX - minX);
      const o = i * 4;
      base[o] = Math.round(10 + t * 18);
      base[o + 1] = Math.round(16 + t * 24);
      base[o + 2] = Math.round(30 + t * 38);
      base[o + 3] = a;
    }
  }
  writeBoth("01_Base.png", await toPng(base, width, height));

  const hard = Buffer.alloc(n);
  for (let i = 0; i < n; i += 1) {
    hard[i] = mask[i] >= 96 ? 255 : 0;
  }
  const hardDilate = dilateMax(hard, width, height, 3);
  const border = Buffer.alloc(n * 4);
  for (let i = 0; i < n; i += 1) {
    if (hardDilate[i] < 255 || hard[i] >= 255) continue;
    const t = (i % width) / Math.max(1, width - 1);
    border[i * 4] = Math.round(232 + t * 16);
    border[i * 4 + 1] = Math.round(246 - t * 40);
    border[i * 4 + 2] = 255;
    border[i * 4 + 3] = 235;
  }
  writeBoth("02_Border.png", await toPng(border, width, height));

  const glass = Buffer.alloc(n * 4);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      const a0 = mask[i];
      if (a0 < 8) continue;
      const nx = (x - minX) / Math.max(1, maxX - minX);
      const ny = (y - minY) / Math.max(1, maxY - minY);
      const u = nx * 0.62 + ny * 0.38;
      const band = Math.exp(-Math.pow((u - 0.22) / 0.08, 2));
      const a = Math.min(160, Math.round(a0 * band * 0.55));
      if (a < 3) continue;
      const o = i * 4;
      glass[o] = 236;
      glass[o + 1] = 248;
      glass[o + 2] = 255;
      glass[o + 3] = a;
    }
  }
  writeBoth("03_Glass.png", await toPng(glass, width, height));

  const glowOuter = await blurGray(hard, width, height, 9);
  const glowInner = await blurGray(hard, width, height, 1.8);
  const glowSoft = await blurGray(hard, width, height, 16);
  const glowMid = await blurGray(hard, width, height, 7);
  const glow = Buffer.alloc(n * 4);
  for (let i = 0; i < n; i += 1) {
    const rim = Math.max(0, glowOuter[i] - glowInner[i] * 0.92);
    const bloom = Math.max(0, glowSoft[i] - glowMid[i] * 0.9) * 0.4;
    const a = Math.min(255, Math.round(rim * 2.05 + bloom * 1.15));
    if (a < 4) continue;
    const t = (i % width) / Math.max(1, width - 1);
    glow[i * 4] = Math.round(159 + t * 41);
    glow[i * 4 + 1] = Math.round(214 - t * 47);
    glow[i * 4 + 2] = 255;
    glow[i * 4 + 3] = a;
  }
  writeBoth("08_Glow.png", await toPng(glow, width, height));

  const shiftX = 22;
  const shiftY = 38;
  const shifted = Buffer.alloc(n);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const sx = x - shiftX;
      const sy = y - shiftY;
      if (sx < 0 || sy < 0 || sx >= width || sy >= height) continue;
      shifted[y * width + x] = hard[sy * width + sx];
    }
  }
  const shadowBlur = await blurGray(shifted, width, height, 8);
  const shadow = Buffer.alloc(n * 4);
  for (let i = 0; i < n; i += 1) {
    const covered = hard[i] / 255;
    const a = Math.min(255, Math.round(shadowBlur[i] * (0.08 + 0.92 * (1 - covered))));
    if (a < 6) continue;
    shadow[i * 4] = 4;
    shadow[i * 4 + 1] = 6;
    shadow[i * 4 + 2] = 14;
    shadow[i * 4 + 3] = a;
  }
  writeBoth("12_Shadow.png", await toPng(shadow, width, height));
  console.log("shadow shift", shiftX, shiftY);

  const crystalFiles = [
    "resonance_dive_shard_a.png",
    "resonance_dive_shard_b.png",
    "resonance_dive_shard_c.png",
    "resonance_dive_shard_d.png",
  ];
  const bits = [];
  for (const name of crystalFiles) {
    const file = path.join(SRC, name);
    if (!fs.existsSync(file)) continue;
    const bit = await isolateCrystal(file, 36);
    if (bit) bits.push(bit);
  }
  const baked = await loadRgba(path.join(ART, "state_normal.png"));
  const platePad = await blurGray(hard, width, height, 6);
  const outside = {
    data: Buffer.alloc(n * 4),
    width,
    height,
  };
  for (let i = 0; i < n; i += 1) {
    if (platePad[i] > 40) continue;
    const o = i * 4;
    const r = baked.data[o];
    const g = baked.data[o + 1];
    const b = baked.data[o + 2];
    const a = keepCrystalAlpha(r, g, b, baked.data[o + 3]);
    if (a < 40) continue;
    outside.data[o] = r;
    outside.data[o + 1] = g;
    outside.data[o + 2] = b;
    outside.data[o + 3] = a;
  }
  const seen = Buffer.alloc(n);
  const extras = [];
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      if (seen[i] || outside.data[i * 4 + 3] < 40) continue;
      const q = [[x, y]];
      seen[i] = 1;
      const cells = [];
      let minCX = x;
      let minCY = y;
      let maxCX = x;
      let maxCY = y;
      while (q.length) {
        const [cx, cy] = q.pop();
        cells.push([cx, cy]);
        minCX = Math.min(minCX, cx);
        minCY = Math.min(minCY, cy);
        maxCX = Math.max(maxCX, cx);
        maxCY = Math.max(maxCY, cy);
        for (const [dx, dy] of [
          [1, 0],
          [-1, 0],
          [0, 1],
          [0, -1],
        ]) {
          const nx = cx + dx;
          const ny = cy + dy;
          if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
          const ni = ny * width + nx;
          if (seen[ni] || outside.data[ni * 4 + 3] < 40) continue;
          seen[ni] = 1;
          q.push([nx, ny]);
        }
      }
      const pw = maxCX - minCX + 1;
      const ph = maxCY - minCY + 1;
      if (cells.length < 70 || cells.length > 2800 || pw > 160 || ph > 160) continue;
      extras.push({ cells, minX: minCX, minY: minCY, maxX: maxCX, maxY: maxCY });
    }
  }
  extras.sort((a, b) => a.cells.length - b.cells.length);
  for (const piece of extras) {
    if (bits.length >= 8) break;
    bits.push(cropComponent(outside, piece, 6));
  }
  if (bits.length === 0) {
    throw new Error("no particle bits isolated");
  }
  console.log("particles", bits.length, "extras", extras.length);
  for (let i = 0; i < 8; i += 1) {
    const bit = bits[i % bits.length];
    writeBoth(`09_p${i}.png`, await toPng(bit.data, bit.width, bit.height));
  }

  const scatter = Buffer.alloc(n * 4);
  const cx = (minX + maxX) / 2;
  const cy = (minY + maxY) / 2;
  const rx = (maxX - minX) * 0.52;
  const ry = (maxY - minY) * 0.46;
  for (let i = 0; i < 8; i += 1) {
    const ang = (i / 8) * Math.PI * 2 + 0.35;
    const px = cx + Math.cos(ang) * rx;
    const py = cy + Math.sin(ang * 1.08) * ry;
    stamp(scatter, width, height, bits[i % bits.length], px, py);
  }
  writeBoth("09_Particles.png", await toPng(scatter, width, height));

  const preview = Buffer.alloc(n * 4);
  for (let i = 0; i < n; i += 1) {
    preview[i * 4] = 158;
    preview[i * 4 + 1] = 198;
    preview[i * 4 + 2] = 232;
    preview[i * 4 + 3] = 255;
  }
  const layers = [shadow, glow, base, plateData, glass, border, decoL.data, decoR.data, scatter];
  const alphas = [1, 0.9, 1, 0.22, 0.28, 0.7, 1, 1, 1];
  for (let L = 0; L < layers.length; L += 1) {
    const src = layers[L];
    const mul = alphas[L];
    for (let i = 0; i < n; i += 1) {
      const sa = (src[i * 4 + 3] / 255) * mul;
      if (sa < 0.01) continue;
      const di = i * 4;
      const da = preview[di + 3] / 255;
      const outA = sa + da * (1 - sa);
      preview[di] = Math.round((src[di] * sa + preview[di] * da * (1 - sa)) / outA);
      preview[di + 1] = Math.round((src[di + 1] * sa + preview[di + 1] * da * (1 - sa)) / outA);
      preview[di + 2] = Math.round((src[di + 2] * sa + preview[di + 2] * da * (1 - sa)) / outA);
      preview[di + 3] = Math.round(outA * 255);
    }
  }
  const text = await loadRgba(path.join(ART, "07_Text.png"));
  for (let i = 0; i < n; i += 1) {
    const sa = text.data[i * 4 + 3] / 255;
    if (sa < 0.01) continue;
    const di = i * 4;
    const da = preview[di + 3] / 255;
    const outA = sa + da * (1 - sa);
    preview[di] = Math.round((text.data[di] * sa + preview[di] * da * (1 - sa)) / outA);
    preview[di + 1] = Math.round((text.data[di + 1] * sa + preview[di + 1] * da * (1 - sa)) / outA);
    preview[di + 2] = Math.round((text.data[di + 2] * sa + preview[di + 2] * da * (1 - sa)) / outA);
    preview[di + 3] = Math.round(outA * 255);
  }
  fs.writeFileSync(path.join(SRC, "resonance_dive_stack_preview.png"), await toPng(preview, width, height));
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
