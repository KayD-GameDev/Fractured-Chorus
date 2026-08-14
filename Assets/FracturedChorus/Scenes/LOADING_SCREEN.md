# Loading Screen — Setup & Play checklist

Overlay **DontDestroyOnLoad** — mọi scene change qua `RunMapSceneLoader.LoadByName`. Không Additive scene, không scene `LoadingScreen` trong Build Settings.

**Prefab:** `Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab`  
**Resources path:** `Resources/UI/LoadingScreen` (runtime `Resources.Load`)

**Cập nhật:** 2026-08-14 — random 3 BG `Resources/UI/LoadingBg/loading_bg_01..03`

---

## Tạo / rebuild prefab

1. Mở Unity project `Fractured-Chorus1`
2. **Fractured Chorus → Build Loading Screen Prefab** — rebuild overlay (BG + bar)
3. **File → Save Project**

**Lưu ý:** Nếu prefab thiếu, `LoadingScreenController.Ensure()` spawn từ Resources hoặc fallback runtime hierarchy. Prefab là source of truth cho layout.

---

## Hierarchy (prefab)

```
LoadingScreen (DontDestroyOnLoad)
└── Canvas (Overlay 1920×1080 · sortingOrder 500)
    ├── SkyFill       ← random loading_bg_01 / 02 / 03 mỗi lần load
    └── UiGroup       ← anchor (0.5, 0.12) · lower-third
        ├── Label     LOADING... · trắng + glow hồng
        └── Bar       Track capsule + Fill live + PercentLabel
```

City slice layers (Clouds / Clef / Floor…) **tắt**. Bar **không** bake 75% — uGUI đè lên vị trí bar trên BG.

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

Runtime BG (random mỗi `BeginLoad`): `Resources/UI/LoadingBg/loading_bg_01.png` … `_03.png` (1024×576, stretch 16:9).

City slice PNG trong `Art/UI/LoadingScreen/` **không** gắn overlay (giữ file).

---

## Scripts

| File | Vai trò |
|------|---------|
| `UI/Loading/LoadingScreenController.cs` | Ensure DDOL · busy · fade · LoadSceneAsync |
| `UI/Loading/LoadingScreenView.cs` | Dim · capsule bar · % · chrome neon |
| `UI/Loading/LoadingProgress.cs` | Timing constants · CanActivate |
| `RunMap/RunMapSceneLoader.cs` | Validate → delegate `BeginLoad` |
| `Editor/LoadingScreenArtImportEditor.cs` | Menu Import Art |
| `Editor/LoadingScreenPrefabBuilder.cs` | Menu Build Prefab |

---

## Look QA (so tấm 1 — chỉ phần loading)

Mở `_source/loading_screen_wish.jpg` cạnh Game view 16:9 khi overlay visible:

| Element | Expect |
|---------|--------|
| **BG** | Random 1/3 Pop City (`loading_bg_01..03`) · không lặp tấm vừa rồi |
| **LOADING...** | Lower-third · trắng · glow hồng · center trên bar |
| **Bar** | Capsule · viền neon `#FF4EC8` · interior tím tối · fill trắng từ trái |
| **%** | Trong fill, neo mép phải phần đầy · không bake 75% |

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
| 2026-08-14 | Random 3 BG `LoadingBg/loading_bg_01..03` mỗi lần load · bỏ `loading_bg` cũ |
| 2026-08-13 | Doc Play checklist · Look QA vs tấm 1 · Editor menus Import/Build |
