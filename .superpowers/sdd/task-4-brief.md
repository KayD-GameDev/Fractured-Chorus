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

