using System;
using System.Collections;
using FracturedChorus.RunMap;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.UI.Loading
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        public const string ResourcesPath = "UI/LoadingScreen";
        private const int SortingOrder = 500;
        private static readonly Color DimColor = new Color(0.02f, 0.01f, 0.06f, 0.82f);

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
                var spawned = UnityEngine.Object.Instantiate(prefab);
                spawned.name = "LoadingScreen";
                UnityEngine.Object.DontDestroyOnLoad(spawned);

                var controller = spawned.GetComponent<LoadingScreenController>();
                if (controller == null)
                {
                    controller = spawned.AddComponent<LoadingScreenController>();
                }

                return controller;
            }

            var fallback = BuildRuntimeHierarchy();
            UnityEngine.Object.DontDestroyOnLoad(fallback.gameObject);
            return fallback;
        }

        public static LoadingScreenController BuildRuntimeHierarchy()
        {
            var root = new GameObject("LoadingScreen", typeof(RectTransform));
            root.SetActive(false);

            var rootRect = root.GetComponent<RectTransform>();
            StretchFull(rootRect);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            var canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var skyFill = CreateImage(root.transform, "SkyFill");
            StretchFull(skyFill.rectTransform);
            skyFill.color = DimColor;

            var uiGroup = CreateRect(root.transform, "UiGroup");
            uiGroup.anchorMin = new Vector2(0.5f, 0.12f);
            uiGroup.anchorMax = new Vector2(0.5f, 0.12f);
            uiGroup.pivot = new Vector2(0.5f, 0.5f);
            uiGroup.sizeDelta = new Vector2(720f, 80f);
            uiGroup.anchoredPosition = Vector2.zero;

            var loadingLabel = CreateText(uiGroup, "Label");
            loadingLabel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            loadingLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            loadingLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            loadingLabel.rectTransform.sizeDelta = new Vector2(720f, 32f);
            loadingLabel.rectTransform.anchoredPosition = new Vector2(0f, 28f);
            loadingLabel.text = "LOADING...";
            loadingLabel.alignment = TextAnchor.MiddleCenter;
            loadingLabel.fontSize = 28;
            loadingLabel.fontStyle = FontStyle.Bold;

            var bar = CreateRect(uiGroup, "Bar");
            bar.anchorMin = new Vector2(0.5f, 0.5f);
            bar.anchorMax = new Vector2(0.5f, 0.5f);
            bar.pivot = new Vector2(0.5f, 0.5f);
            bar.sizeDelta = new Vector2(LoadingScreenView.BarWidth, LoadingScreenView.BarHeight);
            bar.anchoredPosition = new Vector2(0f, -8f);

            var track = CreateImage(bar, "Track");
            StretchFull(track.rectTransform);
            track.type = Image.Type.Sliced;
            track.gameObject.AddComponent<Outline>();

            var fill = CreateImage(bar, "Fill");
            fill.raycastTarget = false;
            fill.type = Image.Type.Sliced;

            var percentLabel = CreateText(fill.rectTransform, "PercentLabel");
            percentLabel.alignment = TextAnchor.MiddleRight;
            percentLabel.fontSize = 16;
            percentLabel.fontStyle = FontStyle.Normal;
            percentLabel.text = "0%";

            var view = root.AddComponent<LoadingScreenView>();
            var controller = root.AddComponent<LoadingScreenController>();
            view.Bind(
                canvasGroup,
                fill,
                percentLabel,
                loadingLabel,
                percentLabel.rectTransform,
                null,
                null);
            view.BindLayers(skyFill, null, null, null, null);
            view.ApplyChrome();
            view.SetProgress(0f);
            view.SetVisible(false, true);

            controller.view = view;
            controller.canvasGroup = canvasGroup;

            root.SetActive(true);
            return controller;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureView();
            view?.ApplyChrome();
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

            EnsureView();
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
            if (view != null)
            {
                view.PickRandomBackground();
                view.SetProgress(0f);
                view.SetVisible(true, false);
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;

            yield return FadeTo(1f, LoadingProgress.FadeInSec);

            var holdStart = Time.unscaledTime;
            AsyncOperation operation;
            try
            {
                var scenePath = RunMapSceneLoader.ResolveScenePath(sceneName);
                var buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
                operation = buildIndex >= 0
                    ? SceneManager.LoadSceneAsync(buildIndex, mode)
                    : SceneManager.LoadSceneAsync(sceneName, mode);
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] LoadingScreen LoadSceneAsync failed: {error}");
                FinishLoad();
                yield break;
            }

            if (operation == null)
            {
                Debug.LogError($"[Fractured Chorus] LoadSceneAsync returned null for '{sceneName}'.");
                FinishLoad();
                yield break;
            }

            operation.allowSceneActivation = false;

            while (!LoadingProgress.CanActivate(_displayedFill, Time.unscaledTime - holdStart))
            {
                var targetFill = LoadingProgress.MapAsyncProgress(operation.progress);
                _displayedFill = Mathf.SmoothDamp(
                    _displayedFill,
                    targetFill,
                    ref _fillVelocity,
                    LoadingProgress.SmoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);

                if (view != null)
                {
                    view.SetProgress(_displayedFill);
                }

                yield return null;
            }

            if (view != null)
            {
                view.SetProgress(1f);
            }

            operation.allowSceneActivation = true;
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return FadeTo(0f, LoadingProgress.FadeOutSec);
            FinishLoad();
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            var startAlpha = canvasGroup.alpha;
            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void FinishLoad()
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

            if (view != null)
            {
                view.SetVisible(false, true);
                view.SetProgress(0f);
            }
        }

        private void EnsureView()
        {
            if (view == null)
            {
                view = GetComponent<LoadingScreenView>();
                if (view == null)
                {
                    view = gameObject.AddComponent<LoadingScreenView>();
                }
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private static Image CreateImage(Transform parent, string name)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text))
                .GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.font = UiFontCatalog.Body;
            return text;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.transform.SetParent(parent, false);
            return rect;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
