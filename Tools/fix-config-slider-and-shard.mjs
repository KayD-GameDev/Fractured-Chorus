import path from "node:path";
import fs from "node:fs";
import sharp from "sharp";

const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";
const SHARD_DIR = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1";

function bbox(px, w, h, thresh) {
  let minX = w;
  let minY = h;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[(y * w + x) * 4 + 3] < thresh) continue;
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x);
      maxY = Math.max(maxY, y);
    }
  }
  if (maxX < minX) {
    throw new Error("empty");
  }
  return { minX, minY, maxX, maxY, cw: maxX - minX + 1, ch: maxY - minY + 1 };
}

async function load(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { px: data, w: info.width, h: info.height };
}

function cropBuf(src, b, pad, maxH) {
  let x0 = Math.max(0, b.minX - pad);
  let y0 = Math.max(0, b.minY - pad);
  let x1 = Math.min(src.w - 1, b.maxX + pad);
  let y1 = Math.min(src.h - 1, b.maxY + pad);
  let cw = x1 - x0 + 1;
  let ch = y1 - y0 + 1;
  if (maxH && ch > maxH) {
    const extra = ch - maxH;
    y0 += Math.floor(extra / 2);
    ch = maxH;
  }
  const out = Buffer.alloc(cw * ch * 4);
  for (let y = 0; y < ch; y++) {
    for (let x = 0; x < cw; x++) {
      const s = ((y0 + y) * src.w + (x0 + x)) * 4;
      const d = (y * cw + x) * 4;
      out[d] = src.px[s];
      out[d + 1] = src.px[s + 1];
      out[d + 2] = src.px[s + 2];
      out[d + 3] = src.px[s + 3];
    }
  }
  return { buf: out, cw, ch };
}

function patchMeta(file, fields) {
  let text = fs.readFileSync(file, "utf8");
  if (fields.border) {
    text = text.replace(
      /spriteBorder: \{x: [^}]+\}/,
      `spriteBorder: {x: ${fields.border.x}, y: ${fields.border.y}, z: ${fields.border.z}, w: ${fields.border.w}}`
    );
  }
  if (fields.rect) {
    text = text.replace(
      /rect:\n        serializedVersion: 2\n        x: \d+\n        y: \d+\n        width: \d+\n        height: \d+/,
      `rect:\n        serializedVersion: 2\n        x: 0\n        y: 0\n        width: ${fields.rect.w}\n        height: ${fields.rect.h}`
    );
  }
  fs.writeFileSync(file, text);
}

const fillSrc = await load(path.join(KIT, "ui_config_slider_fill_v1.png"));
const trackSrc = await load(path.join(KIT, "ui_config_slider_track_v1.png"));
const handleSrc = await load(path.join(KIT, "ui_config_slider_handle_v1.png"));

const fillCrop = cropBuf(fillSrc, bbox(fillSrc.px, fillSrc.w, fillSrc.h, 36), 8, 56);
const trackCrop = cropBuf(trackSrc, bbox(trackSrc.px, trackSrc.w, trackSrc.h, 36), 8, 48);
const handleCrop = cropBuf(handleSrc, bbox(handleSrc.px, handleSrc.w, handleSrc.h, 40), 10, 160);

await sharp(fillCrop.buf, { raw: { width: fillCrop.cw, height: fillCrop.ch, channels: 4 } })
  .png()
  .toFile(path.join(KIT, "ui_config_slider_fill_v1.png"));
await sharp(trackCrop.buf, { raw: { width: trackCrop.cw, height: trackCrop.ch, channels: 4 } })
  .png()
  .toFile(path.join(KIT, "ui_config_slider_track_v1.png"));
await sharp(handleCrop.buf, { raw: { width: handleCrop.cw, height: handleCrop.ch, channels: 4 } })
  .png()
  .toFile(path.join(KIT, "ui_config_slider_handle_v1.png"));

const fillCap = Math.max(24, Math.min(48, Math.floor(fillCrop.cw / 10)));
const trackCap = Math.max(24, Math.min(48, Math.floor(trackCrop.cw / 10)));
patchMeta(path.join(KIT, "ui_config_slider_fill_v1.png.meta"), {
  border: { x: fillCap, y: 8, z: fillCap, w: 8 },
});
patchMeta(path.join(KIT, "ui_config_slider_track_v1.png.meta"), {
  border: { x: trackCap, y: 8, z: trackCap, w: 8 },
});
patchMeta(path.join(KIT, "ui_config_slider_handle_v1.png.meta"), {
  border: { x: 0, y: 0, z: 0, w: 0 },
});

const normal = await load(path.join(SHARD_DIR, "ui_btn_shard_normal_v1_alpha.png"));
const selected = await load(path.join(SHARD_DIR, "ui_btn_shard_selected_v1_alpha.png"));
const nb = bbox(normal.px, normal.w, normal.h, 18);
const sb = bbox(selected.px, selected.w, selected.h, 18);

const canvas = Buffer.alloc(normal.w * normal.h * 4);
const extract = await sharp(selected.px, { raw: { width: selected.w, height: selected.h, channels: 4 } })
  .extract({ left: sb.minX, top: sb.minY, width: sb.cw, height: sb.ch })
  .resize(nb.cw, nb.ch, { fit: "fill", kernel: "lanczos3" })
  .ensureAlpha()
  .raw()
  .toBuffer();

for (let y = 0; y < nb.ch; y++) {
  for (let x = 0; x < nb.cw; x++) {
    const s = (y * nb.cw + x) * 4;
    const d = ((nb.minY + y) * normal.w + (nb.minX + x)) * 4;
    canvas[d] = extract[s];
    canvas[d + 1] = extract[s + 1];
    canvas[d + 2] = extract[s + 2];
    canvas[d + 3] = extract[s + 3];
  }
}

await sharp(canvas, { raw: { width: normal.w, height: normal.h, channels: 4 } })
  .png()
  .toFile(path.join(SHARD_DIR, "ui_btn_shard_selected_v1_alpha.png"));
patchMeta(path.join(SHARD_DIR, "ui_btn_shard_selected_v1_alpha.png.meta"), {
  rect: { w: normal.w, h: normal.h },
});

console.log(JSON.stringify({
  fill: fillCrop,
  track: trackCrop,
  handle: handleCrop,
  shard: { normal: { w: normal.w, h: normal.h, ...nb }, selectedIn: sb, selectedOut: { w: normal.w, h: normal.h } },
}));
