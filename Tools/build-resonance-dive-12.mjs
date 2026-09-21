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

function keepAlpha(r, g, b, a) {
  const lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  const chroma = Math.max(r, g, b) - Math.min(r, g, b);
  let keep = Math.max(0, Math.min(1, (lum - 12) / 18));
  if (chroma > 10) keep = Math.max(keep, Math.min(1, chroma / 36));
  return Math.round(a * keep);
}

async function loadRgba(file) {
  const { data, info } = await sharp(fs.readFileSync(file)).ensureAlpha().raw().toBuffer({
    resolveWithObject: true,
  });
  return { data: Buffer.from(data), width: info.width, height: info.height };
}

async function loadKeyed(file) {
  const img = await loadRgba(file);
  for (let i = 0; i < img.data.length; i += 4) {
    img.data[i + 3] = keepAlpha(img.data[i], img.data[i + 1], img.data[i + 2], img.data[i + 3]);
  }
  return img;
}

async function toPng(rgba, width, height) {
  return sharp(rgba, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

async function blurGray(gray, width, height, sigma) {
  return sharp(gray, { raw: { width, height, channels: 1 } }).blur(sigma).raw().toBuffer();
}

async function mapToLock(layer, lockW, lockH) {
  const scale = lockW / layer.width;
  const newH = Math.max(1, Math.round(layer.height * scale));
  const resized = await sharp(layer.data, {
    raw: { width: layer.width, height: layer.height, channels: 4 },
  })
    .resize(lockW, newH, { fit: "fill" })
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });

  const out = Buffer.alloc(lockW * lockH * 4);
  const top = newH >= lockH ? Math.round((newH - lockH) / 2) : Math.round((lockH - newH) / 2);
  if (newH >= lockH) {
    for (let y = 0; y < lockH; y += 1) {
      const sy = y + top;
      resized.data.copy(out, y * lockW * 4, sy * lockW * 4, (sy + 1) * lockW * 4);
    }
    return { data: out, width: lockW, height: lockH };
  }
  for (let y = 0; y < newH; y += 1) {
    resized.data.copy(out, (y + top) * lockW * 4, y * lockW * 4, (y + 1) * lockW * 4);
  }
  return { data: out, width: lockW, height: lockH };
}

async function main() {
  const plate = await loadRgba(path.join(ART, "state_normal.png"));
  const { width, height, data } = plate;
  const n = width * height;
  const mask = Buffer.alloc(n);
  for (let i = 0; i < n; i += 1) {
    mask[i] = data[i * 4 + 3] >= 80 ? 255 : 0;
  }

  const base = Buffer.alloc(n * 4);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      if (mask[i] < 128) continue;
      const t = x / Math.max(1, width - 1);
      const o = i * 4;
      base[o] = Math.round(8 + t * 20);
      base[o + 1] = Math.round(14 + t * 26);
      base[o + 2] = Math.round(28 + t * 38);
      base[o + 3] = 240;
    }
  }
  writeBoth("01_Base.png", await toPng(base, width, height));

  const dilate = await blurGray(mask, width, height, 1.4);
  const border = Buffer.alloc(n * 4);
  for (let i = 0; i < n; i += 1) {
    const edge = Math.max(0, dilate[i] - mask[i] * 0.72);
    const a = Math.min(255, Math.round(edge * 2.6));
    border[i * 4] = 232;
    border[i * 4 + 1] = 244;
    border[i * 4 + 2] = 255;
    border[i * 4 + 3] = a;
  }
  writeBoth("02_Border.png", await toPng(border, width, height));

  const glass = Buffer.alloc(n * 4);
  for (let i = 0; i < n; i += 1) {
    if (mask[i] < 128) continue;
    const lum = 0.2126 * data[i * 4] + 0.7152 * data[i * 4 + 1] + 0.0722 * data[i * 4 + 2];
    if (lum < 150) continue;
    const a = Math.min(255, Math.round((lum - 150) * 2.2));
    glass[i * 4] = 236;
    glass[i * 4 + 1] = 246;
    glass[i * 4 + 2] = 255;
    glass[i * 4 + 3] = a;
  }
  writeBoth("03_Glass.png", await toPng(glass, width, height));

  const glowSrc = await loadKeyed(path.join(SRC, "resonance_dive_08_glow_v4.png"));
  const glowMapped = await mapToLock(glowSrc, width, height);
  for (let i = 0; i < n; i += 1) {
    if (mask[i] > 128) {
      glowMapped.data[i * 4 + 3] = Math.round(glowMapped.data[i * 4 + 3] * 0.06);
    }
  }
  writeBoth("08_Glow.png", await toPng(glowMapped.data, width, height));

  const shadowBlur = await blurGray(mask, width, height, 16);
  const shiftY = 22;
  const shiftX = 10;
  const shadow = Buffer.alloc(n * 4);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      const sx = x - shiftX;
      const sy = y - shiftY;
      const src = sx >= 0 && sx < width && sy >= 0 && sy < height ? shadowBlur[sy * width + sx] : 0;
      const outside = mask[i] < 128 ? 1 : 0.15;
      const a = Math.min(255, Math.round(src * 0.72 * outside));
      const o = i * 4;
      shadow[o] = 6;
      shadow[o + 1] = 10;
      shadow[o + 2] = 22;
      shadow[o + 3] = a;
    }
  }
  writeBoth("12_Shadow.png", await toPng(shadow, width, height));

  const atlas = await loadKeyed(path.join(SRC, "resonance_dive_09_particle_atlas_v4.png"));
  const visited = Buffer.alloc(atlas.width * atlas.height);
  const pieces = [];
  const dirs = [
    [1, 0],
    [-1, 0],
    [0, 1],
    [0, -1],
  ];
  for (let y = 0; y < atlas.height; y += 1) {
    for (let x = 0; x < atlas.width; x += 1) {
      const i = y * atlas.width + x;
      if (visited[i] || atlas.data[i * 4 + 3] < 24) continue;
      const q = [[x, y]];
      visited[i] = 1;
      let minX = x;
      let minY = y;
      let maxX = x;
      let maxY = y;
      const cells = [];
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
          if (nx < 0 || ny < 0 || nx >= atlas.width || ny >= atlas.height) continue;
          const ni = ny * atlas.width + nx;
          if (visited[ni] || atlas.data[ni * 4 + 3] < 24) continue;
          visited[ni] = 1;
          q.push([nx, ny]);
        }
      }
      const pw = maxX - minX + 1;
      const ph = maxY - minY + 1;
      if (cells.length < 40 || pw < 8 || ph < 8) continue;
      pieces.push({ minX, minY, maxX, maxY, cells });
    }
  }
  pieces.sort((a, b) => a.minX - b.minX || a.minY - b.minY);
  const take = pieces.slice(0, 8);
  console.log("particles", take.length);
  for (let p = 0; p < take.length; p += 1) {
    const piece = take[p];
    const pw = piece.maxX - piece.minX + 1;
    const ph = piece.maxY - piece.minY + 1;
    const pad = 6;
    const tw = pw + pad * 2;
    const th = ph + pad * 2;
    const buf = Buffer.alloc(tw * th * 4);
    for (const [cx, cy] of piece.cells) {
      const s = (cy * atlas.width + cx) * 4;
      const dx = cx - piece.minX + pad;
      const dy = cy - piece.minY + pad;
      const d = (dy * tw + dx) * 4;
      buf[d] = atlas.data[s];
      buf[d + 1] = atlas.data[s + 1];
      buf[d + 2] = atlas.data[s + 2];
      buf[d + 3] = atlas.data[s + 3];
    }
    writeBoth(`09_p${p}.png`, await toPng(buf, tw, th));
  }

  console.log("lock", width, height);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
