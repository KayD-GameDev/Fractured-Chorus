import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";

function punch(px, o) {
  px[o] = 0;
  px[o + 1] = 0;
  px[o + 2] = 0;
  px[o + 3] = 0;
}

function matteNeon(px, w, h) {
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    let a = px[o + 3];
    if (a < 8) {
      punch(px, o);
      continue;
    }

    const maxc = Math.max(r, g, b);
    const minc = Math.min(r, g, b);
    const luma = (r + g + b) / 3;
    const chroma = maxc - minc;
    const purple = (r + b) * 0.5 - g;

    if (maxc < 28 || (chroma < 12 && luma < 42) || (purple < 6 && luma < 72 && maxc < 130)) {
      punch(px, o);
      continue;
    }

    if (luma < 78) {
      const fade = Math.pow(luma / 78, 1.35);
      a = Math.round(a * fade);
      if (a < 10) {
        punch(px, o);
        continue;
      }
      px[o + 3] = a;
    }
  }
}

function punchHandleHole(px, w, h) {
  const cx = (w - 1) * 0.5;
  const cy = (h - 1) * 0.5;
  let inner = 1e9;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const o = (y * w + x) * 4;
      const luma = (px[o] + px[o + 1] + px[o + 2]) / 3;
      if (px[o + 3] < 80 || luma < 200) continue;
      inner = Math.min(inner, Math.hypot(x - cx, y - cy));
    }
  }
  if (!Number.isFinite(inner) || inner > 40) {
    inner = Math.min(w, h) * 0.22;
  }
  const hole = Math.max(10, inner - 2);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (Math.hypot(x - cx, y - cy) < hole) {
        punch(px, (y * w + x) * 4);
      }
    }
  }
}

function punchFillBar(px, w, h) {
  const cy = (h - 1) * 0.5;
  const barHalf = 7;
  const capR = 14;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const o = (y * w + x) * 4;
      const yDist = Math.abs(y - cy);
      const sd =
        x >= capR && x < w - capR
          ? yDist
          : Math.hypot(x < capR ? x - capR : x - (w - 1 - capR), y - cy);
      const luma = (px[o] + px[o + 1] + px[o + 2]) / 3;
      if (sd > barHalf + 5 || luma < 48 || px[o + 3] < 10) {
        punch(px, o);
        continue;
      }
      const edge = Math.max(0, 1 - Math.max(0, sd - barHalf) / 5);
      px[o + 3] = Math.round(px[o + 3] * edge);
      if (px[o + 3] < 12) punch(px, o);
    }
  }
}

function floodOuterBlack(px, w, h, lumaMax = 22) {
  const seen = Buffer.alloc(w * h);
  const q = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= w || y >= h) return;
    const id = y * w + x;
    if (seen[id]) return;
    seen[id] = 1;
    q.push(id);
  };
  for (let x = 0; x < w; x++) {
    push(x, 0);
    push(x, h - 1);
  }
  for (let y = 0; y < h; y++) {
    push(0, y);
    push(w - 1, y);
  }
  while (q.length) {
    const id = q.pop();
    const o = id * 4;
    const luma = (px[o] + px[o + 1] + px[o + 2]) / 3;
    if (luma > lumaMax && px[o + 3] > 18) continue;
    punch(px, o);
    const x = id % w;
    const y = (id / w) | 0;
    push(x - 1, y);
    push(x + 1, y);
    push(x, y - 1);
    push(x, y + 1);
  }
}

async function load(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data, w: info.width, h: info.height };
}

async function save(file, data, w, h) {
  await sharp(data, { raw: { width: w, height: h, channels: 4 } }).png().toFile(file);
  const meta = await sharp(file).metadata();
  console.log("matte", path.basename(file), meta.width + "x" + meta.height);
}

function patchBorder(metaPath, border) {
  let text = fs.readFileSync(metaPath, "utf8");
  text = text.replace(
    /spriteBorder: \{x: [^}]+\}/g,
    `spriteBorder: {x: ${border.x}, y: ${border.y}, z: ${border.z}, w: ${border.w}}`,
  );
  fs.writeFileSync(metaPath, text);
}

async function padVertical(data, w, h, targetH) {
  if (h >= targetH) return { data, w, h };
  const out = Buffer.alloc(w * targetH * 4);
  const y0 = Math.floor((targetH - h) / 2);
  for (let y = 0; y < h; y++) {
    data.copy(out, ((y0 + y) * w) * 4, (y * w) * 4, (y * w + w) * 4);
  }
  return { data: out, w, h: targetH };
}

const outline = [
  "ui_config_btn_minus_v1.png",
  "ui_config_btn_plus_v1.png",
  "ui_config_chip_normal_v1.png",
  "ui_config_chip_selected_v1.png",
  "ui_config_icon_note_v1.png",
  "ui_config_icon_brightness_v1.png",
  "ui_config_icon_skip_v1.png",
  "ui_config_icon_difficulty_v1.png",
  "ui_config_speaker_min_v1.png",
  "ui_config_speaker_max_v1.png",
  "ui_config_toggle_on_v1.png",
  "ui_config_toggle_off_v1.png",
];

for (const name of outline) {
  const file = path.join(KIT, name);
  const img = await load(file);
  matteNeon(img.data, img.w, img.h);
  await save(file, img.data, img.w, img.h);
}

{
  const file = path.join(KIT, "ui_config_slider_handle_v1.png");
  const img = await load(file);
  matteNeon(img.data, img.w, img.h);
  punchHandleHole(img.data, img.w, img.h);
  await save(file, img.data, img.w, img.h);
}

{
  const file = path.join(KIT, "ui_config_slider_fill_v1.png");
  let img = await load(file);
  punchFillBar(img.data, img.w, img.h);
  img = await padVertical(img.data, img.w, img.h, 72);
  await save(file, img.data, img.w, img.h);
  const y = Math.max(8, Math.floor((img.h - 16) / 2));
  patchBorder(file + ".meta", { x: 48, y, z: 48, w: y });
}

{
  const file = path.join(KIT, "ui_config_slider_track_v1.png");
  let img = await load(file);
  matteNeon(img.data, img.w, img.h);
  img = await padVertical(img.data, img.w, img.h, 72);
  await save(file, img.data, img.w, img.h);
  const y = Math.max(8, Math.floor((img.h - 6) / 2));
  patchBorder(file + ".meta", { x: 36, y, z: 36, w: y });
}

{
  const file = path.join(KIT, "ui_config_panel_v1.png");
  const img = await load(file);
  floodOuterBlack(img.data, img.w, img.h, 18);
  await save(file, img.data, img.w, img.h);
}

console.log("done");
