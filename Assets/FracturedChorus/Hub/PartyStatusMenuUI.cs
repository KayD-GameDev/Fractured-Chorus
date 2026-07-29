using System;
using System.Collections.Generic;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class PartyStatusMenuUI : MonoBehaviour
    {
        private static readonly string[] Roster =
        {
            PartyCharacterIds.Ren,
            PartyCharacterIds.Charlotte,
            PartyCharacterIds.Coda
        };

        [SerializeField] private GameObject root;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text skillsLabel;
        [SerializeField] private Text footerLabel;
        [SerializeField] private SkillEquipPanelUI skillEquipPanel;
        [SerializeField] private LevelUpAllocUI levelUpPanel;

        private readonly Dictionary<string, Image> _statBars = new Dictionary<string, Image>();
        private int _memberIndex;
        private GameMetaState _state;
        private bool _built;

        public bool IsOpen => root != null && root.activeSelf;

        public static PartyStatusMenuUI Ensure(Transform host)
        {
            if (host == null)
            {
                return null;
            }

            var existing = host.GetComponentInChildren<PartyStatusMenuUI>(true);
            if (existing != null)
            {
                existing.EnsureBuilt();
                return existing;
            }

            var go = new GameObject("PartyStatusMenu", typeof(RectTransform), typeof(PartyStatusMenuUI));
            go.transform.SetParent(host, false);
            var menu = go.GetComponent<PartyStatusMenuUI>();
            menu.BuildHierarchy(host);
            go.SetActive(false);
            return menu;
        }

        public void Show(GameMetaState state)
        {
            _state = state ?? GameMetaSession.Current;
            EnsureBuilt();
            _memberIndex = 0;
            if (root != null)
            {
                root.SetActive(true);
            }

            transform.SetAsLastSibling();
            Refresh();
            var entry = _state?.Loadout.GetOrCreate(Roster[_memberIndex]);
            if (entry != null && entry.UnspentStatPoints > 0)
            {
                levelUpPanel?.TryPrompt(_state, Roster[_memberIndex], Refresh);
            }
        }

        public void Hide()
        {
            skillEquipPanel?.Hide();
            levelUpPanel?.Hide();
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

            if (skillEquipPanel != null && skillEquipPanel.IsOpen)
            {
                return;
            }

            if (levelUpPanel != null && levelUpPanel.IsOpen)
            {
                return;
            }

            if (TownMapInput.CancelPressed())
            {
                Hide();
                return;
            }

            if (TownMapInput.MonthPrevPressed())
            {
                CycleMember(-1);
            }
            else if (TownMapInput.MonthNextPressed())
            {
                CycleMember(1);
            }

            if (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.vKey.wasPressedThisFrame)
            {
                OpenSkillEquip();
            }
        }

        private void CycleMember(int delta)
        {
            _memberIndex = (_memberIndex + delta + Roster.Length) % Roster.Length;
            Refresh();
        }

        private void OpenSkillEquip()
        {
            var characterId = Roster[_memberIndex];
            skillEquipPanel ??= SkillEquipPanelUI.Ensure(transform.parent != null ? transform.parent : transform);
            skillEquipPanel.Show(_state, characterId, Refresh);
        }

        internal void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            BuildHierarchy(transform.parent != null ? transform.parent : transform);
        }

        private void BuildHierarchy(Transform host)
        {
            root = gameObject;
            var rect = GetComponent<RectTransform>();
            Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.color = FcColorTokens.Surface.Panel;
            bg.raycastTarget = true;

            nameLabel = CreateText(transform, "Name", "Ren", 32, TextAnchor.UpperLeft);
            Stretch(nameLabel.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.55f, 0.94f), Vector2.zero, Vector2.zero);
            nameLabel.color = FcColorTokens.Brand.Cyan;
            nameLabel.fontStyle = FontStyle.Bold;

            levelLabel = CreateText(transform, "Level", "Lv 15", 22, TextAnchor.UpperRight);
            Stretch(levelLabel.rectTransform, new Vector2(0.55f, 0.82f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);

            CreateStatBar("St", 0.74f);
            CreateStatBar("Ma", 0.64f);
            CreateStatBar("En", 0.54f);
            CreateStatBar("Hb", 0.44f);
            CreateStatBar("Lu", 0.34f);

            skillsLabel = CreateText(transform, "Skills", string.Empty, 20, TextAnchor.UpperLeft);
            Stretch(skillsLabel.rectTransform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.32f), Vector2.zero, Vector2.zero);
            skillsLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            footerLabel = CreateText(transform, "Footer", "Q/E swap · V skill equip · Esc close", 18, TextAnchor.LowerCenter);
            Stretch(footerLabel.rectTransform, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.1f), Vector2.zero, Vector2.zero);
            footerLabel.color = FcColorTokens.Brand.Cyan;
            footerLabel.fontStyle = FontStyle.Italic;

            levelUpPanel = LevelUpAllocUI.Ensure(host);
            skillEquipPanel = SkillEquipPanelUI.Ensure(host);
            _built = true;
        }

        private void CreateStatBar(string statKey, float yMax)
        {
            var row = new GameObject($"Stat_{statKey}", typeof(RectTransform));
            row.transform.SetParent(transform, false);
            Stretch(row.GetComponent<RectTransform>(), new Vector2(0.06f, yMax - 0.08f), new Vector2(0.94f, yMax), Vector2.zero, Vector2.zero);

            var label = CreateText(row.transform, "Label", statKey, 18, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, new Vector2(0f, 0f), new Vector2(0.08f, 1f), Vector2.zero, Vector2.zero);

            var track = CreateImage(row.transform, "Track", FcColorTokens.Surface.Track);
            Stretch(track.rectTransform, new Vector2(0.1f, 0.15f), new Vector2(1f, 0.85f), Vector2.zero, Vector2.zero);

            var fill = CreateImage(track.transform, "Fill", FcColorTokens.Brand.CyanSoft);
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0.5f;
            _statBars[statKey] = fill;
        }

        private void Refresh()
        {
            if (_state == null)
            {
                return;
            }

            var characterId = Roster[_memberIndex];
            var entry = _state.Loadout.GetOrCreate(characterId);
            var bases = PartyStatBases.For(characterId);

            if (nameLabel != null)
            {
                nameLabel.text = DisplayName(characterId);
            }

            if (levelLabel != null)
            {
                levelLabel.text = "Lv 15";
            }

            SetBar("St", bases.St + entry.StrPoints, 120f);
            SetBar("Ma", bases.Ma + entry.MaPoints, 120f);
            SetBar("En", bases.En + entry.EnPoints, 200f);
            SetBar("Hb", bases.Hb + entry.HbPoints, 300f);
            SetBar("Lu", bases.Lu, 150f);

            if (skillsLabel != null)
            {
                skillsLabel.text = BuildSkillList(entry);
            }
        }

        private static string BuildSkillList(CharacterLoadoutEntry entry)
        {
            var lines = new List<string> { "Equipped:" };
            var slots = entry.EquippedSkillIds ?? Array.Empty<string>();
            for (var i = 0; i < 3; i++)
            {
                var skillId = i < slots.Length ? slots[i] : string.Empty;
                lines.Add($"  Slot {i + 1}: {SkillUnlockCatalog.DisplayName(skillId)}");
            }

            return string.Join("\n", lines);
        }

        private void SetBar(string key, float value, float max)
        {
            if (!_statBars.TryGetValue(key, out var fill) || fill == null)
            {
                return;
            }

            fill.fillAmount = Mathf.Clamp01(value / max);
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

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private readonly struct StatBaseSet
        {
            public StatBaseSet(float st, float ma, float en, float hb, float lu)
            {
                St = st;
                Ma = ma;
                En = en;
                Hb = hb;
                Lu = lu;
            }

            public float St { get; }
            public float Ma { get; }
            public float En { get; }
            public float Hb { get; }
            public float Lu { get; }
        }

        private static class PartyStatBases
        {
            public static StatBaseSet For(string characterId) => characterId switch
            {
                PartyCharacterIds.Charlotte => new StatBaseSet(35f, 18.2f, 127f, 260f, 8f),
                PartyCharacterIds.Coda => new StatBaseSet(50f, 9.8f, 147f, 73f, 10f),
                _ => new StatBaseSet(42f, 10.8f, 167f, 114f, 12f)
            };
        }
    }
}
