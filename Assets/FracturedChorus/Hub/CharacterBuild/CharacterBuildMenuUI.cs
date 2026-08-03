using System;
using System.Collections.Generic;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using FracturedChorus.Hub;
using FracturedChorus.Meta;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FracturedChorus.Hub.CharacterBuild
{
    /// <summary>
    /// Standalone Character Build screen — view skills/stats, allocate points, change combat loadout.
    /// All layout refs are scene hierarchy SerializeFields (no runtime BuildHierarchy).
    /// </summary>
    public sealed class CharacterBuildMenuUI : MonoBehaviour
    {
        private static readonly string[] Roster =
        {
            PartyCharacterIds.Ren,
            PartyCharacterIds.Charlotte,
            PartyCharacterIds.Coda
        };

        private const int MaxPointsPerStat = 10;
        private const float BarVisualMax = 300f;
        private const int DevUnspentSeed = 14;

        [Header("Header")]
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text elementLabel;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text nextExpLabel;
        [SerializeField] private Image[] elementIcons;
        [SerializeField] private GameObject[] elementHighlightRings;

        [Header("Navigation")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        [Header("Skills")]
        [SerializeField] private CharacterBuildSkillRowView[] skillRows = new CharacterBuildSkillRowView[5];

        [Header("Stats")]
        [SerializeField] private CharacterBuildStatRowView strengthRow;
        [SerializeField] private CharacterBuildStatRowView magicRow;
        [SerializeField] private CharacterBuildStatRowView enduranceRow;
        [SerializeField] private CharacterBuildStatRowView heartBeatRow;
        [SerializeField] private CharacterBuildStatRowView luckRow;

        [Header("Remaining Points")]
        [SerializeField] private Text remainingPointsLabel;

        [Header("Portrait")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private Sprite renMenuPortrait;

        [Header("Footer / Equip")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button viewSkillsButton;
        [SerializeField] private GameObject skillEquipOverlay;
        [SerializeField] private Text skillEquipTitleLabel;
        [SerializeField] private Transform skillEquipSlotRow;
        [SerializeField] private Transform skillEquipPoolRow;
        [SerializeField] private Button skillEquipCloseButton;

        [Header("Dev")]
        [SerializeField] private bool seedUnspentWhenEmpty = true;
        [SerializeField] private int stubLevel = 15;
        [SerializeField] private int stubNextExp = 3600;

        private readonly List<Button> _equipSlotButtons = new List<Button>();
        private readonly List<Button> _equipPoolButtons = new List<Button>();
        private int _memberIndex;
        private GameMetaState _state;
        private int _equipFocusSlot;

        private void Awake()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                CombatInputSetup.ApplyInputModule(eventSystem.gameObject, destroyImmediate: true);
            }
            else
            {
                CombatInputSetup.EnsureEventSystem();
            }

            WireButtons();
            WireStatCallbacks();
        }

        private void Start()
        {
            _state = GameMetaSession.Current;
            EnsureDefaults();
            Refresh();
        }

        private void Update()
        {
            if (skillEquipOverlay != null && skillEquipOverlay.activeSelf)
            {
                if (TownMapInput.CancelPressed())
                {
                    HideSkillEquip();
                }

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
            else if (TownMapInput.CancelPressed())
            {
                // Standalone scene — no hub to return to.
            }
            else if (WasPressed(Key.V))
            {
                OpenSkillEquip(0);
            }
        }

        private static bool WasPressed(Key key)
        {
            var kb = Keyboard.current;
            return kb != null && kb[key].wasPressedThisFrame;
        }

        private void WireButtons()
        {
            if (prevButton != null)
            {
                prevButton.onClick.AddListener(() => CycleMember(-1));
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(() => CycleMember(1));
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(() => Debug.Log("[CharacterBuild] Esc/Back — standalone scene."));
            }

            if (viewSkillsButton != null)
            {
                viewSkillsButton.onClick.AddListener(() => OpenSkillEquip(0));
            }

            if (skillEquipCloseButton != null)
            {
                skillEquipCloseButton.onClick.AddListener(HideSkillEquip);
            }

            if (skillRows != null)
            {
                for (var i = 0; i < skillRows.Length; i++)
                {
                    var index = i;
                    var row = skillRows[i];
                    if (row == null || row.Button == null)
                    {
                        continue;
                    }

                    row.Button.onClick.AddListener(() =>
                    {
                        if (index < 3)
                        {
                            OpenSkillEquip(index);
                        }
                    });
                }
            }
        }

        private void WireStatCallbacks()
        {
            WireAlloc(strengthRow, e => e.StrPoints, (e, v) => e.StrPoints = v);
            WireAlloc(magicRow, e => e.MaPoints, (e, v) => e.MaPoints = v);
            WireAlloc(enduranceRow, e => e.EnPoints, (e, v) => e.EnPoints = v);
            WireAlloc(heartBeatRow, e => e.HbPoints, (e, v) => e.HbPoints = v);
        }

        private void WireAlloc(
            CharacterBuildStatRowView row,
            Func<CharacterLoadoutEntry, int> getter,
            Action<CharacterLoadoutEntry, int> setter)
        {
            if (row == null)
            {
                return;
            }

            row.WireCallbacks(
                () => TryAdjust(getter, setter, -1),
                () => TryAdjust(getter, setter, +1));
        }

        private void EnsureDefaults()
        {
            foreach (var id in Roster)
            {
                var entry = _state.Loadout.GetOrCreate(id);
                if (entry.EquippedSkillIds == null || entry.EquippedSkillIds.Length != 3)
                {
                    entry.EquippedSkillIds = new string[3];
                }

                var any = false;
                for (var i = 0; i < entry.EquippedSkillIds.Length; i++)
                {
                    if (!string.IsNullOrEmpty(entry.EquippedSkillIds[i]))
                    {
                        any = true;
                        break;
                    }
                }

                if (!any)
                {
                    entry.EquippedSkillIds = id switch
                    {
                        PartyCharacterIds.Charlotte => new[] { "Charlott_basic", "tank_skill", "tank_ult" },
                        PartyCharacterIds.Coda => new[] { "mage_basic", "mage_skill", "mage_ult" },
                        _ => new[] { "ren_basic", "ren_skill", "ren_ult" }
                    };
                }

                if (seedUnspentWhenEmpty
                    && entry.UnspentStatPoints <= 0
                    && entry.StrPoints <= 0
                    && entry.MaPoints <= 0
                    && entry.EnPoints <= 0
                    && entry.HbPoints <= 0)
                {
                    entry.UnspentStatPoints = DevUnspentSeed;
                }
            }
        }

        private void CycleMember(int delta)
        {
            _memberIndex = (_memberIndex + delta + Roster.Length) % Roster.Length;
            HideSkillEquip();
            Refresh();
        }

        private void TryAdjust(
            Func<CharacterLoadoutEntry, int> getter,
            Action<CharacterLoadoutEntry, int> setter,
            int delta)
        {
            var entry = CurrentEntry();
            if (entry == null)
            {
                return;
            }

            var spent = getter(entry);
            if (delta > 0)
            {
                if (entry.UnspentStatPoints <= 0 || spent >= MaxPointsPerStat)
                {
                    return;
                }

                setter(entry, spent + 1);
                entry.UnspentStatPoints--;
            }
            else if (delta < 0)
            {
                if (spent <= 0)
                {
                    return;
                }

                setter(entry, spent - 1);
                entry.UnspentStatPoints++;
            }

            GameMetaSession.Save();
            Refresh();
        }

        private CharacterLoadoutEntry CurrentEntry()
        {
            return _state?.Loadout.GetOrCreate(Roster[_memberIndex]);
        }

        private void Refresh()
        {
            var characterId = Roster[_memberIndex];
            var entry = CurrentEntry();
            var bases = ResolveBaseStats(characterId);
            if (entry == null || bases == null)
            {
                return;
            }

            if (nameLabel != null)
            {
                nameLabel.text = DisplayName(characterId);
            }

            if (elementLabel != null)
            {
                elementLabel.text = bases.Element.ToString();
            }

            if (levelLabel != null)
            {
                levelLabel.text = $"Lv {stubLevel}";
            }

            if (nextExpLabel != null)
            {
                nextExpLabel.text = $"NEXT EXP {stubNextExp}";
            }

            RefreshElementHighlights(bases.Element);
            RefreshPortrait(characterId);
            RefreshSkills(entry);
            RefreshStats(characterId, entry, bases);

            if (remainingPointsLabel != null)
            {
                remainingPointsLabel.text = $"Remaining Points: {entry.UnspentStatPoints}";
            }

            if (skillEquipOverlay != null && skillEquipOverlay.activeSelf)
            {
                RefreshSkillEquip(entry);
            }
        }

        private void RefreshElementHighlights(HarmonyElement element)
        {
            if (elementHighlightRings == null)
            {
                return;
            }

            for (var i = 0; i < elementHighlightRings.Length; i++)
            {
                if (elementHighlightRings[i] == null)
                {
                    continue;
                }

                // Order: Rhythm=0, Melody=1, Harmony=2
                elementHighlightRings[i].SetActive(i == (int)element);
            }
        }

        private void RefreshPortrait(string characterId)
        {
            if (portraitImage == null)
            {
                return;
            }

            Sprite sprite = null;
            if (characterId == PartyCharacterIds.Ren)
            {
                sprite = renMenuPortrait;
                if (sprite == null)
                {
                    sprite = Resources.Load<Sprite>("UI/StatusMenu/ren_hima_uniform_menu_fullbody_v1");
                }
            }

            if (sprite == null)
            {
                var preset = LoadPreset(characterId);
                if (preset != null)
                {
                    sprite = preset.battleSprite != null ? preset.battleSprite : preset.combatCardSprite;
                }
            }

            portraitImage.enabled = sprite != null;
            portraitImage.sprite = sprite;
            portraitImage.color = Color.white;
            portraitImage.preserveAspect = true;
        }

        private void RefreshSkills(CharacterLoadoutEntry entry)
        {
            if (skillRows == null)
            {
                return;
            }

            var slots = entry.EquippedSkillIds ?? Array.Empty<string>();
            for (var i = 0; i < skillRows.Length; i++)
            {
                var row = skillRows[i];
                if (row == null)
                {
                    continue;
                }

                var combatSlot = i < 3;
                if (!combatSlot || i >= slots.Length || string.IsNullOrEmpty(slots[i]))
                {
                    row.BindEmpty(combatSlot);
                    continue;
                }

                var skill = FindSkill(slots[i]);
                var display = SkillUnlockCatalog.DisplayName(slots[i]);
                row.Bind(display, skill != null ? skill.icon : null, true);
            }
        }

        private void RefreshStats(string characterId, CharacterLoadoutEntry entry, UnitStats bases)
        {
            var strength = bases.Strength + entry.StrPoints;
            var magic = bases.Magic + entry.MaPoints;
            var endurance = bases.Endurance + entry.EnPoints;
            var heartBeat = bases.HeartBeat + entry.HbPoints * 5;
            var luck = bases.BaseLuck;
            var canPlus = entry.UnspentStatPoints > 0;

            strengthRow?.Refresh("Strength", strength, BarVisualMax, entry.StrPoints, true);
            magicRow?.Refresh("Magic", magic, BarVisualMax, entry.MaPoints, true);
            enduranceRow?.Refresh("Endurance", endurance, BarVisualMax, entry.EnPoints, true);
            heartBeatRow?.Refresh("HeartBeat", heartBeat, BarVisualMax, entry.HbPoints, true);
            luckRow?.Refresh("Luck", luck, BarVisualMax, 0, false);

            strengthRow?.SetPlusInteractable(canPlus && entry.StrPoints < MaxPointsPerStat);
            magicRow?.SetPlusInteractable(canPlus && entry.MaPoints < MaxPointsPerStat);
            enduranceRow?.SetPlusInteractable(canPlus && entry.EnPoints < MaxPointsPerStat);
            heartBeatRow?.SetPlusInteractable(canPlus && entry.HbPoints < MaxPointsPerStat);
        }

        private void OpenSkillEquip(int focusSlot)
        {
            _equipFocusSlot = Mathf.Clamp(focusSlot, 0, 2);
            if (skillEquipOverlay != null)
            {
                skillEquipOverlay.SetActive(true);
            }

            RefreshSkillEquip(CurrentEntry());
        }

        private void HideSkillEquip()
        {
            if (skillEquipOverlay != null)
            {
                skillEquipOverlay.SetActive(false);
            }
        }

        private void RefreshSkillEquip(CharacterLoadoutEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (skillEquipTitleLabel != null)
            {
                skillEquipTitleLabel.text = $"Skill Equip — {DisplayName(Roster[_memberIndex])}";
            }

            ClearButtonList(_equipSlotButtons, skillEquipSlotRow);
            ClearButtonList(_equipPoolButtons, skillEquipPoolRow);

            var slots = entry.EquippedSkillIds ?? new[] { string.Empty, string.Empty, string.Empty };
            for (var i = 0; i < 3; i++)
            {
                var slotIndex = i;
                var skillId = i < slots.Length ? slots[i] : string.Empty;
                var label = string.IsNullOrEmpty(skillId)
                    ? $"Slot {i + 1}: (empty)"
                    : $"Slot {i + 1}: {SkillUnlockCatalog.DisplayName(skillId)}";
                var button = CreateEquipButton(skillEquipSlotRow, label, () =>
                {
                    UnequipSlot(entry, slotIndex);
                });
                _equipSlotButtons.Add(button);
            }

            foreach (var unlock in SkillUnlockCatalog.UnlockedFor(Roster[_memberIndex]))
            {
                var skillId = unlock.SkillId;
                var button = CreateEquipButton(
                    skillEquipPoolRow,
                    unlock.DisplayName,
                    () => EquipIntoFocus(entry, skillId));
                _equipPoolButtons.Add(button);
            }
        }

        private void EquipIntoFocus(CharacterLoadoutEntry entry, string skillId)
        {
            var slots = NormalizeSlots(entry.EquippedSkillIds);
            for (var i = 0; i < slots.Length; i++)
            {
                if (string.Equals(slots[i], skillId, StringComparison.Ordinal))
                {
                    slots[i] = string.Empty;
                }
            }

            if (_equipFocusSlot >= 0 && _equipFocusSlot < slots.Length)
            {
                slots[_equipFocusSlot] = skillId;
            }
            else
            {
                for (var i = 0; i < slots.Length; i++)
                {
                    if (string.IsNullOrEmpty(slots[i]))
                    {
                        slots[i] = skillId;
                        break;
                    }
                }
            }

            entry.EquippedSkillIds = slots;
            GameMetaSession.Save();
            Refresh();
        }

        private void UnequipSlot(CharacterLoadoutEntry entry, int slotIndex)
        {
            var slots = NormalizeSlots(entry.EquippedSkillIds);
            if (slotIndex >= 0 && slotIndex < slots.Length)
            {
                slots[slotIndex] = string.Empty;
            }

            entry.EquippedSkillIds = slots;
            GameMetaSession.Save();
            Refresh();
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

        private static void ClearButtonList(List<Button> buttons, Transform row)
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

        private static Button CreateEquipButton(Transform parent, string label, Action onClick)
        {
            if (parent == null)
            {
                return null;
            }

            var go = new GameObject("SkillButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320f, 40f);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.1f, 0.16f, 0.28f, 0.95f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            UiFontCatalog.Apply(text, UiFontRole.Body, 16);
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private static UnitStats ResolveBaseStats(string characterId)
        {
            var preset = LoadPreset(characterId);
            if (preset != null)
            {
                return preset.ResolveStats();
            }

            return characterId switch
            {
                PartyCharacterIds.Charlotte => UnitStats.CreateTankPreset(),
                PartyCharacterIds.Coda => UnitStats.CreateMagePreset(),
                _ => UnitStats.CreateRenPreset()
            };
        }

        private static UnitPresetSO LoadPreset(string characterId)
        {
            var resourceName = characterId switch
            {
                PartyCharacterIds.Charlotte => "UnitPresets/UnitPreset_Tank",
                PartyCharacterIds.Coda => "UnitPresets/UnitPreset_Mage",
                _ => "UnitPresets/UnitPreset_Ren"
            };
            return Resources.Load<UnitPresetSO>(resourceName);
        }

        private static SkillDefinitionSO FindSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return null;
            }

            var all = Resources.LoadAll<SkillDefinitionSO>("Skills");
            foreach (var skill in all)
            {
                if (skill != null &&
                    (string.Equals(skill.skillId, skillId, StringComparison.OrdinalIgnoreCase)
                     || string.Equals(skill.name, skillId, StringComparison.OrdinalIgnoreCase)))
                {
                    return skill;
                }
            }

            return Resources.Load<SkillDefinitionSO>($"Skills/{skillId}");
        }

        private static string DisplayName(string characterId) => characterId switch
        {
            PartyCharacterIds.Charlotte => "Charlotte Vale",
            PartyCharacterIds.Coda => "Coda",
            _ => "Ren Takahashi"
        };
    }
}
