import path from "node:path";
import fs from "node:fs";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const OUT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";

function purpleBias(r, g, b) {
  return (r + b) * 0.5 - g;
}

function isKeep(r, g, b, punchBlack) {
  const luma = (r + g + b) / 3;
  if (punchBlack && luma < 14) {
    return false;
  }

  const chroma = Math.max(r, g, b) - Math.min(r, g, b);
  const bias = purpleBias(r, g, b);
  if (bias >= 10) {
    return true;
  }

  if (luma > 210 && bias >= 6 && chroma >= 8) {
    return true;
  }

  if (chroma < 16 && luma > 140) {
    return false;
  }

  if (chroma < 10 && luma < 40) {
    return false;
  }

  return bias >= 6 && luma > 28;
}

async function matteCrop(srcName, destName, pad, punchBlack) {
  const src = path.join(SRC, srcName);
  const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const w = info.width;
  const h = info.height;
  const px = data;
  const keep = Buffer.alloc(w * h);

  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    keep[i] = isKeep(px[o], px[o + 1], px[o + 2], punchBlack) ? 1 : 0;
  }

  const dilated = Buffer.alloc(w * h);
  const radius = 3;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      let hit = 0;
      for (let dy = -radius; dy <= radius && !hit; dy++) {
        const yy = y + dy;
        if (yy < 0 || yy >= h) continue;
        for (let dx = -radius; dx <= radius; dx++) {
          const xx = x + dx;
          if (xx < 0 || xx >= w) continue;
          if (keep[yy * w + xx]) {
            hit = 1;
            break;
          }
        }
      }
      dilated[y * w + x] = hit;
    }
  }

  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    if (!dilated[i]) {
      px[o] = 0;
      px[o + 1] = 0;
      px[o + 2] = 0;
      px[o + 3] = 0;
      continue;
    }

    const luma = (px[o] + px[o + 1] + px[o + 2]) / 3;
    const bias = purpleBias(px[o], px[o + 1], px[o + 2]);
    if (!keep[i]) {
      const fade = Math.max(0, Math.min(1, (bias + 4) / 14));
      px[o + 3] = Math.round(px[o + 3] * fade * 0.65);
    } else if (punchBlack && luma < 22) {
      px[o + 3] = 0;
    }
  }

  let minX = w;
  let minY = h;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[(y * w + x) * 4 + 3] < 10) continue;
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x);
      maxY = Math.max(maxY, y);
    }
  }

  if (maxX < minX) {
    throw new Error("No opaque pixels: " + srcName);
  }

  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(w - 1, maxX + pad);
  maxY = Math.min(h - 1, maxY + pad);
  const cw = maxX - minX + 1;
  const ch = maxY - minY + 1;
  const crop = Buffer.alloc(cw * ch * 4);
  for (let y = 0; y < ch; y++) {
    for (let x = 0; x < cw; x++) {
      const s = ((minY + y) * w + (minX + x)) * 4;
      const d = (y * cw + x) * 4;
      crop[d] = px[s];
      crop[d + 1] = px[s + 1];
      crop[d + 2] = px[s + 2];
      crop[d + 3] = px[s + 3];
    }
  }

  const dest = path.join(OUT, destName);
  await sharp(crop, { raw: { width: cw, height: ch, channels: 4 } }).png().toFile(dest);
  return { dest, cw, ch, minX, minY };
}

function patchBorder(metaName, border) {
  const metaPath = path.join(OUT, metaName);
  let text = fs.readFileSync(metaPath, "utf8");
  text = text.replace(
    /spriteBorder: \{x: [^}]+\}/,
    `spriteBorder: {x: ${border.x}, y: ${border.y}, z: ${border.z}, w: ${border.w}}`
  );
  fs.writeFileSync(metaPath, text);
}

const fill = await matteCrop("ui_config_slider_fill_v2.png", "ui_config_slider_fill_v1.png", 10, false);
const handle = await matteCrop("ui_config_slider_handle_v2.png", "ui_config_slider_handle_v1.png", 8, true);
const track = await matteCrop("ui_config_slider_track_v2.png", "ui_config_slider_track_v1.png", 10, false);

const fillCap = Math.max(28, Math.min(80, Math.floor(fill.cw / 6)));
const fillPad = Math.max(6, Math.min(Math.floor(fill.ch / 2) - 2, 28));
patchBorder("ui_config_slider_fill_v1.png.meta", { x: fillCap, y: fillPad, z: fillCap, w: fillPad });

const trackCap = Math.max(32, Math.min(90, Math.floor(track.cw / 6)));
const trackPad = Math.max(8, Math.min(Math.floor(track.ch / 2) - 2, 32));
patchBorder("ui_config_slider_track_v1.png.meta", { x: trackCap, y: trackPad, z: trackCap, w: trackPad });
patchBorder("ui_config_slider_handle_v1.png.meta", { x: 0, y: 0, z: 0, w: 0 });

console.log(JSON.stringify({ fill, handle, track, fillCap, fillPad, trackCap, trackPad }));
