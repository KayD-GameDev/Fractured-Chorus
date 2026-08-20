const sharp = require("sharp");
const fs = require("fs");
const path = require("path");

function chromaScore(r, g, b) {
  return Math.hypot(r - 255, g - 0, b - 255);
}

async function matteFile(input, output, { crop = true, pad = 12 } = {}) {
  const { data, info } = await sharp(input).ensureAlpha().raw().toBuffer({
    resolveWithObject: true,
  });
  const w = info.width;
  const h = info.height;
  const px = data;
  const visited = Buffer.alloc(w * h);
  const q = [];

  const push = (x, y) => {
    if (x < 0 || y < 0 || x >= w || y >= h) {
      return;
    }
    const id = y * w + x;
    if (visited[id]) {
      return;
    }
    visited[id] = 1;
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
    const i = id * 4;
    if (chromaScore(px[i], px[i + 1], px[i + 2]) >= 118) {
      continue;
    }
    px[i + 3] = 0;
    const x = id % w;
    const y = (id / w) | 0;
    push(x - 1, y);
    push(x + 1, y);
    push(x, y - 1);
    push(x, y + 1);
  }

  for (let y = 1; y < h - 1; y++) {
    for (let x = 1; x < w - 1; x++) {
      const i = (y * w + x) * 4;
      if (px[i + 3] === 0) {
        continue;
      }
      let hit = false;
      for (const [dx, dy] of [
        [-1, 0],
        [1, 0],
        [0, -1],
        [0, 1],
      ]) {
        if (px[((y + dy) * w + (x + dx)) * 4 + 3] === 0) {
          hit = true;
          break;
        }
      }
      if (!hit) {
        continue;
      }
      const mag = Math.min(px[i], px[i + 2]);
      const spill = Math.max(0, mag - px[i + 1]);
      if (spill > 16) {
        px[i] = Math.max(0, px[i] - spill * 0.85);
        px[i + 2] = Math.max(0, px[i + 2] - spill * 0.85);
        px[i + 3] = Math.max(20, Math.round(px[i + 3] * (1 - spill / 320)));
      }
    }
  }

  let out = sharp(Buffer.from(px), {
    raw: { width: w, height: h, channels: 4 },
  });

  if (crop) {
    let minX = w;
    let minY = h;
    let maxX = 0;
    let maxY = 0;
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        if (px[(y * w + x) * 4 + 3] < 8) {
          continue;
        }
        if (x < minX) minX = x;
        if (y < minY) minY = y;
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
      }
    }
    if (maxX > minX && maxY > minY) {
      const left = Math.max(0, minX - pad);
      const top = Math.max(0, minY - pad);
      const width = Math.min(w - left, maxX - minX + 1 + pad * 2);
      const height = Math.min(h - top, maxY - minY + 1 + pad * 2);
      out = out.extract({ left, top, width, height });
    }
  }

  await out.png().toFile(output);
}

async function silhouette(input, output) {
  const { data, info } = await sharp(input).ensureAlpha().raw().toBuffer({
    resolveWithObject: true,
  });
  for (let i = 0; i < data.length; i += 4) {
    if (data[i + 3] < 12) {
      data[i + 3] = 0;
      continue;
    }
    data[i] = 12;
    data[i + 1] = 10;
    data[i + 2] = 18;
  }
  await sharp(Buffer.from(data), {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .png()
    .toFile(output);
}

async function main() {
  const srcDir = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
  const dest = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1";
  fs.mkdirSync(dest, { recursive: true });

  const jobs = [
    ["char_astra_title_pose_v1.png", true],
    ["char_ren_title_pose_v1.png", true],
    ["char_coda_title_pose_v1.png", true],
    ["char_charlotte_title_pose_v1.png", true],
    ["logo_fractured_chorus_v2.png", true],
    ["ui_kit_chroma_v1.png", false],
    ["title_cityscape_silhouette_v1.png", true],
    ["ui_hud_ring_v2.png", true],
    ["ui_shards_glitch_chroma_v1.png", false],
    ["ui_prompts_click_enter_v2.png", false],
  ];

  for (const [name, crop] of jobs) {
    const input = path.join(srcDir, name);
    if (!fs.existsSync(input)) {
      console.error("missing", name);
      continue;
    }
    const output = path.join(dest, name.replace(/\.(png)$/i, "_alpha.png"));
    await matteFile(input, output, { crop });
    const meta = await sharp(output).metadata();
    console.log("matted", path.basename(output), meta.width, "x", meta.height, "alpha", meta.hasAlpha);
  }

  const chars = [
    "char_astra_title_pose_v1_alpha.png",
    "char_ren_title_pose_v1_alpha.png",
    "char_coda_title_pose_v1_alpha.png",
    "char_charlotte_title_pose_v1_alpha.png",
  ];
  for (const name of chars) {
    const input = path.join(dest, name);
    const output = path.join(dest, name.replace("_alpha.png", "_silhouette.png"));
    await silhouette(input, output);
    console.log("silhouette", path.basename(output));
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
