using System;
using System.Collections;
using FracturedChorus.RunMap;
using UnityEngine;
using UnityEngine.SceneManagement;

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

            var fallback = new GameObject("LoadingScreen");
            UnityEngine.Object.DontDestroyOnLoad(fallback);
            return fallback.AddComponent<LoadingScreenController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
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
    }
}
