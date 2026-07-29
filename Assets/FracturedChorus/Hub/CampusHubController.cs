using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Meta;
using FracturedChorus.Meta.Economy;
using FracturedChorus.RunMap;
using FracturedChorus.Tutorial;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.Hub
{
    public sealed class CampusHubController : MonoBehaviour
    {
        public enum CampusHubEditorPreview
        {
            Morning = 0,
            TownDay = 1,
            TownNight = 2,
            District = 3,
            StatusMenu = 4,
            Calendar = 5
        }

        [SerializeField] private Image backgroundImage;
        [SerializeField] private CalendarUIView calendarView;
        [SerializeField] private CalendarSlashBanner slashBanner;
        [SerializeField] private MorningBeatUI morningBeatUi;
        [SerializeField] private TownMapView townMapView;
        [SerializeField] private Text statusLabel;
        [SerializeField] private bool beginHubAfterPrologue = true;
#if UNITY_EDITOR
        [SerializeField] private CampusHubEditorPreview editorPreview = CampusHubEditorPreview.TownDay;
#endif

        private HubPhaseDriver _phaseDriver;

        private void Awake()
        {
            ResolveMissingRefs();
            _phaseDriver = new HubPhaseDriver(this, morningBeatUi, townMapView, calendarView, slashBanner);
        }

        private void Start()
        {
            try
            {
                EnsureSession();
                if (GameMetaSession.Current.RunSnapshot.HasActiveRun)
                {
                    GameMetaSession.Current.RunSnapshot.HasActiveRun = false;
                    GameMetaSession.Save();
                }

                _phaseDriver.BeginCurrentPhase();
                TutorialDirector.Ensure().StartHubTrack();
                var canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    NotesHudView.Ensure(canvas.transform);
                    EnsureTutorialCombatHotkey(canvas.transform);
                }
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[Fractured Chorus] CampusHub start failed: {error}");
                ShowStatus("Không thể khởi tạo campus hub.");
            }
        }

        private void EnsureTutorialCombatHotkey(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return;
            }

            var overlay = SceneLinkHotkeyUI.EnsureSceneLinkOverlay(canvasRoot);
            var link = SceneLinkHotkeyUI.Ensure(
                overlay != null ? overlay : canvasRoot,
                "Tutorial Fight",
                LaunchTutorialCombat,
                objectName: "TutorialCombatHotkey",
                placement: SceneLinkHotkeyPlacement.TopRight,
                persistInScene: false);
            link?.SetListening(false);
        }

        private void LaunchTutorialCombat()
        {
            try
            {
                CombatEncounterHandoff.SetPending(
                    EncounterCatalog.Tutorial,
                    RunMapSceneCatalog.CampusHub);
                if (!RunMapSceneLoader.LoadCombatTutorial())
                {
                    Debug.LogError("[Fractured Chorus] Failed to load CombatTutorial for tutorial fight.");
                    ShowStatus("Không thể mở tutorial combat.");
                }
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Tutorial combat launch failed: {error}");
                ShowStatus("Không thể mở tutorial combat.");
            }
        }

        public bool TryHubHealService()
        {
            if (!GameMetaSession.HasSession)
            {
                return false;
            }

            var wallet = GameMetaSession.Current.Wallet;
            if (!wallet.Spend(EconomyTable.HubHealCost))
            {
                ShowStatus($"Need {EconomyTable.HubHealCost} Notes for clinic heal.");
                return false;
            }

            PartyRunHpStore.RestoreFullAtCamp();
            GameMetaSession.Save();
            NotesHudView.Ensure(Object.FindAnyObjectByType<Canvas>()?.transform)?.Refresh();
            ShowStatus($"Clinic heal (−{EconomyTable.HubHealCost} Notes).");
            return true;
        }

        public void ShowStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }

        private void ResolveMissingRefs()
        {
            if (morningBeatUi == null)
            {
                morningBeatUi = GetComponentInChildren<MorningBeatUI>(true);
            }

            if (townMapView == null)
            {
                townMapView = GetComponentInChildren<TownMapView>(true);
            }

            if (slashBanner == null)
            {
                slashBanner = GetComponentInChildren<CalendarSlashBanner>(true);
            }

            if (calendarView == null)
            {
                calendarView = GetComponentInChildren<CalendarUIView>(true);
            }

            if (statusLabel == null)
            {
                var status = transform.Find("CampusHubCanvas/StatusLabel");
                if (status != null)
                {
                    statusLabel = status.GetComponent<Text>();
                }
            }
        }

        private void EnsureSession()
        {
            if (!GameMetaSession.HasSession)
            {
                if (beginHubAfterPrologue)
                {
                    GameMetaSession.BeginHubAfterPrologue();
                }
                else
                {
                    GameMetaSession.Load();
                }

                return;
            }

            GameMetaSession.Load();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += ApplyEditorPreviewDeferred;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EditorApplication.delayCall += ApplyEditorPreviewDeferred;
        }

        private void ApplyEditorPreviewDeferred()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            ApplyEditorPreview();
        }

        public void SetEditorPreview(CampusHubEditorPreview preview)
        {
            editorPreview = preview;
            ApplyEditorPreview();
        }

        public void ApplyEditorPreview()
        {
            ResolveMissingRefs();

            switch (editorPreview)
            {
                case CampusHubEditorPreview.Morning:
                    SetMorningEditorVisible(true);
                    SetTownMapEditorVisible(false, night: false);
                    break;
                case CampusHubEditorPreview.TownDay:
                    SetMorningEditorVisible(false);
                    SetTownMapEditorVisible(true, night: false);
                    HideTownMapOverlays();
                    break;
                case CampusHubEditorPreview.TownNight:
                    SetMorningEditorVisible(false);
                    SetTownMapEditorVisible(true, night: true);
                    HideTownMapOverlays();
                    break;
                case CampusHubEditorPreview.District:
                    SetMorningEditorVisible(false);
                    SetTownMapEditorVisible(true, night: false);
                    HideStatusMenuEditor();
                    HideCalendarEditor();
                    ShowDistrictEditorPreview();
                    break;
                case CampusHubEditorPreview.StatusMenu:
                    SetMorningEditorVisible(false);
                    SetTownMapEditorVisible(true, night: false);
                    HideDistrictEditor();
                    HideCalendarEditor();
                    ShowStatusMenuEditorPreview();
                    break;
                case CampusHubEditorPreview.Calendar:
                    SetMorningEditorVisible(false);
                    SetTownMapEditorVisible(true, night: false);
                    HideDistrictEditor();
                    ShowCalendarEditorPreview();
                    break;
            }
        }

        private void SetMorningEditorVisible(bool visible)
        {
            if (morningBeatUi == null)
            {
                return;
            }

            morningBeatUi.gameObject.SetActive(visible);
            EditorUtility.SetDirty(morningBeatUi);
        }

        private void SetTownMapEditorVisible(bool visible, bool night)
        {
            if (townMapView == null)
            {
                return;
            }

            townMapView.gameObject.SetActive(visible);
            if (visible)
            {
                ApplyTownMapBackground(night);
            }

            EditorUtility.SetDirty(townMapView);
        }

        private void ApplyTownMapBackground(bool night)
        {
            if (townMapView == null)
            {
                return;
            }

            var mapRoot = townMapView.transform.Find("MapRoot");
            if (mapRoot == null)
            {
                return;
            }

            var day = mapRoot.Find("DayBackground");
            var nightBackground = mapRoot.Find("NightBackground");
            if (day != null)
            {
                day.gameObject.SetActive(!night);
            }

            if (nightBackground != null)
            {
                nightBackground.gameObject.SetActive(night);
            }
        }

        private void HideTownMapOverlays()
        {
            HideDistrictEditor();
            HideStatusMenuEditor();
            HideCalendarEditor();
        }

        private void HideDistrictEditor()
        {
            var district = ResolveDistrictPanel();
            if (district != null)
            {
                district.Hide();
                EditorUtility.SetDirty(district);
            }
        }

        private void HideStatusMenuEditor()
        {
            var statusMenu = FindStatusMenu();
            if (statusMenu == null)
            {
                return;
            }

            if (statusMenu.IsCalendarOpen)
            {
                FindCalendar()?.Hide();
            }

            statusMenu.gameObject.SetActive(false);
            EditorUtility.SetDirty(statusMenu);
        }

        private void HideCalendarEditor()
        {
            var calendar = FindCalendar();
            if (calendar != null)
            {
                calendar.Hide();
                EditorUtility.SetDirty(calendar);
            }
        }

        private void ShowDistrictEditorPreview()
        {
            var district = ResolveDistrictPanel();
            if (district == null)
            {
                return;
            }

            var locations = TownLocationCatalog.CreateDefault();
            if (locations.Length == 0)
            {
                return;
            }

            district.Show(locations[0], DayPhase.Day, null, null);
            EditorUtility.SetDirty(district);
        }

        private void ShowStatusMenuEditorPreview()
        {
            var statusMenu = EnsureStatusMenuForPreview();
            if (statusMenu == null)
            {
                return;
            }

            HideCalendarEditor();
            statusMenu.Show(CreatePreviewMetaState());
            EditorUtility.SetDirty(statusMenu);
        }

        private void ShowCalendarEditorPreview()
        {
            var statusMenu = EnsureStatusMenuForPreview();
            if (statusMenu != null)
            {
                statusMenu.gameObject.SetActive(false);
                EditorUtility.SetDirty(statusMenu);
            }

            var calendar = EnsureCalendarForPreview();
            if (calendar == null)
            {
                return;
            }

            calendar.transform.SetAsLastSibling();
            calendar.Show(CreatePreviewMetaState());
            EditorUtility.SetDirty(calendar);
        }

        private static GameMetaState CreatePreviewMetaState()
        {
            var state = GameMetaState.CreateHubStart();
            state.Calendar.CurrentDate = new GameDate(9, 12);
            state.Calendar.CurrentPhase = DayPhase.Day;
            state.Flags.SetBool(StoryFlagIds.VaultQuestActive, true);
            return state;
        }

        private DistrictSelectPanel ResolveDistrictPanel()
        {
            return townMapView != null
                ? townMapView.GetComponentInChildren<DistrictSelectPanel>(true)
                : null;
        }

        private MetaStatusMenuUI FindStatusMenu()
        {
            if (townMapView == null)
            {
                return null;
            }

            var existing = townMapView.transform.Find("StatusMenu");
            return existing != null ? existing.GetComponent<MetaStatusMenuUI>() : null;
        }

        private CalendarOverlayUI FindCalendar()
        {
            if (townMapView == null)
            {
                return null;
            }

            var existing = townMapView.transform.Find("CalendarOverlay");
            return existing != null ? existing.GetComponent<CalendarOverlayUI>() : null;
        }

        private MetaStatusMenuUI EnsureStatusMenuForPreview()
        {
            var existing = FindStatusMenu();
            if (existing != null)
            {
                return existing;
            }

            if (townMapView == null || Application.isPlaying)
            {
                return null;
            }

            var built = MetaStatusMenuUI.Build(townMapView.transform);
            var so = new SerializedObject(townMapView);
            so.FindProperty("menuButton").objectReferenceValue = built.MenuButton;
            so.FindProperty("statusMenu").objectReferenceValue = built.Menu;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(townMapView);
            return built.Menu;
        }

        private CalendarOverlayUI EnsureCalendarForPreview()
        {
            var existing = FindCalendar();
            if (existing != null)
            {
                return existing;
            }

            if (townMapView == null || Application.isPlaying)
            {
                return null;
            }

            var statusMenu = EnsureStatusMenuForPreview();
            if (statusMenu != null)
            {
                statusMenu.EnsureCalendarOverlay(townMapView.transform);
            }

            existing = FindCalendar();
            if (existing != null)
            {
                return existing;
            }

            return CalendarOverlayUI.Build(townMapView.transform).Overlay;
        }
#endif
    }
}
