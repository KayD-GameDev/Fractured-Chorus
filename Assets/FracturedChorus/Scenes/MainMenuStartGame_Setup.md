# MainMenuStartGame — Scene setup

Logic trong **MonoBehaviour `.cs`**. Layout chỉnh trong **Hierarchy**; art nền baked trong PNG, menu button là uGUI overlay.

**Scene:** `Assets/FracturedChorus/Scenes/MainMenuStartGame.unity`

**Cập nhật:** 2026-07-03 — Title voice random · ESC → Attract · QUIT

---

## Tạo / mở scene

1. Mở Unity project `Fractured-Chorus1`
2. Menu **Fractured Chorus → Create MainMenuStartGame Scene** (tạo mới + save + Build Settings)
   - Hoặc mở scene có sẵn → **Fractured Chorus → Setup MainMenuStartGame Scene Hierarchy** (rebuild trên scene active)
   - Scene cũ: **Fractured Chorus → Upgrade MainMenuStartGame Menu And Audio** (HitArea · BGM · OFF-BEAT ARCHIVE)
   - Scene cũ thiếu Config UI: **Fractured Chorus → Upgrade MainMenuStartGame Config UI**
   - Gắn kit Config (panel/slider/toggle/chip): **Fractured Chorus → Apply Config UI Kit** — bật preview Config; layout chỉnh trên Scene rồi Save
   - Scene cũ thiếu player Archive: **Fractured Chorus → Upgrade Off-Beat Archive Player**
   - Scene cũ → SyncPod face player: **Fractured Chorus → Upgrade Off-Beat SyncPod Layout**
3. **File → Build Settings** — thứ tự:
   - `MainMenuStartGame.unity` (index **0**)
   - `RunMapPrototype.unity`
   - `CombatPrototype.unity`
4. **Play**

**Lưu ý:** Nếu Unity Editor đang mở project, batch headless sẽ fail — dùng menu **Create MainMenuStartGame Scene** trong Editor (hoặc đóng Unity rồi chạy batch).

```text
Unity -batchmode -projectPath <repo> -executeMethod FracturedChorus.Editor.MainMenuStartGameSceneSetupEditor.BatchCreateMainMenuStartGameScene
Unity -batchmode -projectPath <repo> -executeMethod FracturedChorus.Editor.MainMenuStartGameSceneSetupEditor.BatchUpgradeMainMenuStartGameConfigUi
```

---

## Hierarchy

```
MainMenuStartGameRoot          ← MainMenuStartGameController (+ Edit Mode Preview)
└── MainMenuCanvas             ← Overlay 1920×1080 · Scale With Screen Size
    ├── AttractLayer           ← CanvasGroup + Image · v2 PNG
    ├── MainMenuBackground     ← CanvasGroup + Image · v5 PNG (tách riêng)
    ├── MenuPanel              ← CanvasGroup + buttons (sibling, không nằm dưới BG)
    ├── SettingsOverlay        ← Config BG + Volume · Brightness · Difficulty · ESC/B Back
    └── OffBeatArchiveOverlay  ← catalog trái + player phải · ESC/B Back
MainMenuBgm                    ← loop Midnight (Menu) · sau voice intro
MainMenuTitleVoice             ← random Female/Male · đọc tên game trước BGM
EventSystem
Main Camera                    ← bg #0a0a1a
```

### Edit Mode Preview (tránh 2 ảnh chồng nhau)

Inspector `MainMenuStartGameRoot` → **Edit Mode Preview**:

| Nút | Hiển thị |
|-----|----------|
| **Attract** | Chỉ AttractLayer |
| **Main Menu** | MainMenuBackground + MenuPanel |
| **Config** | Chỉ Config overlay (Ren background + sliders) |
| **Off-Beat** | Main menu BG + `OffBeatArchiveOverlay` (catalog + player) — chỉnh layout Archive |

Menu: **Fractured Chorus → Upgrade MainMenuStartGame Layers** (scene cũ còn `MainMenuLayer` + MenuPanel con).

---

## Flow Play

| Bước | Hành vi |
|------|---------|
| Boot | Attract · voice intro random → BGM loop |
| Any key / click / gamepad A | Crossfade **0.35s** → Main Menu |
| ↑↓ / W/S | Chọn NEW GAME · LOAD · OFF-BEAT ARCHIVE · CONFIG · QUIT |
| Hover chuột | Label sáng cyan |
| Click | Kích hoạt option (HitArea raycast) |
| Enter / Space / A | Kích hoạt option |
| **ESC / B** (Main Menu panel) | Quay lại Attract (Press Any Button) |
| **ESC / B** (overlay) | Đóng overlay → Main Menu |
| **NEW GAME** | `RunMapSceneLoader.LoadByName(RunMapPrototype)` |
| **LOAD GAME** | Stub — status *"Chưa có dữ liệu lưu."* |
| **OFF-BEAT ARCHIVE** | Catalog + music player — nghe lại BGM in-game |
| **CONFIG** | Mở `SettingsOverlay` — Volume · Background Brightness · Difficulty · Back hoặc ESC/B |
| **QUIT** | Thoát game |
| Boss F16 (map) | Không đổi — vẫn load `CombatPrototype` |

---

## Art assets

| Layer | Path |
|-------|------|
| Attract | `Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_Attract_PressAnyButton_v2.png` |
| Main menu BG | `Assets/FracturedChorus/Art/UI/TitleScreen/TitleScreen_MainMenu_Background_v5.png` |
| Config BG | `Assets/FracturedChorus/Art/UI/ConfigMenu/config_bg_memory_hall_v1.png` |
| Config kit | `Assets/FracturedChorus/Art/UI/ConfigMenu/Kit/` · panel · slider · toggle · chips · icons |
| Menu BGM | `Assets/FracturedChorus/Audio/Music/Midnight_BGM_Menu.mp3` · loop · vol 0.65 |
| Title voice | `Audio/Voice/MainMenu_Female_Voice.mp3` · `MainMenu_Male_Voice.mp3` · random 50/50 |
| Attract → Menu | `Audio/SFX/MainMenu_ChangeMenu_Ting.mp3` · chỉ lúc bấm qua Main Menu |
| Button press | `Audio/SFX/MainMenu_ButtonPress.mp3` · nav menu · confirm · config |

Import: Texture **Sprite (2D and UI)** · Max Size **2048** · Bilinear.

Chữ *Fractured Chorus*, *PRESS ANY BUTTON*, logo *FC* — **baked** trong PNG. Chỉ menu option là Text runtime.

---

## Scripts

| File | Vai trò |
|------|---------|
| `Menu/MainMenuGameSettings.cs` | PlayerPrefs: volume · brightness · skip unread · difficulty |
| `Menu/MainMenuConfigOverlayController.cs` | Config overlay nav · sliders · difficulty cycle |
| `Menu/MainMenuStartGameController.cs` | Attract ↔ MainMenu · fade · Settings + Archive overlay |
| `Menu/OffBeatArchiveController.cs` | Catalog + player UI · seek · shuffle/repeat · favorite Prefs · duck menu BGM |
| `Menu/OffBeatMusicPlayer.cs` | AudioSource playback / playlist transport |
| `Menu/OffBeatTrackSO.cs` / `OffBeatCatalogSO.cs` | Track + catalog data (`Resources/OffBeat/`) |
| `Menu/MainMenuStartGameMenuController.cs` | Keyboard/gamepad nav · highlight bar · load map |
| `Menu/MainMenuButtonRowView.cs` | Hover sáng label · HitArea raycast |
| `Menu/MainMenuBgmController.cs` | Loop menu BGM (sau voice) |
| `Menu/MainMenuTitleVoiceController.cs` | Random voice intro |
| `RunMap/RunMapSceneCatalog.cs` | `MainMenuStartGame` constant |
| `RunMap/RunMapSceneLoader.cs` | Resolve path scene menu |
| `Editor/MainMenuStartGameSceneSetupEditor.cs` | Menu Editor tạo hierarchy |

---

## Chỉnh layout (Hierarchy)

| Object | Chỉnh gì |
|--------|----------|
| `MenuPanel` | Anchor góc phải dưới — vùng trống trên art v5 |
| `Row_*` | Spacing `VerticalLayoutGroup` · font 28 bold |
| `HighlightBar` | Màu `(0.102, 0.227, 0.361)` |
| `AttractLayer` / `MainMenuLayer` | Đổi sprite ref nếu art mới |
| `SettingsOverlay/ConfigUiRoot` | Free Rect (Pos/Scale) · mặc định center trái ~768×734 |
| `ConfigUiRoot/Panel` | 9-slice kit panel — kéo inset trên Rect |
| `ConfigBackground` | `offsetMin.y = 72` — ẩn band CONFIG trắng dưới |
| `ConfigList` / `Row_*` | Free Rect — không LayoutGroup; kéo Pos/Scale tự do |
| `Row_* / Icon · Slider · Chip_* · BtnMinus/Plus` | Sprite kit — **không** sửa Pos trong code; chỉnh trên Scene rồi Save |
| `Row_Volume` / `Row_Background_Brightness` | Slider 0–1 |
| `Row_Skip_Unread_Text` | Toggle switch — click ON/OFF |
| `Row_Difficulty` | **ON BEAT** · **CADENCE** · **OFF-BEAT** (←→) |
| `MainMenuStartGameController` | `transitionDuration` (mặc định 0.35) |

**Quy tắc layout:** Scene = source of truth. **Apply Config UI Kit** chỉ bind sprite, không ghi `RectTransform`. Menu **Upgrade/Ensure** chỉ thêm row thiếu + gỡ LayoutGroup — **không reset Pos/Scale**. Chỉ menu **Rebuild Config UI (Resets Layout)** mới xóa và tạo lại mặc định.

---

## Changelog

| Ngày | Thay đổi |
|------|----------|
| 2026-08-20 | Config layout khóa trên scene (Apply kit không ghi Rect) · slider fill bỏ tấm phông đục |
| 2026-08-19 | Config UI kit gắn scene (Apply Config UI Kit) · preview Config · layout trên Rect |
| 2026-07-24 | SyncPod layout snapshot (VolumeArc pos/size/rot · Controls · hit alpha 0) |
| 2026-07-24 | Off-Beat SyncPod redesign (BG v2 · face waveform · swipe track · volume arc · no prev/next/seek) |
| 2026-07-24 | Off-Beat Archive player + catalog (split UI · seek · shuffle/repeat · favorite · duck BGM) |
| 2026-07-02 | Tách Ting (Attract→Menu) và ButtonPress SFX cho menu/config |
| 2026-07-02 | Config UI — Ren background · volume/brightness sliders · difficulty tiers |
| 2026-07-03 | Title voice random · ESC Main Menu → Attract · QUIT |
| 2026-07-02 | BGM Midnight loop · OFF-BEAT ARCHIVE · button HitArea + hover cyan |
| 2026-07-02 | Tách MainMenuBackground + MenuPanel · Edit Mode Preview (Attract/Main Menu/Settings) |
| 2026-07-02 | Fix Input System-only — bỏ fallback legacy Input |

---

## Off-Beat Archive — Play checklist

1. Mở `MainMenuStartGame` → **Fractured Chorus → Upgrade Off-Beat SyncPod Layout** → Save  
   (stub cũ: **Upgrade Off-Beat Archive Player** trước)
2. Play → Attract → Main Menu → **OFF-BEAT ARCHIVE**
3. Catalog trái · SyncPod player phải (`offbeat_syncpod_bg_v2` · cover / title / Shuffle·Play·Repeat · waveform trên mặt đĩa · volume arc đáy)
4. Click bài hoặc ↑↓ + Enter → Play · Midnight duck · **waveform string cyan trên face**
5. Vuốt trái/phải trên đĩa = Next/Prev · kéo volume arc (outer ring) · Shuffle / Repeat glow · ♥ favorite (Prefs)
6. ESC / BACK → stop Archive · restore Midnight
7. Config Master Volume vẫn nhân Archive volume (`fc_offbeat_volume`)

## Phase sau (chưa làm)

- Contract Select sau NEW GAME
- Load Profile / Echo meta
- Off-Beat unlock theo story flag · cover art per-track
- Silhouette idle pulse theo beat
