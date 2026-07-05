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
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(180f, 220f);
            panelGo.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

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

            var buttonsGo = CreateUiObject("Buttons", panelGo.transform);
            var buttonsRect = buttonsGo.GetComponent<RectTransform>();
            StretchWithPadding(buttonsRect, 0f, 0f, 1f, 1f);
            buttonsRect.offsetMin = new Vector2(8f, 8f);
            buttonsRect.offsetMax = new Vector2(-8f, -40f);
            var layout = buttonsGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            buttonsGo.SetActive(false);

            var radialGo = CreateUiObject("Radial", panelGo.transform);
            var radialRect = radialGo.GetComponent<RectTransform>();
            radialRect.anchorMin = new Vector2(0.5f, 0.5f);
            radialRect.anchorMax = new Vector2(0.5f, 0.5f);
            radialRect.pivot = new Vector2(0.5f, 0.5f);
            radialRect.anchoredPosition = new Vector2(0f, -10f);
            radialRect.sizeDelta = new Vector2(180f, 180f);

            const float slotSize = 70f;
            var slotTop = CreateRadialSkillSlot(radialRect, "SkillSlot_Top", new Vector2(0f, 78f), slotSize);
            var slotLeft = CreateRadialSkillSlot(radialRect, "SkillSlot_Left", new Vector2(-68f, -39f), slotSize);
            var slotRight = CreateRadialSkillSlot(radialRect, "SkillSlot_Right", new Vector2(68f, -39f), slotSize);

            panelGo.SetActive(false);

            var ui = panelGo.AddComponent<SkillPanelUIView>();
            SetField(ui, "panelRect", panelRect);
            SetField(ui, "radialRoot", radialRect);
            SetField(ui, "slotTop", slotTop);
            SetField(ui, "slotLeft", slotLeft);
            SetField(ui, "slotRight", slotRight);
            SetField(ui, "buttonContainer", buttonsRect);
            SetField(ui, "titleLabel", title);
            SetField(ui, "preserveSceneLayout", true);
            ui.WireReferences();
            return ui;
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
            label.fontSize = 12;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = "—";

            var slot = slotGo.AddComponent<SkillRadialSlotView>();
            slot.WireFromScene(name.Contains("Top") ? SkillRadialDirection.Top
                : name.Contains("Left") ? SkillRadialDirection.Left : SkillRadialDirection.Right);
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
            overlayRect.sizeDelta = Vector2.zero;

            var btnGo = CreateUiObject("ExecuteButton", overlayGo.transform);
            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = Vector2.zero;
            btnRect.sizeDelta = new Vector2(160f, 56f);
            btnGo.AddComponent<Image>().color = new Color(0.35f, 0.15f, 0.55f, 0.95f);
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
            layout.spacing = 2f;
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

            var glowGo = CreateUiObject("Glow", segGo.transform);
            StretchWithPadding(glowGo.GetComponent<RectTransform>(), 0.05f, 0.1f, 0.95f, 0.9f);
            glowGo.AddComponent<Image>().color = new Color(1f, 0.2f, 0.2f, 0.15f);

            var portraitGo = CreateUiObject("Portrait", segGo.transform);
            var portraitRect = portraitGo.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.5f);
            portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.pivot = new Vector2(0f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(4f, 0f);
            portraitRect.sizeDelta = new Vector2(24f, 24f);
            portraitGo.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f, 1f);

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
            segment.WireReferences();
            return segment;
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
