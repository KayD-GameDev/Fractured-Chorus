const sharp = require("sharp");
const path = require("path");

const DIR = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1";
const W = 1920;
const H = 1080;
const CHAR_H = 940;
const GROUND = 36;

function isMagentaHalo(r, g, b) {
  return r > 170 && b > 170 && g < 115 && r + b - 2 * g > 180;
}

async function prepareChar(file, protectBottomRatio) {
  const { data, info } = await sharp(path.join(DIR, file))
    .ensureAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true });
  const w = info.width;
  const h = info.height;
  const protectFromY = Math.floor(h * (1 - protectBottomRatio));
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const i = (y * w + x) * 4;
      if (data[i + 3] === 0) {
        continue;
      }
      if (y >= protectFromY) {
        continue;
      }
      if (isMagentaHalo(data[i], data[i + 1], data[i + 2])) {
        data[i + 3] = 0;
      }
    }
  }
  return sharp(Buffer.from(data), {
    raw: { width: w, height: h, channels: 4 },
  })
    .resize({ height: CHAR_H, fit: "inside" })
    .png()
    .toBuffer({ resolveWithObject: true });
}

async function main() {
  const env = await sharp(path.join(DIR, "title_env_bg_v1.png"))
    .resize(W, H, { fit: "cover", position: "centre" })
    .ensureAlpha()
    .png()
    .toBuffer();

  const astra = await prepareChar("char_astra_title_pose_v1_alpha.png", 0);
  const ren = await prepareChar("char_ren_title_pose_v1_alpha.png", 0);
  const coda = await prepareChar("char_coda_title_pose_v1_alpha.png", 0.46);
  const charlotte = await prepareChar("char_charlotte_title_pose_v1_alpha.png", 0);

  const place = (layer, x) => ({
    input: layer.data,
    left: x,
    top: H - GROUND - layer.info.height,
  });

  await sharp(env)
    .composite([
      place(astra, 920),
      place(coda, 1328),
      place(ren, 1148),
      place(charlotte, 1568),
    ])
    .png()
    .toFile(path.join(DIR, "title_keyart_cast_v1.png"));

  const meta = await sharp(path.join(DIR, "title_keyart_cast_v1.png")).metadata();
  console.log("wrote title_keyart_cast_v1.png", meta.width, "x", meta.height);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
