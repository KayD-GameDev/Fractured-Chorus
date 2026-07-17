#if UNITY_EDITOR
using FracturedChorus.Combat.Timeline;
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class TimelineHierarchyBuilder
    {
        public const float SlotWidth = 52f;
        public const float SlotHeight = 64f;

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
            ui.WireReferences();
            return ui;
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

            var borderGo = CreateUiObject("Border", cardGo.transform);
            StretchFull(borderGo.GetComponent<RectTransform>());
            var borderImage = borderGo.AddComponent<Image>();
            borderImage.color = HarmonyElementPalette.GetBorderColor(Combat.Damage.HarmonyElement.Melody);
            borderImage.raycastTarget = false;

            var avatarGo = CreateUiObject("Avatar", cardGo.transform);
            var avatarRect = avatarGo.GetComponent<RectTransform>();
            StretchWithPadding(avatarRect, 0f, 0f, 1f, 1f);
            avatarRect.offsetMin = new Vector2(3f, 10f);
            avatarRect.offsetMax = new Vector2(-3f, -3f);
            var avatarImage = avatarGo.AddComponent<Image>();
            avatarImage.color = new Color(0.35f, 0.35f, 0.42f, 1f);
            avatarImage.preserveAspect = true;
            avatarImage.raycastTarget = false;

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
            SetField(cardView, "borderImage", borderImage);
            SetField(cardView, "avatarImage", avatarImage);
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

            panelGo.SetActive(false);

            var ui = panelGo.AddComponent<SkillPanelUIView>();
            SetField(ui, "panelRect", panelRect);
            SetField(ui, "radialRoot", radialRect);
            SetField(ui, "slotTop", slotTop);
            SetField(ui, "slotLeft", slotLeft);
            SetField(ui, "slotRight", slotRight);
            SetField(ui, "titleLabel", title);
            SetField(ui, "preserveSceneLayout", true);
            ui.WireReferences();
            return ui;
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

            var panelRect = panelTransform as RectTransform;
            var panelBg = panelRect != null ? panelRect.GetComponent<Image>() : null;
            ApplyCircularPanelStyle(panelRect, panelBg, useMaxExistingExtent: true);
            SetField(panel, "panelRect", panelRect);
            SetField(panel, "radialRoot", radialTransform);
            SetField(panel, "slotTop", slotTop);
            SetField(panel, "slotLeft", slotLeft);
            SetField(panel, "slotRight", slotRight);
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

            EnsureSkillSlotFrame(slotTransform);

            var label = slotTransform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                ApplyText(label);
                label.fontSize = RadialSlotLabelFontSize;
                label.color = Color.black;
            }

            EnsureRadialSlotIcon(slotTransform);
        }

        private static void EnsureSkillSlotFrame(Transform slotTransform)
        {
            if (slotTransform == null)
            {
                return;
            }

            var frameTransform = slotTransform.Find("Frame") as RectTransform;
            if (frameTransform == null)
            {
                var frameGo = CreateUiObject("Frame", slotTransform);
                frameTransform = frameGo.GetComponent<RectTransform>();
                Undo.RegisterCreatedObjectUndo(frameGo, "Add Skill Slot Frame");
            }

            frameTransform.SetAsFirstSibling();
            StretchWithPadding(frameTransform, 0f, 0f, 1f, 1f);
            frameTransform.offsetMin = new Vector2(-6f, -6f);
            frameTransform.offsetMax = new Vector2(6f, 6f);

            var frameImg = frameTransform.GetComponent<Image>();
            if (frameImg == null)
            {
                frameImg = frameTransform.gameObject.AddComponent<Image>();
            }

            frameImg.sprite = UiCircleSpriteUtil.Circle;
            frameImg.type = Image.Type.Simple;
            frameImg.color = new Color(0.92f, 0.78f, 0.42f, 0.95f);
            frameImg.raycastTarget = false;

            var ring = slotTransform.Find("Ring");
            if (ring != null)
            {
                ring.SetSiblingIndex(1);
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

            EnsureSkillSlotFrame(slotGo.transform);

            var bg = slotGo.AddComponent<Image>();
            bg.sprite = UiCircleSpriteUtil.Circle;
            bg.type = Image.Type.Simple;
            bg.color = new Color(0.16f, 0.16f, 0.22f, 0.96f);

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

            var iconGo = CreateUiObject("Icon", slotGo.transform);
            var iconRect = iconGo.GetComponent<RectTransform>();
            StretchWithPadding(iconRect, 0.1f, 0.1f, 0.9f, 0.9f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            iconImg.enabled = false;

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
            btnImage.preserveAspect = true;
            var button = btnGo.AddComponent<Button>();

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
            var clefText = clefGo.AddComponent<Text>();
            ApplyText(clefText);
            clefText.text = "\u266A";
            clefText.fontSize = 28;

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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
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
