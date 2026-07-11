using System;
using System.Collections.Generic;
using FracturedChorus.Meta;
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
        [SerializeField] private Text selectMapTitle;
        [SerializeField] private Text selectMapSubtitle;
        [SerializeField] private Image headerPinImage;
        [SerializeField] private Text wordmarkLabel;
        [SerializeField] private Image wordmarkImage;
        [SerializeField] private TownMapPromptBar promptBar;
        [SerializeField] private TownMapSfxController sfx;
        [SerializeField] private Button menuButton;
        [SerializeField] private MetaStatusMenuUI statusMenu;

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
            WireMenuButton();
        }

        private void Update()
        {
            if (!isActiveAndEnabled || _state == null)
            {
                return;
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

        public void Show(GameMetaState state, DayPhase phase, Action<string> onActivityChosen)
        {
            _state = state;
            _phase = phase;
            _onActivityChosen = onActivityChosen;
            gameObject.SetActive(true);

            EnsureStatusMenu();
            ApplyBackground(phase);
            slashBanner?.Refresh(state);
            statusMenu?.Hide();

            if (selectMapTitle != null)
            {
                selectMapTitle.text = "SELECT MAP";
            }

            if (selectMapSubtitle != null)
            {
                selectMapSubtitle.text = "Where should I go?";
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
        }

        public void Hide()
        {
            statusMenu?.Hide();
            districtPanel?.Hide();
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

            districtPanel?.Show(location, _phase, OnDistrictConfirm, ClearPinSelection);
        }

        private void OnDistrictConfirm(TownLocationDefinition location, TownSubLocation sub)
        {
            districtPanel?.Hide();
            ClearPinSelection();
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
