import sharp from "sharp";

const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";
const SRC = `${KIT}/ui_config_slider_v1.png`;
const QA = "D:/Fractured-Chorus1/Tools/_qa_slider_matte.png";

function luma(r, g, b) {
  return (r + g + b) / 3;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function bias(r, g, b) {
  return (r + b) * 0.5 - g;
}

function punch(px, o) {
  px[o] = px[o + 1] = px[o + 2] = px[o + 3] = 0;
}

function glowToAlpha(r, g, b, a, core) {
  if (a < 8) return [0, 0, 0, 0];
  const L = luma(r, g, b);
  const C = chroma(r, g, b);
  const B = bias(r, g, b);
  if (core && ((L > 88 && B > 40) || (L > 198 && C < 55))) {
    return [r, g, b, 255];
  }
  const premul = Math.max(r, g, b);
  if (premul < 14 || (L < 12 && B < 10)) return [0, 0, 0, 0];
  const na = Math.min(255, Math.round(premul * (a / 255) * 1.15));
  if (na < 16) return [0, 0, 0, 0];
  const s = 255 / premul;
  return [
    Math.min(255, Math.round(r * s)),
    Math.min(255, Math.round(g * s)),
    Math.min(255, Math.round(b * s)),
    na,
  ];
}

function sample(px, w, h, x, y) {
  x = Math.max(0, Math.min(w - 1, Math.round(x)));
  y = Math.max(0, Math.min(h - 1, Math.round(y)));
  const o = (y * w + x) * 4;
  return [px[o], px[o + 1], px[o + 2], px[o + 3]];
}

function cropRows(px, w, h, y0, y1, x0, x1) {
  const cw = x1 - x0;
  const ch = y1 - y0;
  const out = Buffer.alloc(cw * ch * 4);
  for (let y = 0; y < ch; y++) {
    for (let x = 0; x < cw; x++) {
      const s = ((y0 + y) * w + (x0 + x)) * 4;
      const d = (y * cw + x) * 4;
      out[d] = px[s];
      out[d + 1] = px[s + 1];
      out[d + 2] = px[s + 2];
      out[d + 3] = px[s + 3];
    }
  }
  return { data: out, w: cw, h: ch };
}

function applyGlow(img, coreHalf, cy) {
  const { data, w, h } = img;
  for (let y = 0; y < h; y++) {
    const inCore = Math.abs(y - cy) <= coreHalf;
    for (let x = 0; x < w; x++) {
      const o = (y * w + x) * 4;
      const [r, g, b, a] = glowToAlpha(data[o], data[o + 1], data[o + 2], data[o + 3], inCore);
      data[o] = r;
      data[o + 1] = g;
      data[o + 2] = b;
      data[o + 3] = a;
    }
  }
}

function makeSlicedBar(strip, cap, outW) {
  const { data, w, h } = strip;
  const out = Buffer.alloc(outW * h * 4);
  const capW = Math.min(cap, Math.floor(w / 2));
  const bodySrc0 = capW;
  const bodySrc1 = w - capW;
  const bodySrcW = Math.max(1, bodySrc1 - bodySrc0);
  for (let x = 0; x < outW; x++) {
    let sx;
    if (x < capW) sx = x;
    else if (x >= outW - capW) sx = capW - 1 - (outW - 1 - x);
    else sx = bodySrc0 + ((x - capW) % bodySrcW);
    for (let y = 0; y < h; y++) {
      const s = (y * w + sx) * 4;
      const d = (y * outW + x) * 4;
      out[d] = data[s];
      out[d + 1] = data[s + 1];
      out[d + 2] = data[s + 2];
      out[d + 3] = data[s + 3];
    }
  }
  return { data: out, w: outW, h };
}

function padCanvas(img, outH) {
  const { data, w, h } = img;
  const out = Buffer.alloc(w * outH * 4);
  const yOff = Math.round((outH - h) / 2);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const s = (y * w + x) * 4;
      const d = ((y + yOff) * w + x) * 4;
      out[d] = data[s];
      out[d + 1] = data[s + 1];
      out[d + 2] = data[s + 2];
      out[d + 3] = data[s + 3];
    }
  }
  return { data: out, w, h: outH };
}

async function writePng(img, file) {
  await sharp(img.data, { raw: { width: img.w, height: img.h, channels: 4 } }).png().toFile(file);
}

const raw = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
const px = raw.data;
const W = raw.info.width;
const H = raw.info.height;

let sx = 0;
let sy = 0;
let n = 0;
for (let y = 0; y < H; y++) {
  for (let x = 0; x < W; x++) {
    const o = (y * W + x) * 4;
    const L = luma(px[o], px[o + 1], px[o + 2]);
    const C = chroma(px[o], px[o + 1], px[o + 2]);
    if (px[o + 3] > 80 && L > 200 && C < 50) {
      sx += x;
      sy += y;
      n++;
    }
  }
}
const hx = sx / n;
const hy = sy / n;

for (let i = 0; i < W * H; i++) {
  const o = i * 4;
  const y = (i / W) | 0;
  const inCore = Math.abs(y - hy) <= 6 && luma(px[o], px[o + 1], px[o + 2]) > 70;
  const [r, g, b, a] = glowToAlpha(px[o], px[o + 1], px[o + 2], px[o + 3], inCore);
  px[o] = r;
  px[o + 1] = g;
  px[o + 2] = b;
  px[o + 3] = a;
}

const fillY0 = 32;
const fillY1 = 78;
const fillStrip = cropRows(px, W, H, fillY0, fillY1, 11, 300);
applyGlow(fillStrip, 8, hy - fillY0);
const fillBar = padCanvas(makeSlicedBar(fillStrip, 48, 1024), 56);
await writePng(fillBar, `${KIT}/ui_config_slider_fill_v1.png`);

const trackY0 = 42;
const trackY1 = 70;
const trackStrip = cropRows(px, W, H, trackY0, trackY1, 470, 649);
applyGlow(trackStrip, 5, hy - trackY0);
const trackBar = padCanvas(makeSlicedBar(trackStrip, 36, 1024), 48);
await writePng(trackBar, `${KIT}/ui_config_slider_track_v1.png`);

const handleSize = 96;
const handle = Buffer.alloc(handleSize * handleSize * 4);
const glowR = 44;
const ringInner = 14.5;
const ringOuter = 24;
for (let y = 0; y < handleSize; y++) {
  for (let x = 0; x < handleSize; x++) {
    const d = (y * handleSize + x) * 4;
    const ox = x - handleSize / 2;
    const oy = y - handleSize / 2;
    const dist = Math.hypot(ox, oy);
    if (dist > glowR) continue;
    const srcX = hx + ox;
    const srcY = hy + oy;
    let [r, g, b, a] = sample(px, W, H, srcX, srcY);
    const [mr, mg, mb, ma] = sample(px, W, H, hx - ox, srcY);
    const stub = ox < -4 && Math.abs(oy) < 9 && dist > ringInner;
    const origWhite = luma(r, g, b) > 190 && chroma(r, g, b) < 55;
    const mirWhite = luma(mr, mg, mb) > 190 && chroma(mr, mg, mb) < 55;
    if (stub || (!origWhite && mirWhite)) {
      r = mr;
      g = mg;
      b = mb;
      a = ma;
    } else if (mirWhite && origWhite) {
      if (luma(mr, mg, mb) > luma(r, g, b)) {
        r = mr;
        g = mg;
        b = mb;
        a = ma;
      }
    }
    const onRing = dist >= ringInner && dist <= ringOuter;
    const [rr, gg, bb, aa] = glowToAlpha(r, g, b, a, onRing);
    if (dist < ringInner - 1 && luma(rr, gg, bb) < 190) {
      punch(handle, d);
      continue;
    }
    const edge = dist > ringOuter ? Math.max(0, 1 - (dist - ringOuter) / (glowR - ringOuter)) : 1;
    handle[d] = rr;
    handle[d + 1] = gg;
    handle[d + 2] = bb;
    handle[d + 3] = Math.round(aa * edge);
  }
}
await writePng({ data: handle, w: handleSize, h: handleSize }, `${KIT}/ui_config_slider_handle_v1.png`);
await writePng({ data: px, w: W, h: H }, SRC);

async function writeQa(files) {
  const loaded = [];
  for (const f of files) {
    const { data, info } = await sharp(f).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    loaded.push({ data, w: info.width, h: info.height });
  }
  const pad = 20;
  const qaW = Math.min(1400, Math.max(...loaded.map((x) => x.w)) + pad * 2);
  const qaH = loaded.reduce((s, x) => s + Math.min(x.h, 160) + pad, pad);
  const qa = Buffer.alloc(qaW * qaH * 4);
  for (let y = 0; y < qaH; y++) {
    for (let x = 0; x < qaW; x++) {
      const o = (y * qaW + x) * 4;
      const cell = ((x >> 3) & 1) ^ ((y >> 3) & 1);
      const v = cell ? 220 : 160;
      qa[o] = qa[o + 1] = qa[o + 2] = v;
      qa[o + 3] = 255;
    }
  }
  let y0 = pad;
  for (const img of loaded) {
    const showH = Math.min(img.h, 160);
    const showW = Math.min(img.w, qaW - pad * 2);
    for (let y = 0; y < showH; y++) {
      for (let x = 0; x < showW; x++) {
        const s = (y * img.w + x) * 4;
        const d = ((y0 + y) * qaW + (pad + x)) * 4;
        const a = img.data[s + 3] / 255;
        qa[d] = Math.round(img.data[s] * a + qa[d] * (1 - a));
        qa[d + 1] = Math.round(img.data[s + 1] * a + qa[d + 1] * (1 - a));
        qa[d + 2] = Math.round(img.data[s + 2] * a + qa[d + 2] * (1 - a));
      }
    }
    y0 += showH + pad;
  }
  await sharp(qa, { raw: { width: qaW, height: qaH, channels: 4 } }).png().toFile(QA);
}

await writeQa([
  SRC,
  `${KIT}/ui_config_slider_fill_v1.png`,
  `${KIT}/ui_config_slider_track_v1.png`,
  `${KIT}/ui_config_slider_handle_v1.png`,
]);

console.log(
  JSON.stringify({
    handle: { hx: +hx.toFixed(1), hy: +hy.toFixed(1) },
    fill: { w: fillBar.w, h: fillBar.h },
    track: { w: trackBar.w, h: trackBar.h },
  }),
);
