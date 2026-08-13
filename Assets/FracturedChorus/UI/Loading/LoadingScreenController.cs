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
        private static readonly Color SkyFillColor = new Color(10f / 255f, 5f / 255f, 24f / 255f, 1f);
        private static readonly Color TrackColor = new Color(1f, 0.306f, 0.784f, 1f);
        private static readonly Color TrackOutlineColor = new Color(1f, 0.3f, 0.78f, 0.55f);
        private static readonly Color FillColor = new Color(1f, 0.92f, 0.96f, 1f);

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
            skyFill.color = SkyFillColor;

            var clouds = CreateImage(root.transform, "Clouds");
            SetTopStretch(clouds.rectTransform, 280f);
            clouds.sprite = LoadSpriteAtPath("Assets/FracturedChorus/Art/UI/LoadingScreen/loading_clouds.png");

            var notesStars = CreateImage(root.transform, "NotesStars");
            SetCenteredRect(notesStars.rectTransform, 900f, 400f, 0f, 0f);
            notesStars.sprite = LoadSpriteAtPath("Assets/FracturedChorus/Art/UI/LoadingScreen/loading_notes_stars.png");

            var skyline = CreateImage(root.transform, "Skyline");
            StretchFull(skyline.rectTransform);
            skyline.rectTransform.offsetMin = new Vector2(0f, 220f);
            skyline.rectTransform.offsetMax = new Vector2(0f, -80f);
            skyline.sprite = LoadSpriteAtPath("Assets/FracturedChorus/Art/UI/LoadingScreen/loading_skyline.png");

            var buildingsSigns = CreateImage(root.transform, "BuildingsSigns");
            StretchFull(buildingsSigns.rectTransform);
            buildingsSigns.sprite = LoadSpriteAtPath("Assets/FracturedChorus/Art/UI/LoadingScreen/loading_buildings_signs.png");

            var clef = CreateImage(root.transform, "Clef");
            SetCenteredRect(clef.rectTransform, 520f, 640f, 0f, 40f);
            clef.sprite = LoadSpriteAtPath("Assets/FracturedChorus/Art/UI/LoadingScreen/loading_clef.png");

            var floor = CreateImage(root.transform, "Floor");
            SetBottomStretch(floor.rectTransform, 380f);
            floor.sprite = LoadSpriteAtPath("Assets/FracturedChorus/Art/UI/LoadingScreen/loading_floor.png");

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
            track.color = TrackColor;
            track.type = Image.Type.Simple;
            var outline = track.gameObject.AddComponent<Outline>();
            outline.effectColor = TrackOutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;

            var fill = CreateImage(bar, "Fill");
            StretchFull(fill.rectTransform);
            fill.color = FillColor;
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;

            var percentLabel = CreateText(bar, "PercentLabel");
            percentLabel.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            percentLabel.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            percentLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            percentLabel.rectTransform.sizeDelta = new Vector2(96f, 24f);
            percentLabel.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            percentLabel.alignment = TextAnchor.MiddleLeft;
            percentLabel.fontSize = 18;
            percentLabel.fontStyle = FontStyle.Bold;
            percentLabel.text = "0%";

            var view = root.AddComponent<LoadingScreenView>();
            var controller = root.AddComponent<LoadingScreenController>();
            view.Bind(
                canvasGroup,
                fill,
                percentLabel,
                loadingLabel,
                percentLabel.rectTransform,
                clef.rectTransform,
                notesStars.rectTransform);
            view.BindLayers(skyFill, clouds, skyline, buildingsSigns, floor);
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

        private static void SetCenteredRect(RectTransform rect, float width, float height, float x, float y)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static void SetTopStretch(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void SetBottomStretch(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static Sprite LoadSpriteAtPath(string assetPath)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
            return null;
#endif
        }
    }
}
