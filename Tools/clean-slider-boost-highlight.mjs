import sharp from "sharp";

const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";
const SHARD = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1";

function bias(r, g, b) {
  return (r + b) * 0.5 - g;
}

function luma(r, g, b) {
  return (r + g + b) / 3;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

async function load(file) {
  const { data, info } = await sharp(file).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { px: data, w: info.width, h: info.height };
}

function rebuildSliderAlpha(img, handle) {
  const { px, w, h } = img;
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    const L = luma(r, g, b);
    const C = chroma(r, g, b);
    const B = bias(r, g, b);

    let a = 0;
    if (handle) {
      if (L > 190) a = 255;
      else if (B > 22 && L > 70) a = Math.min(255, Math.round((B - 10) * 8 + (L - 70) * 1.2));
      else if (B > 18 && L > 90) a = Math.round(Math.min(180, (B - 18) * 14));
      else a = 0;
    } else {
      if (C < 18 && L > 130 && B < 12) a = 0;
      else if (L > 210 && B >= 3) a = 255;
      else if (B >= 28) a = 255;
      else if (B >= 10) a = Math.round(Math.min(220, ((B - 10) / 22) * 220));
      else a = 0;
    }

    if (a < 12) {
      px[o] = 0;
      px[o + 1] = 0;
      px[o + 2] = 0;
      px[o + 3] = 0;
    } else {
      px[o + 3] = a;
    }
  }
}

function cropContent(img, pad) {
  const { px, w, h } = img;
  let minX = w;
  let minY = h;
  let maxX = 0;
  let maxY = 0;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (px[(y * w + x) * 4 + 3] < 14) continue;
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x);
      maxY = Math.max(maxY, y);
    }
  }
  minX = Math.max(0, minX - pad);
  minY = Math.max(0, minY - pad);
  maxX = Math.min(w - 1, maxX + pad);
  maxY = Math.min(h - 1, maxY + pad);
  const cw = maxX - minX + 1;
  const ch = maxY - minY + 1;
  const out = Buffer.alloc(cw * ch * 4);
  for (let y = 0; y < ch; y++) {
    for (let x = 0; x < cw; x++) {
      const s = ((minY + y) * w + (minX + x)) * 4;
      const d = (y * cw + x) * 4;
      out[d] = px[s];
      out[d + 1] = px[s + 1];
      out[d + 2] = px[s + 2];
      out[d + 3] = px[s + 3];
    }
  }
  img.px = out;
  img.w = cw;
  img.h = ch;
}

async function save(img, file) {
  await sharp(img.px, { raw: { width: img.w, height: img.h, channels: 4 } }).png().toFile(file);
}

function mix(a, b, t) {
  return Math.round(a + (b - a) * t);
}

function strengthenHighlight(img) {
  const { px, w, h } = img;
  const mask = Buffer.alloc(w * h);
  for (let i = 0; i < w * h; i++) {
    mask[i] = px[i * 4 + 3] > 90 ? 1 : 0;
  }

  const distIn = new Float32Array(w * h);
  const distOut = new Float32Array(w * h);
  distIn.fill(99);
  distOut.fill(99);
  const R = 10;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const i = y * w + x;
      const inside = mask[i];
      for (let dy = -R; dy <= R; dy++) {
        const yy = y + dy;
        if (yy < 0 || yy >= h) continue;
        for (let dx = -R; dx <= R; dx++) {
          const xx = x + dx;
          if (xx < 0 || xx >= w) continue;
          if (mask[yy * w + xx] === inside) continue;
          const d = Math.hypot(dx, dy);
          if (inside) distIn[i] = Math.min(distIn[i], d);
          else distOut[i] = Math.min(distOut[i], d);
        }
      }
    }
  }

  const out = Buffer.from(px);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const i = y * w + x;
      const o = i * 4;
      if (mask[i]) {
        const d = distIn[i];
        if (d <= 8) {
          const t = Math.max(0, 1 - d / 8);
          const stroke = Math.min(1, t * 1.5);
          out[o] = mix(px[o], 228, stroke);
          out[o + 1] = mix(px[o + 1], 28, stroke);
          out[o + 2] = mix(px[o + 2], 255, stroke);
          out[o + 3] = 255;
        } else {
          out[o] = mix(px[o], 236, 0.12);
          out[o + 1] = mix(px[o + 1], 210, 0.12);
          out[o + 2] = mix(px[o + 2], 255, 0.12);
        }
        continue;
      }

      const d = distOut[i];
      if (d <= 9) {
        const t = 1 - d / 9;
        const glow = Math.round(255 * t);
        if (glow < 16) continue;
        out[o] = 210;
        out[o + 1] = 32;
        out[o + 2] = 255;
        out[o + 3] = glow;
      } else {
        out[o] = 0;
        out[o + 1] = 0;
        out[o + 2] = 0;
        out[o + 3] = 0;
      }
    }
  }
  img.px = out;
}

const fill = await load(`${KIT}/ui_config_slider_fill_v1.png`);
rebuildSliderAlpha(fill, false);
cropContent(fill, 4);
await save(fill, `${KIT}/ui_config_slider_fill_v1.png`);

const track = await load(`${KIT}/ui_config_slider_track_v1.png`);
rebuildSliderAlpha(track, false);
cropContent(track, 4);
await save(track, `${KIT}/ui_config_slider_track_v1.png`);

const handle = await load(`${KIT}/ui_config_slider_handle_v1.png`);
rebuildSliderAlpha(handle, true);
cropContent(handle, 3);
await save(handle, `${KIT}/ui_config_slider_handle_v1.png`);

const selected = await load(`${SHARD}/ui_btn_shard_selected_v1_alpha.png`);
strengthenHighlight(selected);
await save(selected, `${SHARD}/ui_btn_shard_selected_v1_alpha.png`);

console.log(JSON.stringify({ fill: [fill.w, fill.h], track: [track.w, track.h], handle: [handle.w, handle.h] }));
