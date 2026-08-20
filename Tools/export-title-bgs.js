const sharp = require("sharp");
const path = require("path");

const srcDir = "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets";
const sheet = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1";
const title = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/TitleScreen";

async function run() {
  await sharp(path.join(srcDir, "title_attract_bg_v1.png"))
    .resize(1920, 1080, { fit: "fill" })
    .png()
    .toFile(path.join(sheet, "title_attract_bg_v1.png"));
  await sharp(path.join(srcDir, "title_menu_cast_bg_v1.png"))
    .resize(1920, 1080, { fit: "fill" })
    .png()
    .toFile(path.join(sheet, "title_menu_cast_bg_v1.png"));
  await sharp(path.join(sheet, "title_attract_bg_v1.png"))
    .png()
    .toFile(path.join(title, "TitleScreen_Attract_v6.png"));
  await sharp(path.join(sheet, "title_menu_cast_bg_v1.png"))
    .png()
    .toFile(path.join(title, "TitleScreen_MainMenu_Background_v6.png"));
  const a = await sharp(path.join(sheet, "title_attract_bg_v1.png")).metadata();
  const b = await sharp(path.join(sheet, "title_menu_cast_bg_v1.png")).metadata();
  console.log("attract", a.width + "x" + a.height, "menu", b.width + "x" + b.height);
}

run().catch((err) => {
  console.error(err);
  process.exit(1);
});
