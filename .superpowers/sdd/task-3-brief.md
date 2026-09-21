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

