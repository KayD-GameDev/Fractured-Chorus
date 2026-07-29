using System;
using System.Collections.Generic;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class SkillEquipPanelUI : MonoBehaviour
    {
        private static readonly Color PanelBg = FcColorTokens.WithAlpha(FcColorTokens.Surface.Panel, 0.96f);

        [SerializeField] private GameObject root;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Transform slotRow;
        [SerializeField] private Transform poolRow;

        private readonly List<Button> _slotButtons = new List<Button>();
        private readonly List<Button> _poolButtons = new List<Button>();

        private GameMetaState _state;
        private string _characterId;
        private Action _onChanged;
        private bool _built;

        public bool IsOpen => root != null && root.activeSelf;

        public static SkillEquipPanelUI Ensure(Transform host)
        {
            if (host == null)
            {
                return null;
            }

            var existing = host.GetComponentInChildren<SkillEquipPanelUI>(true);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject("SkillEquipPanel", typeof(RectTransform), typeof(SkillEquipPanelUI));
            go.transform.SetParent(host, false);
            var panel = go.GetComponent<SkillEquipPanelUI>();
            panel.BuildHierarchy();
            go.SetActive(false);
            return panel;
        }

        public void Show(GameMetaState state, string characterId, Action onChanged)
        {
            _state = state ?? GameMetaSession.Current;
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
            Stretch(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-360f, -220f), new Vector2(360f, 220f));

            var bg = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.color = PanelBg;
            bg.raycastTarget = true;

            titleLabel = CreateText(transform, "Title", "Skill Equip", 24, TextAnchor.UpperCenter);
            Stretch(titleLabel.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.96f), Vector2.zero, Vector2.zero);
            titleLabel.color = FcColorTokens.Brand.Cyan;
            titleLabel.fontStyle = FontStyle.Bold;

            var slotsGo = new GameObject("Slots", typeof(RectTransform), typeof(VerticalLayoutGroup));
            slotsGo.transform.SetParent(transform, false);
            slotRow = slotsGo.transform;
            Stretch(slotRow.GetComponent<RectTransform>(), new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.8f), Vector2.zero, Vector2.zero);
            ConfigureLayout(slotRow.GetComponent<VerticalLayoutGroup>());

            var poolGo = new GameObject("Pool", typeof(RectTransform), typeof(VerticalLayoutGroup));
            poolGo.transform.SetParent(transform, false);
            poolRow = poolGo.transform;
            Stretch(poolRow.GetComponent<RectTransform>(), new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.48f), Vector2.zero, Vector2.zero);
            ConfigureLayout(poolRow.GetComponent<VerticalLayoutGroup>());

            CreateText(transform, "Hint", "Tap pool skill to equip next empty slot · tap slot to unequip · Esc close", 16,
                TextAnchor.LowerCenter);
            var hint = transform.Find("Hint")?.GetComponent<Text>();
            if (hint != null)
            {
                Stretch(hint.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.08f), Vector2.zero, Vector2.zero);
                hint.color = FcColorTokens.Brand.Cyan;
                hint.fontStyle = FontStyle.Italic;
            }

            _built = true;
        }

        private void Refresh()
        {
            ClearButtons(_slotButtons, slotRow);
            ClearButtons(_poolButtons, poolRow);

            if (_state == null || string.IsNullOrEmpty(_characterId))
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = $"Skill Equip — {DisplayName(_characterId)}";
            }

            var entry = _state.Loadout.GetOrCreate(_characterId);
            var slots = entry.EquippedSkillIds ?? new[] { string.Empty, string.Empty, string.Empty };

            for (var i = 0; i < 3; i++)
            {
                var slotIndex = i;
                var skillId = i < slots.Length ? slots[i] : string.Empty;
                var label = string.IsNullOrEmpty(skillId)
                    ? $"Slot {i + 1}: (empty)"
                    : $"Slot {i + 1}: {SkillUnlockCatalog.DisplayName(skillId)}";
                var button = CreateActionButton(slotRow, label, () => UnequipSlot(entry, slotIndex));
                _slotButtons.Add(button);
            }

            foreach (var unlock in SkillUnlockCatalog.UnlockedFor(_characterId))
            {
                var skillId = unlock.SkillId;
                var button = CreateActionButton(
                    poolRow,
                    SkillUnlockCatalog.DisplayName(skillId),
                    () => EquipSkill(entry, skillId));
                _poolButtons.Add(button);
            }
        }

        private void EquipSkill(CharacterLoadoutEntry entry, string skillId)
        {
            var slots = NormalizeSlots(entry.EquippedSkillIds);
            for (var i = 0; i < slots.Length; i++)
            {
                if (string.Equals(slots[i], skillId, StringComparison.Ordinal))
                {
                    slots[i] = string.Empty;
                }
            }

            for (var i = 0; i < slots.Length; i++)
            {
                if (string.IsNullOrEmpty(slots[i]))
                {
                    slots[i] = skillId;
                    break;
                }
            }

            entry.EquippedSkillIds = slots;
            Persist();
        }

        private void UnequipSlot(CharacterLoadoutEntry entry, int slotIndex)
        {
            var slots = NormalizeSlots(entry.EquippedSkillIds);
            if (slotIndex >= 0 && slotIndex < slots.Length)
            {
                slots[slotIndex] = string.Empty;
            }

            entry.EquippedSkillIds = slots;
            Persist();
        }

        private void Persist()
        {
            GameMetaSession.Save();
            Refresh();
            _onChanged?.Invoke();
        }

        private static string[] NormalizeSlots(string[] source)
        {
            var slots = new string[3];
            if (source == null)
            {
                return slots;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = i < source.Length ? source[i] ?? string.Empty : string.Empty;
            }

            return slots;
        }

        private static void ClearButtons(List<Button> buttons, Transform row)
        {
            foreach (var button in buttons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            buttons.Clear();
            if (row == null)
            {
                return;
            }

            for (var i = row.childCount - 1; i >= 0; i--)
            {
                Destroy(row.GetChild(i).gameObject);
            }
        }

        private static void ConfigureLayout(VerticalLayoutGroup layout)
        {
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static Button CreateActionButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("SkillButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 44f);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.1f, 0.16f, 0.28f, 0.95f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            UiButtonHoverFeedback.Ensure(go);

            var text = CreateText(go.transform, "Label", label, 18, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), Vector2.zero, Vector2.zero);
            return button;
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

    public static class SkillUnlockCatalog
    {
        private readonly struct UnlockEntry
        {
            public UnlockEntry(string skillId, string displayName, int unlockLevel)
            {
                SkillId = skillId;
                DisplayNameValue = displayName;
                UnlockLevel = unlockLevel;
            }

            public string SkillId { get; }
            public string DisplayNameValue { get; }
            public int UnlockLevel { get; }
        }

        private const int PartyLevelStub = 15;

        private static readonly Dictionary<string, UnlockEntry[]> Tables = new Dictionary<string, UnlockEntry[]>
        {
            {
                PartyCharacterIds.Ren, new[]
                {
                    new UnlockEntry("ren_basic", "Strike", 1),
                    new UnlockEntry("ren_skill", "Crosscut", 4),
                    new UnlockEntry("ren_ult", "Finale", 10)
                }
            },
            {
                PartyCharacterIds.Charlotte, new[]
                {
                    new UnlockEntry("Charlott_basic", "Ram", 1),
                    new UnlockEntry("tank_skill", "Anchor", 3),
                    new UnlockEntry("tank_ult", "Bulwark", 9)
                }
            },
            {
                PartyCharacterIds.Coda, new[]
                {
                    new UnlockEntry("mage_basic", "Pulse", 1),
                    new UnlockEntry("mage_skill", "Mend", 5),
                    new UnlockEntry("mage_ult", "Encore", 11)
                }
            }
        };

        public static IEnumerable<(string SkillId, string DisplayName)> UnlockedFor(string characterId)
        {
            if (!Tables.TryGetValue(characterId, out var entries))
            {
                yield break;
            }

            foreach (var entry in entries)
            {
                if (PartyLevelStub >= entry.UnlockLevel)
                {
                    yield return (entry.SkillId, entry.DisplayNameValue);
                }
            }
        }

        public static string DisplayName(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return "(empty)";
            }

            foreach (var table in Tables.Values)
            {
                foreach (var entry in table)
                {
                    if (string.Equals(entry.SkillId, skillId, StringComparison.Ordinal))
                    {
                        return entry.DisplayNameValue;
                    }
                }
            }

            return skillId;
        }
    }
}
