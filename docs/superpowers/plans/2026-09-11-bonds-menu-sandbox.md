# Bonds Menu Sandbox Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a standalone `BondsLayoutSandbox` scene that matches the Bonds glass-HUD composition (Social Stats radar + roster + detail + Link Episodes) before any change to `CampusHub.unity`.

**Architecture:** Layout lives on the sandbox scene (same contract as CharacterBuild). Runtime only binds data (`GameMetaState.SocialStats` + `BondState`). Radar reuses `SocialStatsRadarGraphic` / `SocialStatsNodeView` as scene components — do not call `SocialStatsOverlayUI.Build` (it hardcodes Rects). Chrome is sliced uGUI Images; mock PNG is MockGuide only.

**Tech Stack:** Unity 6 · uGUI legacy `Text` · `FracturedChorus.Hub` · `FracturedChorus.Meta` · `FcColorTokens` · `UiFontCatalog` · NUnit EditMode · GPT-Image-2 (`belt`) for chrome/icons

## Global Constraints

- Canvas **1920×1080**, Scale With Screen Size, Match **0.5**, title-safe ~90%.
- Layout Rect (`anchoredPosition` / `sizeDelta` / anchors / sibling order) **only** on the `.unity` scene. **Never** hardcode layout in C# (no pos/size tables, no `SeedRect` with mock pixels, no runtime `anchoredPosition` / `sizeDelta`). Create named objects on the scene; leave RectTransform at Unity defaults (or stretch-full for Background/MockGuide only, written into the scene file). Heal/Rebuild requires confirm. After Hierarchy tweaks: Ctrl+S; Attach must not overwrite Rects.
- **Do not** open, heal, or wire `Assets/FracturedChorus/Scenes/CampusHub.unity` in this plan.
- **Do not** add `BondsLayoutSandbox.unity` to `EditorBuildSettings`.
- Copy MVP **EN** + JP subtitles from the mock (`絆`, `共鳴ステータス`, `リンクエピソード`). Do not bake any glyphs into PNGs.
- Palette: `FcColorTokens.Brand.Cyan` / `CyanSoft` / `TextPrimary` / `Surface.*`. No new hex literals in runtime C#.
- Input: `TownMapInput` semantics — Left/Right cycle roster, Enter confirm (sandbox logs), Esc no-op in sandbox (hub wire is out of scope). Gamepad South/East later, same semantics.
- Character art: **reuse** existing busts/refs. Do **not** generate new character sprites. Mock’s white-haired “Charlotte” is composition-only; runtime Charlotte = ginger lock (`Art/Characters/Charlotte/CHARACTER_LOCK.md`).
- Radar axis math already puts **Pulse at 12 o’clock** (`SocialStatsRadarGraphic.CacheAxisDirections`). Keep it. Node labels are scene objects around the chart.
- Rank/EXP numbers from `BondProgress` / `SocialStatsState` — mock `0/100` is illustrative. `GetThresholdForRank(1)` is **10**, not 100.
- No comments in new source. Namespace `FracturedChorus.Hub` for UI, `FracturedChorus.Meta` for existing state, `FracturedChorus.Tests` for EditMode tests.
- Do not call `SocialStatsOverlayUI.Build`. Do not assign `_ref_bonds_menu_v1` as production Background sprite.

### Out of scope (this plan)

- Wiring BtnBonds / replacing `OpenSocialStatsOverlay` on CampusHub
- Conversations / Memories / Gallery pages (rows visible, disabled)
- Per-NPC unique Link Episode scripts / VN playback
- Echo Keys Screen B list (Persona-style) — superseded as the Bonds **entry** visual
- New fullbody / bust character art
- Gamepad prompt art swap

---

## Visual lock (from user mock)

Composition reference (copy in Task 2):

`Assets/FracturedChorus/Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg`

Source in this chat:

`C:\Users\Asus\.cursor\projects\d-Fractured-Chorus1\assets\c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_image-304f4865-2349-480c-b3ad-4cf09b608dce.jpg`

Zones (16:9):

```
┌─ date / HIMA CITY ──────────────────────────────── PEOPLE MAKE MUSIC. ★ ─┐
│  ┌ BONDS 絆 ┐     SOCIAL STATS / 共鳴ステータス          LINK EPISODES      │
│  │ Social Stats│        5-axis radar + 5 nodes           01–05 rows       │
│  │ Link        │     roster chips (4 unlocked + 3 lock)  lock hint        │
│  │ Conversations│    ┌ detail card: bust + bio + EXP ┐   promo piano      │
│  │ Memories    │    └────────────────────────────────┘                    │
│  │ Gallery     │                                      [Confirm] [Back]    │
└──────────────────────────────────────────────────────────────────────────┘
```

| Zone | Runtime |
|------|---------|
| Corner date | `HubCornerInfoHud` — date/day/phase from `Calendar`. Location + tagline = scene Text (`HIMA CITY` / `Music Lives in You`). |
| BONDS header | Scene chrome + `BondPresentation.Title` / `TitleJp` |
| Left nav | Social Stats selected + enabled. Other four rows exist, `interactable = false`, muted. |
| Radar | `SocialStatsRadarGraphic.SetRanks` from `SocialStatsState` |
| Nodes | `SocialStatsNodeView.Bind` + icons from Task 2 |
| Roster | Order: Ren, Charlotte, Coda, Astra, Ryo, MeiLin, reserved. Default select **Charlotte**. Unlocked portraits: Ren, Charlotte, Coda (Astra story-gated). |
| Detail | Selected NPC bust + bio/quote + `Rank n` + `{exp}/{threshold}` + NEXT RANK hint |
| Link Episodes | 5 rows; unlock `bond.Rank >= requiredRank`. Confirm on unlocked row = `Debug.Log` only. |
| BG | Reuse `Art/UI/StatusMenu/statusmenu_hima_city_bg_v1.jpg` |
| MockGuide | Full mock at alpha 0.4, default **off** |

---

## File map

| Path | Responsibility |
|------|----------------|
| `docs/superpowers/specs/2026-09-11-bonds-menu-ui-lock.md` | Visual / data lock |
| `.cursor/rules/bonds-ui-scene-layout.mdc` | Scene layout SoT (mirrors CharacterBuild rule) |
| `Hub/BondPresentation.cs` | Copy, roster order, bios, unlock helper |
| `Hub/BondLinkEpisodeCatalog.cs` | 5 episode titles + rank gates |
| `Hub/BondsMenuUI.cs` | Sandbox controller: bind, input, selection |
| `Hub/BondRosterChipView.cs` | One chip |
| `Hub/BondDetailCardView.cs` | Detail card bind |
| `Hub/BondEpisodeRowView.cs` | One episode row |
| `Editor/BondPresentationTests.cs` | NUnit copy + unlock |
| `Editor/BondsArtImportTests.cs` | PNG exist as Sprite |
| `Editor/BondsArtImportEditor.cs` | Sprite import + 9-slice borders |
| `Editor/BondsSceneSetupEditor.cs` | Create / Heal / Attach / MockGuide / Snapshot |
| `Scenes/BondsLayoutSandbox.unity` | Layout SoT — **not** in Build Settings |
| `Art/UI/Bonds/_ref/_ref_bonds_menu_v1.jpg` | Composition mock |
| `Art/UI/Bonds/Kit/*.png` | Glass chrome 9-slice |
| `Art/UI/Bonds/Icons/*.png` | Line icons |
| `Art/UI/Bonds/Decor/*.png` | Promo piano + locked silhouette |
| `Art/UI/Bonds/README.md` | Slot notes |
| `Tools/save-bonds-sandbox-layout-snapshot.mjs` | JSON backup of Rects |
| Modify: `Hub/CampusBgmPlayer.cs` | Treat sandbox as campus music scene |
| Modify: `UI/UiFontRules.cs` | Display role for Bonds titles |

Reuse (do not duplicate):

| Asset / type | Path |
|--------------|------|
| City BG | `Art/UI/StatusMenu/statusmenu_hima_city_bg_v1.jpg` |
| Ren bust | `Art/Characters/Ren/VnBust/ren_bust_neutral_v1.png` |
| Charlotte bust | `Art/Characters/Charlotte/VnBust/charlotte_bust_neutral_v1.png` |
| Coda bust | `Art/Characters/Coda/VnBust/coda_bust_neutral_v1.png` |
| Astra (no VN bust) | `Art/Characters/_Reference/LuxeConcert/astra_ref.png` |
| Radar graphic | `Hub/SocialStatsRadarGraphic.cs` |
| Stat node view | `Hub/SocialStatsNodeView.cs` |
| Stat copy | `Hub/SocialStatPresentation.cs` |
| Corner HUD | `Hub/HubCornerInfoHud.cs` |
| Sun icon | `Art/UI/TownMap/townmap_icon_sun.png` |

---

### Task 1: Presentation + episode catalog (TDD)

**Files:**
- Create: `Assets/FracturedChorus/Hub/BondPresentation.cs`
- Create: `Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs`
- Create: `Assets/FracturedChorus/Editor/BondPresentationTests.cs`

**Interfaces:**
- Produces:
  - `BondPresentation.RosterOrder` → `string[6]` = `ren, charlotte, coda, astra, ryo, mei_lin`
  - `BondPresentation.VisibleChipCount` → `7`
  - `BondPresentation.GetDisplayName(string npcId)` → `string`
  - `BondPresentation.GetRoleLabel(string npcId)` → `"Player"` for `ren`, else `""`
  - `BondPresentation.GetBio(string npcId)` / `GetQuote(string npcId)` → `string`
  - `BondPresentation.IsPortraitUnlocked(string npcId)` → `true` for ren/charlotte/coda/astra
  - `BondLinkEpisodeCatalog.Episodes` → `BondLinkEpisode[5]`
  - `BondLinkEpisodeCatalog.IsUnlocked(int bondRank, int requiredRank)` → `bool`

- [ ] **Step 1: Write the failing tests**

```csharp
using FracturedChorus.Hub;
using FracturedChorus.Meta;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class BondPresentationTests
    {
        [Test]
        public void RosterOrder_MatchesHubBondList()
        {
            CollectionAssert.AreEqual(
                new[]
                {
                    BondNpcIds.Ren,
                    BondNpcIds.Charlotte,
                    BondNpcIds.Coda,
                    BondNpcIds.Astra,
                    BondNpcIds.Ryo,
                    BondNpcIds.MeiLin
                },
                BondPresentation.RosterOrder);
        }

        [Test]
        public void VisibleChipCount_IsSixNpcsPlusReserved()
        {
            Assert.AreEqual(7, BondPresentation.VisibleChipCount);
            Assert.AreEqual(6, BondPresentation.RosterOrder.Length);
        }

        [Test]
        public void DisplayNames_MatchLock()
        {
            Assert.AreEqual("Ren", BondPresentation.GetDisplayName(BondNpcIds.Ren));
            Assert.AreEqual("Charlotte", BondPresentation.GetDisplayName(BondNpcIds.Charlotte));
            Assert.AreEqual("Coda", BondPresentation.GetDisplayName(BondNpcIds.Coda));
            Assert.AreEqual("Astra", BondPresentation.GetDisplayName(BondNpcIds.Astra));
            Assert.AreEqual("Ryo", BondPresentation.GetDisplayName(BondNpcIds.Ryo));
            Assert.AreEqual("Mei Lin", BondPresentation.GetDisplayName(BondNpcIds.MeiLin));
            Assert.AreEqual("???", BondPresentation.GetDisplayName("reserved"));
        }

        [Test]
        public void RoleLabel_OnlyRenIsPlayer()
        {
            Assert.AreEqual("Player", BondPresentation.GetRoleLabel(BondNpcIds.Ren));
            Assert.AreEqual(string.Empty, BondPresentation.GetRoleLabel(BondNpcIds.Charlotte));
        }

        [Test]
        public void PortraitUnlock_Arc1VisibleFour()
        {
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Ren));
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Charlotte));
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Coda));
            Assert.IsTrue(BondPresentation.IsPortraitUnlocked(BondNpcIds.Astra));
            Assert.IsFalse(BondPresentation.IsPortraitUnlocked(BondNpcIds.Ryo));
            Assert.IsFalse(BondPresentation.IsPortraitUnlocked(BondNpcIds.MeiLin));
            Assert.IsFalse(BondPresentation.IsPortraitUnlocked(null));
        }

        [Test]
        public void CharlotteCopy_MatchesMock()
        {
            StringAssert.Contains("quiet yet passionate", BondPresentation.GetBio(BondNpcIds.Charlotte));
            StringAssert.Contains("little kinder", BondPresentation.GetQuote(BondNpcIds.Charlotte));
        }

        [Test]
        public void LinkEpisodes_FiveTitles_RankGates()
        {
            Assert.AreEqual(5, BondLinkEpisodeCatalog.Episodes.Length);
            Assert.AreEqual("A Usual Day", BondLinkEpisodeCatalog.Episodes[0].Title);
            Assert.AreEqual(1, BondLinkEpisodeCatalog.Episodes[0].RequiredRank);
            Assert.AreEqual("After Class", BondLinkEpisodeCatalog.Episodes[1].Title);
            Assert.AreEqual(2, BondLinkEpisodeCatalog.Episodes[1].RequiredRank);
            Assert.AreEqual("A Different Melody", BondLinkEpisodeCatalog.Episodes[2].Title);
            Assert.AreEqual("Unspoken Words", BondLinkEpisodeCatalog.Episodes[3].Title);
            Assert.AreEqual("Toward Tomorrow", BondLinkEpisodeCatalog.Episodes[4].Title);
            Assert.AreEqual(5, BondLinkEpisodeCatalog.Episodes[4].RequiredRank);
        }

        [Test]
        public void EpisodeUnlock_UsesBondRank()
        {
            Assert.IsTrue(BondLinkEpisodeCatalog.IsUnlocked(1, 1));
            Assert.IsFalse(BondLinkEpisodeCatalog.IsUnlocked(1, 2));
            Assert.IsTrue(BondLinkEpisodeCatalog.IsUnlocked(5, 5));
            Assert.IsFalse(BondLinkEpisodeCatalog.IsUnlocked(0, 1));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Unity → Window → General → Test Runner → **EditMode** → `BondPresentationTests`.

Expected: FAIL — `BondPresentation` / `BondLinkEpisodeCatalog` type not found.

- [ ] **Step 3: Write minimal implementation**

`Assets/FracturedChorus/Hub/BondPresentation.cs`:

```csharp
using FracturedChorus.Meta;

namespace FracturedChorus.Hub
{
    public static class BondPresentation
    {
        public const string Title = "BONDS";
        public const string TitleJp = "絆";
        public const string SocialStatsTitle = "SOCIAL STATS";
        public const string SocialStatsJp = "共鳴ステータス";
        public const string LinkEpisodesTitle = "LINK EPISODES";
        public const string LinkEpisodesJp = "リンクエピソード";
        public const string Location = "HIMA CITY";
        public const string LocationTagline = "Music Lives in You";
        public const string TaglineRight = "PEOPLE MAKE MUSIC.";
        public const string Wordmark = "FRACTURE CHORUS";
        public const string WordmarkSub = "MUSIC PEOPLE CONNECT THE WORLD";
        public const string NextRankLabel = "NEXT RANK";
        public const string NextRankHint = "A small step,\na closer heart.";
        public const string EpisodeLockHint = "Reach higher rank to unlock new episodes.";
        public const string PromoCaption = "Music People Connect The World";
        public const int VisibleChipCount = 7;

        public static readonly string[] RosterOrder =
        {
            BondNpcIds.Ren,
            BondNpcIds.Charlotte,
            BondNpcIds.Coda,
            BondNpcIds.Astra,
            BondNpcIds.Ryo,
            BondNpcIds.MeiLin
        };

        public static string GetDisplayName(string npcId) => npcId switch
        {
            BondNpcIds.Ren => "Ren",
            BondNpcIds.Charlotte => "Charlotte",
            BondNpcIds.Coda => "Coda",
            BondNpcIds.Astra => "Astra",
            BondNpcIds.Ryo => "Ryo",
            BondNpcIds.MeiLin => "Mei Lin",
            _ => "???"
        };

        public static string GetRoleLabel(string npcId) =>
            npcId == BondNpcIds.Ren ? "Player" : string.Empty;

        public static bool IsPortraitUnlocked(string npcId) =>
            npcId == BondNpcIds.Ren
            || npcId == BondNpcIds.Charlotte
            || npcId == BondNpcIds.Coda
            || npcId == BondNpcIds.Astra;

        public static string GetBio(string npcId) => npcId switch
        {
            BondNpcIds.Charlotte =>
                "A quiet yet passionate girl who always stays close to music. Her melodies feel like sunlight—gentle, but strong enough to change someone's day.",
            BondNpcIds.Ren =>
                "A HIMA newcomer still learning the city's rhythm. He listens first, then steps in when the chorus starts to fracture.",
            BondNpcIds.Coda =>
                "A Harmony mage who treats every silence like a measure rest—soft-spoken, precise, and stubborn about keeping people together.",
            BondNpcIds.Astra =>
                "Campus guide by day, LUXE pulse on stage. She makes a room feel like a spotlight even when she is only pointing you down a hallway.",
            BondNpcIds.Ryo =>
                "Locked. Story progress required.",
            BondNpcIds.MeiLin =>
                "Locked. Story progress required.",
            _ => "An echo waiting to be named."
        };

        public static string GetQuote(string npcId) => npcId switch
        {
            BondNpcIds.Charlotte => "Maybe... music can make the world a little kinder, right?",
            BondNpcIds.Ren => "If the city is out of tune, someone has to count the beats.",
            BondNpcIds.Coda => "Hold the note. The rest of us will find you.",
            BondNpcIds.Astra => "Keep your chin up. The chorus sounds better when you look at it.",
            _ => string.Empty
        };
    }
}
```

`Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs`:

```csharp
namespace FracturedChorus.Hub
{
    public readonly struct BondLinkEpisode
    {
        public BondLinkEpisode(int index, string title, int requiredRank)
        {
            Index = index;
            Title = title;
            RequiredRank = requiredRank;
        }

        public int Index { get; }
        public string Title { get; }
        public int RequiredRank { get; }
    }

    public static class BondLinkEpisodeCatalog
    {
        public static readonly BondLinkEpisode[] Episodes =
        {
            new BondLinkEpisode(1, "A Usual Day", 1),
            new BondLinkEpisode(2, "After Class", 2),
            new BondLinkEpisode(3, "A Different Melody", 3),
            new BondLinkEpisode(4, "Unspoken Words", 4),
            new BondLinkEpisode(5, "Toward Tomorrow", 5)
        };

        public static bool IsUnlocked(int bondRank, int requiredRank) =>
            bondRank >= requiredRank && requiredRank >= 1;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Test Runner → EditMode → `BondPresentationTests`. Expected: **PASS** (8 tests).

- [ ] **Step 5: Commit**

```bash
git add Assets/FracturedChorus/Hub/BondPresentation.cs Assets/FracturedChorus/Hub/BondPresentation.cs.meta Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs Assets/FracturedChorus/Hub/BondLinkEpisodeCatalog.cs.meta Assets/FracturedChorus/Editor/BondPresentationTests.cs Assets/FracturedChorus/Editor/BondPresentationTests.cs.meta
git commit -m "$(cat <<'EOF'
Add Bonds presentation constants for sandbox HUD copy.

EOF
)"
```

---

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

### Task 3: Sandbox scene seed (layout on create only)

**Files:**
- Create: `Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs`
- Create: `Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity`
- Create: `.cursor/rules/bonds-ui-scene-layout.mdc`
- Modify: `Assets/FracturedChorus/Hub/CampusBgmPlayer.cs` — add sandbox scene name
- Modify: `Assets/FracturedChorus/UI/UiFontRules.cs` — display names for Bonds titles

**Interfaces:**
- Consumes: art paths from Task 2; `BondPresentation` constants from Task 1
- Produces: scene with `BondsCanvas` + named children listed below; menu items Create / Heal / Attach Missing / Toggle Mock Guide
- `CampusBgmPlayer.BondsSandboxScene = "BondsLayoutSandbox"`
- `CampusBgmPlayer.IsCampusMusicScene` returns true for that name

Hierarchy to seed (names are bind contracts — do not rename later):

```
Main Camera
EventSystem
BondsCanvas
  Background
  CornerHud / DateLabel, DayLabel, PhaseIcon, LocationLabel, TaglineLabel
  HeaderBonds / Icon, Label, LabelJp
  LeftNav / Row_SocialStats, Row_Link, Row_Conversations, Row_Memories, Row_Gallery
    (each Row: Icon, Label)
  CenterStats / Title, TitleJp
    ChartRoot / Radar
    Node_Resonance, Node_Cadence, Node_Pulse, Node_Harmony, Node_Rhythm
  Roster / Chip_0 … Chip_6 / Chevron
    (each Chip: Frame, Face, Lock, Name, Role)
  DetailCard / Portrait, Name, Rank, Bio, Quote, ExpTrack, ExpFill, ExpLabel, NextRank, NextHint
  LinkEpisodes / Title, TitleJp, Hint, PromoFrame, PromoImage, PromoCaption
    Row_01 … Row_05 / Icon, Index, Label
  Footer / ConfirmLabel, BackLabel
  Wordmark / Title, Sub
  TaglineRight
  Compass
  MockGuide
```

- [ ] **Step 1: Add cursor rule** `.cursor/rules/bonds-ui-scene-layout.mdc`

```markdown
---
description: Bonds sandbox layout lives in the scene; never re-hardcode RectTransforms.
globs:
  - Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs
  - Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity
  - Assets/FracturedChorus/Hub/BondsMenuUI.cs
  - Assets/FracturedChorus/Hub/BondRosterChipView.cs
  - Assets/FracturedChorus/Hub/BondDetailCardView.cs
  - Assets/FracturedChorus/Hub/BondEpisodeRowView.cs
alwaysApply: false
---

# Bonds sandbox — scene layout is source of truth

Pos / size / anchor and Hierarchy sibling order of `BondsCanvas` live only on `BondsLayoutSandbox.unity`.

1. Do not hard-code `anchoredPosition` / `sizeDelta` / Stretch in apply when the object already exists.
2. Do not `SetSiblingIndex` on existing objects.
3. Seed Rect only when creating a new GameObject (`created == true`).
4. User tweaks Hierarchy then Ctrl+S.
5. Do not OpenScene over an unsaved CampusHub or Bonds sandbox just to attach missing objects.
6. Heal / Rebuild requires confirm and is the only reset.
7. Save layout snapshot is backup only — never apply JSON back onto Rects.
8. Do not add this sandbox to Build Settings. Do not copy into CampusHub in this epic.
```

- [ ] **Step 2: Patch `CampusBgmPlayer`**

In `IsCampusMusicScene`, add `|| sceneName == BondsSandboxScene` and:

```csharp
public const string BondsSandboxScene = "BondsLayoutSandbox";
```

- [ ] **Step 3: Patch `UiFontRules.IsDisplay`**

Add name matches: `LabelJp` under `HeaderBonds` / `CenterStats` / `LinkEpisodes` already goes Display if named `Title`. Add:

```csharp
|| name == "LabelJp"
|| IsUnder(transform, "HeaderBonds")
|| IsUnder(transform, "CenterStats")
|| IsUnder(transform, "DetailCard")
```

Keep Body for Bio / Quote / Hint / episode Label.

- [ ] **Step 4: Write `BondsSceneSetupEditor`**

Use the same EventSystem / Camera / CanvasScaler pattern as `CharacterBuildSceneSetupEditor.CreateSandboxCanvas`.

Helpers (must exist in this file — do not “copy from CharacterBuild” by reference):

```csharp
private const string ScenePath = "Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity";

[MenuItem("Fractured Chorus/Bonds/Create Layout Sandbox Scene")]
public static void CreateScene() { /* NewScene + BuildHierarchy + SaveScene(ScenePath) */ }

[MenuItem("Fractured Chorus/Bonds/Heal Layout Sandbox Hierarchy")]
public static void HealScene()
{
    if (!EditorUtility.DisplayDialog(
            "Heal Bonds Sandbox",
            "Destroys BondsCanvas and rebuilds. Manual layout is lost.\nSave snapshot first if needed.",
            "Rebuild",
            "Cancel"))
    {
        return;
    }
    /* OpenScene ScenePath, Destroy BondsCanvas / camera / EventSystem, BuildHierarchy, Save */
}

[MenuItem("Fractured Chorus/Bonds/Attach Missing Layout Objects")]
public static void AttachMissing()
{
    /* Ensure* only. SeedRect if created. Never OpenScene if active scene is already the sandbox. */
}

[MenuItem("Fractured Chorus/Bonds/Toggle Mock Guide")]
public static void ToggleMockGuide()
{
    var guide = GameObject.Find("BondsCanvas/MockGuide");
    if (guide == null) return;
    guide.SetActive(!guide.activeSelf);
}

private static RectTransform Ensure(Transform parent, string name, out bool created)
{
    var existing = parent.Find(name);
    if (existing != null)
    {
        created = false;
        var rt = existing as RectTransform ?? existing.GetComponent<RectTransform>();
        return rt;
    }

    created = true;
    var go = new GameObject(name, typeof(RectTransform));
    go.transform.SetParent(parent, false);
    return go.GetComponent<RectTransform>();
}

Do **not** add `SeedRect` with pixel tables. Do **not** write `anchoredPosition` / `sizeDelta` / corner anchors in C#.
```

`BuildHierarchy` (Create + Heal only) — **objects on scene, layout in Hierarchy**:

1. `EnsureCamera` solid color `#030914`.
2. `EnsureEventSystem` via `CombatInputSetup.ApplyInputModule`.
3. Create `BondsCanvas` Overlay, scaler 1920×1080, match 0.5. Canvas scaler is engine setup, not widget layout.
4. Add `BondsMenuUI` on the canvas. Create a compile stub in this task so the scene can serialize the component:

```csharp
using UnityEngine;

namespace FracturedChorus.Hub
{
    public sealed class BondsMenuUI : MonoBehaviour
    {
        public static int WrapRosterIndex(int index, int delta, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            var wrapped = (index + delta) % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }
    }
}
```

Task 4 replaces this stub in the same file (do not create a second class).
5. `Ensure` every named child in the hierarchy list. New objects keep Unity default RectTransform. The only allowed stretch in C# is Background + MockGuide (`anchorMin=0,0` `anchorMax=1,1` `offset=0`) because they are full-canvas layers, not authored widgets.
6. Assign sprites on **create only**: Background = hima city jpg; MockGuide = `_ref_bonds_menu_v1.jpg` color white alpha 0.4, raycast off; Kit/Icons as matching names; Radar = `SocialStatsRadarGraphic` on `ChartRoot/Radar`.
7. `HubCornerInfoHud` on `CornerHud`; LocationLabel text `BondPresentation.Location`; TaglineLabel `LocationTagline`; PhaseIcon = `townmap_icon_sun`.
8. `SocialStatsNodeView` on each `Node_*`. Never set node Rects in C#.
9. Chip_0..3 Face sprites = Ren/Charlotte/Coda busts + Astra ref. Chip_4..6 Face = `ui_bonds_silhouette_locked_v1`.
10. LeftNav rows: only `Row_SocialStats` Button.interactable = true.
11. After Create: user (or this session) positions widgets in Hierarchy against MockGuide, then Ctrl+S. Those numbers live only in `BondsLayoutSandbox.unity`.

- [ ] **Step 5: Create scene**

Menu **Fractured Chorus → Bonds → Create Layout Sandbox Scene**. Confirm `BondsLayoutSandbox.unity` exists and is **absent** from `ProjectSettings/EditorBuildSettings.asset`.

- [ ] **Step 6: Manual layout pass (not code)**

Open sandbox, Game view 16:9, Toggle Mock Guide, nudge Rects to match mock, Ctrl+S. Do not put those numbers back into C#.

- [ ] **Step 7: Commit**

```bash
git add Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity.meta .cursor/rules/bonds-ui-scene-layout.mdc Assets/FracturedChorus/Hub/CampusBgmPlayer.cs Assets/FracturedChorus/Hub/BondsMenuUI.cs Assets/FracturedChorus/UI/UiFontRules.cs
git commit -m "$(cat <<'EOF'
Seed Bonds layout sandbox without touching CampusHub.

EOF
)"
```

---

### Task 4: View components + `BondsMenuUI` bind

**Files:**
- Create: `Assets/FracturedChorus/Hub/BondRosterChipView.cs`
- Create: `Assets/FracturedChorus/Hub/BondDetailCardView.cs`
- Create: `Assets/FracturedChorus/Hub/BondEpisodeRowView.cs`
- Modify: `Assets/FracturedChorus/Hub/BondsMenuUI.cs` (replace Task 3 stub in the same file)
- Modify: `Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs` — `AttachMissing` wires SerializeField refs by **name**, never Rects
- Modify: `Assets/FracturedChorus/Editor/BondPresentationTests.cs` — add selection wrap test against a static helper on `BondsMenuUI`

**Interfaces:**
- Consumes: `BondPresentation.*`, `BondLinkEpisodeCatalog.*`, `SocialStatPresentation`, `SocialStatsRadarGraphic.SetRanks(IReadOnlyList<int>)`, `SocialStatsNodeView.Bind(SocialStatType, int, Sprite)`, `GameMetaState`
- Produces:
  - `BondsMenuUI.Show(GameMetaState state)`
  - `BondsMenuUI.Hide()`
  - `BondsMenuUI.SelectedNpcId` → `string`
  - `BondsMenuUI.WrapRosterIndex(int index, int delta, int count)` → `int` (pure, testable)
  - `BondRosterChipView.Bind(string npcId, bool selected, Sprite face, Sprite frame, Sprite lockIcon)`
  - `BondDetailCardView.Bind(GameMetaState state, string npcId, Sprite portrait)`
  - `BondEpisodeRowView.Bind(BondLinkEpisode episode, bool unlocked, bool selected)`

- [ ] **Step 1: Failing wrap test** (append to `BondPresentationTests`)

```csharp
[Test]
public void WrapRosterIndex_Cycles()
{
    Assert.AreEqual(1, BondsMenuUI.WrapRosterIndex(0, 1, 7));
    Assert.AreEqual(0, BondsMenuUI.WrapRosterIndex(6, 1, 7));
    Assert.AreEqual(6, BondsMenuUI.WrapRosterIndex(0, -1, 7));
}
```

Run: FAIL until `WrapRosterIndex` exists.

- [ ] **Step 2: Views**

`BondRosterChipView.cs`:

```csharp
using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondRosterChipView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image face;
        [SerializeField] private Image lockIcon;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text roleLabel;
        [SerializeField] private Button button;

        public string NpcId { get; private set; }
        public Button Button => button;

        public void Bind(string npcId, bool selected, Sprite faceSprite, Sprite frameSprite, Sprite lockSprite)
        {
            NpcId = npcId;
            var unlocked = BondPresentation.IsPortraitUnlocked(npcId);
            if (nameLabel != null)
            {
                nameLabel.text = unlocked ? BondPresentation.GetDisplayName(npcId) : string.Empty;
            }

            if (roleLabel != null)
            {
                roleLabel.text = BondPresentation.GetRoleLabel(npcId);
            }

            if (face != null)
            {
                face.sprite = faceSprite;
                face.enabled = faceSprite != null;
                face.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                face.preserveAspect = true;
            }

            if (frame != null)
            {
                frame.sprite = frameSprite;
                frame.enabled = frameSprite != null;
            }

            if (lockIcon != null)
            {
                lockIcon.sprite = lockSprite;
                lockIcon.enabled = !unlocked && lockSprite != null;
            }

            if (button != null)
            {
                button.interactable = unlocked;
            }
        }
    }
}
```

`BondDetailCardView.cs`:

```csharp
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondDetailCardView : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text rankLabel;
        [SerializeField] private Text bioLabel;
        [SerializeField] private Text quoteLabel;
        [SerializeField] private Image expFill;
        [SerializeField] private Text expLabel;
        [SerializeField] private Text nextRankLabel;
        [SerializeField] private Text nextHintLabel;

        public void Bind(GameMetaState state, string npcId, Sprite portraitSprite)
        {
            var unlocked = BondPresentation.IsPortraitUnlocked(npcId);
            var bond = state != null ? state.GetBond(npcId) : null;
            if (portrait != null)
            {
                portrait.sprite = portraitSprite;
                portrait.enabled = portraitSprite != null;
                portrait.preserveAspect = true;
            }

            if (nameLabel != null)
            {
                nameLabel.text = BondPresentation.GetDisplayName(npcId);
            }

            if (rankLabel != null)
            {
                rankLabel.text = bond != null && unlocked ? $"Rank {bond.Rank}" : "Locked";
            }

            if (bioLabel != null)
            {
                bioLabel.text = BondPresentation.GetBio(npcId);
            }

            if (quoteLabel != null)
            {
                var quote = BondPresentation.GetQuote(npcId);
                quoteLabel.text = string.IsNullOrEmpty(quote) ? string.Empty : $"\"{quote}\"";
            }

            var threshold = bond != null ? bond.GetThresholdForRank(bond.Rank) : 0;
            var exp = bond != null ? bond.Exp : 0;
            if (expLabel != null)
            {
                expLabel.text = unlocked ? $"{exp} / {threshold}" : "—";
            }

            if (expFill != null)
            {
                expFill.type = Image.Type.Filled;
                expFill.fillMethod = Image.FillMethod.Horizontal;
                expFill.fillAmount = unlocked && threshold > 0 ? Mathf.Clamp01(exp / (float)threshold) : 0f;
            }

            if (nextRankLabel != null)
            {
                nextRankLabel.text = BondPresentation.NextRankLabel;
            }

            if (nextHintLabel != null)
            {
                nextHintLabel.text = BondPresentation.NextRankHint;
            }
        }
    }
}
```

`BondEpisodeRowView.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondEpisodeRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text indexLabel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Button button;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite lockSprite;

        public int EpisodeIndex { get; private set; }
        public bool Unlocked { get; private set; }
        public Button Button => button;

        public void Bind(BondLinkEpisode episode, bool unlocked, bool selected)
        {
            EpisodeIndex = episode.Index;
            Unlocked = unlocked;
            if (indexLabel != null)
            {
                indexLabel.text = episode.Index.ToString("00");
            }

            if (titleLabel != null)
            {
                titleLabel.text = episode.Title;
                titleLabel.color = unlocked
                    ? FracturedChorus.UI.FcColorTokens.Brand.TextPrimary
                    : FracturedChorus.UI.FcColorTokens.Brand.TextMuted;
            }

            if (icon != null)
            {
                icon.sprite = unlocked ? playSprite : lockSprite;
                icon.enabled = icon.sprite != null;
            }

            if (button != null)
            {
                button.interactable = unlocked;
            }
        }
    }
}
```

`BondEpisodeRowView` uses `FracturedChorus.UI.FcColorTokens` fully qualified — no extra using required.

- [ ] **Step 3: `BondsMenuUI`**

```csharp
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class BondsMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private HubCornerInfoHud cornerHud;
        [SerializeField] private SocialStatsRadarGraphic radar;
        [SerializeField] private SocialStatsNodeView[] nodes = new SocialStatsNodeView[5];
        [SerializeField] private Sprite[] statIcons = new Sprite[5];
        [SerializeField] private BondRosterChipView[] chips = new BondRosterChipView[7];
        [SerializeField] private Sprite chipFrameNormal;
        [SerializeField] private Sprite chipFrameSelected;
        [SerializeField] private Sprite chipFrameLocked;
        [SerializeField] private Sprite lockIcon;
        [SerializeField] private Sprite[] portraitSprites = new Sprite[6];
        [SerializeField] private Sprite reservedPortrait;
        [SerializeField] private BondDetailCardView detail;
        [SerializeField] private BondEpisodeRowView[] episodeRows = new BondEpisodeRowView[5];
        [SerializeField] private Text socialStatsTitle;
        [SerializeField] private Text socialStatsJp;
        [SerializeField] private Text linkTitle;
        [SerializeField] private Text linkJp;
        [SerializeField] private Text episodeHint;
        [SerializeField] private Text headerLabel;
        [SerializeField] private Text headerJp;
        [SerializeField] private Text confirmLabel;
        [SerializeField] private Text backLabel;

        private GameMetaState _state;
        private int _rosterIndex = 1;
        private int _episodeIndex;

        public string SelectedNpcId => NpcIdAt(_rosterIndex);

        public static int WrapRosterIndex(int index, int delta, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            var wrapped = (index + delta) % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }
        }

        private void Start()
        {
            if (_state == null)
            {
                Show(CreateSandboxState());
            }
        }

        public void Show(GameMetaState state)
        {
            _state = state ?? CreateSandboxState();
            UiFontCatalog.ApplyHierarchy(transform);
            if (cornerHud != null)
            {
                cornerHud.Refresh(_state);
            }

            ApplyStaticCopy();
            Refresh();
        }

        public void Hide()
        {
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (TownMapInput.MonthNextPressed() || (keyboard != null && keyboard.rightArrowKey.wasPressedThisFrame))
            {
                MoveRoster(1);
            }
            else if (TownMapInput.MonthPrevPressed() || (keyboard != null && keyboard.leftArrowKey.wasPressedThisFrame))
            {
                MoveRoster(-1);
            }

            if (keyboard != null && keyboard.upArrowKey.wasPressedThisFrame)
            {
                MoveEpisode(-1);
            }
            else if (keyboard != null && keyboard.downArrowKey.wasPressedThisFrame)
            {
                MoveEpisode(1);
            }

            if (TownMapInput.ConfirmPressed())
            {
                ConfirmEpisode();
            }
        }

        private void MoveRoster(int delta)
        {
            for (var i = 0; i < BondPresentation.VisibleChipCount; i++)
            {
                _rosterIndex = WrapRosterIndex(_rosterIndex, delta, BondPresentation.VisibleChipCount);
                if (BondPresentation.IsPortraitUnlocked(SelectedNpcId))
                {
                    Refresh();
                    return;
                }
            }
        }

        private void MoveEpisode(int delta)
        {
            var count = BondLinkEpisodeCatalog.Episodes.Length;
            _episodeIndex = WrapRosterIndex(_episodeIndex, delta, count);
            Refresh();
        }

        private void ConfirmEpisode()
        {
            var episode = BondLinkEpisodeCatalog.Episodes[_episodeIndex];
            var bond = _state.GetBond(SelectedNpcId);
            if (!BondLinkEpisodeCatalog.IsUnlocked(bond.Rank, episode.RequiredRank))
            {
                return;
            }

            Debug.Log($"[Bonds sandbox] play {SelectedNpcId} episode {episode.Index} {episode.Title}");
        }

        private void ApplyStaticCopy()
        {
            SetText(headerLabel, BondPresentation.Title);
            SetText(headerJp, BondPresentation.TitleJp);
            SetText(socialStatsTitle, BondPresentation.SocialStatsTitle);
            SetText(socialStatsJp, BondPresentation.SocialStatsJp);
            SetText(linkTitle, BondPresentation.LinkEpisodesTitle);
            SetText(linkJp, BondPresentation.LinkEpisodesJp);
            SetText(episodeHint, BondPresentation.EpisodeLockHint);
            SetText(confirmLabel, "Confirm");
            SetText(backLabel, "Back");
        }

        private void Refresh()
        {
            if (_state == null)
            {
                return;
            }

            var ranks = new int[SocialStatPresentation.OrderedStats.Length];
            for (var i = 0; i < ranks.Length; i++)
            {
                ranks[i] = _state.SocialStats.GetRank(SocialStatPresentation.OrderedStats[i]);
            }

            radar?.SetRanks(ranks);
            for (var i = 0; i < nodes.Length && i < SocialStatPresentation.OrderedStats.Length; i++)
            {
                var stat = SocialStatPresentation.OrderedStats[i];
                var icon = i < statIcons.Length ? statIcons[i] : null;
                nodes[i]?.Bind(stat, _state.SocialStats.GetRank(stat), icon);
            }

            for (var i = 0; i < chips.Length; i++)
            {
                var npcId = NpcIdAt(i);
                var unlocked = BondPresentation.IsPortraitUnlocked(npcId);
                var frame = !unlocked
                    ? chipFrameLocked
                    : i == _rosterIndex
                        ? chipFrameSelected
                        : chipFrameNormal;
                chips[i]?.Bind(npcId, i == _rosterIndex, PortraitAt(i), frame, lockIcon);
            }

            detail?.Bind(_state, SelectedNpcId, PortraitAt(_rosterIndex));

            var selectedBond = _state.GetBond(SelectedNpcId);
            for (var i = 0; i < episodeRows.Length && i < BondLinkEpisodeCatalog.Episodes.Length; i++)
            {
                var episode = BondLinkEpisodeCatalog.Episodes[i];
                var unlocked = BondLinkEpisodeCatalog.IsUnlocked(selectedBond.Rank, episode.RequiredRank);
                episodeRows[i]?.Bind(episode, unlocked, i == _episodeIndex);
            }
        }

        private string NpcIdAt(int index)
        {
            if (index < 0 || index >= BondPresentation.RosterOrder.Length)
            {
                return string.Empty;
            }

            return BondPresentation.RosterOrder[index];
        }

        private Sprite PortraitAt(int index)
        {
            if (index < 0 || index >= BondPresentation.RosterOrder.Length)
            {
                return reservedPortrait;
            }

            return index < portraitSprites.Length ? portraitSprites[index] : reservedPortrait;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        public static GameMetaState CreateSandboxState()
        {
            var state = GameMetaState.CreateHubStart();
            state.GetBond(BondNpcIds.Ryo).IsLocked = true;
            state.GetBond(BondNpcIds.MeiLin).IsLocked = true;
            return state;
        }
    }
}
```

Q/E already maps through `TownMapInput.MonthPrevPressed` / `MonthNextPressed`. Reusing them for roster in **sandbox only** is acceptable. Left/Right arrows are extra.

Do **not** call `Hide` on Esc in this plan.

Default `_rosterIndex = 1` (Charlotte). Skip locked chips in `MoveRoster`.

- [ ] **Step 4: Wire refs by name in AttachMissing** (`SerializedObject` find `DateLabel` etc.). Never write Rects.

- [ ] **Step 5: Run EditMode tests** — `BondPresentationTests` including wrap: PASS.

- [ ] **Step 6: Play Mode** on `BondsLayoutSandbox` (not CampusHub).

Expected:
- Charlotte selected, Rank 1, EXP `0 / 10`
- Radar pentagon at rank 1
- Episode 01 playable, 02–05 locked
- Left/Right skips Ryo/MeiLin/reserved
- Enter logs episode 01
- Esc does not unload the scene
- MockGuide off

- [ ] **Step 7: Commit**

```bash
git add Assets/FracturedChorus/Hub/BondsMenuUI.cs Assets/FracturedChorus/Hub/BondRosterChipView.cs Assets/FracturedChorus/Hub/BondDetailCardView.cs Assets/FracturedChorus/Hub/BondEpisodeRowView.cs Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs Assets/FracturedChorus/Editor/BondPresentationTests.cs Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity
git commit -m "$(cat <<'EOF'
Bind Bonds sandbox HUD to social stats and bond ranks.

EOF
)"
```

---

### Task 5: Layout snapshot tool + acceptance

**Files:**
- Create: `Tools/save-bonds-sandbox-layout-snapshot.mjs`
- Create: `Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_layout_snapshot.json` (generated)
- Modify: `Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs` — menu **Save Layout Snapshot** shells the node tool (copy `CampusHubSceneSetupEditor` `RunNodeTool` pattern, scene filter `BondsLayoutSandbox.unity`)

**Interfaces:**
- Produces: JSON dump of every `BondsCanvas` child path + anchor/pos/size + spritePath. **Never** applied back onto Rects.

- [ ] **Step 1: Snapshot script**

Clone `Tools/save-campushub-status-menu-layout-snapshot.mjs`. Change:

```javascript
const SCENE = `${ROOT}/Assets/FracturedChorus/Scenes/BondsLayoutSandbox.unity`;
const OUT = `${ROOT}/Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_layout_snapshot.json`;
```

Root GameObject name to walk: `BondsCanvas`.

- [ ] **Step 2: Menu item**

```csharp
[MenuItem("Fractured Chorus/Bonds/Save Layout Snapshot")]
public static void SaveLayoutSnapshot()
{
    var scenePath = EditorSceneManager.GetActiveScene().path;
    if (!scenePath.EndsWith("BondsLayoutSandbox.unity"))
    {
        EditorUtility.DisplayDialog("Save Bonds Layout", "Focus BondsLayoutSandbox, Ctrl+S, run again.", "OK");
        return;
    }

    if (EditorSceneManager.GetActiveScene().isDirty)
    {
        EditorSceneManager.SaveOpenScenes();
    }

    RunNodeTool("Tools/save-bonds-sandbox-layout-snapshot.mjs");
}
```

Implement `RunNodeTool` identically to `CampusHubSceneSetupEditor` (Process `node`, capture exit code).

- [ ] **Step 3: Run snapshot after Ctrl+S.** JSON exists. Confirm it is not read by runtime C#.

- [ ] **Step 4: Acceptance checklist (Play Mode, 1920×1080)**

- [ ] Safe-zone: header, nav, footer inside ~90%
- [ ] Social Stats title + JP visible
- [ ] 5 nodes named Resonance/Cadence/Pulse/Harmony/Rhythm with flavors from `SocialStatPresentation`
- [ ] Pulse node sits at top of radar
- [ ] 4 portraits + 3 locks; default Charlotte
- [ ] Detail bio/quote from `BondPresentation`
- [ ] Link 01 unlocked, 02–05 locked + hint copy
- [ ] Promo piano has **no** baked caption; caption is uGUI `PromoCaption`
- [ ] Left nav: only Social Stats looks selected; other rows grey
- [ ] Fonts via `UiFontCatalog.ApplyHierarchy`
- [ ] `EditorBuildSettings` still has no sandbox scene
- [ ] `git diff -- Assets/FracturedChorus/Scenes/CampusHub.unity` is empty for this epic

- [ ] **Step 5: Commit**

```bash
git add Tools/save-bonds-sandbox-layout-snapshot.mjs Assets/FracturedChorus/Art/UI/Bonds/bonds_sandbox_layout_snapshot.json Assets/FracturedChorus/Editor/BondsSceneSetupEditor.cs
git commit -m "$(cat <<'EOF'
Snapshot Bonds sandbox layout without applying it from code.

EOF
)"
```

---

## Later plan (do not execute here)

Wire `MetaStatusMenuUI` BtnBonds → instantiate/show Bonds canvas (or additive sandbox pattern copied into CampusHub) and hide `SocialStatsOverlayUI` as the Bonds entry. Esc pops Bonds then Status. That is a separate plan after visual sign-off.

---

## Spec coverage (self-review)

| Requirement | Task |
|-------------|------|
| Sandbox scene before CampusHub | Task 3, Global, Later plan |
| Glass HUD zones from mock | Task 2–3 |
| Social Stats radar + 5 nodes | Task 4 (reuse graphic) |
| Roster + Charlotte default | Task 1 + 4 |
| Detail bio/quote/EXP | Task 1 + 4 |
| Link Episodes 01–05 rank gate | Task 1 + 4 |
| Nav stubs disabled | Task 3 |
| P0 chrome/icons generated, no baked text | Task 2 |
| Reuse busts / city BG | Task 2 table |
| Layout SoT scene | Task 3 rule + seed-on-create |
| EditMode tests | Task 1, 2, 4 |
| Snapshot backup | Task 5 |
| No Build Settings entry | Task 3 step 5 |

## Placeholder scan

No TBD / “implement later” / “similar to Task N” without inlined code. Hub wire explicitly named as a **later plan**, not a hidden step.

## Type consistency

- `BondLinkEpisode` struct with `Index`, `Title`, `RequiredRank` — used in catalog, row view, and `BondsMenuUI.ConfirmEpisode`
- `BondPresentation.RosterOrder` / `VisibleChipCount = 7` — chips array length 7, portraits array length 6 + reserved sprite
- `WrapRosterIndex` shared for roster and episode
- Scene names `Chip_0`…`Chip_6`, `Row_01`…`Row_05`, `Node_Resonance`…`Node_Rhythm` match bind-by-name

## Estimate

| Task | Effort |
|------|--------|
| 1 Presentation TDD | 0.5h |
| 2 Art gen + import | 0.5–1d (gen + QA) |
| 3 Sandbox seed | 0.5d |
| 4 Bind + Play Mode | 0.5d |
| 5 Snapshot + polish | 0.5d |
| **Total** | **~2–3 days** |
