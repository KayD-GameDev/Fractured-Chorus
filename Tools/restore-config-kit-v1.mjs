import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";
const WIDGETS = path.join(KIT, "ui_config_widgets_v1.png");
const SLIDER = path.join(KIT, "ui_config_slider_v1.png");

const SKIP = new Set([
  "ui_config_widgets_v1.png",
  "ui_config_kit_v1.png",
  "ui_config_slider_v1.png",
  "ui_config_panel_v1.png",
]);

function components(px, w, h, minArea = 2500) {
  const seen = Buffer.alloc(w * h);
  const out = [];
  const isInk = (id) => px[id * 4 + 3] >= 10;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const start = y * w + x;
      if (seen[start] || !isInk(start)) continue;
      const q = [start];
      seen[start] = 1;
      let minX = x;
      let minY = y;
      let maxX = x;
      let maxY = y;
      let area = 0;
      while (q.length) {
        const id = q.pop();
        area++;
        const cx = id % w;
        const cy = (id / w) | 0;
        if (cx < minX) minX = cx;
        if (cy < minY) minY = cy;
        if (cx > maxX) maxX = cx;
        if (cy > maxY) maxY = cy;
        const nbs = [id - 1, id + 1, id - w, id + w];
        for (const n of nbs) {
          if (n < 0 || n >= w * h || seen[n] || !isInk(n)) continue;
          const nx = n % w;
          const ny = (n / w) | 0;
          if (Math.abs(nx - cx) + Math.abs(ny - cy) !== 1) continue;
          seen[n] = 1;
          q.push(n);
        }
      }
      const bw = maxX - minX + 1;
      const bh = maxY - minY + 1;
      if (area < minArea || bw < 60 || bh < 50) continue;
      out.push({ minX, minY, maxX, maxY, area, cx: (minX + maxX) / 2, cy: (minY + maxY) / 2 });
    }
  }
  return out;
}

function clusterRows(boxes, rowGap = 56) {
  const sorted = [...boxes].sort((a, b) => a.cy - b.cy || a.cx - b.cx);
  const rows = [];
  for (const box of sorted) {
    const last = rows[rows.length - 1];
    if (!last || box.cy - last[0].cy > rowGap) {
      rows.push([box]);
    } else {
      last.push(box);
    }
  }
  for (const row of rows) row.sort((a, b) => a.cx - b.cx);
  return rows;
}

function boundsOf(px, w, h, pad = 8) {
  let minX = w;
  let minY = h;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[(y * w + x) * 4 + 3] < 8) continue;
      if (x < minX) minX = x;
      if (y < minY) minY = y;
      if (x > maxX) maxX = x;
      if (y > maxY) maxY = y;
    }
  }
  if (maxX < minX) return null;
  const left = Math.max(0, minX - pad);
  const top = Math.max(0, minY - pad);
  return {
    left,
    top,
    width: Math.min(w - left, maxX - minX + 1 + pad * 2),
    height: Math.min(h - top, maxY - minY + 1 + pad * 2),
  };
}

async function saveCrop(px, w, h, box, pad, dest) {
  const left = Math.max(0, box.minX - pad);
  const top = Math.max(0, box.minY - pad);
  const width = Math.min(w - left, box.maxX - box.minX + 1 + pad * 2);
  const height = Math.min(h - top, box.maxY - box.minY + 1 + pad * 2);
  await sharp(Buffer.from(px), { raw: { width: w, height: h, channels: 4 } })
    .extract({ left, top, width, height })
    .png()
    .toFile(dest);
  const meta = await sharp(dest).metadata();
  console.log("slice", path.basename(dest), meta.width + "x" + meta.height);
}

async function writeCroppedPng(px, w, h, dest, pad = 6) {
  const b = boundsOf(px, w, h, pad);
  let img = sharp(Buffer.from(px), { raw: { width: w, height: h, channels: 4 } });
  if (b) img = img.extract(b);
  await img.png().toFile(dest);
  const meta = await sharp(dest).metadata();
  console.log("wrote", path.basename(dest), meta.width + "x" + meta.height);
}

async function readRgba(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { data, w: info.width, h: info.height };
}

const widgets = await readRgba(WIDGETS);
const boxes = components(widgets.data, widgets.w, widgets.h, 2500);
const rows = clusterRows(boxes, 56);
console.log(
  "widget rows",
  rows.map((r) => r.length).join(","),
  "boxes",
  boxes.length,
);

const namesByRow = [
  ["ui_config_speaker_min_v1.png", "ui_config_slider_v1.png", "ui_config_speaker_max_v1.png", "ui_config_btn_minus_v1.png", "ui_config_btn_plus_v1.png"],
  ["ui_config_toggle_on_v1.png", "ui_config_toggle_off_v1.png"],
  ["ui_config_chip_normal_v1.png", "ui_config_chip_selected_v1.png"],
  ["ui_config_icon_note_v1.png", "ui_config_icon_brightness_v1.png", "ui_config_icon_skip_v1.png", "ui_config_icon_difficulty_v1.png"],
];

for (let i = 0; i < rows.length; i++) {
  const names = namesByRow[i] ?? [];
  for (let j = 0; j < rows[i].length; j++) {
    const name = names[j] ?? `ui_config_row${i}_${j}.png`;
    if (SKIP.has(name)) {
      console.log("skip", name);
      continue;
    }
    await saveCrop(widgets.data, widgets.w, widgets.h, rows[i][j], 10, path.join(KIT, name));
  }
}

const slider = await readRgba(SLIDER);
const px = slider.data;
const w = slider.w;
const h = slider.h;

function lum(o) {
  return (px[o] + px[o + 1] + px[o + 2]) / 3;
}

let sumX = 0;
let sumY = 0;
let n = 0;
for (let y = 0; y < h; y++) {
  for (let x = 0; x < w; x++) {
    const o = (y * w + x) * 4;
    if (px[o + 3] < 80) continue;
    if (lum(o) < 210) continue;
    if (px[o + 1] < 180) continue;
    sumX += x;
    sumY += y;
    n++;
  }
}

const cx = Math.round(sumX / Math.max(1, n));
const cy = Math.round(sumY / Math.max(1, n));
const handleR = 48;
const x0 = Math.max(0, cx - handleR);
const y0 = Math.max(0, cy - handleR);
const x1 = Math.min(w, cx + handleR);
const y1 = Math.min(h, cy + handleR);

const handle = Buffer.alloc((x1 - x0) * (y1 - y0) * 4);
for (let y = y0; y < y1; y++) {
  for (let x = x0; x < x1; x++) {
    const s = (y * w + x) * 4;
    const d = ((y - y0) * (x1 - x0) + (x - x0)) * 4;
    const dx = x - cx;
    const dy = y - cy;
    const dist = Math.hypot(dx, dy);
    handle[d] = px[s];
    handle[d + 1] = px[s + 1];
    handle[d + 2] = px[s + 2];
    handle[d + 3] = dist > handleR - 2 ? 0 : px[s + 3];
  }
}

const empty = Buffer.alloc(w * h * 4);
const fill = Buffer.alloc(w * h * 4);
const sampleX = Math.min(w - 1, cx + handleR + 18);
for (let y = 0; y < h; y++) {
  const so = (y * w + sampleX) * 4;
  for (let x = 0; x < w; x++) {
    const o = (y * w + x) * 4;
    empty[o] = px[so];
    empty[o + 1] = px[so + 1];
    empty[o + 2] = px[so + 2];
    empty[o + 3] = px[so + 3];

    const a = px[o + 3];
    if (a < 20) continue;
    const onBar = Math.abs(y - cy) <= 12 && x < cx - handleR + 2 && a > 40;
    const bright = lum(o) > 70 && px[o] > 110 && px[o + 2] > 130 && px[o + 1] < 160;
    if (onBar || (x < cx - handleR + 2 && bright)) {
      fill[o] = px[o];
      fill[o + 1] = px[o + 1];
      fill[o + 2] = px[o + 2];
      fill[o + 3] = a;
    }
  }
}

await writeCroppedPng(handle, x1 - x0, y1 - y0, path.join(KIT, "ui_config_slider_handle_v1.png"), 4);
await writeCroppedPng(empty, w, h, path.join(KIT, "ui_config_slider_track_v1.png"), 8);
await writeCroppedPng(fill, w, h, path.join(KIT, "ui_config_slider_fill_v1.png"), 6);

function patchBorder(metaPath, border) {
  if (!fs.existsSync(metaPath)) return;
  let text = fs.readFileSync(metaPath, "utf8");
  text = text.replace(/spriteBorder: \{x: [^}]+\}/g, `spriteBorder: {x: ${border.x}, y: ${border.y}, z: ${border.z}, w: ${border.w}}`);
  fs.writeFileSync(metaPath, text);
}

patchBorder(path.join(KIT, "ui_config_slider_fill_v1.png.meta"), { x: 48, y: 10, z: 48, w: 10 });
patchBorder(path.join(KIT, "ui_config_slider_track_v1.png.meta"), { x: 36, y: 8, z: 36, w: 8 });
patchBorder(path.join(KIT, "ui_config_slider_handle_v1.png.meta"), { x: 0, y: 0, z: 0, w: 0 });

console.log("handleCenter", { cx, cy, n });
console.log("done");
