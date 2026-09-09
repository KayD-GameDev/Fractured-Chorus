import path from "node:path";
import sharp from "sharp";

const ROOT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatMenu";
const SRC = path.join(ROOT, "_ref/_ref_ringhud_waveform_src.png");
const CHIP_PAIR = path.join(ROOT, "_ref/_ref_portrait_chips_open_locked.png");
const HUD = path.join(ROOT, "Decor/Hud");
const KIT = path.join(ROOT, "Kit");
const FRAME_COUNT = 24;
const BAR_KEEP = 0.3;

function toAlpha(px) {
  for (let i = 0; i < px.length; i += 4) {
    const m = Math.max(px[i], px[i + 1], px[i + 2]);
    const a = Math.max(0, Math.min(255, (m - 12) * 5));
    px[i + 3] = a;
    if (a < 8) {
      px[i] = 0;
      px[i + 1] = 0;
      px[i + 2] = 0;
      px[i + 3] = 0;
    }
  }
}

function extract(px, w, h, left, top, width, height) {
  left = Math.max(0, left);
  top = Math.max(0, top);
  width = Math.min(w - left, width);
  height = Math.min(h - top, height);
  const out = Buffer.alloc(width * height * 4);
  for (let y = 0; y < height; y++) {
    px.copy(out, y * width * 4, ((top + y) * w + left) * 4, ((top + y) * w + left + width) * 4);
  }
  return { buf: out, width, height };
}

function retint(px, width) {
  const w = width || 1;
  for (let i = 0; i < px.length; i += 4) {
    if (px[i + 3] < 8) continue;
    const x = Math.floor(i / 4) % w;
    const t = x / Math.max(1, w - 1);
    const lum = Math.max(px[i], px[i + 1], px[i + 2]) / 255;
    const tr = 187 + t * 68;
    const tg = 170 - Math.abs(t - 0.4) * 40 + lum * 40;
    const tb = 255 - t * 18;
    const mix = 0.82;
    const hi = lum > 0.88 ? 1.12 : 1;
    px[i] = Math.min(255, Math.round((px[i] * (1 - mix) + tr * lum * mix) * hi));
    px[i + 1] = Math.min(255, Math.round((px[i + 1] * (1 - mix) + tg * lum * mix) * hi));
    px[i + 2] = Math.min(255, Math.round((px[i + 2] * (1 - mix) + tb * lum * mix) * hi));
  }
}

function cleanBar(src, w, h) {
  const rows = new Array(h);
  for (let y = 0; y < h; y++) {
    let minX = w;
    let maxX = -1;
    let count = 0;
    for (let x = 0; x < w; x++) {
      if (src[(y * w + x) * 4 + 3] < 20) continue;
      count++;
      if (x < minX) minX = x;
      if (x > maxX) maxX = x;
    }
    rows[y] = { minX, maxX, count };
  }
  const counts = rows.filter((r) => r.count > 6).map((r) => r.count).sort((a, b) => a - b);
  const med = counts[counts.length >> 1] || 0;
  let y0 = h;
  let y1 = -1;
  for (let y = 0; y < h; y++) {
    if (rows[y].count < Math.max(8, med * 0.32)) continue;
    if (y < y0) y0 = y;
    if (y > y1) y1 = y;
  }
  if (y1 < y0) return Buffer.from(src);

  const lefts = [];
  const rights = [];
  for (let y = y0; y <= y1; y++) {
    if (rows[y].count < 6) continue;
    lefts.push([y, rows[y].minX]);
    rights.push([y, rows[y].maxX]);
  }
  const fit = (pts) => {
    const n = pts.length;
    let sx = 0;
    let sy = 0;
    let sxy = 0;
    let sx2 = 0;
    for (const [y, x] of pts) {
      sx += y;
      sy += x;
      sxy += y * x;
      sx2 += y * y;
    }
    const den = n * sx2 - sx * sx;
    const m = den === 0 ? 0 : (n * sxy - sx * sy) / den;
    const b = (sy - m * sx) / n;
    return (y) => m * y + b;
  };
  const leftAt = fit(lefts);
  const rightAt = fit(rights);

  const palette = new Array(w);
  for (let x = 0; x < w; x++) {
    let r = 0;
    let g = 0;
    let b = 0;
    let n = 0;
    for (let y = y0; y <= y1; y++) {
      const o = (y * w + x) * 4;
      if (src[o + 3] < 40) continue;
      r += src[o];
      g += src[o + 1];
      b += src[o + 2];
      n++;
    }
    palette[x] = n
      ? [Math.round(r / n), Math.round(g / n), Math.round(b / n)]
      : null;
  }
  let last = [187, 187, 255];
  for (let x = 0; x < w; x++) {
    if (palette[x]) last = palette[x];
    else palette[x] = last;
  }
  last = palette[w - 1];
  for (let x = w - 1; x >= 0; x--) {
    if (palette[x] === last && x < w - 1) continue;
    if (!palette[x]) palette[x] = last;
    else last = palette[x];
  }

  const out = Buffer.alloc(w * h * 4);
  const band = y1 - y0 + 1;
  for (let y = y0; y <= y1; y++) {
    const x0 = Math.max(0, Math.round(leftAt(y)));
    const x1 = Math.min(w - 1, Math.round(rightAt(y)));
    const ty = (y - y0) / Math.max(1, band - 1);
    const highlight = ty < 0.22 ? 1 + (0.22 - ty) * 0.9 : ty > 0.78 ? 1 - (ty - 0.78) * 0.55 : 1;
    for (let x = x0; x <= x1; x++) {
      const col = palette[x];
      const o = (y * w + x) * 4;
      const edge = x === x0 || x === x1 || y === y0 || y === y1 ? 1.12 : 1;
      out[o] = Math.min(255, Math.round(col[0] * highlight * edge));
      out[o + 1] = Math.min(255, Math.round(col[1] * highlight * edge));
      out[o + 2] = Math.min(255, Math.round(col[2] * highlight * edge));
      const dx0 = x - x0;
      const dx1 = x1 - x;
      const dy0 = y - y0;
      const dy1 = y1 - y;
      const edgeDist = Math.min(dx0, dx1, dy0, dy1);
      out[o + 3] = edgeDist <= 0 ? 220 : 255;
    }
  }
  return out;
}

function thinWave(src, w, h) {
  const col = new Array(w);
  for (let x = 0; x < w; x++) {
    let top = -1;
    let bot = -1;
    let count = 0;
    for (let y = 0; y < h; y++) {
      if (src[(y * w + x) * 4 + 3] < 16) continue;
      if (top < 0) top = y;
      bot = y;
      count++;
    }
    col[x] = count < 2 ? null : { top, bot, count };
  }

  const rowCover = new Array(h).fill(0);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      if (src[(y * w + x) * 4 + 3] >= 16) rowCover[y]++;
    }
  }
  let baseY = h - 1;
  let best = -1;
  for (let y = Math.floor(h * 0.45); y < h; y++) {
    if (rowCover[y] > best) {
      best = rowCover[y];
      baseY = y;
    }
  }
  const baseLo = Math.max(0, baseY - 2);
  const baseHi = Math.min(h - 1, baseY + 2);

  const groups = [];
  let i = 0;
  while (i < w) {
    if (!col[i] || col[i].count < 3) {
      i++;
      continue;
    }
    const start = i;
    while (i < w && col[i] && col[i].count >= 3) i++;
    const width = i - start;
    if (width < 2) continue;
    if (width > 18) {
      const mid = start + Math.floor(width / 2);
      groups.push([start, mid - 1]);
      groups.push([mid + 1, i - 1]);
    } else {
      groups.push([start, i - 1]);
    }
  }

  const keep = new Uint8Array(w);
  for (const [a, b] of groups) {
    const width = b - a + 1;
    const keepW = Math.max(1, Math.round(width * BAR_KEEP));
    const mid = (a + b) / 2;
    const x0 = Math.round(mid - (keepW - 1) / 2);
    const x1 = x0 + keepW - 1;
    for (let x = x0; x <= x1; x++) {
      if (x >= 0 && x < w) keep[x] = 1;
    }
  }

  const out = Buffer.alloc(w * h * 4);
  for (let x = 0; x < w; x++) {
    for (let y = 0; y < h; y++) {
      const o = (y * w + x) * 4;
      const a = src[o + 3];
      if (a < 8) continue;
      const onBase = y >= baseLo && y <= baseHi;
      const tall = col[x] && col[x].top < baseLo - 6;
      if (keep[x] || onBase) {
        src.copy(out, o, o, o + 4);
        continue;
      }
      if (!tall && a >= 40) {
        src.copy(out, o, o, o + 4);
      }
    }
  }
  return out;
}

function sampleBilinear(src, w, h, x, y) {
  const x0 = Math.max(0, Math.min(w - 1, Math.floor(x)));
  const y0 = Math.max(0, Math.min(h - 1, Math.floor(y)));
  const x1 = Math.min(w - 1, x0 + 1);
  const y1 = Math.min(h - 1, y0 + 1);
  const tx = x - x0;
  const ty = y - y0;
  const i00 = (y0 * w + x0) * 4;
  const i10 = (y0 * w + x1) * 4;
  const i01 = (y1 * w + x0) * 4;
  const i11 = (y1 * w + x1) * 4;
  const out = [0, 0, 0, 0];
  for (let c = 0; c < 4; c++) {
    const a = src[i00 + c] * (1 - tx) + src[i10 + c] * tx;
    const b = src[i01 + c] * (1 - tx) + src[i11 + c] * tx;
    out[c] = a * (1 - ty) + b * ty;
  }
  return out;
}

function waveformFrames(src, w, h, count) {
  const cols = new Array(w);
  for (let x = 0; x < w; x++) {
    let bot = -1;
    let barTop = -1;
    for (let y = h - 1; y >= 0; y--) {
      if (src[(y * w + x) * 4 + 3] < 16) {
        if (bot >= 0) break;
        continue;
      }
      if (bot < 0) bot = y;
      barTop = y;
    }
    cols[x] = bot < 0 ? null : { top: barTop, bot };
  }

  const frames = [];
  for (let f = 0; f < count; f++) {
    const out = Buffer.alloc(w * h * 4);
    const phase = (f / count) * Math.PI * 2;
    for (let x = 0; x < w; x++) {
      const col = cols[x];
      if (!col) continue;
      const baseH = col.bot - col.top + 1;
      const nx = x / Math.max(1, w - 1);
      const wave =
        1 +
        0.08 * Math.sin(nx * 8.4 + phase) +
        0.045 * Math.sin(nx * 19.2 - phase * 1.15) +
        0.02 * Math.sin(nx * 5.1 + phase * 0.55);
      const scale = Math.max(0.9, Math.min(1.08, wave));
      const keep = Math.min(4, baseH);
      const barH = Math.max(keep + 1, baseH * scale);
      const destTop = col.bot - barH + 1;
      for (let y = Math.max(0, Math.floor(destTop)); y <= col.bot; y++) {
        const fromBot = col.bot - y;
        let srcY;
        if (fromBot < keep) {
          srcY = col.bot - fromBot;
        } else {
          const t = (fromBot - keep) / Math.max(1, barH - keep);
          srcY = col.bot - keep - t * (baseH - keep);
        }
        const samp = sampleBilinear(src, w, h, x, srcY);
        const o = (y * w + x) * 4;
        out[o] = samp[0];
        out[o + 1] = samp[1];
        out[o + 2] = samp[2];
        out[o + 3] = samp[3];
      }
    }
    frames.push(out);
  }
  return frames;
}

async function saveRaw(buf, width, height, dest) {
  await sharp(buf, { raw: { width, height, channels: 4 } }).png().toFile(dest);
}

async function main() {
  const { data, info } = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const px = Buffer.from(data);
  toAlpha(px);
  const { width: w, height: h } = info;

  const barEx = extract(px, w, h, 280, 198, 720, 44);
  toAlpha(barEx.buf);
  const bar = cleanBar(barEx.buf, barEx.width, barEx.height);
  retint(bar, barEx.width);
  await saveRaw(bar, barEx.width, barEx.height, path.join(HUD, "ui_stat_hud_bar_v1.png"));

  const waveEx = extract(px, w, h, 575, 78, 430, 88);
  toAlpha(waveEx.buf);
  const thinned = thinWave(waveEx.buf, waveEx.width, waveEx.height);
  retint(thinned, waveEx.width);
  await saveRaw(thinned, waveEx.width, waveEx.height, path.join(HUD, "ui_stat_hud_wave_base_v1.png"));
  const frames = waveformFrames(thinned, waveEx.width, waveEx.height, FRAME_COUNT);
  for (let i = 0; i < frames.length; i++) {
    await saveRaw(
      frames[i],
      waveEx.width,
      waveEx.height,
      path.join(HUD, `ui_stat_hud_wave_${String(i).padStart(2, "0")}.png`)
    );
  }

  const pair = sharp(CHIP_PAIR);
  await pair
    .clone()
    .extract({ left: 0, top: 0, width: 320, height: 320 })
    .png()
    .toFile(path.join(KIT, "ui_stat_slot_portrait_v1.png"));
  await pair
    .clone()
    .extract({ left: 320, top: 0, width: 320, height: 320 })
    .png()
    .toFile(path.join(KIT, "ui_stat_slot_portrait_locked_v1.png"));

  console.log("bar", barEx.width + "x" + barEx.height, "wave", waveEx.width + "x" + waveEx.height, "chips 320");
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
