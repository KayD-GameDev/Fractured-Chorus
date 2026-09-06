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
        [SerializeField] private Text indexLabel;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text elementLabel;
        [SerializeField] private Text battleStyleNameLabel;
        [SerializeField] private Text battleStyleDescLine1;
        [SerializeField] private Text battleStyleDescLine2;
        [SerializeField] private Text levelLabel;
        [SerializeField] private Text nextExpLabel;
        [SerializeField] private Image[] elementIcons;
        [SerializeField] private GameObject[] elementHighlightRings;
        [SerializeField] private CharacterBuildPortraitChipView[] portraitChips;

        [Header("Navigation")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        [Header("Skills")]
        [SerializeField] private CharacterBuildSkillRowView[] skillRows = new CharacterBuildSkillRowView[3];

        [Header("Stats")]
        [SerializeField] private CharacterBuildStatRowView strengthRow;
        [SerializeField] private CharacterBuildStatRowView magicRow;
        [SerializeField] private CharacterBuildStatRowView enduranceRow;
        [SerializeField] private CharacterBuildStatRowView heartBeatRow;
        [SerializeField] private CharacterBuildStatRowView luckRow;

        [Header("Remaining Points")]
        [SerializeField] private Text remainingPointsLabel;

        [Header("Portrait")]
        [SerializeField] private CharacterBuildPortraitLayersView portraitLayers;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Sprite[] menuPortraits;
        [SerializeField] private Sprite[] chipFaces;

        [Header("Footer / Equip")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button viewSkillsButton;
        [SerializeField] private GameObject skillEquipHost;
        [SerializeField] private GameObject skillEquipOverlay;
        [SerializeField] private GameObject skillEquipDimmer;
        [SerializeField] private Text skillEquipTitleLabel;
        [SerializeField] private CharacterBuildEquipSlotView[] equipPoolViews;
        [SerializeField] private Button skillEquipCloseButton;
        [SerializeField] private Sprite skillSlotUnlocked;
        [SerializeField] private Sprite skillSlotLocked;

        [Header("Stat Details")]
        [SerializeField] private Button detailsButton;
        [SerializeField] private GameObject statDetailsHost;
        [SerializeField] private GameObject statDetailsOverlay;
        [SerializeField] private GameObject statDetailsDimmer;
        [SerializeField] private Text statDetailsBodyLabel;

        [Header("Dev")]
        [SerializeField] private bool seedUnspentWhenEmpty = true;
        [SerializeField] private int stubLevel = 15;
        [SerializeField] private int stubNextExp = 3600;

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

            ResolveSkillEquipHost();
            ResolveStatDetailsHost();
            EnsureDetailsUi();
            ApplyLockedSkillSlotDecor();
            WireButtons();
            WireStatCallbacks();
            HideSkillEquip();
            HideStatDetails();
        }

        private void Start()
        {
            if (indexLabel == null && (portraitChips == null || portraitChips.Length == 0))
            {
                Debug.LogWarning(
                    "[CharacterBuild] Layout chips not bound. Sandbox: Fractured Chorus → Heal CharacterBuild Layout Sandbox Hierarchy.");
            }

            _state = GameMetaSession.Current;
            EnsureDefaults();
            Refresh();
        }

        private void Update()
        {
            if (IsStatDetailsOpen())
            {
                if (TownMapInput.CancelPressed())
                {
                    HideStatDetails();
                }

                return;
            }

            if (IsSkillEquipOpen())
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

            if (portraitChips != null)
            {
                for (var i = 0; i < portraitChips.Length; i++)
                {
                    var index = i;
                    var chip = portraitChips[i];
                    if (chip == null || chip.Button == null)
                    {
                        continue;
                    }

                    chip.Button.onClick.AddListener(() => SelectMember(index));
                }
            }

            if (skillEquipCloseButton != null)
            {
                skillEquipCloseButton.onClick.AddListener(HideSkillEquip);
            }

            if (detailsButton != null)
            {
                detailsButton.onClick.AddListener(ToggleStatDetails);
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

                    row.Button.onClick.AddListener(() => OpenSkillEquip(index));
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
                entry.EquippedSkillIds = PartyLoadoutState.NormalizeSkillSlots(entry.EquippedSkillIds);

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
                        PartyCharacterIds.Charlotte => PartyLoadoutState.NormalizeSkillSlots(
                            new[] { "Charlott_basic", "tank_skill", "tank_ult" }),
                        PartyCharacterIds.Coda => PartyLoadoutState.NormalizeSkillSlots(
                            new[] { "mage_basic", "mage_skill", "mage_ult" }),
                        _ => PartyLoadoutState.NormalizeSkillSlots(
                            new[] { "ren_basic", "ren_skill", "ren_ult" })
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
            SelectMember((_memberIndex + delta + Roster.Length) % Roster.Length);
        }

        private void SelectMember(int index)
        {
            if (index < 0 || index >= Roster.Length || index == _memberIndex)
            {
                return;
            }

            _memberIndex = index;
            HideSkillEquip();
            HideStatDetails();
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

            if (indexLabel != null)
            {
                indexLabel.text = (_memberIndex + 1).ToString("00");
            }

            if (nameLabel != null)
            {
                nameLabel.text = DisplayName(characterId);
            }

            if (elementLabel != null)
            {
                elementLabel.text = bases.Element.ToString();
            }

            var battleStyle = CharacterBuildBattleStyleCatalog.For(characterId);
            if (battleStyleNameLabel != null)
            {
                battleStyleNameLabel.text = battleStyle.Title;
            }

            if (battleStyleDescLine1 != null)
            {
                battleStyleDescLine1.text = battleStyle.Line1;
            }

            if (battleStyleDescLine2 != null)
            {
                battleStyleDescLine2.text = battleStyle.Line2;
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
            RefreshPortrait();
            RefreshPortraitChips();
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

            var active = (int)element;
            for (var i = 0; i < elementHighlightRings.Length; i++)
            {
                if (elementHighlightRings[i] != null)
                {
                    elementHighlightRings[i].SetActive(i == active);
                }

                if (elementIcons != null && i < elementIcons.Length && elementIcons[i] != null)
                {
                    elementIcons[i].color = i == active ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                }
            }
        }

        private void RefreshPortraitChips()
        {
            if (portraitChips == null)
            {
                return;
            }

            for (var i = 0; i < portraitChips.Length; i++)
            {
                var chip = portraitChips[i];
                if (chip == null)
                {
                    continue;
                }

                chip.BindFace(ResolveChipFace(i));
                chip.SetSelected(i == _memberIndex);
            }
        }

        private Sprite ResolveChipFace(int index)
        {
            if (chipFaces != null && index >= 0 && index < chipFaces.Length && chipFaces[index] != null)
            {
                return chipFaces[index];
            }

            return ResolveMenuPortrait(index);
        }

        private void RefreshPortrait()
        {
            if (portraitLayers != null)
            {
                portraitLayers.SetActiveIndex(_memberIndex);
                return;
            }

            if (portraitImage == null)
            {
                return;
            }

            var sprite = ResolveMenuPortrait(_memberIndex);
            portraitImage.enabled = sprite != null;
            portraitImage.sprite = sprite;
            portraitImage.color = Color.white;
            portraitImage.preserveAspect = true;
        }

        private Sprite ResolveMenuPortrait(int index)
        {
            if (menuPortraits != null && index >= 0 && index < menuPortraits.Length && menuPortraits[index] != null)
            {
                return menuPortraits[index];
            }

            if (index < 0 || index >= Roster.Length)
            {
                return null;
            }

            var preset = LoadPreset(Roster[index]);
            if (preset == null)
            {
                return null;
            }

            return preset.battleSprite != null ? preset.battleSprite : preset.combatCardSprite;
        }

        private void RefreshSkills(CharacterLoadoutEntry entry)
        {
            if (skillRows == null)
            {
                return;
            }

            var slots = NormalizeSlots(entry.EquippedSkillIds);
            for (var i = 0; i < skillRows.Length; i++)
            {
                var row = skillRows[i];
                if (row == null)
                {
                    continue;
                }

                if (i >= slots.Length || string.IsNullOrEmpty(slots[i]))
                {
                    row.BindEmpty(true);
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

            strengthRow?.Refresh("STR", strength, BarVisualMax, entry.StrPoints, true);
            magicRow?.Refresh("MA", magic, BarVisualMax, entry.MaPoints, true);
            enduranceRow?.Refresh("EN", endurance, BarVisualMax, entry.EnPoints, true);
            heartBeatRow?.Refresh("HB", heartBeat, BarVisualMax, entry.HbPoints, true);
            luckRow?.Refresh("LUCK", luck, BarVisualMax, 0, false);

            strengthRow?.SetPlusInteractable(canPlus && entry.StrPoints < MaxPointsPerStat);
            magicRow?.SetPlusInteractable(canPlus && entry.MaPoints < MaxPointsPerStat);
            enduranceRow?.SetPlusInteractable(canPlus && entry.EnPoints < MaxPointsPerStat);
            heartBeatRow?.SetPlusInteractable(canPlus && entry.HbPoints < MaxPointsPerStat);
        }

        private void OpenSkillEquip(int focusSlot)
        {
            HideStatDetails();
            _equipFocusSlot = Mathf.Clamp(focusSlot, 0, CharacterLoadoutEntry.EquippedSkillSlotCount - 1);
            SetSkillEquipVisible(true);
            RefreshSkillEquip(CurrentEntry());
        }

        private void HideSkillEquip()
        {
            SetSkillEquipVisible(false);
        }

        private bool IsSkillEquipOpen()
        {
            var host = ResolveSkillEquipHost();
            return host != null && host.activeSelf;
        }

        private GameObject ResolveSkillEquipHost()
        {
            if (skillEquipHost != null)
            {
                return skillEquipHost;
            }

            if (skillEquipOverlay != null && skillEquipOverlay.transform.parent != null)
            {
                skillEquipHost = skillEquipOverlay.transform.parent.gameObject;
            }

            return skillEquipHost;
        }

        private void SetSkillEquipVisible(bool visible)
        {
            var host = ResolveSkillEquipHost();
            if (host != null)
            {
                if (visible)
                {
                    var rt = host.GetComponent<RectTransform>();
                    if (rt != null && (rt.localScale.x < 0.01f || rt.localScale.y < 0.01f))
                    {
                        rt.localScale = Vector3.one;
                    }
                }

                host.SetActive(visible);
            }

            if (skillEquipOverlay != null)
            {
                skillEquipOverlay.SetActive(visible);
            }

            if (skillEquipDimmer != null)
            {
                skillEquipDimmer.SetActive(visible);
            }
        }

        private void ApplyLockedSkillSlotDecor()
        {
            if (skillSlotLocked == null)
            {
                return;
            }

            var skillsPanel = transform.Find("SkillsPanel");
            if (skillsPanel == null)
            {
                return;
            }

            for (var i = 4; i <= 10; i++)
            {
                var slot = skillsPanel.Find($"SkillSlot_{i}");
                if (slot == null)
                {
                    continue;
                }

                var frame = slot.GetComponent<Image>();
                if (frame != null)
                {
                    frame.sprite = skillSlotLocked;
                    frame.preserveAspect = true;
                    frame.color = new Color(0.78f, 0.82f, 0.9f, 1f);
                }

                var note = slot.Find("NoteCircle");
                if (note != null)
                {
                    note.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureDetailsUi()
        {
            if (detailsButton == null)
            {
                var detailsPanel = transform.Find("DetailsPanel");
                if (detailsPanel != null)
                {
                    detailsButton = detailsPanel.GetComponent<Button>();
                    if (detailsButton == null)
                    {
                        detailsButton = detailsPanel.gameObject.AddComponent<Button>();
                    }

                    var graphic = detailsPanel.GetComponent<Image>();
                    if (graphic != null)
                    {
                        graphic.raycastTarget = true;
                        detailsButton.targetGraphic = graphic;
                    }
                }
            }

            ResolveStatDetailsHost();
            if (statDetailsOverlay == null)
            {
                var host = ResolveStatDetailsHost();
                Transform existing = null;
                if (host != null)
                {
                    existing = host.transform.Find("StatDetailsOverlay");
                }

                if (existing == null)
                {
                    existing = transform.Find("StatDetailsOverlay");
                }

                if (existing == null)
                {
                    var found = GameObject.Find("StatDetailsOverlay");
                    existing = found != null ? found.transform : null;
                }

                if (existing != null)
                {
                    statDetailsOverlay = existing.gameObject;
                }
            }

            WireExistingStatDetailsOverlay();
        }

        private void WireExistingStatDetailsOverlay()
        {
            if (statDetailsOverlay == null)
            {
                return;
            }

            if (statDetailsBodyLabel == null)
            {
                var body = statDetailsOverlay.transform.Find("Body")
                           ?? statDetailsOverlay.transform.Find("Panel/Body");
                if (body != null)
                {
                    statDetailsBodyLabel = body.GetComponent<Text>();
                }
            }

            BindStatDetailsClose(statDetailsOverlay.transform.Find("Panel/Close"));
            BindStatDetailsClose(statDetailsOverlay.transform.Find("Close"));
            BindStatDetailsClose(statDetailsOverlay.transform.Find("CloseButton"));
            if (statDetailsDimmer != null)
            {
                BindStatDetailsClose(statDetailsDimmer.transform);
            }
        }

        private void BindStatDetailsClose(Transform target)
        {
            if (target == null)
            {
                return;
            }

            var button = target.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(HideStatDetails);
            button.onClick.AddListener(HideStatDetails);
        }

        private GameObject ResolveStatDetailsHost()
        {
            if (statDetailsHost != null)
            {
                return statDetailsHost;
            }

            if (statDetailsOverlay != null && statDetailsOverlay.transform.parent != null)
            {
                statDetailsHost = statDetailsOverlay.transform.parent.gameObject;
            }

            return statDetailsHost;
        }

        private void SetStatDetailsVisible(bool visible)
        {
            var host = ResolveStatDetailsHost();
            if (host != null)
            {
                host.SetActive(visible);
            }

            if (statDetailsOverlay != null)
            {
                statDetailsOverlay.SetActive(visible);
            }

            if (statDetailsDimmer != null)
            {
                statDetailsDimmer.SetActive(visible);
            }
        }

        private void ToggleStatDetails()
        {
            if (IsStatDetailsOpen())
            {
                HideStatDetails();
                return;
            }

            ShowStatDetails();
        }

        private bool IsStatDetailsOpen()
        {
            var host = ResolveStatDetailsHost();
            if (host != null)
            {
                return host.activeSelf;
            }

            return statDetailsOverlay != null && statDetailsOverlay.activeSelf;
        }

        private void ShowStatDetails()
        {
            HideSkillEquip();
            EnsureDetailsUi();
            if (statDetailsBodyLabel != null)
            {
                statDetailsBodyLabel.text = BuildStatDetailsText(Roster[_memberIndex]);
            }

            SetStatDetailsVisible(true);
        }

        private void HideStatDetails()
        {
            SetStatDetailsVisible(false);
        }

        private static string BuildStatDetailsText(string characterId)
        {
            var hp = characterId switch
            {
                PartyCharacterIds.Charlotte => "STR×6 + 50",
                PartyCharacterIds.Coda => "STR×2 + MA×0.35 + 15",
                _ => "STR×2 + 30"
            };
            var ma = characterId == PartyCharacterIds.Coda
                ? "MA   Magical power · HP"
                : "MA   Magical power";

            return
                $"STR  Physical power · HP ({hp})\n" +
                $"{ma}\n" +
                "EN   Guard / incoming damage\n" +
                "HB   +5 · action priority\n" +
                "LUCK Crit % · locked";
        }

        private void RefreshSkillEquip(CharacterLoadoutEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            var slots = NormalizeSlots(entry.EquippedSkillIds);
            if (skillEquipTitleLabel != null)
            {
                skillEquipTitleLabel.text =
                    $"Choose skill for Slot {_equipFocusSlot + 1} — {DisplayName(Roster[_memberIndex])}";
            }

            SkillUnlockCatalog.PartyLevel = stubLevel;
            var kit = new List<(string SkillId, string DisplayName, int UnlockLevel, bool Unlocked)>();
            foreach (var unlock in SkillUnlockCatalog.KitFor(Roster[_memberIndex]))
            {
                kit.Add(unlock);
            }

            if (equipPoolViews == null)
            {
                return;
            }

            for (var i = 0; i < equipPoolViews.Length; i++)
            {
                var view = equipPoolViews[i];
                if (view == null)
                {
                    continue;
                }

                if (i >= kit.Count)
                {
                    view.Bind(string.Empty, true, true);
                    view.ApplyFrame(skillSlotUnlocked, skillSlotLocked, true, false);
                    if (view.Button != null)
                    {
                        view.Button.onClick.RemoveAllListeners();
                    }

                    continue;
                }

                var entryKit = kit[i];
                var skillId = entryKit.SkillId;
                if (!entryKit.Unlocked)
                {
                    view.Bind($"Lv {entryKit.UnlockLevel}", true, true);
                    view.ApplyFrame(skillSlotUnlocked, skillSlotLocked, true, false);
                    if (view.Button != null)
                    {
                        view.Button.onClick.RemoveAllListeners();
                    }

                    continue;
                }

                var equippedSlot = IndexOfSkill(slots, skillId);
                var poolLabel = equippedSlot < 0
                    ? entryKit.DisplayName
                    : equippedSlot == _equipFocusSlot
                        ? $"▶ {entryKit.DisplayName}"
                        : $"{entryKit.DisplayName}  [{equippedSlot + 1}]";
                view.Bind(poolLabel, true, false);
                view.ApplyFrame(
                    skillSlotUnlocked,
                    skillSlotLocked,
                    false,
                    equippedSlot == _equipFocusSlot);
                if (view.Button == null)
                {
                    continue;
                }

                view.Button.onClick.RemoveAllListeners();
                view.Button.onClick.AddListener(() => EquipIntoFocus(CurrentEntry(), skillId));
            }
        }

        private void EquipIntoFocus(CharacterLoadoutEntry entry, string skillId)
        {
            var slots = NormalizeSlots(entry.EquippedSkillIds);
            if (_equipFocusSlot >= 0
                && _equipFocusSlot < slots.Length
                && string.Equals(slots[_equipFocusSlot], skillId, StringComparison.Ordinal))
            {
                slots[_equipFocusSlot] = string.Empty;
                entry.EquippedSkillIds = slots;
                GameMetaSession.Save();
                Refresh();
                return;
            }

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

            entry.EquippedSkillIds = slots;
            GameMetaSession.Save();
            Refresh();
        }

        private static int IndexOfSkill(string[] slots, string skillId)
        {
            if (slots == null || string.IsNullOrEmpty(skillId))
            {
                return -1;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                if (string.Equals(slots[i], skillId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string[] NormalizeSlots(string[] source)
        {
            return PartyLoadoutState.NormalizeSkillSlots(source);
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
