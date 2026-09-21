using System;
using System.Collections.Generic;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class TownMapView : MonoBehaviour
    {
        [SerializeField] private RectTransform mapRoot;
        [SerializeField] private Image dayBackground;
        [SerializeField] private Image nightBackground;
        [SerializeField] private TownMapPinView pinTemplate;
        [SerializeField] private DistrictSelectPanel districtPanel;
        [SerializeField] private CalendarSlashBanner slashBanner;
        [SerializeField] private HubCornerInfoHud cornerInfoHud;
        [SerializeField] private Text selectMapTitle;
        [SerializeField] private Text selectMapSubtitle;
        [SerializeField] private Image headerPinImage;
        [SerializeField] private Text wordmarkLabel;
        [SerializeField] private Image wordmarkImage;
        [SerializeField] private TownMapPromptBar promptBar;
        [SerializeField] private TownMapSfxController sfx;
        [SerializeField] private Button menuButton;
        [SerializeField] private MetaStatusMenuUI statusMenu;
        [SerializeField] private SceneLinkHotkeyUI runMapHotkey;

        [Header("P0 Sprites")]
        [SerializeField] private Sprite pinIdle;
        [SerializeField] private Sprite pinSelected;
        [SerializeField] private Sprite iconSchool;
        [SerializeField] private Sprite iconShop;
        [SerializeField] private Sprite iconFlower;
        [SerializeField] private Sprite iconShrine;
        [SerializeField] private Sprite iconVault;
        [SerializeField] private Sprite wordmarkSprite;

        private readonly List<TownMapPinView> _pins = new List<TownMapPinView>();
        private TownLocationDefinition[] _locations;
        private Action<string> _onActivityChosen;
        private DayPhase _phase;
        private GameMetaState _state;
        private bool _hasRestoredSavedPin;

        private void Awake()
        {
            if (pinTemplate != null)
            {
                pinTemplate.gameObject.SetActive(false);
            }

            if (districtPanel != null)
            {
                districtPanel.Hide();
                if (sfx != null)
                {
                    districtPanel.BindSfx(sfx);
                }
            }

            EnsureStatusMenu();
            EnsureCornerInfoHud();
            WireMenuButton();
        }

        private void EnsureCornerInfoHud()
        {
            if (cornerInfoHud == null)
            {
                cornerInfoHud = GetComponentInChildren<HubCornerInfoHud>(true);
            }

            cornerInfoHud?.WireReferences();
        }

        private void Update()
        {
            if (!isActiveAndEnabled || _state == null)
            {
                return;
            }

            var allowHotkey = statusMenu == null || !statusMenu.IsOpen;
            if (runMapHotkey != null)
            {
                runMapHotkey.SetListening(allowHotkey);
            }

            if (statusMenu != null && statusMenu.IsOpen)
            {
                if (statusMenu.IsCalendarOpen)
                {
                    return;
                }

                if (TownMapInput.MenuPressed())
                {
                    OnMenuClicked();
                }

                return;
            }

            if (TownMapInput.MenuPressed())
            {
                OnMenuClicked();
            }
        }

        public static bool OpenStatusMenuOnNextShow { get; set; }

        public static bool OpenSystemSubmenuOnNextShow { get; set; }

        public static MetaStatusMenuUI.Tab? OpenStatusMenuTabOnNextShow { get; set; }

        public static void PrepareReturnToHub(
            bool openStatusMenu,
            MetaStatusMenuUI.Tab tab = MetaStatusMenuUI.Tab.Stats)
        {
            OpenStatusMenuOnNextShow = openStatusMenu;
            OpenStatusMenuTabOnNextShow = openStatusMenu ? tab : null;
            if (!openStatusMenu)
            {
                OpenSystemSubmenuOnNextShow = false;
            }
        }

        public void Show(GameMetaState state, DayPhase phase, Action<string> onActivityChosen)
        {
            _state = state;
            _phase = phase;
            _onActivityChosen = onActivityChosen;
            gameObject.SetActive(true);

            EnsureStatusMenu();
            ApplyBackground(phase);
            slashBanner?.Refresh(state);
            cornerInfoHud?.Refresh(state);
            statusMenu?.Hide();
            if (OpenStatusMenuOnNextShow)
            {
                OpenStatusMenuOnNextShow = false;
                var openSystem = OpenSystemSubmenuOnNextShow;
                OpenSystemSubmenuOnNextShow = false;
                var tab = OpenStatusMenuTabOnNextShow ?? MetaStatusMenuUI.Tab.Stats;
                OpenStatusMenuTabOnNextShow = null;
                if (openSystem)
                {
                    statusMenu?.ShowSystemSubmenu(state);
                }
                else
                {
                    statusMenu?.Show(state, tab);
                }

                HubNavigationEscContext.ConsumeReopenAfterRunMapEsc(out _);
            }
            else
            {
                OpenSystemSubmenuOnNextShow = false;
            }

            FulfillPendingStatusMenuReturn(state);

            if (cornerInfoHud == null)
            {
                if (selectMapTitle != null)
                {
                    selectMapTitle.text = "SELECT MAP";
                }

                if (selectMapSubtitle != null)
                {
                    selectMapSubtitle.text = "Where should I go?";
                }
            }

            if (wordmarkLabel != null)
            {
                wordmarkLabel.text = "TOWNMAP";
                wordmarkLabel.gameObject.SetActive(wordmarkImage == null || wordmarkImage.sprite == null);
            }

            if (wordmarkImage != null)
            {
                if (wordmarkSprite != null)
                {
                    wordmarkImage.sprite = wordmarkSprite;
                }

                wordmarkImage.enabled = wordmarkImage.sprite != null;
            }

            promptBar?.ApplyDefaultLabels();

            EnsurePins();
            RefreshPinVisibility();
            ClearPinSelection();
            districtPanel?.Hide();
            RestoreSavedPin();
        }

        /// <summary>
        /// Chọn lại pin người chơi đang đứng lúc save. Chỉ chạy đúng một lần cho mỗi lần vào scene —
        /// Show() còn được gọi lại sau mỗi activity, tự mở district panel những lần đó sẽ rất phiền.
        /// Pin đã ẩn ở phase hiện tại thì bỏ qua.
        /// </summary>
        private void RestoreSavedPin()
        {
            if (_hasRestoredSavedPin)
            {
                return;
            }

            _hasRestoredSavedPin = true;

            var hubLocation = _state?.HubLocation;
            if (hubLocation == null || !hubLocation.HasLocation)
            {
                return;
            }

            foreach (var pin in _pins)
            {
                if (pin.LocationId != hubLocation.LastLocationId)
                {
                    continue;
                }

                if (!pin.MatchesPhase(_phase, _state))
                {
                    return;
                }

                OnPinSelected(pin.Definition);
                return;
            }
        }

        public void FulfillPendingStatusMenuReturn(GameMetaState state)
        {
            if (state == null)
            {
                return;
            }

            if (statusMenu != null && statusMenu.IsOpen)
            {
                if (HubNavigationEscContext.HasPendingReopenAfterRunMapEsc)
                {
                    HubNavigationEscContext.ConsumeReopenAfterRunMapEsc(out _);
                }

                return;
            }

            if (!HubNavigationEscContext.HasPendingReopenAfterRunMapEsc)
            {
                return;
            }

            if (!HubNavigationEscContext.ConsumeReopenAfterRunMapEsc(out var tab))
            {
                return;
            }

            EnsureStatusMenu();
            statusMenu?.Show(state, tab);
        }

        public void Hide()
        {
            statusMenu?.Hide();
            districtPanel?.Hide();
            if (runMapHotkey != null)
            {
                runMapHotkey.SetListening(false);
                runMapHotkey.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        public void RefreshCalendar(GameMetaState state)
        {
            _state = state;
            slashBanner?.Refresh(state);
            if (statusMenu != null && statusMenu.IsOpen)
            {
                statusMenu.Show(state);
            }
        }

        private void EnsureStatusMenu()
        {
            var built = MetaStatusMenuUI.Build(transform);
            menuButton = built.MenuButton;
            statusMenu = built.Menu;
            if (sfx != null)
            {
                statusMenu.BindSfx(sfx);
            }

            WireMenuButton();
        }

        private void WireMenuButton()
        {
            if (menuButton == null)
            {
                return;
            }

            menuButton.onClick.RemoveListener(OnMenuClicked);
            menuButton.onClick.AddListener(OnMenuClicked);
        }

        private void OnMenuClicked()
        {
            if (_state == null)
            {
                _state = GameMetaSession.Current;
            }

            sfx?.PlaySelect();
            statusMenu?.Toggle(_state);
        }

        private void ApplyBackground(DayPhase phase)
        {
            var night = phase == DayPhase.Evening;
            if (dayBackground != null)
            {
                dayBackground.gameObject.SetActive(!night);
            }

            if (nightBackground != null)
            {
                nightBackground.gameObject.SetActive(night);
            }
        }

        private void EnsurePins()
        {
            if (_pins.Count > 0 || pinTemplate == null || mapRoot == null)
            {
                return;
            }

            _locations = TownLocationCatalog.CreateDefault();
            foreach (var location in _locations)
            {
                var pin = Instantiate(pinTemplate, mapRoot);
                pin.gameObject.SetActive(true);
                pin.gameObject.name = $"Pin_{location.Id}";

                var rect = pin.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = location.AnchorNormalized;
                    rect.anchorMax = location.AnchorNormalized;
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(96f, 96f);
                }

                pin.Bind(location, ResolveIcon(location.PinIcon), pinIdle, pinSelected, OnPinSelected);
                _pins.Add(pin);
            }
        }

        private void RefreshPinVisibility()
        {
            foreach (var pin in _pins)
            {
                pin.SetVisible(pin.MatchesPhase(_phase, _state));
            }
        }

        private void OnPinSelected(TownLocationDefinition location)
        {
            if (statusMenu != null && statusMenu.IsOpen)
            {
                statusMenu.Hide();
            }

            sfx?.PlaySelect();
            foreach (var pin in _pins)
            {
                pin.SetSelected(pin.LocationId == location.Id);
            }

            _state?.HubLocation.SetLocation(location.Id);
            districtPanel?.Show(location, _phase, OnDistrictConfirm, ClearPinSelection);
        }

        private void OnDistrictConfirm(TownLocationDefinition location, TownSubLocation sub)
        {
            districtPanel?.Hide();
            ClearPinSelection();

            // Ghi trước khi chạy activity: activity có thể đổi scene, và khi quay lại
            // ta muốn map mở đúng chỗ người chơi vừa đứng.
            _state?.HubLocation.SetLocation(location.Id, sub.Id);

            _onActivityChosen?.Invoke(sub.ActivityId);
        }

        private void ClearPinSelection()
        {
            foreach (var pin in _pins)
            {
                pin.SetSelected(false);
            }
        }

        private Sprite ResolveIcon(TownPinIcon icon) => icon switch
        {
            TownPinIcon.School => iconSchool,
            TownPinIcon.Shop => iconShop,
            TownPinIcon.Flower => iconFlower,
            TownPinIcon.Shrine => iconShrine,
            TownPinIcon.Vault => iconVault,
            _ => iconSchool
        };
    }
}
