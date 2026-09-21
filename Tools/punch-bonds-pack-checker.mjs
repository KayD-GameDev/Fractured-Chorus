import fs from "fs";
import path from "path";
import sharp from "sharp";

const DIR = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/Bonds/Pack";

function chroma(r, g, b) {
  return Math.max(r, g, b) - Math.min(r, g, b);
}

function luma(r, g, b) {
  return 0.299 * r + 0.587 * g + 0.114 * b;
}

function punch(data, width, height) {
  const n = width * height;
  const cand = new Uint8Array(n);
  for (let idx = 0; idx < n; idx++) {
    const i = idx * 4;
    const a = data[i + 3];
    if (a < 180) continue;
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    const y = luma(r, g, b);
    if (y >= 246 || y < 78) continue;
    if (chroma(r, g, b) > 40) continue;
    if (y > 242) continue;
    cand[idx] = 1;
  }

  const seen = new Uint8Array(n);
  const queue = new Uint32Array(n);
  let punched = 0;
  let components = 0;
  for (let start = 0; start < n; start++) {
    if (!cand[start] || seen[start]) continue;
    let head = 0;
    let tail = 0;
    queue[tail++] = start;
    seen[start] = 1;
    let lumaMin = 255;
    let lumaMax = 0;
    const first = tail - 1;
    while (head < tail) {
      const idx = queue[head++];
      const i = idx * 4;
      const y = luma(data[i], data[i + 1], data[i + 2]);
      if (y < lumaMin) lumaMin = y;
      if (y > lumaMax) lumaMax = y;
      const x = idx % width;
      const yy = (idx / width) | 0;
      const tryPush = (nx, ny) => {
        if (nx < 0 || ny < 0 || nx >= width || ny >= height) return;
        const nidx = ny * width + nx;
        if (seen[nidx] || !cand[nidx]) return;
        seen[nidx] = 1;
        queue[tail++] = nidx;
      };
      tryPush(x - 1, yy);
      tryPush(x + 1, yy);
      tryPush(x, yy - 1);
      tryPush(x, yy + 1);
    }
    const size = tail - first;
    const range = lumaMax - lumaMin;
    const drop = (size >= 140 && range >= 16) || (size >= 1800 && lumaMin >= 85 && lumaMax <= 242);
    if (!drop) continue;
    components += 1;
    for (let q = first; q < tail; q++) {
      const i = queue[q] * 4;
      data[i] = 0;
      data[i + 1] = 0;
      data[i + 2] = 0;
      data[i + 3] = 0;
      punched += 1;
    }
  }
  return { punched, components };
}

const files = fs.readdirSync(DIR).filter((f) => f.endsWith(".png")).sort();
const report = [];
for (const file of files) {
  const filePath = path.join(DIR, file);
  const { data, info } = await sharp(filePath).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const pixels = Buffer.from(data);
  const result = punch(pixels, info.width, info.height);
  await sharp(pixels, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .png()
    .toFile(filePath);
  report.push({ file, ...result, px: info.width * info.height });
  console.log(`${file} punched=${result.punched} comps=${result.components}`);
}
fs.writeFileSync(path.join(DIR, "PUNCH_REPORT.json"), JSON.stringify(report, null, 2) + "\n");
console.log(JSON.stringify({ count: report.length, totalPunched: report.reduce((s, r) => s + r.punched, 0) }));
