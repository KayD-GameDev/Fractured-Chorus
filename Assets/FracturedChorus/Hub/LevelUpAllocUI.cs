using System;
using FracturedChorus.Hub;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class LevelUpAllocUI : MonoBehaviour
    {
        private static readonly Color PanelBg = FcColorTokens.WithAlpha(FcColorTokens.Surface.Modal, 0.96f);

        [SerializeField] private GameObject root;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text pointsLabel;

        private Button _strButton;
        private Button _maButton;
        private Button _enButton;
        private Button _hbButton;
        private Button _closeButton;

        private GameMetaState _state;
        private string _characterId;
        private Action _onChanged;
        private bool _built;

        public bool IsOpen => root != null && root.activeSelf;

        public static LevelUpAllocUI Ensure(Transform host)
        {
            if (host == null)
            {
                return null;
            }

            var existing = host.GetComponentInChildren<LevelUpAllocUI>(true);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("LevelUpAlloc", typeof(RectTransform), typeof(LevelUpAllocUI));
            go.transform.SetParent(host, false);
            var panel = go.GetComponent<LevelUpAllocUI>();
            panel.BuildHierarchy();
            go.SetActive(false);
            return panel;
        }

        public void TryPrompt(GameMetaState state, string characterId, Action onChanged)
        {
            var entry = state?.Loadout.GetOrCreate(characterId);
            if (entry == null || entry.UnspentStatPoints <= 0)
            {
                return;
            }

            Show(state, characterId, onChanged);
        }

        public void Show(GameMetaState state, string characterId, Action onChanged)
        {
            _state = state;
            _characterId = characterId;
            _onChanged = onChanged;
            EnsureBuilt();
            if (root != null)
            {
                root.SetActive(true);
            }

            transform.SetAsLastSibling();
            Refresh();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (TownMapInput.CancelPressed())
            {
                Hide();
            }
        }

        private void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            BuildHierarchy();
        }

        private void BuildHierarchy()
        {
            root = gameObject;
            var rect = GetComponent<RectTransform>();
            Stretch(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280f, -180f), new Vector2(280f, 180f));

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.color = PanelBg;
            bg.raycastTarget = true;

            titleLabel = CreateText(transform, "Title", "Allocate Stats", 22, TextAnchor.UpperCenter);
            Stretch(titleLabel.rectTransform, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
            titleLabel.color = FcColorTokens.Brand.Cyan;
            titleLabel.fontStyle = FontStyle.Bold;

            pointsLabel = CreateText(transform, "Points", "Unspent: 0", 18, TextAnchor.UpperCenter);
            Stretch(pointsLabel.rectTransform, new Vector2(0.06f, 0.64f), new Vector2(0.94f, 0.76f), Vector2.zero, Vector2.zero);

            _strButton = CreateStatButton("Str", 0.48f, () => Spend(s => s.StrPoints++));
            _maButton = CreateStatButton("Ma", 0.36f, () => Spend(s => s.MaPoints++));
            _enButton = CreateStatButton("En", 0.24f, () => Spend(s => s.EnPoints++));
            _hbButton = CreateStatButton("Hb", 0.12f, () => Spend(s => s.HbPoints++));
            _closeButton = CreateStatButton("Done", 0.02f, Hide);
            _built = true;
        }

        private Button CreateStatButton(string label, float yMin, Action onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            Stretch(go.GetComponent<RectTransform>(), new Vector2(0.12f, yMin), new Vector2(0.88f, yMin + 0.1f), Vector2.zero, Vector2.zero);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.1f, 0.18f, 0.3f, 0.95f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            UiButtonHoverFeedback.Ensure(go);

            var text = CreateText(go.transform, "Label", $"+ {label}", 18, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private void Spend(Action<CharacterLoadoutEntry> apply)
        {
            if (_state == null || string.IsNullOrEmpty(_characterId))
            {
                return;
            }

            var entry = _state.Loadout.GetOrCreate(_characterId);
            if (entry.UnspentStatPoints <= 0)
            {
                Hide();
                return;
            }

            apply(entry);
            entry.UnspentStatPoints--;
            GameMetaSession.Save();
            Refresh();
            _onChanged?.Invoke();

            if (entry.UnspentStatPoints <= 0)
            {
                Hide();
            }
        }

        private void Refresh()
        {
            if (_state == null || string.IsNullOrEmpty(_characterId))
            {
                return;
            }

            var entry = _state.Loadout.GetOrCreate(_characterId);
            if (titleLabel != null)
            {
                titleLabel.text = $"Allocate — {DisplayName(_characterId)}";
            }

            if (pointsLabel != null)
            {
                pointsLabel.text =
                    $"Unspent: {entry.UnspentStatPoints}  ·  Str {entry.StrPoints}  Ma {entry.MaPoints}  En {entry.EnPoints}  Hb {entry.HbPoints}";
            }
        }

        private static string DisplayName(string characterId) => characterId switch
        {
            PartyCharacterIds.Charlotte => "Charlotte",
            PartyCharacterIds.Coda => "Coda",
            _ => "Ren"
        };

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            UiFontCatalog.Apply(text, UiFontRole.Body, fontSize);
            text.text = content;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
