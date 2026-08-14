# Loading Screen — Design Spec

> **Ngày:** 2026-08-13  
> **Trạng thái:** Approved  
> **Plan:** [`docs/superpowers/plans/2026-08-13-loading-screen.md`](../plans/2026-08-13-loading-screen.md)  
> **Refs:** tấm 1 look lock `Loading_Screen_Wissh` (1024×576 JPEG); tấm 2 sheet `Loading_Screen_Part` (1024×682 JPEG)  
> **Canvas:** 1920×1080, Scale With Screen Size (giống menu/combat)

---

## 1. Vấn đề

Đổi scene dùng `SceneManager.LoadScene` sync. Không overlay persist, không progress. Player thấy fade/hitch/blank.

| # | Hiện trạng | Vấn đề |
|---|-----------|--------|
| 1 | `RunMapSceneLoader.LoadByName` → `LoadScene` sync | Main thread block, không đo tiến độ |
| 2 | Main Menu `sceneFadeOverlay` fade đen rồi load | Overlay chết cùng scene; không phải loading screen |
| 3 | Boss gate / Cadence `SetLoading` + status text | Chỉ lock input + copy, không UI loading |
| 4 | Combat retry `SceneManager.LoadScene(active)` | Lách loader |
| 5 | Unity Splash `m_ShowUnitySplashScreen` | Splash engine, không phải gameplay load |

---

## 2. Mục tiêu

1. Mọi lần đổi scene (menu, hub, map, combat, VN, flower work) hiện **cùng một** loading screen.
2. Look = tấm 1. Thành phần = slice tấm 2 (không dùng tấm 1 làm BG runtime — có `75%` bake).
3. Bar live: `LOADING...` + capsule neon + `%` trong fill.
4. Progress từ `LoadSceneAsync` (Unity cap 0.9 trước activate), lerp, min hold 0.8s.
5. Fail → ẩn overlay, giữ scene cũ, log error. Không retry UI.

---

## 3. Quyết định thiết kế

### 3.1 Hướng: overlay `DontDestroyOnLoad` (không Additive scene)

Một canvas sống qua `LoadSceneMode.Single`. Mọi load đi qua `RunMapSceneLoader`.

**Không** thêm scene `LoadingScreen` vào Build Settings.

### 3.2 API

```
RunMapSceneLoader.LoadByName(name, mode = Single)
  → LoadingScreenController.Ensure().Load(name, mode)
```

`LoadByName` giữ signature `bool`. Trả `false` ngay nếu busy, tên rỗng, hoặc scene không load được (`GetBuildIndex` < 0 và `CanStreamedLevelBeLoaded` false). Trả `true` khi coroutine load đã **bắt đầu**.

Callers hiện tại không đổi trừ combat retry.

### 3.3 Runtime types

| Type | Path | Vai trò |
|------|------|---------|
| `LoadingScreenController` | `Assets/FracturedChorus/UI/Loading/LoadingScreenController.cs` | Singleton persist, Show/Hide, chạy async load |
| `LoadingScreenView` | `Assets/FracturedChorus/UI/Loading/LoadingScreenView.cs` | Bind layer + bar, `SetProgress(0–1)` |
| Prefab | `Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab` | Canvas + layers + bar; `Ensure()` `Resources.Load("UI/LoadingScreen")` |

Một prefab. Không nhân đôi dưới `Prefabs/`. Không Editor menu bắt buộc — `Ensure()` spawn nếu chưa có instance.

### 3.4 Timing

| Param | Giá trị |
|-------|---------|
| Fade in | 0.20s |
| Fade out | 0.25s |
| Min hold (từ lúc overlay alpha=1) | **0.80s** |
| Progress lerp | SmoothDamp, ~0.12s smoothTime |
| `allowSceneActivation` | `true` khi `displayedFill ≥ 0.99` **và** `holdElapsed ≥ 0.80s` |
| Async mapping | `raw = async.progress / 0.9f`, clamp 0–1 |

`async.allowSceneActivation = false` cho đến điều kiện trên. Sau activate, đợi `async.isDone`, rồi fade out.

### 3.5 Busy / chồng load

`_busy == true` → `LoadByName` return `false`, ignore. Boss gate / menu `_transitioning` giữ nguyên như lớp UI; loader là hàng rào thứ hai.

---

## 4. Art pipeline

Nguồn user (workspace Cursor images, JPEG masquerading `.png`):

| Role | File | Size |
|------|------|------|
| Layout lock (QA only, không import runtime BG) | `Loading_Screen_Wissh-…png` | 1024×576 |
| Slice source | `Loading_Screen_Part-…png` | 1024×682 |

Import runtime:

`Assets/FracturedChorus/Art/UI/LoadingScreen/`

| Asset | Ghi chú |
|-------|---------|
| `loading_sky_fill.png` | Gradient `#0a0518 → #1a0a3a`, 8×1080 stretched, hoặc Image color — không cần photo |
| `loading_clouds.png` | crop tấm 2, key near-black → RGBA |
| `loading_notes_stars.png` | notes + stars rời hoặc atlas nhỏ |
| `loading_skyline.png` | panorama city |
| `loading_buildings_signs.png` | buildings + neon signs (POP CITY, FEEL THE BEAT, Live Your Music, LET'S PLAY!, MUSIC, DANCE) — có thể nhiều file nếu crop tách |
| `loading_clef.png` | treble clef + swirl |
| `loading_floor.png` | tiled floor perspective |

Import Unity: Sprite (2D and UI), Bilinear, Max Size 2048, Alpha from Transparency, no mipmaps.

Key: RGB gần đen (`max(R,G,B) < 18` và không nằm trong neon glow) → alpha 0. Không key nhầm glow hồng/tím.

Bar **không** slice từ sheet — uGUI.

### 4.1 Hierarchy (Canvas Overlay, sort order 500)

```
LoadingScreen (DontDestroyOnLoad)
└── Canvas (Overlay, 1920×1080, sortingOrder 500)
    ├── SkyFill       ← dim tối, không photo city
    └── UiGroup       ← title-safe ~88% bottom, center
        ├── Label     LOADING...
        └── Bar
            ├── Track (capsule interior tím + stroke neon pink)
            ├── Fill  (trắng, width theo progress, leading edge capsule + glow hồng)
            └── PercentLabel  (neo mép phải fill; ẩn khi fill=0)
```

City layers không hiện trên overlay.

---

## 5. Bar live (look tấm 1)

| Field | Giá trị |
|-------|---------|
| Label | `LOADING...` trắng, bold, glow hồng, center, ngay trên bar |
| Bar size | 720×36 px @ 1080p, capsule |
| Track | interior tím tối, stroke neon pink `#FF4EC8`, glow Outline |
| Fill | trắng, width theo progress (capsule), glow hồng ở mép dẫn |
| Percent | `{0:0}%` trong fill, neo phải phần đầy; ẩn khi progress < 0.02 |
| Font | `UiFontCatalog.Body` (không nhúng font mới) |

---

## 6. Tích hợp call sites

Mọi load gameplay phải qua `RunMapSceneLoader.LoadByName`.

| Caller | Đổi |
|--------|-----|
| `RunMapSceneLoader` | Đổi body sang controller async; giữ helper `LoadCombatPrototype` / `LoadCombatTutorial` / `LoadRunMapPrototype` |
| `CombatController` retry | `LoadByName(activeScene.name)` thay `SceneManager.LoadScene` |
| `MainMenuStartGameController` NEW/LOAD | Bỏ fade-to-black rồi load: gọi `LoadByName` ngay (overlay loading che). Có thể giữ SFX/duck BGM hiện tại. |
| Hub / VN / Flower / Cadence / RunMap | Không đổi call site |

Editor Play từ scene giữa (không qua menu): `RuntimeInitializeOnLoadMethod` **không** auto-show. Overlay chỉ hiện khi có `LoadByName`.

---

## 7. Lỗi

| Case | Hành vi |
|------|---------|
| Tên rỗng / scene không có trong Build Settings | `false`, log error hiện tại, không Show overlay |
| `LoadAsync` trả null | Hide nếu đã Show, log, `_busy=false`, scene cũ |
| Exception trong coroutine | try/catch, log `console.error` tương đương `Debug.LogError`, Hide, `_busy=false` |
| Double load | `_busy` → `false`, ignore |
| Reload scene đang active | Vẫn Show + async reload |

Không fallback load sync. Không queue load thứ hai.

---

## 8. Test

1. Main Menu NEW GAME → overlay → bar chạy → PrologueVN → overlay tắt.
2. LOAD slot → đúng scene (PrologueVN / CampusHub / RunMapPrototype).
3. Hub ↔ FlowerShopWork ↔ RunMap ↔ Combat ↔ VN: mọi `LoadByName`.
4. Combat retry: overlay, không `SceneManager.LoadScene` trực tiếp.
5. Tên scene sai: overlay không kẹt, scene không đổi.
6. Double-click Fight / NEW GAME: một load.
7. Fast load: overlay ≥ 0.80s.

---

## 9. Ngoài scope

- Unity Splash custom / logo FC
- Tip text / lore / character splash theo destination
- Parallax pointer / music riêng cho loading
- Additive loading scene
- Progress theo Addressables (project chưa dùng)

---

## 10. Success criteria

- Không còn `SceneManager.LoadScene` gameplay ngoài `LoadingScreenController`.
- Look khớp tấm 1 **phần loading** (LOADING + capsule bar) trên dim; không city BG.
- `%` thay đổi theo load thật, không bake 75%.
