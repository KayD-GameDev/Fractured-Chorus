# Loading Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mọi `RunMapSceneLoader.LoadByName` hiện overlay loading (look tấm 1, art slice tấm 2) + `LoadSceneAsync` progress thật, min hold 0.8s.

**Architecture:** `LoadingScreenController` DontDestroyOnLoad load prefab `Resources/UI/LoadingScreen`. `RunMapSceneLoader` chỉ validate rồi ủy quyền. Bar live uGUI; scene không Additive.

**Tech Stack:** Unity 6 · C# · uGUI · `SceneManager.LoadSceneAsync` · NUnit EditMode.

## Global Constraints

- Canvas Overlay 1920×1080, `sortingOrder` **500**.
- Fade in **0.20s**, fade out **0.25s**, min hold **0.80s**, SmoothDamp **0.12s**.
- `raw = async.progress / 0.9`, activate khi `displayedFill ≥ 0.99` **và** hold ≥ 0.80s.
- Prefab: `Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab` — một nguồn, không nhân `Prefabs/`.
- Font: `UiFontCatalog.Body`. Bar 720×36, stroke `#FF4EC8`. Label `LOADING...`.
- Không dùng tấm 1 làm BG runtime. Bar không slice từ sheet.
- Không `SceneManager.LoadScene` gameplay ngoài `LoadingScreenController`.
- Không comment giải thích trong source mới.
- Overlay chỉ hiện khi `LoadByName` — không auto-show lúc Play scene giữa.

## File Structure

| File | Trách nhiệm |
|------|-------------|
| `Assets/FracturedChorus/UI/Loading/LoadingProgress.cs` | Hằng số + map progress + CanActivate |
| `Assets/FracturedChorus/UI/Loading/LoadingScreenView.cs` | Layers, bar fill, %, pulse clef, float notes |
| `Assets/FracturedChorus/UI/Loading/LoadingScreenController.cs` | Ensure, busy, fade, LoadSceneAsync |
| `Assets/FracturedChorus/Editor/LoadingProgressTests.cs` | NUnit math + CanLoad + busy |
| `Assets/FracturedChorus/Editor/LoadingScreenArtImportEditor.cs` | Copy sheet, key black, slice PNG, import sprite |
| `Assets/FracturedChorus/Editor/LoadingScreenPrefabBuilder.cs` | Build prefab Resources |
| `Assets/FracturedChorus/Art/UI/LoadingScreen/*.png` | Sliced layers |
| Sửa: `RunMapSceneLoader.cs`, `CombatController.cs`, `MainMenuStartGameController.cs` | Cổng async + retry + bỏ fade đen |

---

### Task 1: LoadingProgress math

**Files:**
- Create: `Assets/FracturedChorus/UI/Loading/LoadingProgress.cs`
- Create: `Assets/FracturedChorus/Editor/LoadingProgressTests.cs`

**Interfaces:**
- Produces: `LoadingProgress.MapAsyncProgress(float)`, `LoadingProgress.CanActivate(float displayedFill, float holdElapsedSec)`, constants `UnityActivationCap`, `MinHoldSec`, `ActivateFill`, `FadeInSec`, `FadeOutSec`, `SmoothTime`, `PercentVisibleMin`.

- [ ] **Step 1: Write the failing tests**

```csharp
using FracturedChorus.UI.Loading;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class LoadingProgressTests
    {
        [Test]
        public void MapAsyncProgress_Zero_IsZero()
        {
            Assert.AreEqual(0f, LoadingProgress.MapAsyncProgress(0f), 0.0001f);
        }

        [Test]
        public void MapAsyncProgress_Cap_IsOne()
        {
            Assert.AreEqual(1f, LoadingProgress.MapAsyncProgress(0.9f), 0.0001f);
        }

        [Test]
        public void MapAsyncProgress_HalfCap_IsHalf()
        {
            Assert.AreEqual(0.5f, LoadingProgress.MapAsyncProgress(0.45f), 0.0001f);
        }

        [Test]
        public void MapAsyncProgress_AboveCap_ClampsToOne()
        {
            Assert.AreEqual(1f, LoadingProgress.MapAsyncProgress(1f), 0.0001f);
        }

        [Test]
        public void CanActivate_RequiresFillAndHold()
        {
            Assert.IsFalse(LoadingProgress.CanActivate(1f, 0.79f));
            Assert.IsFalse(LoadingProgress.CanActivate(0.98f, 1f));
            Assert.IsTrue(LoadingProgress.CanActivate(0.99f, 0.80f));
        }
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL (type missing)**

Unity: Window → General → Test Runner → EditMode → `LoadingProgressTests`.

Expected: fail compile / type not found `LoadingProgress`.

- [ ] **Step 3: Implement**

```csharp
using UnityEngine;

namespace FracturedChorus.UI.Loading
{
    public static class LoadingProgress
    {
        public const float UnityActivationCap = 0.9f;
        public const float FadeInSec = 0.20f;
        public const float FadeOutSec = 0.25f;
        public const float MinHoldSec = 0.80f;
        public const float SmoothTime = 0.12f;
        public const float ActivateFill = 0.99f;
        public const float PercentVisibleMin = 0.02f;

        public static float MapAsyncProgress(float unityProgress)
        {
            if (unityProgress <= 0f)
            {
                return 0f;
            }

            var mapped = unityProgress / UnityActivationCap;
            return Mathf.Clamp01(mapped);
        }

        public static bool CanActivate(float displayedFill, float holdElapsedSec)
        {
            return displayedFill >= ActivateFill && holdElapsedSec >= MinHoldSec;
        }
    }
}
```

- [ ] **Step 4: Re-run tests — expect PASS**

- [ ] **Step 5: Commit**

```
git add Assets/FracturedChorus/UI/Loading/LoadingProgress.cs Assets/FracturedChorus/UI/Loading/LoadingProgress.cs.meta Assets/FracturedChorus/Editor/LoadingProgressTests.cs Assets/FracturedChorus/Editor/LoadingProgressTests.cs.meta
git commit -m "Add loading progress mapping for async scene activation."
```

---

### Task 2: RunMapSceneLoader.CanLoad

**Files:**
- Modify: `Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs`
- Modify: `Assets/FracturedChorus/Editor/LoadingProgressTests.cs` (thêm tests CanLoad)

**Interfaces:**
- Consumes: existing `ResolveScenePath` (đổi thành `public static` hoặc giữ private + `CanLoad` public).
- Produces: `RunMapSceneLoader.CanLoad(string sceneName)` — `true` nếu build index ≥ 0 hoặc `CanStreamedLevelBeLoaded`. Không load scene. Tên rỗng → `false`.

- [ ] **Step 1: Add failing tests**

```csharp
using FracturedChorus.RunMap;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class RunMapSceneLoaderCanLoadTests
    {
        [Test]
        public void CanLoad_Empty_IsFalse()
        {
            Assert.IsFalse(RunMapSceneLoader.CanLoad(""));
            Assert.IsFalse(RunMapSceneLoader.CanLoad("   "));
            Assert.IsFalse(RunMapSceneLoader.CanLoad(null));
        }

        [Test]
        public void CanLoad_KnownScenes_IsTrue()
        {
            Assert.IsTrue(RunMapSceneLoader.CanLoad(RunMapSceneCatalog.MainMenuStartGame));
            Assert.IsTrue(RunMapSceneLoader.CanLoad(RunMapSceneCatalog.PrologueVN));
            Assert.IsTrue(RunMapSceneLoader.CanLoad(RunMapSceneCatalog.CombatPrototype));
        }

        [Test]
        public void CanLoad_Unknown_IsFalse()
        {
            Assert.IsFalse(RunMapSceneLoader.CanLoad("DefinitelyMissingScene_XYZ"));
        }
    }
}
```

Đặt class này trong cùng file `LoadingProgressTests.cs` hoặc file `RunMapSceneLoaderCanLoadTests.cs`. Prefer file mới: `Assets/FracturedChorus/Editor/RunMapSceneLoaderCanLoadTests.cs`.

- [ ] **Step 2: Run — FAIL (`CanLoad` missing)**

- [ ] **Step 3: Implement CanLoad — chưa đổi LoadByName sang async**

Replace `RunMapSceneLoader` body methods:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.RunMap
{
    public static class RunMapSceneLoader
    {
        private const string MainMenuStartGameScenePath = "Assets/FracturedChorus/Scenes/MainMenuStartGame.unity";
        private const string PrologueVNScenePath = "Assets/FracturedChorus/Scenes/PrologueVN.unity";
        private const string OpeningInvestigationScenePath = "Assets/FracturedChorus/Scenes/OpeningInvestigation.unity";
        private const string CampusHubScenePath = "Assets/FracturedChorus/Scenes/CampusHub.unity";
        private const string CombatScenePath = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
        private const string CombatTutorialScenePath = "Assets/FracturedChorus/Scenes/CombatTutorial.unity";
        private const string RunMapScenePath = "Assets/FracturedChorus/Scenes/RunMapPrototype.unity";

        public static bool CanLoad(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            var buildIndex = SceneUtility.GetBuildIndexByScenePath(ResolveScenePath(sceneName));
            if (buildIndex >= 0)
            {
                return true;
            }

            return Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public static bool LoadByName(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[Fractured Chorus] RunMapSceneLoader: scene name rỗng.");
                return false;
            }

            var buildIndex = SceneUtility.GetBuildIndexByScenePath(ResolveScenePath(sceneName));
            if (buildIndex >= 0)
            {
                Debug.Log($"[Fractured Chorus] Load scene index {buildIndex} ({sceneName}).");
                SceneManager.LoadScene(buildIndex, mode);
                return true;
            }

            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.Log($"[Fractured Chorus] Load scene by name: {sceneName}.");
                SceneManager.LoadScene(sceneName, mode);
                return true;
            }

            Debug.LogError(
                $"[Fractured Chorus] Không load được scene '{sceneName}'. " +
                $"Thêm scene vào File → Build Settings.");
            return false;
        }

        public static bool LoadCombatPrototype() => LoadByName(RunMapSceneCatalog.CombatPrototype);

        public static bool LoadCombatTutorial() => LoadByName(RunMapSceneCatalog.CombatTutorial);

        public static bool LoadRunMapPrototype() => LoadByName(RunMapSceneCatalog.RunMapPrototype);

        public static string ResolveScenePath(string sceneName)
        {
            if (sceneName == RunMapSceneCatalog.MainMenuStartGame)
            {
                return MainMenuStartGameScenePath;
            }

            if (sceneName == RunMapSceneCatalog.PrologueVN)
            {
                return PrologueVNScenePath;
            }

            if (sceneName == RunMapSceneCatalog.OpeningInvestigation)
            {
                return OpeningInvestigationScenePath;
            }

            if (sceneName == RunMapSceneCatalog.CampusHub)
            {
                return CampusHubScenePath;
            }

            if (sceneName == RunMapSceneCatalog.CombatPrototype)
            {
                return CombatScenePath;
            }

            if (sceneName == RunMapSceneCatalog.CombatTutorial)
            {
                return CombatTutorialScenePath;
            }

            if (sceneName == RunMapSceneCatalog.RunMapPrototype)
            {
                return RunMapScenePath;
            }

            return $"Assets/FracturedChorus/Scenes/{sceneName}.unity";
        }
    }
}
```

`ResolveScenePath` đổi `public` để test/builder không cần duplicate path. FlowerShopWork vẫn đi nhánh fallback `Scenes/{name}.unity`.

- [ ] **Step 4: Tests PASS** (`CanLoad_KnownScenes` phụ thuộc Build Settings — nếu fail, thêm scene vào Build Settings trước, không fake test).

- [ ] **Step 5: Commit**

```
git add Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs Assets/FracturedChorus/Editor/RunMapSceneLoaderCanLoadTests.cs Assets/FracturedChorus/Editor/RunMapSceneLoaderCanLoadTests.cs.meta
git commit -m "Expose scene load checks without starting a load."
```

---

### Task 3: LoadingScreenView (bar live)

**Files:**
- Create: `Assets/FracturedChorus/UI/Loading/LoadingScreenView.cs`
- Modify: `Assets/FracturedChorus/Editor/LoadingProgressTests.cs` — thêm `LoadingScreenViewTests`

**Interfaces:**
- Consumes: `LoadingProgress.PercentVisibleMin`, `UiFontCatalog.Body`
- Produces: `void SetProgress(float normalized01)`, `void SetVisible(bool visible, bool instant)`, `CanvasGroup Group`, `void TickMotion(float unscaledDeltaTime)`

- [ ] **Step 1: Failing view test**

```csharp
using FracturedChorus.UI.Loading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tests
{
    public class LoadingScreenViewTests
    {
        [Test]
        public void SetProgress_SetsFillAndPercent()
        {
            var go = new GameObject("LoadingScreenViewTest");
            var view = go.AddComponent<LoadingScreenView>();
            view.BuildForTests();
            view.SetProgress(0.75f);
            Assert.AreEqual(0.75f, view.FillAmount, 0.001f);
            Assert.AreEqual("75%", view.PercentText);
            Assert.IsTrue(view.PercentVisible);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetProgress_HidesPercentNearZero()
        {
            var go = new GameObject("LoadingScreenViewTestZero");
            var view = go.AddComponent<LoadingScreenView>();
            view.BuildForTests();
            view.SetProgress(0f);
            Assert.IsFalse(view.PercentVisible);
            Object.DestroyImmediate(go);
        }
    }
}
```

- [ ] **Step 2: Run — FAIL (type missing)**

- [ ] **Step 3: Implement view**

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        public const float BarWidth = 720f;
        public const float BarHeight = 36f;
        private static readonly Color NeonPink = new Color(1f, 0.306f, 0.784f, 1f);
        private static readonly Color FillWhite = new Color(1f, 0.92f, 0.96f, 1f);

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fill;
        [SerializeField] private Text percentLabel;
        [SerializeField] private Text loadingLabel;
        [SerializeField] private RectTransform percentRect;
        [SerializeField] private RectTransform clef;
        [SerializeField] private RectTransform notesStars;
        [SerializeField] private Image skyFill;
        [SerializeField] private Image clouds;
        [SerializeField] private Image skyline;
        [SerializeField] private Image buildingsSigns;
        [SerializeField] private Image floor;

        private float _clefPhase;
        private float _notesPhase;

        public float FillAmount => fill != null ? fill.fillAmount : 0f;
        public string PercentText => percentLabel != null ? percentLabel.text : string.Empty;
        public bool PercentVisible => percentLabel != null && percentLabel.gameObject.activeSelf;
        public CanvasGroup Group => canvasGroup;

        public void Bind(
            CanvasGroup group,
            Image fillImage,
            Text percent,
            Text loading,
            RectTransform percentTransform,
            RectTransform clefTransform,
            RectTransform notesTransform)
        {
            canvasGroup = group;
            fill = fillImage;
            percentLabel = percent;
            loadingLabel = loading;
            percentRect = percentTransform;
            clef = clefTransform;
            notesStars = notesTransform;
        }

        public void BindLayers(Image sky, Image cloudImage, Image skylineImage, Image buildings, Image floorImage)
        {
            skyFill = sky;
            clouds = cloudImage;
            skyline = skylineImage;
            buildingsSigns = buildings;
            floor = floorImage;
        }

        public void BuildForTests()
        {
            canvasGroup = gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(transform, false);
            fill = fillGo.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            var percentGo = new GameObject("Percent", typeof(RectTransform), typeof(Text));
            percentGo.transform.SetParent(transform, false);
            percentLabel = percentGo.GetComponent<Text>();
            percentLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            percentRect = percentGo.GetComponent<RectTransform>();
            SetProgress(0f);
        }

        public void SetProgress(float normalized01)
        {
            var p = Mathf.Clamp01(normalized01);
            if (fill != null)
            {
                fill.fillAmount = p;
            }

            if (percentLabel != null)
            {
                percentLabel.text = $"{Mathf.RoundToInt(p * 100f)}%";
                var show = p >= LoadingProgress.PercentVisibleMin;
                if (percentLabel.gameObject.activeSelf != show)
                {
                    percentLabel.gameObject.SetActive(show);
                }
            }

            if (percentRect != null)
            {
                var x = Mathf.Lerp(24f, BarWidth - 40f, p);
                percentRect.anchoredPosition = new Vector2(x, 0f);
            }
        }

        public void SetVisible(bool visible, bool instant)
        {
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.alpha = visible && instant ? 1f : instant ? 0f : canvasGroup.alpha;
            canvasGroup.blocksRaycasts = visible;
            canvasGroup.interactable = false;
            if (!visible && instant)
            {
                canvasGroup.alpha = 0f;
            }
        }

        public void TickMotion(float unscaledDeltaTime)
        {
            _clefPhase += unscaledDeltaTime * (Mathf.PI * 2f / 2.4f);
            _notesPhase += unscaledDeltaTime * (Mathf.PI * 2f / 3.5f);
            if (clef != null)
            {
                var s = Mathf.Lerp(0.97f, 1.03f, (Mathf.Sin(_clefPhase) + 1f) * 0.5f);
                clef.localScale = new Vector3(s, s, 1f);
            }

            if (notesStars != null)
            {
                var y = Mathf.Sin(_notesPhase) * 6f;
                notesStars.anchoredPosition = new Vector2(notesStars.anchoredPosition.x, y);
            }
        }
    }
}
```

`SetVisible` logic: caller fades alpha. `SetVisible(true, false)` only sets `blocksRaycasts`. Implement:

```csharp
public void SetVisible(bool visible, bool instant)
{
    if (canvasGroup == null)
    {
        return;
    }

    canvasGroup.blocksRaycasts = visible;
    canvasGroup.interactable = false;
    if (instant)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
    }
}
```

Dùng bản thứ hai (không phải ternary rối).

- [ ] **Step 4: Tests PASS**

- [ ] **Step 5: Commit**

```
git add Assets/FracturedChorus/UI/Loading/LoadingScreenView.cs Assets/FracturedChorus/UI/Loading/LoadingScreenView.cs.meta Assets/FracturedChorus/Editor/LoadingProgressTests.cs
git commit -m "Add loading bar view with live fill and percent."
```

Nếu tests nằm file riêng: add đúng path.

---

### Task 4: LoadingScreenController + async load

**Files:**
- Create: `Assets/FracturedChorus/UI/Loading/LoadingScreenController.cs`
- Modify: `Assets/FracturedChorus/Editor/LoadingProgressTests.cs` — busy tests
- Modify: `Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs` — `LoadByName` ủy quyền controller

**Interfaces:**
- Consumes: `LoadingProgress.*`, `LoadingScreenView`, `RunMapSceneLoader.CanLoad` / `ResolveScenePath`
- Produces: `static LoadingScreenController Ensure()`, `static bool IsBusy`, `bool BeginLoad(string sceneName, LoadSceneMode mode)`

- [ ] **Step 1: Failing busy tests**

```csharp
using FracturedChorus.UI.Loading;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class LoadingScreenControllerBusyTests
    {
        [TearDown]
        public void TearDown()
        {
            if (LoadingScreenController.Instance != null)
            {
                Object.DestroyImmediate(LoadingScreenController.Instance.gameObject);
            }
        }

        [Test]
        public void BeginLoad_Empty_ReturnsFalse()
        {
            var c = LoadingScreenController.Ensure();
            Assert.IsFalse(c.BeginLoad(" ", LoadSceneMode.Single));
            Assert.IsFalse(LoadingScreenController.IsBusy);
        }

        [Test]
        public void BeginLoad_Unknown_ReturnsFalse()
        {
            var c = LoadingScreenController.Ensure();
            Assert.IsFalse(c.BeginLoad("DefinitelyMissingScene_XYZ", LoadSceneMode.Single));
            Assert.IsFalse(LoadingScreenController.IsBusy);
        }
    }
}
```

Cần `using UnityEngine.SceneManagement`.

- [ ] **Step 2: Run — FAIL**

- [ ] **Step 3: Implement controller**

```csharp
using System;
using System.Collections;
using FracturedChorus.RunMap;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.UI.Loading
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        public const string ResourcesPath = "UI/LoadingScreen";

        public static LoadingScreenController Instance { get; private set; }
        public static bool IsBusy => Instance != null && Instance._busy;

        [SerializeField] private LoadingScreenView view;
        [SerializeField] private CanvasGroup canvasGroup;

        private bool _busy;
        private Coroutine _loadRoutine;
        private float _displayedFill;
        private float _fillVelocity;

        public static LoadingScreenController Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var prefab = Resources.Load<GameObject>(ResourcesPath);
            if (prefab != null)
            {
                var spawned = Instantiate(prefab);
                spawned.name = "LoadingScreen";
                DontDestroyOnLoad(spawned);
                var controller = spawned.GetComponent<LoadingScreenController>();
                if (controller == null)
                {
                    controller = spawned.AddComponent<LoadingScreenController>();
                }

                return controller;
            }

            var go = new GameObject("LoadingScreen");
            DontDestroyOnLoad(go);
            return go.AddComponent<LoadingScreenController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureView();
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_busy)
            {
                view?.TickMotion(Time.unscaledDeltaTime);
            }
        }

        public bool BeginLoad(string sceneName, LoadSceneMode mode)
        {
            if (_busy)
            {
                return false;
            }

            if (!RunMapSceneLoader.CanLoad(sceneName))
            {
                return false;
            }

            _busy = true;
            _displayedFill = 0f;
            _fillVelocity = 0f;
            if (_loadRoutine != null)
            {
                StopCoroutine(_loadRoutine);
            }

            _loadRoutine = StartCoroutine(LoadRoutine(sceneName, mode));
            return true;
        }

        private IEnumerator LoadRoutine(string sceneName, LoadSceneMode mode)
        {
            EnsureView();
            view.SetProgress(0f);
            view.SetVisible(true, false);
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;

            yield return FadeTo(1f, LoadingProgress.FadeInSec);

            var holdStart = Time.unscaledTime;
            AsyncOperation op = null;
            try
            {
                var buildIndex = SceneUtility.GetBuildIndexByScenePath(RunMapSceneLoader.ResolveScenePath(sceneName));
                op = buildIndex >= 0
                    ? SceneManager.LoadSceneAsync(buildIndex, mode)
                    : SceneManager.LoadSceneAsync(sceneName, mode);
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] LoadingScreen LoadSceneAsync failed: {error}");
                FinishFail();
                yield break;
            }

            if (op == null)
            {
                Debug.LogError($"[Fractured Chorus] LoadSceneAsync returned null for '{sceneName}'.");
                FinishFail();
                yield break;
            }

            op.allowSceneActivation = false;

            while (!LoadingProgress.CanActivate(_displayedFill, Time.unscaledTime - holdStart))
            {
                var target = LoadingProgress.MapAsyncProgress(op.progress);
                _displayedFill = Mathf.SmoothDamp(
                    _displayedFill,
                    target,
                    ref _fillVelocity,
                    LoadingProgress.SmoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);
                if (target >= 1f && _displayedFill < 0.995f && Time.unscaledTime - holdStart >= LoadingProgress.MinHoldSec - 0.05f)
                {
                    _displayedFill = Mathf.Max(_displayedFill, 0.995f);
                }

                view.SetProgress(_displayedFill);
                yield return null;
            }

            view.SetProgress(1f);
            op.allowSceneActivation = true;
            while (!op.isDone)
            {
                yield return null;
            }

            yield return FadeTo(0f, LoadingProgress.FadeOutSec);
            HideImmediate();
            _busy = false;
            _loadRoutine = null;
        }

        private IEnumerator FadeTo(float alpha, float duration)
        {
            var start = canvasGroup.alpha;
            if (duration <= 0.01f)
            {
                canvasGroup.alpha = alpha;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, alpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = alpha;
        }

        private void FinishFail()
        {
            HideImmediate();
            _busy = false;
            _loadRoutine = null;
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            view?.SetVisible(false, true);
            view?.SetProgress(0f);
        }

        private void EnsureView()
        {
            if (view == null)
            {
                view = GetComponent<LoadingScreenView>() ?? gameObject.AddComponent<LoadingScreenView>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            }

            view.Bind(
                canvasGroup,
                view.GetComponentInChildren<Image>(true),
                null,
                null,
                null,
                null,
                null);
        }
    }
}
```

`EnsureView` bind Image lung tung nếu chưa có prefab — **Task 6** builder gán serialize đầy đủ. Cho EditMode tests `BeginLoad` empty/unknown: `CanLoad` false **trước** khi start coroutine, không cần Image. Sửa `BeginLoad` để **không** gọi `EnsureView` trước `CanLoad` (đã đúng).

`Ensure()` EditMode: `StartCoroutine` không chạy trừ Play Mode. Tests empty/unknown return false trước coroutine — OK.

Sửa `RunMapSceneLoader.LoadByName`:

```csharp
public static bool LoadByName(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
{
    if (string.IsNullOrWhiteSpace(sceneName))
    {
        Debug.LogError("[Fractured Chorus] RunMapSceneLoader: scene name rỗng.");
        return false;
    }

    if (FracturedChorus.UI.Loading.LoadingScreenController.IsBusy)
    {
        return false;
    }

    if (!CanLoad(sceneName))
    {
        Debug.LogError(
            $"[Fractured Chorus] Không load được scene '{sceneName}'. " +
            $"Thêm scene vào File → Build Settings.");
        return false;
    }

    return FracturedChorus.UI.Loading.LoadingScreenController.Ensure().BeginLoad(sceneName, mode);
}
```

Không còn `SceneManager.LoadScene` trong file này.

- [ ] **Step 4: Busy tests PASS**

- [ ] **Step 5: Commit**

```
git add Assets/FracturedChorus/UI/Loading/LoadingScreenController.cs Assets/FracturedChorus/UI/Loading/LoadingScreenController.cs.meta Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs Assets/FracturedChorus/Editor/LoadingProgressTests.cs
git commit -m "Route scene loads through async loading overlay."
```

---

### Task 5: Slice tấm 2 → PNG layers

**Files:**
- Create: `Assets/FracturedChorus/Editor/LoadingScreenArtImportEditor.cs`
- Create: `Assets/FracturedChorus/Art/UI/LoadingScreen/` PNGs (output)
- Create: `Assets/FracturedChorus/Editor/LoadingScreenArtImportTests.cs`

**Interfaces:**
- Consumes: JPEG nguồn (copy vào `_source`)
- Produces: `loading_clouds.png`, `loading_notes_stars.png`, `loading_skyline.png`, `loading_buildings_signs.png`, `loading_clef.png`, `loading_floor.png` — Sprite 2D UI, Bilinear, Max 2048, no mips, alphaIsTransparency.

Nguồn:

`C:\Users\Asus\.cursor\projects\d-Fractured-Chorus1\assets\c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_Loading_Screen_Part-421ee341-baf0-4f90-b671-0648ba5ede20.png`

Copy thêm wish JPEG vào `_source/loading_screen_wish.jpg` (QA only, không gán Image runtime).

- [ ] **Step 1: Failing test (files missing)**

```csharp
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class LoadingScreenArtImportTests
    {
        private static readonly string[] Paths =
        {
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_clouds.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_notes_stars.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_skyline.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_buildings_signs.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_clef.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_floor.png"
        };

        [Test]
        public void LayerPngs_ExistAsSprites()
        {
            foreach (var path in Paths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.IsNotNull(sprite, path);
            }
        }
    }
}
```

- [ ] **Step 2: Run — FAIL missing sprites**

- [ ] **Step 3: Implement import editor + chạy menu**

`LoadingScreenArtImportEditor`:

1. `File.Copy` sheet → `Assets/FracturedChorus/Art/UI/LoadingScreen/_source/loading_screen_part.jpg`
2. `Texture2D.LoadImage(bytes)`
3. Pixel `max(r,g,b)*255 < 18` → alpha 0
4. Connected components 4-way, bỏ blob area < 80
5. Bỏ blob UI bar: centroidY > 0.78h && centroidX > 0.58w && width/height > 2.2
6. Classify:
   - `clef`: blob area lớn nhất trong vùng `x 0.28–0.72`, `y 0.55–1.0` (Unity texture y=0 bottom — **dùng pixel row from top**: `topY = height-1-y`)
   - `clouds`: union blobs `topY < 0.28h` && `x < 0.42w`
   - `floor`: blob area lớn nhất `topY > 0.58h`
   - `skyline`: blob rộng nhất còn lại width > 0.45w
   - `buildings_signs`: union blobs còn lại area > 200 trừ notes nhỏ
   - `notes_stars`: blobs area 80–900, không thuộc nhóm trên
7. Crop union bounds + 4px pad, `EncodeToPNG`, write files
8. `AssetDatabase.ImportAsset` + TextureImporter: `textureType = Sprite`, `spriteMode = Single`, `filterMode = Bilinear`, `mipmapEnabled = false`, `maxTextureSize = 2048`, `alphaIsTransparency = true`

Sky fill: không PNG — Image color `#0a0518` trên prefab (Task 6).

Menu: `Fractured Chorus/Import Loading Screen Art`

Chạy menu trong Editor trước khi re-test.

Nếu heuristic lệch so với tấm 1: chỉnh ngưỡng trong cùng file, chạy lại menu — không bake tấm 1.

- [ ] **Step 4: Tests PASS**

- [ ] **Step 5: Commit** (PNG + editor + tests, **không** commit wish như runtime BG)

```
git add Assets/FracturedChorus/Art/UI/LoadingScreen Assets/FracturedChorus/Editor/LoadingScreenArtImportEditor.cs Assets/FracturedChorus/Editor/LoadingScreenArtImportTests.cs
git commit -m "Slice loading screen layers from the component sheet."
```

---

### Task 6: Prefab Resources/UI/LoadingScreen

**Files:**
- Create: `Assets/FracturedChorus/Editor/LoadingScreenPrefabBuilder.cs`
- Create: `Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab`
- Create: `Assets/FracturedChorus/Editor/LoadingScreenPrefabTests.cs`

**Interfaces:**
- Consumes: sprites Task 5, `LoadingScreenView.Bind` / serialized refs, `LoadingProgress` bar size
- Produces: prefab `Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab` với hierarchy spec §4.1

- [ ] **Step 1: Failing test**

```csharp
using FracturedChorus.UI.Loading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tests
{
    public class LoadingScreenPrefabTests
    {
        private const string Path = "Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab";

        [Test]
        public void Prefab_HasControllerViewAndBar()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<LoadingScreenController>());
            Assert.IsNotNull(prefab.GetComponent<LoadingScreenView>());
            var canvas = prefab.GetComponentInChildren<Canvas>(true);
            Assert.IsNotNull(canvas);
            Assert.AreEqual(500, canvas.sortingOrder);
            Assert.IsNotNull(prefab.GetComponentInChildren<CanvasGroup>(true));
            var fill = Find(prefab.transform, "Fill");
            Assert.IsNotNull(fill);
            Assert.AreEqual(Image.Type.Filled, fill.GetComponent<Image>().type);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }
    }
}
```

- [ ] **Step 2: FAIL missing prefab**

- [ ] **Step 3: Builder — menu `Fractured Chorus/Build Loading Screen Prefab`**

Hierarchy + Rect (anchor stretch trừ UiGroup):

```
LoadingScreen (RectTransform full, Canvas Overlay, CanvasScaler 1920×1080, GraphicRaycaster, CanvasGroup, LoadingScreenController, LoadingScreenView)
└── Canvas (nếu controller ở root: Canvas trên cùng root, không lồng Canvas thứ hai)
    SkyFill     stretch, color (10/255, 5/255, 24/255, 1)
    Clouds      anchor top stretch, height 280, sprite loading_clouds
    NotesStars  center, size 900×400, sprite loading_notes_stars
    Skyline     stretch mid, offsetMin.y=220, offsetMax.y=-80, sprite loading_skyline
    BuildingsSigns stretch, sprite loading_buildings_signs
    Clef        center, size 520×640, pos y=40, sprite loading_clef
    Floor       bottom stretch, height 380, sprite loading_floor
    UiGroup     anchor (0.5, 0.12), size 720×80
        Label   y=28, "LOADING...", white, bold 28, UiFontCatalog.Body
        Bar     y=-8, size 720×36
            Track Image sliced/simple, color NeonPink, Outline color (1,0.3,0.78,0.55)
            Fill  Image Filled Horizontal, color (1,0.92,0.96,1), raycastTarget false
            PercentLabel font 18 bold white, anchor left-center
```

Root **là** Canvas (một Canvas). `sortingOrder = 500`. Wire `LoadingScreenController` serialized `view` + `canvasGroup`. `LoadingScreenView.Bind(...)` + `BindLayers` rồi ApplyModifiedProperties.

Load sprites:

`AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FracturedChorus/Art/UI/LoadingScreen/loading_clef.png")` v.v.

`PrefabUtility.SaveAsPrefabAsset(root, "Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab")` rồi DestroyImmediate root.

- [ ] **Step 4: Tests PASS**

- [ ] **Step 5: Commit**

```
git add Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab.meta Assets/FracturedChorus/Editor/LoadingScreenPrefabBuilder.cs Assets/FracturedChorus/Editor/LoadingScreenPrefabTests.cs
git commit -m "Build loading screen prefab from sliced layers."
```

---

### Task 7: Combat retry + Main Menu bỏ fade đen

**Files:**
- Modify: `Assets/FracturedChorus/Combat/Core/CombatController.cs` — `OnResultRetry`
- Modify: `Assets/FracturedChorus/Menu/MainMenuStartGameController.cs` — `BeginNewGameRoutine`, `LoadGameRoutine`
- Modify: `Assets/FracturedChorus/Editor/LoadingProgressTests.cs` — source grep test

**Interfaces:**
- Consumes: `RunMapSceneLoader.LoadByName`
- Produces: không còn `SceneManager.LoadScene(` trong gameplay scripts

- [ ] **Step 1: Failing grep test**

```csharp
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class LoadingScreenNoSyncLoadTests
    {
        [Test]
        public void GameplayScripts_DoNotCallSyncLoadScene()
        {
            var root = Path.Combine(Application.dataPath, "FracturedChorus");
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.Replace('\\', '/').Contains("/Editor/"))
                {
                    continue;
                }

                if (file.Replace('\\', '/').EndsWith("LoadingScreenController.cs"))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.IsFalse(
                    text.Contains("SceneManager.LoadScene(") && !text.Contains("LoadSceneAsync"),
                    file);
            }
        }
    }
}
```

Điệu kiện: flag sync nếu có `SceneManager.LoadScene(` mà không phải `LoadSceneAsync`. `LoadScene(` khớp cả `LoadSceneAsync` vì prefix — **sửa test**:

```csharp
Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text, @"SceneManager\.LoadScene\s*\("), file);
```

`LoadSceneAsync` không match `LoadScene\s*\(`.

- [ ] **Step 2: FAIL trên `CombatController.OnResultRetry` (và loader nếu Task 4 chưa xóa hết — Task 4 đã xóa)**

- [ ] **Step 3: Combat retry**

```csharp
private void OnResultRetry()
{
    var sceneName = SceneManager.GetActiveScene().name;
    if (!RunMapSceneLoader.LoadByName(sceneName))
    {
        Debug.LogError($"[Combat] Retry failed to load '{sceneName}'.");
    }
}
```

Giữ `using UnityEngine.SceneManagement` cho `GetActiveScene`.

Main menu — thay hai coroutine:

```csharp
private IEnumerator BeginNewGameRoutine()
{
    _transitioning = true;
    menuController?.SetEnabled(false);
    HideSettingsImmediate();
    HideOffBeatArchiveImmediate();
    PlayAttractTransitionSfx();
    _bgmController?.Duck(bgmDuckMultiplier);
    if (!RunMapSceneLoader.LoadByName(RunMapSceneCatalog.PrologueVN))
    {
        _transitioning = false;
        menuController?.SetEnabled(true);
    }

    yield break;
}

private IEnumerator LoadGameRoutine(int slot)
{
    _transitioning = true;
    menuController?.SetEnabled(false);
    HideSettingsImmediate();
    HideOffBeatArchiveImmediate();
    PlayAttractTransitionSfx();
    _bgmController?.Duck(bgmDuckMultiplier);
    GameMetaSession.LoadSlot(slot);
    var state = GameMetaSession.Current;
    var sceneName = ResolveLoadScene(state);
    if (!RunMapSceneLoader.LoadByName(sceneName))
    {
        _transitioning = false;
        menuController?.SetEnabled(true);
    }

    yield break;
}
```

Có thể đổi `BeginNewGame`/`LoadGame` gọi load trực tiếp không coroutine — giữ coroutine + `yield break` để ít đụng signature. SFX vẫn phát.

- [ ] **Step 4: Grep test PASS**

- [ ] **Step 5: Commit**

```
git add Assets/FracturedChorus/Combat/Core/CombatController.cs Assets/FracturedChorus/Menu/MainMenuStartGameController.cs Assets/FracturedChorus/Editor/LoadingProgressTests.cs
git commit -m "Cover combat retry and menu start with the loading overlay."
```

---

### Task 8: Play Mode checklist (không tự động hóa hết)

**Files:**
- Create: `Assets/FracturedChorus/Scenes/LOADING_SCREEN.md` (ngắn, giống style `MainMenuStartGame_Setup.md`)

- [ ] **Step 1: Write setup note**

Nội dung bắt buộc:

- Prefab `Resources/UI/LoadingScreen`
- Menu Editor: Import Art → Build Prefab
- Play checklist spec §8 (NEW GAME, LOAD, Hub↔map↔combat, retry, fail name, double-click, min 0.8s)
- Look QA: so tấm 1 (clef center, floor dưới, bar lower-third)

- [ ] **Step 2: Chạy Play Mode tay theo checklist; sửa layout Rect trên prefab nếu lệch tấm 1 (chỉ Rect/scale, không thay logic)**

- [ ] **Step 3: Commit doc**

```
git add Assets/FracturedChorus/Scenes/LOADING_SCREEN.md
git commit -m "Document loading screen Play Mode checks."
```

---

## Spec coverage

| Spec | Task |
|------|------|
| Overlay DDOL, không Additive scene | 4, 6 |
| LoadByName → controller async | 4 |
| bool false busy / empty / missing | 2, 4 |
| Timing 0.2 / 0.25 / 0.8 / 0.12 / 0.9 map | 1, 4 |
| Slice tấm 2, không BG tấm 1, bar uGUI | 3, 5, 6 |
| Hierarchy + bar look | 3, 6 |
| Combat retry | 7 |
| Menu bỏ fade đen | 7 |
| Fail giữ scene | 4 (`FinishFail`) |
| Play tests | 8 |
| Không LoadScene gameplay khác controller | 7 grep |

## Self-review

- Không TBD.
- `LoadingScreenController.EnsureView` bind Image tạm — Task 6 prefab ghi serialized fields; sau Task 6 Awake dùng inspector refs, Bind chỉ fallback.
- `CanLoad_KnownScenes` cần scene trong Build Settings (đã có theo setup docs).
- Grep test loại trừ `LoadingScreenController` và Editor.
