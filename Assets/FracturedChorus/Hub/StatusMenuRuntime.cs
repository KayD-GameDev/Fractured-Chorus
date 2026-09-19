using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Hub.CharacterBuild;
using FracturedChorus.Menu;
using FracturedChorus.Meta;
using FracturedChorus.Narrative;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using FracturedChorus.Tutorial;
using FracturedChorus.UI;
using FracturedChorus.UI.Loading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Hub
{
    /// <summary>
    /// Chủ sở hữu duy nhất của phím ESC ngoài các panel modal: bật/tắt status menu ở mọi scene.
    /// Ưu tiên instance có sẵn trong scene (Campus Hub dựng menu riêng dưới TownMapView), chỉ khi
    /// scene không có mới dựng một instance nằm trên canvas bền vững qua các lần đổi scene.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class StatusMenuRuntime : MonoBehaviour
    {
        private const int OverlaySortingOrder = 700;

        private static StatusMenuRuntime s_instance;

        private Canvas _canvas;
        private MetaStatusMenuUI _fallbackMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            Ensure();
        }

        public static StatusMenuRuntime Ensure()
        {
            if (s_instance != null)
            {
                return s_instance;
            }

            var host = new GameObject(nameof(StatusMenuRuntime));
            DontDestroyOnLoad(host);
            s_instance = host.AddComponent<StatusMenuRuntime>();
            return s_instance;
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        /// <summary>Menu dự phòng sống qua scene, nên phải tự đóng khi đổi scene kẻo nó nằm đè cảnh mới.</summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CombatInputSetup.EnsureEventSystem();
            TutorialDirector.HideOverlay();
            if (!LoadingScreenController.IsBusy)
            {
                LoadingScreenController.HideCoverNow();
            }

            _fallbackMenu?.Hide();
            if (scene.name == RunMapSceneCatalog.MainMenuStartGame)
            {
                TitleScreenUiGuard.SuppressStrayResonanceDiveButtons();
                ClearFallbackMenu();
                return;
            }

            if (scene.name == RunMapSceneCatalog.CampusHub)
            {
                ClearFallbackMenu();
                StartCoroutine(FulfillCampusStatusMenuReturnNextFrame());
            }
        }

        private System.Collections.IEnumerator FulfillCampusStatusMenuReturnNextFrame()
        {
            yield return null;
            if (!HubNavigationEscContext.HasPendingReopenAfterRunMapEsc
                && !TownMapView.OpenStatusMenuOnNextShow)
            {
                yield break;
            }

            var townMap = UnityEngine.Object.FindFirstObjectByType<TownMapView>();
            townMap?.FulfillPendingStatusMenuReturn(GameMetaSession.Current);
        }

        private void Update()
        {
            if (!UiCancelInput.WasPressed())
            {
                return;
            }

            if (IsCharacterBuildActive())
            {
                return;
            }

            if (IsBondsMenuActive())
            {
                if (UiEscapeGate.TryConsumeBackground())
                {
                    var bonds = UnityEngine.Object.FindFirstObjectByType<BondsMenuUI>();
                    if (bonds != null && bonds.TryHandleCancelInput())
                    {
                        return;
                    }

                    BondsMenuUI.ReturnToCampusHub();
                }

                return;
            }

            if (IsRunMapActive())
            {
                if (UiEscapeGate.TryConsumeBackground())
                {
                    _fallbackMenu?.Hide();
                    var cadence = CadenceMapController.Instance;
                    if (cadence != null)
                    {
                        if (cadence.TryHandleCancelInput())
                        {
                            return;
                        }

                        if (cadence.IsInsideVaultRun())
                        {
                            return;
                        }
                    }

                    RunMapHubBridge.ReturnFromRunMapNavigation();
                }

                return;
            }

            var menu = ResolveMenu(createIfMissing: false);

            // Calendar / social là overlay con của status menu và tự xử lý ESC của chúng.
            if (menu != null && (menu.IsCalendarOpen || menu.IsSocialStatsOpen))
            {
                return;
            }

            if (menu != null && menu.IsOpen)
            {
                if (UiEscapeGate.TryConsumeBackground())
                {
                    if (menu.TryNavigateBack())
                    {
                        return;
                    }

                    menu.Hide();
                }

                return;
            }

            if (IsSuppressed() || !UiEscapeGate.TryConsumeBackground())
            {
                return;
            }

            menu = ResolveMenu(createIfMissing: true);
            menu?.Show(GameMetaSession.Current);
        }

        /// <summary>
        /// Menu của scene luôn thắng menu dự phòng: nó đã được nối SFX, overlay lịch và dữ liệu hub.
        /// </summary>
        private MetaStatusMenuUI ResolveMenu(bool createIfMissing)
        {
            var candidates = FindObjectsByType<MetaStatusMenuUI>(FindObjectsInactive.Include);

            foreach (var candidate in candidates)
            {
                if (candidate != null && candidate != _fallbackMenu)
                {
                    return candidate;
                }
            }

            if (_fallbackMenu != null)
            {
                return _fallbackMenu;
            }

            return createIfMissing ? BuildFallbackMenu() : null;
        }

        private MetaStatusMenuUI BuildFallbackMenu()
        {
            EnsureCanvas();
            var built = MetaStatusMenuUI.Build(_canvas.transform);
            _fallbackMenu = built.Menu;

            // Nút MENU góc màn hình là HUD của Campus Hub; ở scene combat hay run map nó chỉ đè lên UI khác.
            if (built.MenuButton != null)
            {
                built.MenuButton.gameObject.SetActive(false);
            }

            return _fallbackMenu;
        }

        private void EnsureCanvas()
        {
            if (_canvas != null)
            {
                return;
            }

            var canvasGo = new GameObject("StatusMenuCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = OverlaySortingOrder;

            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        /// <summary>Những ngữ cảnh mà ESC đã có nghĩa khác, mở status menu vào sẽ giẫm chân.</summary>
        private static bool IsSuppressed()
        {
            if (UiEscapeGate.IsBlocked || LoadingScreenController.IsBusy || IsCharacterBuildActive() || IsBondsMenuActive())
            {
                return true;
            }

            // Disclaimer / VN / contract: ESC không được mở settings trước khi người chơi ký.
            if (!RunProfile.CanOpenPauseMenu)
            {
                return true;
            }

            // Main menu: ESC dùng để lùi khỏi overlay hoặc quay về attract screen.
            if (FindAnyObjectByType<MainMenuStartGameController>() != null)
            {
                return true;
            }

            var district = FindAnyObjectByType<DistrictSelectPanel>();
            if (district != null && district.isActiveAndEnabled && district.gameObject.activeInHierarchy)
            {
                return true;
            }

            // Backlog của visual novel cũng đóng bằng ESC.
            var log = FindAnyObjectByType<VnLogPanelView>();
            return log != null && log.IsOpen;
        }

        private static bool IsCharacterBuildActive()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            return sceneName == CampusBgmPlayer.CharacterBuildScene
                || FindAnyObjectByType<CharacterBuildMenuUI>() != null;
        }

        private static bool IsBondsMenuActive()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            return sceneName == CampusBgmPlayer.BondsScene
                || sceneName == RunMapSceneCatalog.Bonds
                || FindAnyObjectByType<BondsMenuUI>() != null;
        }

        private static bool IsRunMapActive()
        {
            return SceneManager.GetActiveScene().name == RunMapSceneCatalog.RunMapPrototype;
        }

        private void ClearFallbackMenu()
        {
            if (_fallbackMenu == null)
            {
                return;
            }

            var go = _fallbackMenu.gameObject;
            _fallbackMenu = null;
            if (go != null)
            {
                Destroy(go);
            }
        }
    }
}
