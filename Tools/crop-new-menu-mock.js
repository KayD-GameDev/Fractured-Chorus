const sharp = require("sharp");
const path = require("path");
const fs = require("fs");

const src =
  "C:/Users/Asus/.cursor/projects/d-Fractured-Chorus1/assets/c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_BG_main_menu-443a6940-78eb-479e-8e9b-b029115a62be.png";
const dest = "D:/Fractured-Chorus1/Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1";

async function run() {
  fs.copyFileSync(src, path.join(dest, "_ref_BG_main_menu_stack.png"));
  const meta = await sharp(src).metadata();
  const half = Math.floor(meta.height / 2);
  await sharp(src)
    .extract({ left: 0, top: 0, width: meta.width, height: half })
    .png()
    .toFile(path.join(dest, "_crop_attract_raw.png"));
  await sharp(src)
    .extract({ left: 0, top: half, width: meta.width, height: meta.height - half })
    .png()
    .toFile(path.join(dest, "_crop_menu_raw.png"));
  console.log("stack", meta.width + "x" + meta.height, "half", half);
}

run().catch((err) => {
  console.error(err);
  process.exit(1);
});
