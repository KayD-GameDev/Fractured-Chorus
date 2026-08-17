#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class TimelineHierarchyBuilder
    {
        public const float SlotWidth = TimelineLayoutLock.SlotWidth;
        public const float SlotHeight = TimelineLayoutLock.SlotHeight;

        public const float PartyCardWidth = 115f;
        public const float PartyCardHeight = 167f;
        public const float PartyBarWidth = 713f;
        private const float DefaultSkillPanelSize = 300f;
        private const float RadialSlotSize = 96f;
        private const float RadialRootSize = 248f;
        private const float RadialSlotTopY = 107f;
        private const float RadialSlotSideX = 93f;
        private const float RadialSlotBottomY = -53f;
        private const int RadialSlotLabelFontSize = 12;

        private static readonly string[] PreviewPresetResourcePaths =
        {
            "UnitPresets/UnitPreset_Ren",
            "UnitPresets/UnitPreset_Tank",
            "UnitPresets/UnitPreset_Mage",
        };

        public static BeatTimelineUIView BuildTimeline(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("BeatTimelineUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var timelineGo = CreateUiObject("BeatTimelineUI", canvasTransform);
            var rootRect = timelineGo.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.02f, 0.02f);
            rootRect.anchorMax = new Vector2(0.98f, 0.22f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            timelineGo.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            CreateTimelineHeader(timelineGo.transform);
            var viewport = CreateViewport(timelineGo.transform, out var scrollContent, out var scanBar, out var segmentTemplate);

            var ui = timelineGo.AddComponent<BeatTimelineUIView>();
            SetField(ui, "viewport", viewport);
            SetField(ui, "slotsRow", scrollContent);
            SetField(ui, "segmentTemplate", segmentTemplate);
            SetField(ui, "scanBar", scanBar);
            SetField(ui, "slotWidth", SlotWidth);
            EnsureBrowseChevrons(ui);
            ui.WireReferences();
            return ui;
        }

        /// <summary>
        /// Scene-first browse chevrons: create only if missing; never overwrite existing RectTransform layout.
        /// </summary>
        public static void EnsureBrowseChevrons(BeatTimelineUIView timeline)
        {
            if (timeline == null)
            {
                return;
            }

            var left = EnsureBrowseButton(timeline.transform, "BrowseLeftButton", pointLeft: true);
            var right = EnsureBrowseButton(timeline.transform, "BrowseRightButton", pointLeft: false);
            SetField(timeline, "browseLeftButton", left);
            SetField(timeline, "browseRightButton", right);
            EditorUtility.SetDirty(timeline);
        }

        private static Button EnsureBrowseButton(Transform root, string name, bool pointLeft)
        {
            var normal = Resources.Load<Sprite>(pointLeft
                ? "UI/Combat/Timeline/Controls/tlb_browse_chevron_left_v1"
                : "UI/Combat/Timeline/Controls/tlb_browse_chevron_right_v1");
            var hover = Resources.Load<Sprite>(pointLeft
                ? "UI/Combat/Timeline/Controls/tlb_browse_chevron_left_hover_v1"
                : "UI/Combat/Timeline/Controls/tlb_browse_chevron_right_hover_v1");

            var existing = root.Find(name);
            if (existing != null)
            {
                var existingBtn = existing.GetComponent<Button>();
                if (existingBtn == null)
                {
                    existingBtn = existing.gameObject.AddComponent<Button>();
                }

                var existingImg = existing.GetComponent<Image>();
                if (existingImg != null && existingBtn.targetGraphic == null)
                {
                    existingBtn.targetGraphic = existingImg;
                }

                // Seed SpriteSwap hover only when scene has not authored one yet.
                if (existingBtn.transition != Selectable.Transition.SpriteSwap
                    || existingBtn.spriteState.highlightedSprite == null)
                {
                    ApplyBrowseSpriteSwap(existingBtn, existingImg, normal, hover, overwriteNormalSprite: false);
                }

                return existingBtn;
            }

            var go = CreateUiObject(name, root);
            var rt = go.GetComponent<RectTransform>();
            // Initial seed only — scene becomes SoT after first authoring pass.
            if (pointLeft)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(214f, 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-8f, 0f);
            }

            rt.sizeDelta = new Vector2(36f, 36f);

            var img = go.AddComponent<Image>();
            img.raycastTarget = true;
            img.preserveAspect = true;
            img.color = Color.white;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            ApplyBrowseSpriteSwap(btn, img, normal, hover, overwriteNormalSprite: true);
            go.transform.SetAsLastSibling();
            return btn;
        }

        private static void ApplyBrowseSpriteSwap(
            Button btn,
            Image img,
            Sprite normal,
            Sprite hover,
            bool overwriteNormalSprite)
        {
            if (btn == null)
            {
                return;
            }

            if (img != null && overwriteNormalSprite && normal != null)
            {
                img.sprite = normal;
            }

            btn.transition = Selectable.Transition.SpriteSwap;
            var state = btn.spriteState;
            if (hover != null)
            {
                state.highlightedSprite = hover;
                state.selectedSprite = hover;
                state.pressedSprite = hover;
            }

            btn.spriteState = state;
        }

        public static PartyStatusBarUIView BuildPartyStatusBar(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("PartyStatusBarUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var barGo = CreateUiObject("PartyStatusBarUI", canvasTransform);
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(0f, 1f);
            barRect.pivot = new Vector2(0f, 1f);
            barRect.anchoredPosition = new Vector2(12f, -12f);
            barRect.sizeDelta = new Vector2(PartyBarWidth, PartyCardHeight);

            var cardsRowGo = CreateUiObject("CardsRow", barGo.transform);
            var cardsRowRect = cardsRowGo.GetComponent<RectTransform>();
            StretchFull(cardsRowRect);
            var rowLayout = cardsRowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = PartyStatusBarUIView.DefaultCardSpacing;
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            var cardTemplate = CreatePartyCardTemplate(barGo.transform);
            cardTemplate.gameObject.SetActive(false);

            var barUi = barGo.AddComponent<PartyStatusBarUIView>();
            SetField(barUi, "cardsRow", cardsRowRect);
            SetField(barUi, "cardTemplate", cardTemplate);
            SetField(barUi, "cardSpacing", PartyStatusBarUIView.DefaultCardSpacing);
            barUi.WireReferences();
            return barUi;
        }

        public static EnemyStatusBarUIView BuildEnemyStatusBar(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("EnemyStatusBarUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var barGo = CreateUiObject("EnemyStatusBarUI", canvasTransform);
            var barRect = barGo.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(1f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(1f, 1f);
            barRect.anchoredPosition = new Vector2(-12f, -12f);
            barRect.sizeDelta = new Vector2(PartyBarWidth, PartyCardHeight);

            var cardsRowGo = CreateUiObject("CardsRow", barGo.transform);
            var cardsRowRect = cardsRowGo.GetComponent<RectTransform>();
            StretchFull(cardsRowRect);
            var rowLayout = cardsRowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = EnemyStatusBarUIView.DefaultCardSpacing;
            rowLayout.childAlignment = TextAnchor.UpperRight;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.enabled = false;

            var cardTemplate = CreatePartyCardTemplate(barGo.transform);
            cardTemplate.gameObject.SetActive(false);

            var barUi = barGo.AddComponent<EnemyStatusBarUIView>();
            SetField(barUi, "cardsRow", cardsRowRect);
            SetField(barUi, "cardTemplate", cardTemplate);
            SetField(barUi, "cardSpacing", EnemyStatusBarUIView.DefaultCardSpacing);
            barUi.WireReferences();
            return barUi;
        }

        private static PartyMemberCardView CreatePartyCardTemplate(Transform parent, string templateName = "CardTemplate")
        {
            var cardGo = CreateUiObject(templateName, parent);
            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0f, 1f);
            cardRect.anchorMax = new Vector2(0f, 1f);
            cardRect.pivot = new Vector2(0f, 1f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(PartyCardWidth, PartyCardHeight);
            cardGo.AddComponent<LayoutElement>().preferredWidth = PartyCardWidth;
            cardGo.GetComponent<LayoutElement>().preferredHeight = PartyCardHeight;

            var cardArtGo = CreateUiObject("CardArt", cardGo.transform);
            StretchFull(cardArtGo.GetComponent<RectTransform>());
            var cardArtImage = cardArtGo.AddComponent<Image>();
            cardArtImage.color = Color.white;
            cardArtImage.preserveAspect = false;
            cardArtImage.raycastTarget = false;
            cardArtGo.transform.SetAsFirstSibling();

            var healthBgGo = CreateUiObject("HealthBarBg", cardGo.transform);
            var healthBgRect = healthBgGo.GetComponent<RectTransform>();
            healthBgRect.anchorMin = new Vector2(0f, 0f);
            healthBgRect.anchorMax = new Vector2(1f, 0f);
            healthBgRect.pivot = new Vector2(0.5f, 0f);
            healthBgRect.anchoredPosition = new Vector2(0f, 3f);
            healthBgRect.sizeDelta = new Vector2(-6f, 10f);
            var healthBgImage = healthBgGo.AddComponent<Image>();
            healthBgImage.sprite = UiCircleSpriteUtil.White;
            healthBgImage.type = Image.Type.Simple;
            healthBgImage.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            healthBgImage.raycastTarget = false;

            var healthFillGo = CreateUiObject("HealthBarFill", healthBgGo.transform);
            var healthFillRect = healthFillGo.GetComponent<RectTransform>();
            healthFillRect.anchorMin = Vector2.zero;
            healthFillRect.anchorMax = Vector2.one;
            healthFillRect.pivot = new Vector2(0f, 0.5f);
            healthFillRect.offsetMin = Vector2.zero;
            healthFillRect.offsetMax = Vector2.zero;
            var healthFillImage = healthFillGo.AddComponent<Image>();
            healthFillImage.sprite = UiCircleSpriteUtil.White;
            healthFillImage.type = Image.Type.Simple;
            healthFillImage.color = new Color(0.18f, 0.92f, 0.28f, 1f);
            healthFillImage.raycastTarget = false;

            var elementGo = CreateUiObject("ElementBadge", cardGo.transform);
            var elementRect = elementGo.GetComponent<RectTransform>();
            elementRect.anchorMin = new Vector2(1f, 1f);
            elementRect.anchorMax = new Vector2(1f, 1f);
            elementRect.pivot = new Vector2(0.5f, 0.5f);
            elementRect.anchoredPosition = Vector2.zero;
            elementRect.sizeDelta = new Vector2(PartyCardLayout.BadgeSize, PartyCardLayout.BadgeSize);
            var elementRingImage = elementGo.AddComponent<Image>();
            elementRingImage.sprite = UiCircleSpriteUtil.Circle;
            elementRingImage.color = HarmonyElementPalette.GetBadgeRingColor(Combat.Damage.HarmonyElement.Melody);
            elementRingImage.raycastTarget = false;

            var elementIconGo = CreateUiObject("ElementIcon", elementGo.transform);
            var elementIconRect = elementIconGo.GetComponent<RectTransform>();
            StretchFull(elementIconRect);
            elementIconRect.offsetMin = new Vector2(4f, 4f);
            elementIconRect.offsetMax = new Vector2(-4f, -4f);
            var elementIconImage = elementIconGo.AddComponent<Image>();
            elementIconImage.sprite = UiCircleSpriteUtil.Circle;
            elementIconImage.color = Color.white;
            elementIconImage.preserveAspect = true;
            elementIconImage.raycastTarget = false;

            var cardView = cardGo.AddComponent<PartyMemberCardView>();
            SetField(cardView, "cardArtImage", cardArtImage);
            SetField(cardView, "healthBarBg", healthBgImage);
            SetField(cardView, "healthBarFill", healthFillImage);
            SetField(cardView, "healthBarFillRect", healthFillRect);
            SetField(cardView, "elementBadgeRing", elementRingImage);
            SetField(cardView, "elementIcon", elementIconImage);
            cardView.WireReferences();
            return cardView;
        }

        public static SkillPanelUIView BuildSkillPanel(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("SkillPanelUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var panelGo = CreateUiObject("SkillPanelUI", canvasTransform);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchoredPosition = Vector2.zero;
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
            ApplyCircularPanelStyle(panelRect, panelBg);

            var titleGo = CreateUiObject("Title", panelGo.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);
            titleRect.sizeDelta = new Vector2(-16f, 28f);
            var title = titleGo.AddComponent<Text>();
            ApplyText(title);
            title.fontStyle = FontStyle.Bold;
            title.text = "Skills";

            var radialGo = CreateUiObject("Radial", panelGo.transform);
            var radialRect = radialGo.GetComponent<RectTransform>();
            radialRect.anchorMin = new Vector2(0.5f, 0.5f);
            radialRect.anchorMax = new Vector2(0.5f, 0.5f);
            radialRect.pivot = new Vector2(0.5f, 0.5f);
            radialRect.anchoredPosition = new Vector2(0f, -10f);
            radialRect.sizeDelta = new Vector2(RadialRootSize, RadialRootSize);

            const float slotSize = RadialSlotSize;
            var slotTop = CreateRadialSkillSlot(radialRect, "SkillSlot_Top", new Vector2(0f, RadialSlotTopY), slotSize);
            var slotLeft = CreateRadialSkillSlot(radialRect, "SkillSlot_Left", new Vector2(-RadialSlotSideX, RadialSlotBottomY), slotSize);
            var slotRight = CreateRadialSkillSlot(radialRect, "SkillSlot_Right", new Vector2(RadialSlotSideX, RadialSlotBottomY), slotSize);
            var slotTemplate = EnsureSkillSlotTemplate(radialRect, slotTop);

            panelGo.SetActive(false);

            var ui = panelGo.AddComponent<SkillPanelUIView>();
            SetField(ui, "panelRect", panelRect);
            SetField(ui, "radialRoot", radialRect);
            SetField(ui, "slotTop", slotTop);
            SetField(ui, "slotLeft", slotLeft);
            SetField(ui, "slotRight", slotRight);
            SetField(ui, "skillSlotTemplate", slotTemplate);
            SetField(ui, "titleLabel", title);
            SetField(ui, "preserveSceneLayout", true);
            ui.WireReferences();
            return ui;
        }

        /// <summary>
        /// Chỉ thêm SkillSlot_Template + đưa Frame lên trên art — không reset vị trí/size ô đã author.
        /// </summary>
        public static void EnsureSkillChromeTemplateOnPanel(SkillPanelUIView panel)
        {
            if (panel == null)
            {
                return;
            }

            var radialTransform = panel.transform.Find("Radial") as RectTransform;
            if (radialTransform == null)
            {
                return;
            }

            var slotTop = radialTransform.Find("SkillSlot_Top")?.GetComponent<SkillRadialSlotView>();
            var slotLeft = radialTransform.Find("SkillSlot_Left")?.GetComponent<SkillRadialSlotView>();
            var slotRight = radialTransform.Find("SkillSlot_Right")?.GetComponent<SkillRadialSlotView>();

            void UpgradeSlotChrome(Transform slotTransform)
            {
                if (slotTransform == null)
                {
                    return;
                }

                EnsureRadialSlotIcon(slotTransform);
                EnsureSkillSlotFrame(slotTransform);
                if (slotTransform is RectTransform slotRt)
                {
                    SkillSlotChromeSync.ApplySiblingOrder(slotRt);
                }
            }

            UpgradeSlotChrome(slotTop != null ? slotTop.transform : null);
            UpgradeSlotChrome(slotLeft != null ? slotLeft.transform : null);
            UpgradeSlotChrome(slotRight != null ? slotRight.transform : null);

            var slotTemplate = EnsureSkillSlotTemplate(radialTransform, slotTop);
            SetField(panel, "radialRoot", radialTransform);
            SetField(panel, "slotTop", slotTop);
            SetField(panel, "slotLeft", slotLeft);
            SetField(panel, "slotRight", slotRight);
            SetField(panel, "skillSlotTemplate", slotTemplate);
            SetField(panel, "preserveSceneLayout", true);
            panel.WireReferences();
        }

        /// <summary>
        /// Adds Radial + 3 slots to an existing SkillPanelUI and removes legacy Buttons subtree.
        /// </summary>
        public static void MigrateExistingSkillPanel(SkillPanelUIView panel)
        {
            if (panel == null)
            {
                return;
            }

            var panelTransform = panel.transform;
            var buttons = panelTransform.Find("Buttons");
            if (buttons != null)
            {
                Undo.DestroyObjectImmediate(buttons.gameObject);
            }

            var radialTransform = panelTransform.Find("Radial") as RectTransform;
            if (radialTransform == null)
            {
                var radialGo = CreateUiObject("Radial", panelTransform);
                radialTransform = radialGo.GetComponent<RectTransform>();
                radialTransform.anchorMin = new Vector2(0.5f, 0.5f);
                radialTransform.anchorMax = new Vector2(0.5f, 0.5f);
                radialTransform.pivot = new Vector2(0.5f, 0.5f);
                radialTransform.anchoredPosition = new Vector2(0f, -10f);
                radialTransform.sizeDelta = new Vector2(RadialRootSize, RadialRootSize);
                Undo.RegisterCreatedObjectUndo(radialGo, "Setup Skill Panel Radial");
            }

            const float slotSize = RadialSlotSize;
            var slotTop = EnsureRadialSkillSlot(radialTransform, "SkillSlot_Top", new Vector2(0f, RadialSlotTopY), slotSize);
            var slotLeft = EnsureRadialSkillSlot(radialTransform, "SkillSlot_Left", new Vector2(-RadialSlotSideX, RadialSlotBottomY), slotSize);
            var slotRight = EnsureRadialSkillSlot(radialTransform, "SkillSlot_Right", new Vector2(RadialSlotSideX, RadialSlotBottomY), slotSize);
            var slotTemplate = EnsureSkillSlotTemplate(radialTransform, slotTop);

            var panelRect = panelTransform as RectTransform;
            var panelBg = panelRect != null ? panelRect.GetComponent<Image>() : null;
            ApplyCircularPanelStyle(panelRect, panelBg, useMaxExistingExtent: true);
            SetField(panel, "panelRect", panelRect);
            SetField(panel, "radialRoot", radialTransform);
            SetField(panel, "slotTop", slotTop);
            SetField(panel, "slotLeft", slotLeft);
            SetField(panel, "slotRight", slotRight);
            SetField(panel, "skillSlotTemplate", slotTemplate);
            SetField(panel, "preserveSceneLayout", true);

            var title = panelTransform.Find("Title")?.GetComponent<Text>();
            if (title != null)
            {
                SetField(panel, "titleLabel", title);
            }

            panel.WireReferences();
        }

        private static SkillRadialSlotView EnsureRadialSkillSlot(RectTransform parent, string name, Vector2 pos, float size)
        {
            var existing = parent.Find(name)?.GetComponent<SkillRadialSlotView>();
            if (existing != null)
            {
                if (existing.transform is RectTransform slotRect)
                {
                    slotRect.anchoredPosition = pos;
                    slotRect.sizeDelta = new Vector2(size, size);
                }

                UpgradeRadialSlotStyle(existing.transform);
                existing.WireFromScene(DirectionFromSlotName(name));
                return existing;
            }

            return CreateRadialSkillSlot(parent, name, pos, size);
        }

        /// <summary>
        /// Inactive SkillSlot_Template — nguồn chrome cho Top/Left/Right khi mở skill panel.
        /// </summary>
        private static RectTransform EnsureSkillSlotTemplate(RectTransform radial, SkillRadialSlotView sourceSlot)
        {
            if (radial == null)
            {
                return null;
            }

            var existing = radial.Find(SkillSlotChromeSync.TemplateName) as RectTransform;
            if (existing != null)
            {
                EnsureSkillSlotFrame(existing);
                SkillSlotChromeSync.ApplySiblingOrder(existing);
                existing.gameObject.SetActive(false);
                return existing;
            }

            if (sourceSlot == null)
            {
                return null;
            }

            var clone = Object.Instantiate(sourceSlot.gameObject, radial);
            Undo.RegisterCreatedObjectUndo(clone, "Create SkillSlot_Template");
            clone.name = SkillSlotChromeSync.TemplateName;
            clone.SetActive(false);

            var slotView = clone.GetComponent<SkillRadialSlotView>();
            if (slotView != null)
            {
                Object.DestroyImmediate(slotView);
            }

            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                Object.DestroyImmediate(button);
            }

            var templateRt = clone.GetComponent<RectTransform>();
            templateRt.anchoredPosition = Vector2.zero;
            EnsureSkillSlotFrame(clone.transform);
            SkillSlotChromeSync.ApplySiblingOrder(templateRt);
            clone.transform.SetAsFirstSibling();
            return templateRt;
        }

        private static SkillRadialDirection DirectionFromSlotName(string name)
        {
            if (name.Contains("Top"))
            {
                return SkillRadialDirection.Top;
            }

            if (name.Contains("Left"))
            {
                return SkillRadialDirection.Left;
            }

            return SkillRadialDirection.Right;
        }

        private static void ApplyCircularPanelStyle(RectTransform panelRect, Image bg, bool useMaxExistingExtent = false)
        {
            if (panelRect == null)
            {
                return;
            }

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);

            var size = useMaxExistingExtent
                ? Mathf.Max(panelRect.sizeDelta.x, panelRect.sizeDelta.y, DefaultSkillPanelSize)
                : DefaultSkillPanelSize;
            panelRect.sizeDelta = new Vector2(size, size);

            if (bg == null)
            {
                bg = panelRect.GetComponent<Image>();
            }

            if (bg != null)
            {
                bg.sprite = UiCircleSpriteUtil.Circle;
                bg.type = Image.Type.Simple;
            }
        }

        private static void UpgradeRadialSlotStyle(Transform slotTransform)
        {
            if (slotTransform == null)
            {
                return;
            }

            if (slotTransform is RectTransform slotRect)
            {
                var extent = Mathf.Max(slotRect.sizeDelta.x, slotRect.sizeDelta.y);
                if (extent < 1f)
                {
                    extent = RadialSlotSize;
                }

                slotRect.sizeDelta = new Vector2(extent, extent);
            }

            var bg = slotTransform.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = UiCircleSpriteUtil.Circle;
                bg.type = Image.Type.Simple;
            }

            var ring = slotTransform.Find("Ring")?.GetComponent<Image>();
            if (ring != null)
            {
                ring.sprite = UiCircleSpriteUtil.Circle;
                ring.type = Image.Type.Simple;
            }

            EnsureRadialSlotIcon(slotTransform);
            EnsureSkillSlotFrame(slotTransform);

            var label = slotTransform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                ApplyText(label);
                label.fontSize = RadialSlotLabelFontSize;
                label.color = Color.black;
            }

            if (slotTransform is RectTransform slotRt)
            {
                SkillSlotChromeSync.ApplySiblingOrder(slotRt);
            }
        }

        private static void EnsureSkillSlotFrame(Transform slotTransform)
        {
            if (slotTransform == null)
            {
                return;
            }

            var frameTransform = slotTransform.Find("Frame") as RectTransform;
            var created = false;
            if (frameTransform == null)
            {
                var frameGo = CreateUiObject("Frame", slotTransform);
                frameTransform = frameGo.GetComponent<RectTransform>();
                Undo.RegisterCreatedObjectUndo(frameGo, "Add Skill Slot Frame");
                created = true;
            }

            var frameImg = frameTransform.GetComponent<Image>();
            if (frameImg == null)
            {
                frameImg = frameTransform.gameObject.AddComponent<Image>();
                created = true;
            }

            // Chỉ ghi default khi mới tạo — giữ Frame đã author trên SkillSlot_Template.
            if (created)
            {
                StretchWithPadding(frameTransform, 0f, 0f, 1f, 1f);
                frameTransform.offsetMin = new Vector2(-6f, -6f);
                frameTransform.offsetMax = new Vector2(6f, 6f);
                frameImg.sprite = UiCircleSpriteUtil.Circle;
                frameImg.type = Image.Type.Simple;
                frameImg.color = new Color(0.92f, 0.78f, 0.42f, 0.95f);
            }
            else if (frameImg.sprite == null)
            {
                frameImg.sprite = UiCircleSpriteUtil.Circle;
                frameImg.type = Image.Type.Simple;
            }

            frameImg.raycastTarget = false;

            if (slotTransform is RectTransform slotRt)
            {
                SkillSlotChromeSync.ApplySiblingOrder(slotRt);
            }
        }

        private static void EnsureRadialSlotIcon(Transform slotTransform)
        {
            var iconTransform = slotTransform.Find("Icon") as RectTransform;
            if (iconTransform == null)
            {
                var iconGo = CreateUiObject("Icon", slotTransform);
                iconTransform = iconGo.GetComponent<RectTransform>();
            }

            StretchWithPadding(iconTransform, 0.08f, 0.08f, 0.92f, 0.92f);

            var maskGraphic = iconTransform.GetComponent<Image>();
            if (maskGraphic == null)
            {
                maskGraphic = iconTransform.gameObject.AddComponent<Image>();
            }

            maskGraphic.sprite = UiCircleSpriteUtil.Circle;
            maskGraphic.type = Image.Type.Simple;
            maskGraphic.color = Color.white;
            maskGraphic.raycastTarget = false;

            var mask = iconTransform.GetComponent<Mask>();
            if (mask == null)
            {
                mask = iconTransform.gameObject.AddComponent<Mask>();
            }

            mask.showMaskGraphic = false;

            var artTransform = iconTransform.Find("Art") as RectTransform;
            if (artTransform == null)
            {
                var artGo = CreateUiObject("Art", iconTransform);
                artTransform = artGo.GetComponent<RectTransform>();
                StretchWithPadding(artTransform, 0f, 0f, 1f, 1f);
                var artImg = artGo.AddComponent<Image>();
                artImg.raycastTarget = false;
                artImg.preserveAspect = true;
                artImg.enabled = false;
            }
        }

        private static SkillRadialSlotView CreateRadialSkillSlot(RectTransform parent, string name, Vector2 pos, float size)
        {
            var slotGo = CreateUiObject(name, parent);
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = pos;
            slotRect.sizeDelta = new Vector2(size, size);

            var bg = slotGo.AddComponent<Image>();
            bg.sprite = UiCircleSpriteUtil.Circle;
            bg.type = Image.Type.Simple;
            bg.color = new Color(0.16f, 0.16f, 0.22f, 0.96f);

            var ringGo = CreateUiObject("Ring", slotGo.transform);
            var ringRect = ringGo.GetComponent<RectTransform>();
            StretchWithPadding(ringRect, 0f, 0f, 1f, 1f);
            ringRect.offsetMin = new Vector2(-3f, -3f);
            ringRect.offsetMax = new Vector2(3f, 3f);
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.sprite = UiCircleSpriteUtil.Circle;
            ringImg.type = Image.Type.Simple;
            ringImg.color = new Color(0.75f, 0.8f, 0.95f, 1f);
            ringImg.raycastTarget = false;

            EnsureRadialSlotIcon(slotGo.transform);
            EnsureSkillSlotFrame(slotGo.transform);

            var labelGo = CreateUiObject("Label", slotGo.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            StretchWithPadding(labelRect, 0f, 0f, 1f, 1f);
            labelRect.offsetMin = new Vector2(2f, 2f);
            labelRect.offsetMax = new Vector2(-2f, -2f);
            var label = labelGo.AddComponent<Text>();
            ApplyText(label);
            label.fontSize = RadialSlotLabelFontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.color = Color.black;
            label.text = "—";

            SkillSlotChromeSync.ApplySiblingOrder(slotRect);

            var slot = slotGo.AddComponent<SkillRadialSlotView>();
            slot.WireFromScene(DirectionFromSlotName(name));
            return slot;
        }

        public static CombatExecuteOverlayUIView BuildExecuteOverlay(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("ExecuteOverlayUI");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var overlayGo = CreateUiObject("ExecuteOverlayUI", canvasTransform);
            var overlayRect = overlayGo.GetComponent<RectTransform>();
            overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
            overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
            overlayRect.pivot = new Vector2(0.5f, 0.5f);
            overlayRect.anchoredPosition = Vector2.zero;
            overlayRect.sizeDelta = new Vector2(360f, 140f);

            var btnGo = CreateUiObject("ExecuteButton", overlayGo.transform);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = new Vector2(360f, 140f);
            var btnImage = btnGo.AddComponent<Image>();
            btnImage.color = Color.white;
            btnImage.type = Image.Type.Simple;
            btnImage.preserveAspect = true;
            var button = btnGo.AddComponent<Button>();
            btnGo.AddComponent<UiButtonHoverFeedback>();

            var labelGo = CreateUiObject("Label", btnGo.transform);
            StretchFull(labelGo.GetComponent<RectTransform>());
            var label = labelGo.AddComponent<Text>();
            ApplyText(label);
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.text = "Deploy";

            var overlay = overlayGo.AddComponent<CombatExecuteOverlayUIView>();
            SetField(overlay, "executeButton", button);
            SetField(overlay, "labelText", label);
            SetField(overlay, "buttonImage", btnImage);
            SetField(overlay, "buttonRect", btnRect);
            SetField(overlay, "buttonSize", 360f, 140f);
            overlay.WireReferences();
            return overlay;
        }

        private static RectTransform CreateViewport(Transform parent, out RectTransform scrollContent,
            out RectTransform scanBar, out BeatSegmentView segmentTemplate)
        {
            var viewportGo = CreateUiObject("Viewport", parent);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            StretchWithPadding(viewportRect, 0f, 0f, 1f, 1f);
            viewportRect.offsetMin = new Vector2(120f, 8f);
            viewportRect.offsetMax = new Vector2(-8f, -8f);
            var viewportBg = viewportGo.AddComponent<Image>();
            viewportBg.color = new Color(0f, 0f, 0f, 0.25f);
            viewportGo.AddComponent<RectMask2D>();

            var scrollGo = CreateUiObject("ScrollContent", viewportGo.transform);
            scrollContent = scrollGo.GetComponent<RectTransform>();
            scrollContent.anchorMin = new Vector2(0f, 0f);
            scrollContent.anchorMax = new Vector2(0f, 1f);
            scrollContent.pivot = new Vector2(0f, 0.5f);
            scrollContent.anchoredPosition = Vector2.zero;
            scrollContent.offsetMin = Vector2.zero;
            scrollContent.offsetMax = Vector2.zero;

            var layout = scrollGo.AddComponent<HorizontalLayoutGroup>();
            layout.enabled = false;
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            segmentTemplate = CreateBeatSegment(scrollGo.transform, 0);

            var scanGo = CreateUiObject("ScanBar", viewportGo.transform);
            scanBar = scanGo.GetComponent<RectTransform>();
            scanBar.anchorMin = new Vector2(0f, 0f);
            scanBar.anchorMax = new Vector2(0f, 1f);
            scanBar.pivot = new Vector2(0.5f, 0.5f);
            scanBar.sizeDelta = new Vector2(6f, -4f);
            scanBar.anchoredPosition = new Vector2(SlotWidth * 0.5f, 0f);
            var scanImg = scanGo.AddComponent<Image>();
            scanImg.color = new Color(1f, 0.15f, 0.1f, 0.85f);

            var trackGo = CreateUiObject("TrackLine", viewportGo.transform);
            trackGo.transform.SetAsFirstSibling();
            var trackRect = trackGo.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(1f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = new Vector2(0f, 6f);
            trackRect.sizeDelta = new Vector2(0f, 2f);
            trackGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);

            return viewportRect;
        }

        private static BeatSegmentView CreateBeatSegment(Transform parent, int index)
        {
            var segGo = CreateUiObject($"Beat_{index}", parent);
            var segRect = segGo.GetComponent<RectTransform>();
            segRect.sizeDelta = new Vector2(SlotWidth, SlotHeight);
            segGo.AddComponent<LayoutElement>().preferredWidth = SlotWidth;
            segGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.85f);

            var beatFrameGo = CreateUiObject("BeatFrame", segGo.transform);
            StretchWithPadding(beatFrameGo.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            var beatFrameRect = beatFrameGo.GetComponent<RectTransform>();
            beatFrameRect.offsetMin = new Vector2(1.5f, 1.5f);
            beatFrameRect.offsetMax = new Vector2(-1.5f, -1.5f);
            var beatFrameImg = beatFrameGo.AddComponent<Image>();
            beatFrameImg.color = new Color(1f, 1f, 1f, 0.55f);
            beatFrameImg.raycastTarget = false;
            beatFrameImg.enabled = false;

            var glowGo = CreateUiObject("Glow", segGo.transform);
            StretchWithPadding(glowGo.GetComponent<RectTransform>(), 0.05f, 0.1f, 0.95f, 0.9f);
            glowGo.AddComponent<Image>().color = new Color(1f, 0.2f, 0.2f, 0.15f);

            var noteTierGo = CreateUiObject("NoteTier", segGo.transform);
            var noteTierRect = noteTierGo.GetComponent<RectTransform>();
            noteTierRect.anchorMin = new Vector2(0.5f, 0.72f);
            noteTierRect.anchorMax = new Vector2(0.5f, 0.72f);
            noteTierRect.pivot = new Vector2(0.5f, 0.5f);
            noteTierRect.anchoredPosition = Vector2.zero;
            noteTierRect.sizeDelta = new Vector2(40f, 40f);
            var noteTierImg = noteTierGo.AddComponent<Image>();
            noteTierImg.color = new Color(0.4f, 0.4f, 0.5f, 1f);
            noteTierImg.raycastTarget = false;
            noteTierImg.preserveAspect = true;

            // Legacy Portrait alias — kept for older WireReferences paths / scene patches.
            var portraitGo = CreateUiObject("Portrait", segGo.transform);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.anchoredPosition = Vector2.zero;
            portraitRect.sizeDelta = new Vector2(24f, 24f);
            var portraitImg = portraitGo.AddComponent<Image>();
            portraitImg.color = new Color(0.4f, 0.4f, 0.5f, 0.15f);
            portraitImg.raycastTarget = false;
            portraitGo.SetActive(false);

            var labelGo = CreateUiObject("ActionLabel", segGo.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 4f);
            labelRect.offsetMax = new Vector2(-4f, -4f);
            var label = labelGo.AddComponent<Text>();
            ApplyText(label);
            label.fontSize = 10;
            label.fontStyle = FontStyle.Italic;
            label.alignment = TextAnchor.LowerCenter;

            if (TimelineConstants.IsPhaseDividerAfter(index))
            {
                CreatePhaseDivider(segGo.transform);
            }
            else if (index == 0)
            {
                CreatePhaseDivider(segGo.transform);
            }

            var segment = segGo.AddComponent<BeatSegmentView>();
            segment.SetDisplayBeatIndex(index);
            SetField(segment, "beatFrame", beatFrameImg);
            SetField(segment, "noteTier", noteTierImg);
            SetField(segment, "portrait", noteTierImg);
            segment.WireReferences();
            return segment;
        }

        /// <summary>Ensure BeatFrame + NoteTier exist on the timeline segment template (scene-first tuning).</summary>
        public static void EnsureBeatTemplateVisuals(BeatTimelineUIView timeline)
        {
            if (timeline == null)
            {
                return;
            }

            EnsureBrowseChevrons(timeline);

            var templateField = typeof(BeatTimelineUIView).GetField("segmentTemplate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public);
            var template = templateField?.GetValue(timeline) as BeatSegmentView;
            if (template == null)
            {
                return;
            }

            EnsureBeatSegmentAuthoredVisuals(template);
        }

        /// <summary>
        /// Seed edit-mode preview: BorderTop@215, LaneLines Top=15/Bottom=-15,
        /// 4 lane/avatar shells, NoteSingle_0 + Beat_1 enemy frame/note (RemainingHits editable).
        /// </summary>
        public static bool SeedTimelineLanePreview(BeatTimelineUIView timeline = null)
        {
            if (timeline == null)
            {
                timeline = Object.FindAnyObjectByType<BeatTimelineUIView>(FindObjectsInactive.Include);
            }

            if (timeline == null)
            {
                Debug.LogWarning("[Fractured Chorus] BeatTimelineUIView not found — open CombatPrototype first.");
                return false;
            }

            Undo.RegisterFullObjectHierarchyUndo(timeline.gameObject, "Seed Timeline Lane Preview");
            timeline.WireReferences();

            const int maxLanes = 4;
            const float railY = 215f;
            const float laneGap = 32f;
            const float defaultAvatarSize = 42f;
            const float noteW = 52.95f;
            const float noteH = 67.24f;
            const float beamedW = 99.13f;
            const float beamedH = 125.88f;
            const float laneLinesTop = 15f;
            const float laneLinesBottom = -15f;
            const int beat1Hits = 3;

            var so = new SerializedObject(timeline);
            SetFloatProp(so.FindProperty("bossNoteRailAnchoredY"), railY);
            SetFloatProp(so.FindProperty("laneGapBelowRail"), laneGap);
            SetFloatProp(so.FindProperty("laneLinesTopInset"), laneLinesTop);
            SetFloatProp(so.FindProperty("laneLinesBottomInset"), laneLinesBottom);
            var noteVisuals = so.FindProperty("noteVisuals");
            if (noteVisuals != null)
            {
                SetFloatProp(noteVisuals.FindPropertyRelative("NoteDisplayWidth"), noteW);
                SetFloatProp(noteVisuals.FindPropertyRelative("NoteDisplayHeight"), noteH);
                SetFloatProp(noteVisuals.FindPropertyRelative("NoteDisplaySize"), noteW);
                SetFloatProp(noteVisuals.FindPropertyRelative("NoteBeamedWidth"), beamedW);
                SetFloatProp(noteVisuals.FindPropertyRelative("NoteBeamedHeight"), beamedH);
            }

            var leftRail = so.FindProperty("leftRailLayout");
            if (leftRail != null)
            {
                // Hierarchy-first: never force auto gutter layout on seed.
                var forceProp = leftRail.FindPropertyRelative("forceAvatarLayout");
                if (forceProp != null)
                {
                    forceProp.boolValue = false;
                }

                var preserveProp = leftRail.FindPropertyRelative("preserveSceneRects");
                if (preserveProp != null)
                {
                    preserveProp.boolValue = true;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            var viewport = so.FindProperty("viewport")?.objectReferenceValue as RectTransform;
            var laneAvatarGutter = so.FindProperty("laneAvatarGutter")?.objectReferenceValue as RectTransform;
            var slotsRow = so.FindProperty("slotsRow")?.objectReferenceValue as RectTransform;
            var laneMinY = so.FindProperty("laneBandMinNormalizedY")?.floatValue ?? 0.12f;
            var bossBorder = so.FindProperty("bossTrackFrameBorderThickness")?.floatValue ?? 2f;
            var slotWidth = so.FindProperty("slotWidth")?.floatValue ?? SlotWidth;
            slotWidth = Mathf.Max(slotWidth, TimelineLayoutLock.SlotWidth);

            if (viewport == null)
            {
                viewport = timeline.transform.Find("Viewport") as RectTransform;
            }

            if (viewport == null)
            {
                Debug.LogWarning("[Fractured Chorus] Viewport missing under BeatTimelineUI.");
                return false;
            }

            if (slotsRow == null)
            {
                slotsRow = viewport.Find("ScrollContent") as RectTransform;
            }

            if (laneAvatarGutter == null)
            {
                laneAvatarGutter = timeline.transform.Find("LaneAvatarGutter") as RectTransform;
            }

            // Prefer authored avatar size / gutter from scene before recreating shells.
            var avatarSize = defaultAvatarSize;
            var avatarAnchorX = 0f;
            if (laneAvatarGutter != null)
            {
                var existing0 = laneAvatarGutter.Find("LaneAvatar_0") as RectTransform;
                if (existing0 != null && existing0.sizeDelta.x > 1f)
                {
                    avatarSize = existing0.sizeDelta.x;
                    avatarAnchorX = existing0.anchoredPosition.x;
                }
            }

            if (leftRail != null)
            {
                // Mirror scene size into layout as fallback only (Play reads Hierarchy first).
                SetFloatProp(leftRail.FindPropertyRelative("avatarSlotSize"), avatarSize);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)timeline.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
            var viewportHeight = ResolvePreviewViewportHeight(timeline.transform as RectTransform, viewport);

            var presets = LoadPreviewPartyPresets();
            var minY = viewportHeight * laneMinY;
            var maxY = Mathf.Max(minY + 8f, railY - laneGap);

            var laneLines = EnsureStretchLayer(viewport, "LaneLines");
            ApplyLaneLinesInsets(laneLines, laneLinesTop, laneLinesBottom);
            ClearNamedChildren(laneLines, "Lane_");

            // Seed 4 shells; aesthetic stretch as if 4 lanes (Play re-stretches by live count).
            for (var i = 0; i < maxLanes; i++)
            {
                var laneY = maxLanes == 1
                    ? (minY + maxY) * 0.5f
                    : Mathf.Lerp(maxY, minY, (float)i / (maxLanes - 1));
                UnitPresetSO preset = i < presets.Length ? presets[i] : null;
                var tint = preset != null
                    ? preset.ResolveTimelineLaneColor()
                    : new Color(0.45f, 0.45f, 0.55f, 0.55f);
                var label = preset != null
                    ? (!string.IsNullOrEmpty(preset.displayName)
                        ? preset.displayName.ToUpperInvariant()
                        : preset.unitId)
                    : $"SLOT {i + 1}";
                CreatePreviewLane(laneLines, i, label, tint, laneY);
            }

            if (laneAvatarGutter == null)
            {
                var gutterGo = CreateUiObject("LaneAvatarGutter", timeline.transform);
                Undo.RegisterCreatedObjectUndo(gutterGo, "Create LaneAvatarGutter");
                laneAvatarGutter = gutterGo.GetComponent<RectTransform>();
                // Only brand-new gutter gets default shell; existing Hierarchy rect is kept.
                laneAvatarGutter.anchorMin = new Vector2(0f, 0f);
                laneAvatarGutter.anchorMax = new Vector2(0f, 1f);
                laneAvatarGutter.pivot = new Vector2(0f, 0.5f);
                laneAvatarGutter.sizeDelta = new Vector2(72f, 0f);
                laneAvatarGutter.anchoredPosition = new Vector2(139f, 0f);
                SetField(timeline, "laneAvatarGutter", laneAvatarGutter);
            }

            ClearNamedChildren(laneAvatarGutter, "LaneAvatar_");
            for (var i = 0; i < maxLanes; i++)
            {
                var laneY = maxLanes == 1
                    ? (minY + maxY) * 0.5f
                    : Mathf.Lerp(maxY, minY, (float)i / (maxLanes - 1));
                UnitPresetSO preset = i < presets.Length ? presets[i] : null;
                var tint = preset != null
                    ? preset.ResolveTimelineLaneColor()
                    : new Color(0.45f, 0.45f, 0.55f, 0.55f);
                CreatePreviewLaneAvatar(
                    laneAvatarGutter,
                    i,
                    tint,
                    laneY,
                    avatarAnchorX,
                    avatarSize,
                    preset != null ? preset.timelineAvatarSprite : null);
            }

            SeedBossTrackFrame(viewport, railY, bossBorder);
            SeedExampleNotes(timeline, viewport, slotsRow, railY, slotWidth, noteW, noteH, beat1Hits);

            EditorUtility.SetDirty(timeline);
            EditorSceneManager.MarkSceneDirty(timeline.gameObject.scene);
            Debug.Log(
                "[Fractured Chorus] Seeded rail@215, LaneLines Top=15/Bottom=-15, notes from NoteSimulator, Beat_1. Save scene.");
            return true;
        }

        private static void SetFloatProp(SerializedProperty prop, float value)
        {
            if (prop != null)
            {
                prop.floatValue = value;
            }
        }

        private static UnitPresetSO[] LoadPreviewPartyPresets()
        {
            var list = new System.Collections.Generic.List<UnitPresetSO>();
            foreach (var path in PreviewPresetResourcePaths)
            {
                var preset = Resources.Load<UnitPresetSO>(path);
                if (preset != null)
                {
                    list.Add(preset);
                }
            }

            return list.ToArray();
        }

        private static float ResolvePreviewViewportHeight(RectTransform timelineRoot, RectTransform viewport)
        {
            var h = viewport != null ? viewport.rect.height : 0f;
            if (h >= 40f && h <= 400f)
            {
                return h;
            }

            if (timelineRoot != null)
            {
                var rootH = timelineRoot.rect.height;
                if (rootH >= 40f && rootH <= 400f)
                {
                    // Viewport usually fills most of BeatTimelineUI after header gutter.
                    return Mathf.Max(40f, rootH - 8f);
                }
            }

            // Fallback when Canvas has not laid out yet (edit-mode / batch).
            return TimelineLayoutLock.SlotHeight > 0f
                ? Mathf.Max(96f, TimelineLayoutLock.SlotHeight)
                : 130f;
        }

        private static RectTransform EnsureStretchLayer(RectTransform viewport, string name)
        {
            var existing = viewport.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var go = CreateUiObject(name, viewport);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            var rect = go.GetComponent<RectTransform>();
            StretchFull(rect);
            return rect;
        }

        private static void ClearNamedChildren(Transform parent, string namePrefix)
        {
            if (parent == null)
            {
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name.StartsWith(namePrefix))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        private static void CreatePreviewLane(
            RectTransform parent, int index, string labelText, Color tint, float laneY)
        {
            var lineGo = CreateUiObject($"Lane_{index}", parent);
            Undo.RegisterCreatedObjectUndo(lineGo, "Create Lane Preview");
            var lineRect = lineGo.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0f, 0f);
            lineRect.anchorMax = new Vector2(1f, 0f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(0f, 5f);
            lineRect.anchoredPosition = new Vector2(0f, laneY);
            var lineImage = lineGo.AddComponent<Image>();
            lineImage.color = new Color(
                Mathf.Min(1f, tint.r * 1.15f + 0.08f),
                Mathf.Min(1f, tint.g * 1.15f + 0.08f),
                Mathf.Min(1f, tint.b * 1.15f + 0.08f),
                0.92f);
            lineImage.raycastTarget = false;

            var labelGo = CreateUiObject("Label", lineGo.transform);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(4f, 8f);
            labelRect.sizeDelta = new Vector2(90f, 14f);
            var label = labelGo.AddComponent<Text>();
            ApplyText(label);
            label.fontSize = 10;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = new Color(tint.r, tint.g, tint.b, 0.9f);
            label.text = labelText;
            label.raycastTarget = false;
        }

        private static void CreatePreviewLaneAvatar(
            RectTransform parent,
            int index,
            Color tint,
            float laneY,
            float anchorX,
            float slotSize,
            Sprite avatarSprite)
        {
            var slotGo = CreateUiObject($"LaneAvatar_{index}", parent);
            Undo.RegisterCreatedObjectUndo(slotGo, "Create LaneAvatar Preview");
            var slotRect = slotGo.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0f);
            slotRect.anchorMax = new Vector2(0.5f, 0f);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(anchorX, laneY);
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);

            var avatar = slotGo.GetComponent<Image>();
            if (avatar == null)
            {
                avatar = slotGo.AddComponent<Image>();
            }

            if (avatarSprite != null)
            {
                avatar.sprite = avatarSprite;
                avatar.preserveAspect = true;
                avatar.color = Color.white;
            }
            else
            {
                avatar.sprite = UiCircleSpriteUtil.Circle;
                avatar.color = new Color(tint.r, tint.g, tint.b, 1f);
            }

            avatar.raycastTarget = false;

            // Hierarchy-first FrameRing — Play assigns laneAvatarRingSprite onto this Image.
            var frameGo = CreateUiObject("FrameRing", slotRect);
            var frameRect = frameGo.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = Vector2.zero;
            frameRect.offsetMax = Vector2.zero;
            var frameImage = frameGo.AddComponent<Image>();
            frameImage.type = Image.Type.Simple;
            frameImage.preserveAspect = true;
            frameImage.raycastTarget = false;
            frameImage.color = Color.white;

            var selGo = CreateUiObject("SelectionRing", slotRect);
            var selRect = selGo.GetComponent<RectTransform>();
            selRect.anchorMin = Vector2.zero;
            selRect.anchorMax = Vector2.one;
            selRect.offsetMin = new Vector2(-4f, -4f);
            selRect.offsetMax = new Vector2(4f, 4f);
            var selImage = selGo.AddComponent<Image>();
            selImage.type = Image.Type.Simple;
            selImage.preserveAspect = true;
            selImage.raycastTarget = false;
            selImage.color = new Color(1f, 0.55f, 1f, 1f);
            selImage.enabled = false;

            var slotView = slotGo.AddComponent<TimelineLaneAvatarSlotView>();
            slotView.Bind(null, null);
        }

        private static void SeedBossTrackFrame(
            RectTransform viewport,
            float railY,
            float borderThickness)
        {
            var existing = viewport.Find("BossTrackFrame");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var rootGo = CreateUiObject("BossTrackFrame", viewport);
            Undo.RegisterCreatedObjectUndo(rootGo, "Create BossTrackFrame");
            var root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0f, 0.5f);

            var borderH = Mathf.Max(1f, borderThickness);
            var width = Mathf.Max(viewport.rect.width, SlotWidth * 8f);
            root.sizeDelta = new Vector2(width, borderH);
            root.anchoredPosition = new Vector2(0f, railY);

            // BorderTop only — note head belly pins here. No Fill / BorderBottom.
            var top = CreateBossTrackChildPreview("BorderTop", root, stretch: false);
            top.color = FcColorTokens.WithAlpha(FcColorTokens.Brand.CyanNeonCore, 0.95f);
            var topRt = top.rectTransform;
            topRt.anchoredPosition = Vector2.zero;
            topRt.sizeDelta = new Vector2(0f, borderH);
        }

        private static Image CreateBossTrackChildPreview(string name, RectTransform parent, bool stretch)
        {
            var go = CreateUiObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            if (stretch)
            {
                StretchFull(rect);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(0f, 2f);
            }

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        private static void ApplyLaneLinesInsets(RectTransform laneLines, float topInset, float bottomInset)
        {
            if (laneLines == null)
            {
                return;
            }

            laneLines.anchorMin = Vector2.zero;
            laneLines.anchorMax = Vector2.one;
            laneLines.pivot = new Vector2(0.5f, 0.5f);
            laneLines.anchoredPosition = Vector2.zero;
            laneLines.sizeDelta = Vector2.zero;
            laneLines.offsetMin = new Vector2(0f, bottomInset);
            laneLines.offsetMax = new Vector2(0f, -topInset);
        }

        private static void SeedExampleNotes(
            BeatTimelineUIView timeline,
            RectTransform viewport,
            RectTransform slotsRow,
            float railY,
            float slotWidth,
            float noteW,
            float noteH,
            int beat1Hits)
        {
            var existingLayer = viewport.Find("BossNoteClusterLayer");
            RectTransform layer;
            if (existingLayer != null)
            {
                layer = existingLayer as RectTransform;
                ClearNamedChildren(layer, "NoteSingle_");
                ClearNamedChildren(layer, "NoteBeamed_");
                // Keep NoteSimulator — user-tuned size + RailAnchor survive reseeds.
            }
            else
            {
                var go = CreateUiObject("BossNoteClusterLayer", viewport);
                Undo.RegisterCreatedObjectUndo(go, "Create BossNoteClusterLayer");
                layer = go.GetComponent<RectTransform>();
                StretchFull(layer);
                if (go.GetComponent<BossNoteClusterView>() == null)
                {
                    go.AddComponent<BossNoteClusterView>();
                }
            }

            var catalog = BuildSeedCatalog(timeline, noteW, noteH);
            CreateNoteSimulator(layer, catalog, railY, slotWidth, noteW, noteH, beat1Hits);
            SeedBeat1EnemyFrame(timeline, slotsRow, slotWidth, catalog, beat1Hits);
        }

        private static TimelineNoteVisualCatalog BuildSeedCatalog(
            BeatTimelineUIView timeline, float noteW, float noteH)
        {
            var catalog = new TimelineNoteVisualCatalog();
            var so = new SerializedObject(timeline);
            var noteVisualsProp = so.FindProperty("noteVisuals");
            if (noteVisualsProp != null)
            {
                catalog.NoteRed = noteVisualsProp.FindPropertyRelative("NoteRed")?.objectReferenceValue as Sprite;
                catalog.BeatFrameImpact =
                    noteVisualsProp.FindPropertyRelative("BeatFrameImpact")?.objectReferenceValue as Sprite;
            }

            catalog.NoteDisplayWidth = noteW;
            catalog.NoteDisplayHeight = noteH;
            catalog.NoteDisplaySize = noteW;
            catalog.EnsureDefaultsLoaded();
            return catalog;
        }

        private static void CreateNoteSimulator(
            RectTransform layer,
            TimelineNoteVisualCatalog catalog,
            float railY,
            float slotWidth,
            float noteW,
            float noteH,
            int remainingHits)
        {
            var existing = BossNoteSimulator.FindInLayer(layer);
            if (existing != null)
            {
                existing.SyncLayoutToCatalog();
                return;
            }

            var sprite = catalog.MusicSingle(0, BossNoteTier.Red) ?? catalog.NoteRed;
            var layout = new BossNoteNumberLayout();
            layout.EnsureSingleHeadNormByVariant();
            var w = noteW;
            var h = noteH;
            var x = slotWidth * 1.5f;
            var headLocal = FittedPreviewHeadLocal(layout, w, h, sprite);
            var knobSize = new Vector2(
                Mathf.Max(20f, w * layout.numberSizeFactor),
                Mathf.Max(20f, w * layout.numberSizeFactor));

            var noteGo = CreateUiObject(BossNoteSimulator.ObjectName, layer);
            Undo.RegisterCreatedObjectUndo(noteGo, "Create NoteSimulator");
            var noteRt = noteGo.GetComponent<RectTransform>();
            noteRt.anchorMin = new Vector2(0f, 0f);
            noteRt.anchorMax = new Vector2(0f, 0f);
            noteRt.pivot = new Vector2(0.5f, 0.5f);
            noteRt.anchoredPosition = new Vector2(x - headLocal.x, railY - headLocal.y);
            noteRt.sizeDelta = new Vector2(w, h);
            var img = noteGo.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = true;
            img.color = new Color(1f, 1f, 1f, catalog.NoteAlpha > 0.01f ? catalog.NoteAlpha : 0.78f);

            var shape = BossNoteShapeLayout.FromKnob(headLocal, knobSize, Vector2.zero, Vector2.zero);
            var knob = BossNoteSimulator.EnsureKnobOn(noteRt, shape);

            var numGo = CreateUiObject(BossNoteSimulator.NoteNumName, knob);
            var numRt = numGo.GetComponent<RectTransform>();
            numRt.anchorMin = new Vector2(0.5f, 0.5f);
            numRt.anchorMax = new Vector2(0.5f, 0.5f);
            numRt.pivot = new Vector2(0.5f, 0.5f);
            numRt.anchoredPosition = Vector2.zero;
            numRt.sizeDelta = knobSize;
            var numText = numGo.AddComponent<Text>();
            ApplyText(numText);
            numText.fontSize = Mathf.RoundToInt(Mathf.Max(10f, knobSize.x * 0.55f));
            numText.alignment = TextAnchor.MiddleCenter;
            numText.raycastTarget = false;
            numText.text = remainingHits > 0 ? Mathf.Clamp(remainingHits, 0, 9).ToString() : string.Empty;

            var sim = noteGo.AddComponent<BossNoteSimulator>();
            var soTpl = new SerializedObject(sim);
            var timelineProp = soTpl.FindProperty("timeline");
            if (timelineProp != null)
            {
                timelineProp.objectReferenceValue = layer.GetComponentInParent<BeatTimelineUIView>();
            }

            soTpl.FindProperty("shapePreview").intValue = 0;
            soTpl.ApplyModifiedPropertiesWithoutUndo();
            sim.EnsureKnobHierarchy();
            sim.SaveCurrentShapeLayout();
        }

        private static Vector2 FittedPreviewHeadLocal(
            BossNoteNumberLayout layout, float w, float h, Sprite sprite)
        {
            var headNorm = layout.ResolveSingleHeadNorm(0);
            var headLocal = new Vector2(headNorm.x * w, headNorm.y * h);
            if (sprite == null)
            {
                return headLocal;
            }

            var sprAspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            var rectAspect = w / Mathf.Max(0.01f, h);
            var drawW = rectAspect > sprAspect ? h * sprAspect : w;
            var drawH = rectAspect > sprAspect ? h : w / sprAspect;
            return new Vector2(headNorm.x * drawW, headNorm.y * drawH);
        }

        private static Vector2 FittedPreviewNumberLocal(
            BossNoteNumberLayout layout, float w, float h, Sprite sprite) =>
            FittedPreviewHeadLocal(layout, w, h, sprite) + layout.numberNudgeSingle;

        private static void SeedBeat1EnemyFrame(
            BeatTimelineUIView timeline,
            RectTransform slotsRow,
            float slotWidth,
            TimelineNoteVisualCatalog catalog,
            int remainingHits)
        {
            if (slotsRow == null)
            {
                return;
            }

            var beat0 = slotsRow.Find("Beat_0");
            if (beat0 == null)
            {
                Debug.LogWarning("[Fractured Chorus] Beat_0 missing — cannot seed Beat_1.");
                return;
            }

            var existing = slotsRow.Find("Beat_1");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var cloneGo = Object.Instantiate(beat0.gameObject, slotsRow);
            Undo.RegisterCreatedObjectUndo(cloneGo, "Create Beat_1");
            cloneGo.name = "Beat_1";
            cloneGo.hideFlags = HideFlags.None;

            var rt = cloneGo.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(slotWidth, rt.anchoredPosition.y);
                rt.sizeDelta = new Vector2(slotWidth, rt.sizeDelta.y);
            }

            var segment = cloneGo.GetComponent<BeatSegmentView>();
            if (segment == null)
            {
                segment = cloneGo.AddComponent<BeatSegmentView>();
            }

            EnsureBeatSegmentAuthoredVisuals(segment);
            segment.SetDisplayBeatIndex(1);
            segment.WireReferences();
            segment.SetNoteVisualCatalog(catalog);
            segment.SetNoteBandNormalizedY(0.78f);

            // Preview enemy impact frame + hits (edit-mode). Play refreshes from telegraphs.
            ApplyImpactFramePreview(segment, catalog, remainingHits);
            EditorUtility.SetDirty(segment);
            EditorUtility.SetDirty(timeline);
        }

        private static void ApplyImpactFramePreview(
            BeatSegmentView segment,
            TimelineNoteVisualCatalog catalog,
            int remainingHits)
        {
            if (segment == null)
            {
                return;
            }

            var root = segment.transform;
            var beatFrame = root.Find("BeatFrame")?.GetComponent<Image>();
            if (beatFrame == null)
            {
                return;
            }

            catalog?.EnsureDefaultsLoaded();
            var sprite = catalog != null ? catalog.BeatFrame(hasTelegraph: true, isWindup: false) : null;
            beatFrame.enabled = true;
            beatFrame.sprite = sprite;
            beatFrame.type = Image.Type.Simple;
            beatFrame.preserveAspect = false;
            beatFrame.color = new Color(1f, 1f, 1f, 0.55f);
            beatFrame.raycastTarget = false;

            var action = root.Find("ActionLabel")?.GetComponent<Text>();
            if (action != null)
            {
                action.text = remainingHits > 0 ? $"◆ ENEMY · {remainingHits}" : "◆ PERFECT";
            }
        }

        private static void SeedExampleNoteOnBeat0(
            BeatTimelineUIView timeline,
            RectTransform viewport,
            float railY,
            float slotWidth,
            float noteW,
            float noteH)
        {
            SeedExampleNotes(timeline, viewport, viewport?.Find("ScrollContent") as RectTransform,
                railY, slotWidth, noteW, noteH, beat1Hits: 3);
        }

        private static void EnsureBeatSegmentAuthoredVisuals(BeatSegmentView segment)
        {
            if (segment == null)
            {
                return;
            }

            var root = segment.transform;

            var beatFrame = root.Find("BeatFrame")?.GetComponent<Image>();
            if (beatFrame == null)
            {
                var go = CreateUiObject("BeatFrame", root);
                Undo.RegisterCreatedObjectUndo(go, "Add BeatFrame");
                var rt = go.GetComponent<RectTransform>();
                StretchWithPadding(rt, 0f, 0f, 1f, 1f);
                rt.offsetMin = new Vector2(1.5f, 1.5f);
                rt.offsetMax = new Vector2(-1.5f, -1.5f);
                beatFrame = go.AddComponent<Image>();
                beatFrame.raycastTarget = false;
                beatFrame.enabled = false;
                beatFrame.color = new Color(1f, 1f, 1f, 0.55f);
                go.transform.SetSiblingIndex(1);
            }

            var noteTier = root.Find("NoteTier")?.GetComponent<Image>();
            if (noteTier == null)
            {
                var portrait = root.Find("Portrait");
                if (portrait != null)
                {
                    Undo.RecordObject(portrait.gameObject, "Rename Portrait to NoteTier");
                    portrait.name = "NoteTier";
                    noteTier = portrait.GetComponent<Image>();
                }
                else
                {
                    var go = CreateUiObject("NoteTier", root);
                    Undo.RegisterCreatedObjectUndo(go, "Add NoteTier");
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.72f);
                    rt.anchorMax = new Vector2(0.5f, 0.72f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(40f, 40f);
                    noteTier = go.AddComponent<Image>();
                    noteTier.raycastTarget = false;
                    noteTier.preserveAspect = true;
                }
            }

            if (noteTier != null)
            {
                noteTier.raycastTarget = false;
                noteTier.preserveAspect = true;
            }

            SetField(segment, "beatFrame", beatFrame);
            SetField(segment, "noteTier", noteTier);
            if (noteTier != null)
            {
                SetField(segment, "portrait", noteTier);
            }

            segment.WireReferences();
            EditorUtility.SetDirty(segment);
        }

        private static void CreatePhaseDivider(Transform parent)
        {
            var divGo = CreateUiObject("PhaseDivider", parent);
            var divRect = divGo.GetComponent<RectTransform>();
            divRect.anchorMin = new Vector2(1f, 0f);
            divRect.anchorMax = new Vector2(1f, 1f);
            divRect.pivot = new Vector2(0.5f, 0.5f);
            divRect.sizeDelta = new Vector2(3f, 0f);
            divRect.anchoredPosition = new Vector2(2f, 0f);
            divGo.AddComponent<Image>().color = Color.white;
        }

        private static void CreateTimelineHeader(Transform parent)
        {
            var headerGo = CreateUiObject("Header", parent);
            var headerRect = headerGo.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0f);
            headerRect.anchorMax = new Vector2(0f, 1f);
            headerRect.pivot = new Vector2(0f, 0.5f);
            headerRect.sizeDelta = new Vector2(110f, 0f);

            var clefGo = CreateUiObject("Clef", headerGo.transform);
            var clefRect = clefGo.GetComponent<RectTransform>();
            clefRect.anchorMin = new Vector2(0f, 0.5f);
            clefRect.anchorMax = new Vector2(0f, 0.5f);
            clefRect.anchoredPosition = new Vector2(12f, 0f);
            clefRect.sizeDelta = new Vector2(24f, 48f);
            ApplyClefSprite(clefGo);

            var budgetGo = CreateUiObject("Budget", headerGo.transform);
            var budgetRect = budgetGo.GetComponent<RectTransform>();
            budgetRect.anchorMin = new Vector2(0f, 0.5f);
            budgetRect.anchorMax = new Vector2(0f, 0.5f);
            budgetRect.anchoredPosition = new Vector2(58f, 8f);
            budgetRect.sizeDelta = new Vector2(36f, 28f);
            budgetGo.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.6f, 0.8f);
            var budgetTextGo = CreateUiObject("BudgetText", budgetGo.transform);
            StretchFull(budgetTextGo.GetComponent<RectTransform>());
            var budgetText = budgetTextGo.AddComponent<Text>();
            ApplyText(budgetText);
            budgetText.text = "1/10";

            var avGo = CreateUiObject("AvLabel", headerGo.transform);
            var avRect = avGo.GetComponent<RectTransform>();
            avRect.anchorMin = new Vector2(0f, 0.5f);
            avRect.anchorMax = new Vector2(0f, 0.5f);
            avRect.anchoredPosition = new Vector2(58f, -16f);
            avRect.sizeDelta = new Vector2(96f, 20f);
            var avText = avGo.AddComponent<Text>();
            ApplyText(avText);
            avText.fontSize = 11;
            avText.alignment = TextAnchor.MiddleLeft;
            avText.horizontalOverflow = HorizontalWrapMode.Overflow;
            avText.text = "AV 150/150";

            var phaseGo = CreateUiObject("PhaseLabel", headerGo.transform);
            var phaseRect = phaseGo.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0f, 1f);
            phaseRect.anchorMax = new Vector2(0f, 1f);
            phaseRect.pivot = new Vector2(0f, 1f);
            phaseRect.anchoredPosition = new Vector2(0f, 4f);
            phaseRect.sizeDelta = new Vector2(110f, 18f);
            var phaseText = phaseGo.AddComponent<Text>();
            ApplyText(phaseText);
            phaseText.fontSize = 11;
            phaseText.alignment = TextAnchor.MiddleLeft;
            phaseText.text = "PHASE";
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchWithPadding(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyText(Text text)
        {
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            SceneFontSetupEditor.ApplyAutomatic(text);
        }

        /// <summary>G-clef sprite (Unity Text không render SMP 𝄞).</summary>
        private static void ApplyClefSprite(GameObject clefGo)
        {
            if (clefGo == null)
            {
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/FracturedChorus/Resources/UI/clef_g_v1.png");
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>("UI/clef_g_v1");
            }

            var image = clefGo.GetComponent<Image>();
            if (image == null)
            {
                image = clefGo.AddComponent<Image>();
            }

            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static void SetField(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(Object target, string fieldName, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.floatValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(Object target, string fieldName, float x, float y)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Vector2)
            {
                prop.vector2Value = new Vector2(x, y);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(Object target, string fieldName, bool value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.boolValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetField(Object target, string fieldName, BeatSegmentView value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
