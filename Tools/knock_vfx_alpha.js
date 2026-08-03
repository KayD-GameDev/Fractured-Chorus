const sharp = require("sharp");
const fs = require("fs");
const path = require("path");

const art = "Assets/FracturedChorus/Art/VFX/Combat/Ren";
const res = "Assets/FracturedChorus/Resources/VFX/Combat/Ren";

function knockVfx(data, channels) {
  for (let i = 0; i < data.length; i += channels) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    const mx = Math.max(r, g, b);
    const mn = Math.min(r, g, b);
    const sat = mx - mn;
    const pinkBias = (r + b) * 0.5 - g;
    const isFx = pinkBias > 8 || (sat > 28 && mx > 45) || (r > 90 && b > 70);
    let a;
    if (!isFx) {
      if (mx < 70) a = 0;
      else a = Math.round(Math.max(0, (mx - 70) * 2.2));
    } else {
      a = Math.min(255, Math.round(mx * 1.25 + Math.max(0, pinkBias)));
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
  const { data, info } = await sharp(src).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
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
  const t = info.width * info.height;
  console.log(name, "a0%" + ((100 * a0) / t).toFixed(1), "cornerA", data[3]);
}

(async () => {
  for (const n of [
    "ren_bullet_trail_v1.png",
    "ren_bullet_impact_v1.png",
    "ren_bullet_head_v1.png",
    "ren_melee_arc_v1.png",
    "ren_melee_impact_v1.png",
  ]) {
    await process(n);
  }
})().catch((e) => {
  console.error(e);
  process.exit(1);
});
