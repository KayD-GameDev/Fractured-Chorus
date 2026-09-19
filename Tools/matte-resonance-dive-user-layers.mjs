import fs from "fs";
import path from "path";
import sharp from "sharp";

const SRC = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const ART = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ResonanceDive";
const RES = "D:/Fractured-Chorus1/Assets/FracturedChorus/Resources/UI/ResonanceDive";

const FILES = [
  { id: "02_Border", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_93187fec-9b2e-476a-b381-e52e306be4f1-efa62591-4b55-435a-b6c8-a840fe26a716.jpg", mode: "line" },
  { id: "01_Base", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_1d697642-8c34-40ba-b42d-816539494580-b41831d0-4da2-497f-8b70-6af620439d34.jpg", mode: "plate" },
  { id: "03_Glass", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_8fafe10c-5370-47ec-b117-04f8abbd2dc2-5dbf3e05-f4e3-4eab-b6ec-e02fb81a2372.jpg", mode: "soft" },
  { id: "04_Gradient", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_b02325d6-5875-4ec2-b184-0bf64a8bf8ca-8b4093d6-0a7c-4f11-9342-c3012f2f1a84.jpg", mode: "soft" },
  { id: "05_Deco_Left", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_b359d357-975f-4dc0-9e01-76688b421920-09f049e4-fc9c-4551-bdd5-b2e191fc43a9.jpg", mode: "line" },
  { id: "06_Deco_Right", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_cb7a3031-6990-4ce0-90a4-7d26e2aaeccd-bf7ad0c2-1dbe-4004-8e48-951e98d4b91e.jpg", mode: "line" },
  { id: "08_Glow", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_d9829055-e6d3-4e14-a402-f7166ad85fd7-c66be7a7-b78a-435a-b127-517d7c020c0b.jpg", mode: "glow" },
  { id: "11_Scanline", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_1dc1c703-6575-4d50-811a-9e7d16f36df3-772fd4c3-b5b0-4f55-b659-2cd1bf8608d2.jpg", mode: "line" },
  { id: "09_Particles", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_1a2439bc-bc4e-4e94-8d07-0aad3ba934af-dbf8ba40-28b0-4e03-8efd-7e171cdab5de.jpg", mode: "line" },
  { id: "10_Wave", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_f1e9e007-18f7-4c01-995a-4077c9df466c-32a94398-1cb5-43a9-be46-44fda55a5912.jpg", mode: "line" },
  { id: "12_Shadow", src: "c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_369c4878-fc57-446c-b6e8-f2bfb07b90f3-69e4b051-4489-4a7a-8a96-4b773c011008.jpg", mode: "shadow" },
];

function lum(r, g, b) {
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function tileSize(data, width) {
  const scores = { 8: 0, 16: 0 };
  for (const size of [8, 16]) {
    let ok = 0;
    let n = 0;
    for (let y = 0; y < 64; y += size) {
      for (let x = 0; x < 64; x += 1) {
        const a = lum(data[(y * width + x) * 4], data[(y * width + x) * 4 + 1], data[(y * width + x) * 4 + 2]);
        const b = lum(data[((y + size) * width + x) * 4], data[((y + size) * width + x) * 4 + 1], data[((y + size) * width + x) * 4 + 2]);
        n += 1;
        if (Math.abs(a - b) < 14) ok += 1;
      }
    }
    scores[size] = ok / n;
  }
  return scores[8] >= scores[16] ? 8 : 16;
}

function sampleTileColors(data, width, height, size) {
  const light = [0, 0, 0];
  const dark = [0, 0, 0];
  let ln = 0;
  let dn = 0;
  for (let y = 0; y < Math.min(height, size * 6); y += 1) {
    for (let x = 0; x < Math.min(width, size * 6); x += 1) {
      const o = (y * width + x) * 4;
      const L = lum(data[o], data[o + 1], data[o + 2]);
      if (chroma(data[o], data[o + 1], data[o + 2]) > 16) continue;
      const tx = Math.floor(x / size);
      const ty = Math.floor(y / size);
      if ((tx + ty) % 2 === 0) {
        light[0] += data[o];
        light[1] += data[o + 1];
        light[2] += data[o + 2];
        ln += 1;
      } else {
        dark[0] += data[o];
        dark[1] += data[o + 1];
        dark[2] += data[o + 2];
        dn += 1;
      }
    }
  }
  return {
    light: ln ? [light[0] / ln, light[1] / ln, light[2] / ln] : [200, 200, 200],
    dark: dn ? [dark[0] / dn, dark[1] / dn, dark[2] / dn] : [145, 145, 145],
  };
}

function expectedChecker(x, y, size, colors) {
  const even = (Math.floor(x / size) + Math.floor(y / size)) % 2 === 0;
  return even ? colors.light : colors.dark;
}

function checkerScore(r, g, b, expected) {
  const C = chroma(r, g, b);
  const d = Math.abs(r - expected[0]) + Math.abs(g - expected[1]) + Math.abs(b - expected[2]);
  const L = lum(r, g, b);
  const eL = lum(expected[0], expected[1], expected[2]);
  if (C > 22 && Math.abs(L - eL) > 18) return 0;
  if (d < 28 && C < 18) return 1;
  if (d < 42 && C < 14) return 0.7;
  return 0;
}

function floodChecker(data, width, height, size, colors) {
  const n = width * height;
  const outside = Buffer.alloc(n);
  const queue = [];
  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= width || y >= height) return;
    const i = y * width + x;
    if (outside[i]) return;
    const o = i * 4;
    const exp = expectedChecker(x, y, size, colors);
    if (checkerScore(data[o], data[o + 1], data[o + 2], exp) < 0.65) return;
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
  return outside;
}

function applyAlpha(data, outside, width, height, mode) {
  const out = Buffer.from(data);
  for (let y = 0; y < height; y += 1) {
    for (let x = 0; x < width; x += 1) {
      const i = y * width + x;
      const o = i * 4;
      const r = out[o];
      const g = out[o + 1];
      const b = out[o + 2];
      const L = lum(r, g, b);
      const C = chroma(r, g, b);
      if (outside[i]) {
        let keep = 0;
        if (mode === "glow" && C > 10 && L > 150) keep = Math.min(1, (C - 10) / 30);
        if (mode === "soft" && C > 12) keep = Math.min(1, (C - 12) / 28);
        if (mode === "shadow" && L < 120 && C < 20) keep = Math.min(1, (140 - L) / 80);
        out[o + 3] = Math.round(255 * keep);
        if (out[o + 3] < 8) {
          out[o] = 0;
          out[o + 1] = 0;
          out[o + 2] = 0;
          out[o + 3] = 0;
        }
        continue;
      }
      if (mode === "glow") {
        const a = Math.min(255, Math.round((C * 4 + Math.max(0, L - 160)) * 1.1));
        out[o + 3] = Math.max(40, a);
      } else if (mode === "soft") {
        out[o + 3] = Math.min(255, Math.round(140 + C * 2.4));
      } else if (mode === "shadow") {
        out[o + 3] = Math.min(255, Math.round(Math.max(0, 170 - L) * 2.2));
        out[o] = 8;
        out[o + 1] = 10;
        out[o + 2] = 16;
      } else {
        out[o + 3] = 255;
      }
    }
  }
  for (let y = 1; y < height - 1; y += 1) {
    for (let x = 1; x < width - 1; x += 1) {
      const o = (y * width + x) * 4;
      if (out[o + 3] < 10) continue;
      if (chroma(out[o], out[o + 1], out[o + 2]) > 18) continue;
      let near = 0;
      for (let dy = -2; dy <= 2; dy += 1) {
        for (let dx = -2; dx <= 2; dx += 1) {
          if (out[((y + dy) * width + (x + dx)) * 4 + 3] > 20) near += 1;
        }
      }
      if (near < 8) {
        out[o] = 0;
        out[o + 1] = 0;
        out[o + 2] = 0;
        out[o + 3] = 0;
      }
    }
  }
  return out;
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

function starPivot(data, width, height, box) {
  let sx = 0;
  let sy = 0;
  let w = 0;
  const x0 = box.minX;
  const x1 = Math.round(box.minX + box.w * 0.55);
  const y0 = box.minY;
  const y1 = box.maxY;
  for (let y = y0; y <= y1; y += 1) {
    for (let x = x0; x <= x1; x += 1) {
      const o = (y * width + x) * 4;
      if (data[o + 3] < 40) continue;
      const L = lum(data[o], data[o + 1], data[o + 2]);
      if (L < 170) continue;
      sx += x * L;
      sy += y * L;
      w += L;
    }
  }
  if (!w) return { x: 0.5, y: 0.5 };
  return { x: sx / w / (width - 1), y: 1 - sy / w / (height - 1) };
}

async function main() {
  const report = [];
  let star = { x: 0.32, y: 0.52 };
  for (const file of FILES) {
    const { data, info } = await sharp(fs.readFileSync(path.join(SRC, file.src)))
      .ensureAlpha()
      .raw()
      .toBuffer({ resolveWithObject: true });
    const pixels = Buffer.from(data);
    const size = tileSize(pixels, info.width);
    const colors = sampleTileColors(pixels, info.width, info.height, size);
    const outside = floodChecker(pixels, info.width, info.height, size, colors);
    const matted = applyAlpha(pixels, outside, info.width, info.height, file.mode);
    const box = bbox(matted, info.width, info.height);
    const png = await sharp(matted, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toBuffer();
    fs.writeFileSync(path.join(ART, `${file.id}.png`), png);
    fs.writeFileSync(path.join(RES, `${file.id}.png`), png);
    if (file.id === "05_Deco_Left" && box) {
      star = starPivot(matted, info.width, info.height, box);
    }
    const flooded = outside.reduce((n, v) => n + (v ? 1 : 0), 0);
    report.push({
      id: file.id,
      source: "jpeg-no-alpha",
      bakedChecker: true,
      tile: size,
      box,
      flooded,
    });
    console.log("matted", file.id, box, "tile", size);
  }
  fs.writeFileSync(
    path.join(ART, "layer_import_report.json"),
    JSON.stringify({ bakedChecker: true, format: "jpeg", canvas: "1024x576", starPivot: star, layers: report }, null, 2),
  );
  console.log("starPivot", star);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
