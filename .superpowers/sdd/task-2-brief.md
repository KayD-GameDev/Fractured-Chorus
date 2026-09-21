### Task 2: Spec lock + generate / import P0 art

**Files:**
- Create: `docs/superpowers/specs/2026-09-11-bonds-menu-ui-lock.md`
- Create: `Assets/FracturedChorus/Art/UI/Bonds/README.md`
- Create: `Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg` (copy mock)
- Create: `Assets/FracturedChorus/Art/UI/Bonds/Icons/*.png`
- Create: `Assets/FracturedChorus/Art/UI/Bonds/Kit/*.png`
- Create: `Assets/FracturedChorus/Art/UI/Bonds/Decor/*.png`
- Create: `Assets/FracturedChorus/Editor/BondsArtImportEditor.cs`
- Create: `Assets/FracturedChorus/Editor/BondsArtImportTests.cs`

**Interfaces:**
- Produces: Sprite assets at the exact paths in the table below; `BondsArtImportEditor.ConfigureAll()` sets Sprite + Clamp + alpha + 9-slice borders.
- Consumes: Task 1 names only for README slot notes.

#### P0 generate (no text, transparent)

Shared style lock (append to every prompt):

```text
Fractured Chorus UI chrome. Clean vector HUD. Ice-glass cyan and white on transparent.
Hairline geometric strokes, soft inner glow, no photoreal noise, no characters, no letters,
no numbers, no watermark, no drop shadow blobs, square PNG with true alpha.
Palette #00D4FF #8CF3FF #EAFBFF. Flat graphic, not 3D render.
```

| File | Size | 9-slice `border L,B,R,T` | Prompt subject |
|------|------|--------------------------|----------------|
| `Icons/ui_bonds_icon_people_v1.png` | 128×128 | 0 | Two-person silhouette line icon, front view |
| `Icons/ui_bonds_icon_social_stats_v1.png` | 128×128 | 0 | Small pentagon radar + rising bars, line icon |
| `Icons/ui_bonds_icon_link_v1.png` | 128×128 | 0 | Chain-link / connected nodes line icon |
| `Icons/ui_bonds_icon_conversations_v1.png` | 128×128 | 0 | Speech-bubble line icon |
| `Icons/ui_bonds_icon_memories_v1.png` | 128×128 | 0 | Landscape photo-frame line icon |
| `Icons/ui_bonds_icon_gallery_v1.png` | 128×128 | 0 | Grid of four squares line icon |
| `Icons/ui_bonds_icon_stat_resonance_v1.png` | 128×128 | 0 | Simple heart outline |
| `Icons/ui_bonds_icon_stat_cadence_v1.png` | 128×128 | 0 | Open book outline |
| `Icons/ui_bonds_icon_stat_pulse_v1.png` | 128×128 | 0 | ECG / audio waveform |
| `Icons/ui_bonds_icon_stat_harmony_v1.png` | 128×128 | 0 | Three-node network / people cluster |
| `Icons/ui_bonds_icon_stat_rhythm_v1.png` | 128×128 | 0 | Eighth-note outline |
| `Icons/ui_bonds_icon_lock_v1.png` | 128×128 | 0 | Padlock outline |
| `Icons/ui_bonds_icon_play_v1.png` | 128×128 | 0 | Play triangle in circle |
| `Icons/ui_bonds_icon_chevron_v1.png` | 128×128 | 0 | Right-pointing chevron |
| `Icons/ui_bonds_icon_compass_v1.png` | 128×128 | 0 | Four-point star / compass rose |
| `Kit/ui_bonds_panel_glass_v1.png` | 512×256 | 48,48,48,48 | Empty rounded-rect glass panel, even margins, hollow center |
| `Kit/ui_bonds_header_plate_v1.png` | 640×160 | 80,40,80,40 | Empty parallelogram glass plate leaning right, no glyph |
| `Kit/ui_bonds_nav_selected_v1.png` | 512×96 | 40,24,40,24 | Empty wide selected row plate, left accent bar only |
| `Kit/ui_bonds_chip_frame_normal_v1.png` | 256×320 | 24,24,24,24 | Empty portrait chip frame, thin cyan stroke |
| `Kit/ui_bonds_chip_frame_selected_v1.png` | 256×320 | 24,24,24,24 | Same chip frame, brighter cyan outer glow, empty center |
| `Kit/ui_bonds_chip_locked_v1.png` | 256×320 | 24,24,24,24 | Dark glass chip, empty, dim lock-ready frame |
| `Kit/ui_bonds_episode_row_v1.png` | 512×80 | 32,20,32,20 | Empty list row glass bar |
| `Kit/ui_bonds_bar_track_v1.png` | 256×16 | 8,6,8,6 | Empty horizontal bar track |
| `Kit/ui_bonds_bar_fill_v1.png` | 256×16 | 8,6,8,6 | Solid cyan bar fill, no ticks |
| `Decor/ui_bonds_promo_piano_v1.png` | 768×768 | 0 | Anime interior, sunlit room, grand piano, hanging plants, no people, no text |
| `Decor/ui_bonds_silhouette_locked_v1.png` | 256×320 | 0 | Generic long-hair bust silhouette, solid near-black, transparent outside, no face detail |

Do **not** generate: city BG, character busts, radar polygon, Confirm/Back keycaps (uGUI Text + existing prompt sprites if needed).

- [ ] **Step 1: Write failing art-import tests**

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class BondsArtImportTests
    {
        private static readonly string[] Paths =
        {
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_people_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_social_stats_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_link_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_conversations_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_memories_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_gallery_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_resonance_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_cadence_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_pulse_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_harmony_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_rhythm_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_lock_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_play_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_chevron_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_compass_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_panel_glass_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_header_plate_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_nav_selected_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_frame_normal_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_frame_selected_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_locked_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_episode_row_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_bar_track_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_bar_fill_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Decor/ui_bonds_promo_piano_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Decor/ui_bonds_silhouette_locked_v1.png"
        };

        [Test]
        public void P0Sprites_Exist()
        {
            foreach (var path in Paths)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(path), path);
            }
        }

        [Test]
        public void MockRef_Exists()
        {
            Assert.IsNotNull(
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg"));
        }

        [Test]
        public void CityBackground_Reused()
        {
            Assert.IsNotNull(
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/FracturedChorus/Art/UI/StatusMenu/statusmenu_hima_city_bg_v1.jpg"));
        }
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL** (`P0Sprites_Exist` null paths).

- [ ] **Step 3: Copy mock + write spec + README**

Copy mock to `Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg`.

Write `docs/superpowers/specs/2026-09-11-bonds-menu-ui-lock.md` with the Visual lock + File map + Global Constraints copied from this plan header (zones table, roster order, episode titles, art reuse rules, “sandbox before CampusHub”).

`Assets/FracturedChorus/Art/UI/Bonds/README.md`:

```markdown
# Bonds UI art

- `_ref/_ref_bonds_menu_v1.jpg` = composition only. Never assign as runtime Background.
- `Icons/` `Kit/` `Decor/` = production sprites. No baked text.
- City BG reused from StatusMenu. Character busts reused from VnBust / Astra ref.
```

- [ ] **Step 4: Generate each P0 PNG**

From repo root, one command per file. Replace `SUBJECT` with the table’s Prompt subject and `OUT` with the filename.

```bash
belt app run openai/gpt-image-2 --input "{\"prompt\":\"SUBJECT. Fractured Chorus UI chrome. Clean vector HUD. Ice-glass cyan and white on transparent. Hairline geometric strokes, soft inner glow, no photoreal noise, no characters, no letters, no numbers, no watermark, no drop shadow blobs, square PNG with true alpha. Palette #00D4FF #8CF3FF #EAFBFF. Flat graphic, not 3D render.\",\"quality\":\"high\"}"
```

Save the returned image to the exact `Assets/FracturedChorus/Art/UI/Bonds/...` path. Promo piano uses the Decor row prompt (interior allowed; still **no letters**). If a file has baked glyphs or opaque background, regenerate that file only — do not invent a new character.

QA gate per file: open on `#030914` in an image viewer; silhouette reads; **zero** Latin/JP glyphs.

- [ ] **Step 5: Import configurator**

`Assets/FracturedChorus/Editor/BondsArtImportEditor.cs`:

```csharp
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class BondsArtImportEditor
    {
        private static readonly (string path, int l, int b, int r, int t)[] Sliced =
        {
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_panel_glass_v1.png", 48, 48, 48, 48),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_header_plate_v1.png", 80, 40, 80, 40),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_nav_selected_v1.png", 40, 24, 40, 24),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_frame_normal_v1.png", 24, 24, 24, 24),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_frame_selected_v1.png", 24, 24, 24, 24),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_chip_locked_v1.png", 24, 24, 24, 24),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_episode_row_v1.png", 32, 20, 32, 20),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_bar_track_v1.png", 8, 6, 8, 6),
            ("Assets/FracturedChorus/Art/UI/Bonds/Kit/ui_bonds_bar_fill_v1.png", 8, 6, 8, 6)
        };

        private static readonly string[] Simple =
        {
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_people_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_social_stats_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_link_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_conversations_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_memories_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_gallery_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_resonance_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_cadence_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_pulse_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_harmony_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_stat_rhythm_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_lock_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_play_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_chevron_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Icons/ui_bonds_icon_compass_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Decor/ui_bonds_promo_piano_v1.png",
            "Assets/FracturedChorus/Art/UI/Bonds/Decor/ui_bonds_silhouette_locked_v1.png"
        };

        [MenuItem("Fractured Chorus/Bonds/Configure Art Importers")]
        public static void ConfigureAll()
        {
            foreach (var entry in Sliced)
            {
                Configure(entry.path, entry.l, entry.b, entry.r, entry.t);
            }

            foreach (var path in Simple)
            {
                Configure(path, 0, 0, 0, 0);
            }

            Configure("Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg", 0, 0, 0, 0, sprite: false);
            AssetDatabase.SaveAssets();
        }

        private static void Configure(string assetPath, int l, int b, int r, int t, bool sprite = true)
        {
            if (!File.Exists(ToFullPath(assetPath)))
            {
                Debug.LogWarning("[Bonds art] missing " + assetPath);
                return;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (sprite)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(l, b, r, t);
            }

            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static string ToFullPath(string assetPath)
        {
            return Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
#endif
```

Run menu **Fractured Chorus → Bonds → Configure Art Importers**.

- [ ] **Step 6: Re-run `BondsArtImportTests`.** Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add docs/superpowers/specs/2026-09-11-bonds-menu-ui-lock.md Assets/FracturedChorus/Art/UI/Bonds Assets/FracturedChorus/Editor/BondsArtImportEditor.cs Assets/FracturedChorus/Editor/BondsArtImportTests.cs
git commit -m "$(cat <<'EOF'
Add Bonds HUD chrome kit and composition lock for sandbox.

EOF
)"
```

---

