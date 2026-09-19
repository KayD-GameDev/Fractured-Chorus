import fs from "fs";
import { createRequire } from "module";

const require = createRequire("C:/Users/Asus/AppData/Local/Temp/package.json");
const Jimp = require("jimp");

const OUTS = [
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_prompt_confirm.png",
  "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/StatusMenu/statusmenu_prompt_confirm.png",
];

const W = 256;
const H = 256;

function setPx(im, x, y, a = 255) {
  if (x < 0 || y < 0 || x >= W || y >= H) return;
  const idx = (y * W + x) * 4;
  im.bitmap.data[idx] = 255;
  im.bitmap.data[idx + 1] = 255;
  im.bitmap.data[idx + 2] = 255;
  im.bitmap.data[idx + 3] = a;
}

function fillPoly(im, pts, a = 255) {
  const minY = Math.max(0, Math.floor(Math.min(...pts.map((p) => p[1]))));
  const maxY = Math.min(H - 1, Math.ceil(Math.max(...pts.map((p) => p[1]))));
  for (let y = minY; y <= maxY; y++) {
    const xs = [];
    for (let i = 0; i < pts.length; i++) {
      const [x1, y1] = pts[i];
      const [x2, y2] = pts[(i + 1) % pts.length];
      if (y1 === y2) {
        if (y === y1) xs.push(x1, x2);
        continue;
      }
      if ((y >= Math.min(y1, y2)) && (y <= Math.max(y1, y2))) {
        const t = (y - y1) / (y2 - y1);
        xs.push(x1 + t * (x2 - x1));
      }
    }
    xs.sort((a, b) => a - b);
    for (let i = 0; i + 1 < xs.length; i += 2) {
      const x0 = Math.max(0, Math.floor(xs[i]));
      const x1 = Math.min(W - 1, Math.ceil(xs[i + 1]));
      for (let x = x0; x <= x1; x++) setPx(im, x, y, a);
    }
  }
}

function strokeLine(im, x0, y0, x1, y1, w = 3, a = 255) {
  const dx = Math.abs(x1 - x0);
  const dy = Math.abs(y1 - y0);
  const sx = x0 < x1 ? 1 : -1;
  const sy = y0 < y1 ? 1 : -1;
  let err = dx - dy;
  let x = x0;
  let y = y0;
  while (true) {
    for (let oy = -w; oy <= w; oy++) {
      for (let ox = -w; ox <= w; ox++) {
        if (ox * ox + oy * oy <= w * w + 1) setPx(im, x + ox, y + oy, a);
      }
    }
    if (x === x1 && y === y1) break;
    const e2 = err * 2;
    if (e2 > -dy) {
      err -= dy;
      x += sx;
    }
    if (e2 < dx) {
      err += dx;
      y += sy;
    }
  }
}

function strokeArc(im, cx, cy, r, a0, a1, w = 3, a = 255) {
  const steps = Math.max(24, Math.round(Math.abs(a1 - a0) * r));
  let px = null;
  for (let i = 0; i <= steps; i++) {
    const t = a0 + ((a1 - a0) * i) / steps;
    const x = Math.round(cx + Math.cos(t) * r);
    const y = Math.round(cy + Math.sin(t) * r);
    if (px) strokeLine(im, px[0], px[1], x, y, w, a);
    px = [x, y];
  }
}

const im = new Jimp(W, H, 0x00000000);

const body = [
  [72, 168],
  [184, 168],
  [198, 152],
  [198, 118],
  [188, 96],
  [168, 82],
  [128, 74],
  [88, 82],
  [68, 96],
  [58, 118],
  [58, 152],
];

fillPoly(im, body, 255);

const leftBtn = [
  [72, 168],
  [126, 168],
  [126, 118],
  [72, 118],
];
fillPoly(im, leftBtn, 255);

for (let y = 118; y <= 168; y++) {
  for (let x = 127; x <= 184; x++) {
    const idx = (y * W + x) * 4;
    im.bitmap.data[idx + 3] = 0;
  }
}

strokeLine(im, 72, 168, 184, 168, 3, 255);
strokeLine(im, 72, 118, 72, 168, 3, 255);
strokeLine(im, 184, 118, 184, 168, 3, 255);
strokeLine(im, 126, 118, 126, 168, 3, 255);
strokeLine(im, 72, 118, 126, 118, 3, 255);
strokeLine(im, 126, 118, 184, 118, 3, 255);

strokeArc(im, 128, 118, 70, Math.PI * 1.04, Math.PI * 1.96, 3, 255);

strokeLine(im, 118, 108, 138, 108, 2, 255);
strokeLine(im, 118, 108, 118, 118, 2, 255);
strokeLine(im, 138, 108, 138, 118, 2, 255);

for (const out of OUTS) {
  await im.clone().writeAsync(out);
  console.log("wrote", out);
}
