const sharp = require("sharp");
const fs = require("fs");
const path = require("path");

const art = "Assets/FracturedChorus/Art/VFX/Combat/Coda";
const res = "Assets/FracturedChorus/Resources/VFX/Combat/Coda";

function knockVfx(data, channels) {
  for (let i = 0; i < data.length; i += channels) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    const mx = Math.max(r, g, b);
    const mn = Math.min(r, g, b);
    const sat = mx - mn;
    const cyanBias = (g + b) * 0.5 - r;
    const blueBias = b - r;
    const isFx =
      cyanBias > 10 ||
      blueBias > 12 ||
      (sat > 28 && mx > 45 && b > 60) ||
      (g > 80 && b > 100);
    let a;
    if (!isFx) {
      if (mx < 70) a = 0;
      else a = Math.round(Math.max(0, (mx - 70) * 2.2));
    } else {
      a = Math.min(255, Math.round(mx * 1.25 + Math.max(0, cyanBias)));
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
  const src = path.join(art, name);
  const { data, info } = await sharp(src)
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  knockVfx(data, info.channels);
  const out = await sharp(data, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .png()
    .toBuffer();
  fs.writeFileSync(src, out);
  fs.writeFileSync(path.join(res, name), out);
  let a0 = 0;
  for (let i = 3; i < data.length; i += 4) if (data[i] === 0) a0++;
  console.log(
    name,
    "a0%" + ((100 * a0) / (info.width * info.height)).toFixed(1),
    "cornerA",
    data[3]
  );
}

(async () => {
  for (const n of [
    "coda_vfx_crescent_slash_v1.png",
    "coda_vfx_beam_v1.png",
    "coda_vfx_impact_v1.png",
  ]) {
    await process(n);
  }
})().catch((e) => {
  console.error(e);
  process.exit(1);
});
