import path from "node:path";
import sharp from "sharp";

const KIT = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/ConfigMenu/Kit";
const SRC = path.join(KIT, "ui_config_widgets_v1.png");

function lumaMatte(px, w, h, floor = 48, knee = 0.35) {
  for (let i = 0; i < w * h; i++) {
    const o = i * 4;
    const r = px[o];
    const g = px[o + 1];
    const b = px[o + 2];
    const max = Math.max(r, g, b);
    if (max < floor) {
      px[o] = 0;
      px[o + 1] = 0;
      px[o + 2] = 0;
      px[o + 3] = 0;
      continue;
    }

    const a8 = Math.round(Math.min(1, max / 255 / knee) * 255);
    if (a8 < 8) {
      px[o] = 0;
      px[o + 1] = 0;
      px[o + 2] = 0;
      px[o + 3] = 0;
      continue;
    }

    px[o + 3] = a8;
  }
}

function components(px, w, h, minArea = 2500) {
  const seen = Buffer.alloc(w * h);
  const out = [];
  const isInk = (id) => px[id * 4 + 3] >= 10;
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const start = y * w + x;
      if (seen[start] || !isInk(start)) {
        continue;
      }

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
        if (cx < minX) {
          minX = cx;
        }
        if (cy < minY) {
          minY = cy;
        }
        if (cx > maxX) {
          maxX = cx;
        }
        if (cy > maxY) {
          maxY = cy;
        }
        const nbs = [id - 1, id + 1, id - w, id + w];
        for (const n of nbs) {
          if (n < 0 || n >= w * h || seen[n] || !isInk(n)) {
            continue;
          }
          const nx = n % w;
          const ny = (n / w) | 0;
          if (Math.abs(nx - cx) + Math.abs(ny - cy) !== 1) {
            continue;
          }
          seen[n] = 1;
          q.push(n);
        }
      }

      const bw = maxX - minX + 1;
      const bh = maxY - minY + 1;
      if (area < minArea || bw < 60 || bh < 50) {
        continue;
      }

      out.push({ minX, minY, maxX, maxY, cx: (minX + maxX) / 2, cy: (minY + maxY) / 2 });
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

  for (const row of rows) {
    row.sort((a, b) => a.cx - b.cx);
  }

  return rows;
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
  console.log("restore", path.basename(dest), width + "x" + height);
}

const { data, info } = await sharp(SRC).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
lumaMatte(data, info.width, info.height);
const boxes = components(data, info.width, info.height);
const rows = clusterRows(boxes);
console.log("widget rows", rows.map((r) => r.length).join(","));

const namesByRow = [
  ["ui_config_speaker_min_v1.png", "ui_config_slider_v1.png", "ui_config_speaker_max_v1.png", "ui_config_btn_minus_v1.png", "ui_config_btn_plus_v1.png"],
  ["ui_config_toggle_on_v1.png", "ui_config_toggle_off_v1.png"],
  ["ui_config_chip_normal_v1.png", "ui_config_chip_selected_v1.png"],
  ["ui_config_icon_note_v1.png", "ui_config_icon_brightness_v1.png", "ui_config_icon_skip_v1.png", "ui_config_icon_difficulty_v1.png"],
];

for (let i = 0; i < rows.length; i++) {
  const names = namesByRow[i] ?? [];
  for (let j = 0; j < rows[i].length; j++) {
    const name = names[j];
    if (!name) {
      continue;
    }

    await saveCrop(data, info.width, info.height, rows[i][j], 10, path.join(KIT, name));
  }
}
