const sharp = require("sharp");
const fs = require("fs");
const path = require("path");

const srcDir = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const art = "Assets/FracturedChorus/Art/VFX/Combat/Charlotte";
const res = "Assets/FracturedChorus/Resources/VFX/Combat/Charlotte";

function knockVfx(data, channels) {
  for (let i = 0; i < data.length; i += channels) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    const mx = Math.max(r, g, b);
    const mn = Math.min(r, g, b);
    const sat = mx - mn;
    const warmBias = r - b + (r - g) * 0.5;
    const isFx =
      warmBias > 12 ||
      (sat > 28 && mx > 45 && r > g) ||
      (r > 100 && g > 40 && b < r);
    let a;
    if (!isFx) {
      if (mx < 70) a = 0;
      else a = Math.round(Math.max(0, (mx - 70) * 2.2));
    } else {
      a = Math.min(255, Math.round(mx * 1.25 + Math.max(0, warmBias * 0.5)));
    }
    if (a < 8) a = 0;
    data[i + 3] = a;
    if (a === 0) {
      data[i] = 0;
      data[i + 1] = 0;
      data[i + 2] = 0;
    }
  }
}

async function process(name) {
  const from = path.join(srcDir, name);
  const { data, info } = await sharp(from)
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  knockVfx(data, info.channels);
  const out = await sharp(data, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .png()
    .toBuffer();
  fs.mkdirSync(art, { recursive: true });
  fs.mkdirSync(res, { recursive: true });
  fs.writeFileSync(path.join(art, name), out);
  fs.writeFileSync(path.join(res, name), out);
  let a0 = 0;
  for (let i = 3; i < data.length; i += 4) if (data[i] === 0) a0++;
  console.log(
    name,
    "a0%" + ((100 * a0) / (info.width * info.height)).toFixed(1)
  );
}

(async () => {
  for (const n of [
    "charlotte_vfx_note_scatter_v1.png",
    "charlotte_vfx_shield_create_v1.png",
    "charlotte_vfx_counter_shield_v1.png",
  ]) {
    await process(n);
  }
})().catch((e) => {
  console.error(e);
  process.exit(1);
});
