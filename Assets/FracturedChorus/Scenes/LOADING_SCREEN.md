# Loading Screen — Setup & Play checklist

Overlay **DontDestroyOnLoad** — mọi scene change qua `RunMapSceneLoader.LoadByName`. Không Additive scene, không scene `LoadingScreen` trong Build Settings.

**Prefab:** `Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab`  
**Resources path:** `Resources/UI/LoadingScreen` (runtime `Resources.Load`)

**Cập nhật:** 2026-08-13 — async load · min hold 0.8s · Play checklist

---

## Tạo / rebuild prefab

1. Mở Unity project `Fractured-Chorus1`
2. **Fractured Chorus → Import Loading Screen Art** — copy sheet tấm 2, key near-black, slice PNG vào `Art/UI/LoadingScreen/`
3. **Fractured Chorus → Build Loading Screen Prefab** — rebuild prefab Resources (Canvas + layers + bar)
4. **File → Save Project** — commit prefab + art nếu layout thay đổi

**Lưu ý:** Nếu prefab thiếu, `LoadingScreenController.Ensure()` spawn từ Resources hoặc fallback runtime hierarchy. Prefab là source of truth cho layout.

---

## Hierarchy (prefab)

```
LoadingScreen (DontDestroyOnLoad)
└── Canvas (Overlay 1920×1080 · sortingOrder 500)
    ├── SkyFill
    ├── Clouds
    ├── NotesStars
    ├── Skyline
    ├── BuildingsSigns
    ├── Clef          ← pulse scale 0.97–1.03 · period ~2.4s
    ├── Floor
    └── UiGroup       ← anchor (0.5, 0.12) · lower-third
        ├── Label     LOADING...
        └── Bar       Track + Fill + PercentLabel (uGUI live)
```

Bar **không** slice từ sheet — uGUI capsule neon `#FF4EC8`, fill trắng→hồng, `%` trong fill.

---

## Timing

| Param | Giá trị |
|-------|---------|
| Fade in | 0.20s |
| Fade out | 0.25s |
| Min hold (alpha=1) | **0.80s** |
| Progress lerp | SmoothDamp ~0.12s |
| Async map | `raw = async.progress / 0.9` |
| Activate scene | `displayedFill ≥ 0.99` **và** hold ≥ 0.80s |

---

## Art assets

| Layer | Path |
|-------|------|
| Clouds | `Art/UI/LoadingScreen/loading_clouds.png` |
| Notes/stars | `Art/UI/LoadingScreen/loading_notes_stars.png` |
| Skyline | `Art/UI/LoadingScreen/loading_skyline.png` |
| Buildings/signs | `Art/UI/LoadingScreen/loading_buildings_signs.png` |
| Clef | `Art/UI/LoadingScreen/loading_clef.png` |
| Floor | `Art/UI/LoadingScreen/loading_floor.png` |

Look lock QA (không import runtime BG): `_source/loading_screen_wish.jpg` (tấm 1). Slice source: `_source/loading_screen_part.jpg` (tấm 2).

Import: Sprite (2D and UI) · Bilinear · Max Size 2048 · Alpha from Transparency.

---

## Scripts

| File | Vai trò |
|------|---------|
| `UI/Loading/LoadingScreenController.cs` | Ensure DDOL · busy · fade · LoadSceneAsync |
| `UI/Loading/LoadingScreenView.cs` | Layers · bar fill · % · clef pulse · notes float |
| `UI/Loading/LoadingProgress.cs` | Timing constants · CanActivate |
| `RunMap/RunMapSceneLoader.cs` | Validate → delegate `BeginLoad` |
| `Editor/LoadingScreenArtImportEditor.cs` | Menu Import Art |
| `Editor/LoadingScreenPrefabBuilder.cs` | Menu Build Prefab |

---

## Look QA (so tấm 1)

Mở `_source/loading_screen_wish.jpg` cạnh Game view 16:9 khi overlay visible:

| Element | Expect |
|---------|--------|
| **Clef** | Center canvas · không lệch trái/phải |
| **Floor** | Sát đáy · perspective tiled |
| **Bar + LOADING...** | Lower-third · ngay trên sàn |
| **Skyline / buildings** | Fill mid-ground · neon signs đọc được |
| **Bar live** | `%` chạy theo load thật · không bake 75% |

Nếu lệch: chỉnh Rect/scale trên prefab `Resources/UI/LoadingScreen` — **không** đổi logic controller.

---

## Play Mode checklist

Chạy tay từ `MainMenuStartGame` (index 0). Ghi pass/fail vào cột **OK**.

| # | Case | Steps | Expect | OK |
|---|------|-------|--------|----|
| 1 | **NEW GAME** | Main Menu → NEW GAME | Overlay fade in → bar chạy → `PrologueVN` → overlay fade out | ☐ |
| 2 | **LOAD slot** | Main Menu → LOAD (slot có data) | Overlay → đúng scene (PrologueVN / CampusHub / RunMapPrototype) | ☐ |
| 3 | **Hub ↔ Flower** | CampusHub ↔ FlowerShopWork | Mỗi `LoadByName` hiện overlay | ☐ |
| 4 | **Hub ↔ RunMap** | Hub gate / map entry | Overlay → RunMapPrototype | ☐ |
| 5 | **RunMap ↔ Combat** | Boss gate / Fight | Overlay → CombatPrototype | ☐ |
| 6 | **Combat ↔ VN** | Combat end → VN (nếu có route) | Overlay qua loader | ☐ |
| 7 | **Combat retry** | Thua → Retry | Overlay reload combat · **không** sync `SceneManager.LoadScene` | ☐ |
| 8 | **Bad scene name** | Dev: gọi `LoadByName("NotARealScene")` | Return `false` · overlay **không** kẹt · scene cũ giữ nguyên | ☐ |
| 9 | **Double-click** | Spam NEW GAME hoặc Fight | Một load duy nhất · `_busy` chặn lần 2 | ☐ |
| 10 | **Min hold 0.8s** | Load scene nhẹ (fast SSD) | Overlay visible ≥ **0.80s** trước fade out | ☐ |

**Menu note:** NEW/LOAD bỏ fade đen cũ — overlay loading che thay.

**Play từ scene giữa:** overlay **không** auto-show lúc boot; chỉ hiện khi có `LoadByName`.

---

## Lỗi / edge cases

| Case | Hành vi |
|------|---------|
| Tên rỗng / scene không trong Build Settings | `false` · log error · không Show |
| LoadAsync null / exception | Hide overlay · log · `_busy=false` · giữ scene cũ |
| Double load | `false` · ignore |
| Reload scene đang active | Vẫn Show + async reload |

---

## Changelog

| Ngày | Thay đổi |
|------|----------|
| 2026-08-13 | Doc Play checklist · Look QA vs tấm 1 · Editor menus Import/Build |
