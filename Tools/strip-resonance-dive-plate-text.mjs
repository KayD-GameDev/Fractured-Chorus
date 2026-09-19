import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";

const STATE_FILES = [
  { src: "resonance_dive_state_normal_notext.png", out: "state_normal.png" },
  { src: "resonance_dive_state_hover_notext.png", out: "state_hover.png" },
  { src: "resonance_dive_state_pressed_notext.png", out: "state_pressed.png" },
];

function lum(r, g, b) {
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
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

async function loadRaw(file) {
  const buf = fs.readFileSync(path.join(SRC, file));
  const { data, info } = await sharp(buf).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  keyBackdropOnly(pixels, info.width, info.height);
  return { pixels, width: info.width, height: info.height };
}

async function toPng(rgba, width, height) {
  return sharp(rgba, { raw: { width, height, channels: 4 } }).png().toBuffer();
}

function writeBoth(name, buf) {
  fs.writeFileSync(path.join(ART, name), buf);
  fs.writeFileSync(path.join(RES, name), buf);
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

function unionMask(a, b) {
  const out = Buffer.alloc(a.length);
  for (let i = 0; i < a.length; i += 1) {
    out[i] = Math.max(a[i], b[i]);
  }
  return out;
}

function runLength(data, width, height, x, y, dx, dy) {
  const o0 = (y * width + x) * 4;
  const a0 = data[o0 + 3];
  const L0 = lum(data[o0], data[o0 + 1], data[o0 + 2]);
  if (a0 < 50 || L0 < 90) return 0;
  let n = 1;
  let cx = x + dx;
  let cy = y + dy;
  while (cx >= 0 && cy >= 0 && cx < width && cy < height && n < 160) {
    const o = (cy * width + cx) * 4;
    const a = data[o + 3];
    const L = lum(data[o], data[o + 1], data[o + 2]);
    if (a < 40 || Math.abs(L - L0) > 55) break;
    n += 1;
    cx += dx;
    cy += dy;
  }
  return n;
}

function isChromeLine(data, width, height, x, y) {
  const h = runLength(data, width, height, x, y, 1, 0) + runLength(data, width, height, x, y, -1, 0);
  const v = runLength(data, width, height, x, y, 0, 1) + runLength(data, width, height, x, y, 0, -1);
  return (h > 200 && v < 14) || (v > 200 && h < 14);
}

function median(values, fallback) {
  if (!values.length) return fallback;
  values.sort((a, b) => a - b);
  return values[(values.length / 2) | 0];
}

function seedFromTextLayer(data, width, height) {
  const seed = Buffer.alloc(width * height);
  for (let i = 0; i < width * height; i += 1) {
    const o = i * 4;
    if (data[o + 3] < 24) continue;
    if (lum(data[o], data[o + 1], data[o + 2]) > 16) seed[i] = 255;
  }
  return seed;
}

function seedGlyphs(plate, width, height) {
  const seed = Buffer.alloc(width * height);
  const box = bbox(plate, width, height);
  if (!box) return seed;
  const x0 = Math.round(box.minX + box.w * 0.34);
  const x1 = Math.round(box.minX + box.w * 0.92);
  const yTitle0 = Math.round(box.minY + box.h * 0.34);
  const yTitle1 = Math.round(box.minY + box.h * 0.60);
  const ySub0 = Math.round(box.minY + box.h * 0.54);
  const ySub1 = Math.round(box.minY + box.h * 0.72);

  for (let y = yTitle0; y <= yTitle1; y += 1) {
    for (let x = x0; x <= x1; x += 1) {
      if (isChromeLine(plate, width, height, x, y)) continue;
      const o = (y * width + x) * 4;
      if (plate[o + 3] < 40) continue;
      const L = lum(plate[o], plate[o + 1], plate[o + 2]);
      const C = chroma(plate[o], plate[o + 1], plate[o + 2]);
      if (L > 128 && C < 70) seed[y * width + x] = 255;
    }
  }
  for (let y = ySub0; y <= ySub1; y += 1) {
    for (let x = x0; x <= x1; x += 1) {
      if (isChromeLine(plate, width, height, x, y)) continue;
      const o = (y * width + x) * 4;
      if (plate[o + 3] < 30) continue;
      const r = plate[o];
      const g = plate[o + 1];
      const b = plate[o + 2];
      const L = lum(r, g, b);
      const cyan = b > r + 12 && g > r && b > 78 && L > 42 && L < 210;
      if (cyan || (L > 128 && chroma(r, g, b) < 70)) {
        seed[y * width + x] = 255;
      }
    }
  }
  return seed;
}

function expandLetterBodies(plate, seed, width, height) {
  const box = bbox(plate, width, height);
  if (!box) return seed;
  const x0 = Math.round(box.minX + box.w * 0.32);
  const x1 = Math.round(box.minX + box.w * 0.93);
  const y0 = Math.round(box.minY + box.h * 0.32);
  const y1 = Math.round(box.minY + box.h * 0.74);
  const out = Buffer.from(seed);
  const queue = [];
  for (let i = 0; i < out.length; i += 1) {
    if (out[i] > 0) queue.push(i);
  }
  const dirs = [1, -1, width, -width, width + 1, width - 1, -width + 1, -width - 1];
  while (queue.length) {
    const i = queue.pop();
    const x = i % width;
    const y = (i / width) | 0;
    for (const d of dirs) {
      const ni = i + d;
      if (ni < 0 || ni >= out.length || out[ni]) continue;
      const nx = ni % width;
      const ny = (ni / width) | 0;
      if (nx < x0 || nx > x1 || ny < y0 || ny > y1) continue;
      if (Math.abs(nx - x) > 1 || Math.abs(ny - y) > 1) continue;
      if (isChromeLine(plate, width, height, nx, ny)) continue;
      const o = ni * 4;
      if (plate[o + 3] < 30) continue;
      const L = lum(plate[o], plate[o + 1], plate[o + 2]);
      const C = chroma(plate[o], plate[o + 1], plate[o + 2]);
      const cyan = plate[o + 2] > plate[o] + 12 && plate[o + 1] > plate[o];
      if (L < 68 && !cyan) continue;
      if (L < 48) continue;
      if (C > 90 && !cyan) continue;
      out[ni] = 255;
      queue.push(ni);
    }
  }
  return out;
}

function cloneStamp(data, mask, width, height, light) {
  const out = Buffer.from(data);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      if (mask[i] < 16) continue;
      let copied = false;
      for (let d = 6; d <= 96 && !copied; d += 1) {
        const samples = [y - d, y + d];
        for (const sy of samples) {
          if (sy < 0 || sy >= height) continue;
          const ni = sy * width + x;
          if (mask[ni] >= 16) continue;
          const no = ni * 4;
          if (data[no + 3] < 50) continue;
          if (!isUsableGlass(data, no, light)) continue;
          const o = i * 4;
          out[o] = data[no];
          out[o + 1] = data[no + 1];
          out[o + 2] = data[no + 2];
          out[o + 3] = data[no + 3];
          copied = true;
          break;
        }
      }
    }
  }
  return out;
}

function isUsableGlass(data, o, light) {
  if (data[o + 3] < 50) return false;
  const L = lum(data[o], data[o + 1], data[o + 2]);
  const C = chroma(data[o], data[o + 1], data[o + 2]);
  if (light) return L >= 55 && L <= 240 && C < 110;
  return L >= 8 && L <= 110 && C < 58;
}

function inpaintGlass(data, mask, width, height, light) {
  const n = width * height;
  const out = Buffer.from(data);
  const filled = Buffer.alloc(n);
  for (let i = 0; i < n; i += 1) {
    if (mask[i] < 16) filled[i] = 1;
  }
  const dirs = [];
  for (let r = 1; r <= 10; r += 1) {
    dirs.push([r, 0], [-r, 0], [0, r], [0, -r]);
    if (r <= 6) {
      dirs.push([r, r], [-r, r], [r, -r], [-r, -r]);
    }
  }
  for (let pass = 0; pass < 120; pass += 1) {
    let changed = 0;
    for (let y = 0; y < height; y += 1) {
      for (let x = 0; x < width; x += 1) {
        const i = y * width + x;
        if (filled[i]) continue;
        let r = 0;
        let g = 0;
        let b = 0;
        let a = 0;
        let wsum = 0;
        for (const [dx, dy] of dirs) {
          const nx = x + dx;
          const ny = y + dy;
          if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
          const ni = ny * width + nx;
          if (!filled[ni]) continue;
          const no = ni * 4;
          if (out[no + 3] < 40) continue;
          if (mask[ni] < 16 && !isUsableGlass(out, no, light)) continue;
          const w = 1 / (Math.abs(dx) + Math.abs(dy));
          r += out[no] * w;
          g += out[no + 1] * w;
          b += out[no + 2] * w;
          a += out[no + 3] * w;
          wsum += w;
        }
        if (wsum === 0) continue;
        const o = i * 4;
        out[o] = Math.round(r / wsum);
        out[o + 1] = Math.round(g / wsum);
        out[o + 2] = Math.round(b / wsum);
        out[o + 3] = Math.round(a / wsum);
        filled[i] = 1;
        changed += 1;
      }
    }
    if (changed === 0) break;
  }

  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      if (filled[i]) continue;
      const rs = [];
      const gs = [];
      const bs = [];
      const as = [];
      for (let dy = -18; dy <= 18; dy += 1) {
        const ny = y + dy;
        if (ny < 0 || ny >= height) continue;
        for (let dx = -18; dx <= 18; dx += 1) {
          const nx = x + dx;
          if (nx < 0 || nx >= width) continue;
          const ni = ny * width + nx;
          if (!filled[ni]) continue;
          const no = ni * 4;
          if (out[no + 3] < 40) continue;
          if (mask[ni] < 16 && !isUsableGlass(out, no, light)) continue;
          rs.push(out[no]);
          gs.push(out[no + 1]);
          bs.push(out[no + 2]);
          as.push(out[no + 3]);
        }
      }
      const o = i * 4;
      out[o] = median(rs, out[o]);
      out[o + 1] = median(gs, out[o + 1]);
      out[o + 2] = median(bs, out[o + 2]);
      out[o + 3] = median(as, out[o + 3]);
      filled[i] = 1;
    }
  }
  return out;
}

async function feather(orig, filled, mask, width, height) {
  const soft = await blurGray(mask, width, height, 1.6);
  const out = Buffer.from(orig);
  for (let i = 0; i < mask.length; i += 1) {
    const m = Math.min(1, soft[i] / 255);
    if (m < 0.02) continue;
    const o = i * 4;
    out[o] = Math.round(orig[o] * (1 - m) + filled[o] * m);
    out[o + 1] = Math.round(orig[o + 1] * (1 - m) + filled[o + 1] * m);
    out[o + 2] = Math.round(orig[o + 2] * (1 - m) + filled[o + 2] * m);
    out[o + 3] = Math.round(orig[o + 3] * (1 - m) + filled[o + 3] * m);
  }
  return out;
}

function restoreChrome(orig, out, mask, width, height) {
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      if (mask[i] < 16) continue;
      if (!isChromeLine(orig, width, height, x, y)) continue;
      const o = i * 4;
      out[o] = orig[o];
      out[o + 1] = orig[o + 1];
      out[o + 2] = orig[o + 2];
      out[o + 3] = orig[o + 3];
    }
  }
}

function erase(data, mask) {
  const out = Buffer.from(data);
  for (let i = 0; i < mask.length; i += 1) {
    if (mask[i] < 16) continue;
    const o = i * 4;
    const keep = Math.max(0, 1 - mask[i] / 255);
    out[o + 3] = Math.round(out[o + 3] * keep);
    if (out[o + 3] < 8) {
      out[o] = 0;
      out[o + 1] = 0;
      out[o + 2] = 0;
      out[o + 3] = 0;
    }
  }
  return out;
}

async function rebuildShadow(width, height) {
  const gradPath = path.join(ART, "04_Gradient.png");
  const { data, info } = await sharp(fs.readFileSync(gradPath)).ensureAlpha().raw().toBuffer({
    resolveWithObject: true,
  });
  const gw = info.width;
  const gh = info.height;
  const n = width * height;
  const hard = Buffer.alloc(n);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const gx = Math.min(gw - 1, Math.round((x / Math.max(1, width - 1)) * (gw - 1)));
      const gy = Math.min(gh - 1, Math.round((y / Math.max(1, height - 1)) * (gh - 1)));
      hard[y * width + x] = data[(gy * gw + gx) * 4 + 3] >= 96 ? 255 : 0;
    }
  }
  const closed = dilate(hard, width, height, 6);
  const shiftX = 22;
  const shiftY = 38;
  const shifted = Buffer.alloc(n);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const sx = x - shiftX;
      const sy = y - shiftY;
      if (sx < 0 || sy < 0 || sx >= width || sy >= height) continue;
      shifted[y * width + x] = closed[sy * width + sx];
    }
  }
  const shadowBlur = await blurGray(shifted, width, height, 24);
  const shadow = Buffer.alloc(n * 4);
  for (let i = 0; i < n; i += 1) {
    const covered = hard[i] / 255;
    const a = Math.min(255, Math.round(shadowBlur[i] * 0.55 * (1 - covered)));
    if (a < 6) continue;
    shadow[i * 4] = 4;
    shadow[i * 4 + 1] = 6;
    shadow[i * 4 + 2] = 14;
    shadow[i * 4 + 3] = a;
  }
  return shadow;
}

async function main() {
  const left = 84;
  const top = 74;
  const cropW = 1149;
  const cropH = 517;
  const outW = 1536;
  const outH = 691;
  console.log("crop", { left, top, cropW, cropH, outW, outH });

  async function cropToLock(img) {
    const png = await sharp(img.pixels, {
      raw: { width: img.width, height: img.height, channels: 4 },
    })
      .extract({ left, top, width: cropW, height: cropH })
      .resize(outW, outH, { fit: "fill" })
      .raw()
      .toBuffer({ resolveWithObject: true });
    return { data: Buffer.from(png.data), width: png.info.width, height: png.info.height };
  }

  for (const file of STATE_FILES) {
    const src = await loadRaw(file.src);
    const cropped = await cropToLock(src);
    writeBoth(file.out, await toPng(cropped.data, cropped.width, cropped.height));
    console.log("locked", file.out);
  }

  const shadow = await rebuildShadow(outW, outH);
  writeBoth("12_Shadow.png", await toPng(shadow, outW, outH));
  console.log("rebuilt 12_Shadow.png");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
