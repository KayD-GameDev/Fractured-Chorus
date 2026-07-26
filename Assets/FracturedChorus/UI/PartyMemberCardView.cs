using System.Collections;
using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Thẻ party/enemy — hiển thị nhân vật chỉ qua CardArt (+ BarStack Hierarchy).
    /// Clone từ CardTemplate lúc Play. Vị trí/size lấy từ Hierarchy.
    /// </summary>
    public class PartyMemberCardView : MonoBehaviour
    {
        private static readonly Color HealthFillColor = new Color(0.18f, 0.92f, 0.28f, 1f);
        private static readonly Color HealthTrackColor = new Color(0.08f, 0.08f, 0.1f, 0.95f);

        [SerializeField] private Image healthBarBg;
        [SerializeField] private Image healthBarFill;
        [SerializeField] private RectTransform healthBarFillRect;
        [SerializeField] private Image elementBadgeRing;
        [SerializeField] private Image elementIcon;
        [SerializeField] private Image cardArtImage;
        [SerializeField] private RectTransform barStack;
        [SerializeField] private RectTransform healthSlot;
        [SerializeField] private RectTransform gaugeSlot;

        private CombatUnit _unit;
        private PrepPipsView _prepPips;
        private Image _reduceS2BuffIcon;
        private Coroutine _barPunchRoutine;
        private Vector3 _barPunchBaseScale = Vector3.one;
        private bool _embeddedSkin;
        private Vector2 _authoredCardSize;
        private bool _authoredCardSizeCached;
        /// <summary>Enemy clone: giữ object/Rect giống Enemy CardTemplate; chỉ bind data + healthSlotTop.</summary>
        private bool _useEnemyTemplateHierarchy;

        public CombatUnit BoundUnit => _unit;
        public bool UsesEmbeddedCardArt => _embeddedSkin;

        /// <summary>
        /// Gọi ngay sau Instantiate từ EnemyStatusBar — copy Rect chrome từ template factory,
        /// khóa mọi path layout party.
        /// </summary>
        public void UseEnemyTemplateHierarchy(PartyMemberCardView templateSource)
        {
            _useEnemyTemplateHierarchy = true;
            if (templateSource != null && templateSource != this)
            {
                CopyChromeRectsFrom(templateSource);
            }
        }

        /// <summary>Luôn lấy size từ CardTemplate Hierarchy (scan: 115×178) — không ép hằng số embedded.</summary>
        public Vector2 PreferredCardSize =>
            _authoredCardSizeCached
                ? _authoredCardSize
                : new Vector2(PartyCardLayout.CardWidth, PartyCardLayout.CardHeight);

        public void WireReferences()
        {
            DestroyLegacyChrome();

            if (healthBarBg == null)
            {
                healthBarBg = transform.Find("HealthBarBg")?.GetComponent<Image>()
                              ?? transform.Find("BarStack/HealthSlot/HealthBarBg")?.GetComponent<Image>();
            }

            if (healthBarFill == null)
            {
                healthBarFill = transform.Find("HealthBarBg/HealthBarFill")?.GetComponent<Image>()
                                ?? transform.Find("BarStack/HealthSlot/HealthBarBg/HealthBarFill")?.GetComponent<Image>();
            }

            if (healthBarFillRect == null && healthBarFill != null)
            {
                healthBarFillRect = healthBarFill.rectTransform;
            }

            if (healthBarFillRect == null)
            {
                healthBarFillRect = transform.Find("HealthBarBg/HealthBarFill") as RectTransform
                                    ?? transform.Find("BarStack/HealthSlot/HealthBarBg/HealthBarFill") as RectTransform;
            }

            if (elementBadgeRing == null)
            {
                elementBadgeRing = transform.Find("ElementBadge")?.GetComponent<Image>();
            }

            if (elementIcon == null)
            {
                elementIcon = transform.Find("ElementBadge/ElementIcon")?.GetComponent<Image>();
            }

            if (cardArtImage == null)
            {
                cardArtImage = transform.Find("CardArt")?.GetComponent<Image>();
            }

            if (barStack == null)
            {
                barStack = transform.Find("BarStack") as RectTransform;
            }

            if (healthSlot == null)
            {
                healthSlot = transform.Find("BarStack/HealthSlot") as RectTransform;
            }

            if (gaugeSlot == null)
            {
                gaugeSlot = transform.Find("BarStack/GaugeSlot") as RectTransform;
            }

            CacheClassicCardSize();
            if (!_useEnemyTemplateHierarchy)
            {
                EnsureEmbeddedHierarchy();
            }

            EnsureHealthBarVisuals();
            EnsureCircleBadgeSprites();
            EnsurePrepPips();
            // Enemy: không tạo BuffReduceS2 nếu template không có — object phải khớp CardTemplate.
            if (!IsEnemyCard())
            {
                EnsureReduceS2BuffIcon();
            }
            else
            {
                WireExistingBuffIconOnly();
            }
        }

        /// <summary>
        /// Chuẩn hóa chrome theo CardTemplate grammar.
        /// Enemy: chỉ wire + sprite badge — không reorder / không ép Rect.
        /// </summary>
        public void NormalizeTemplateChrome()
        {
            WireReferences();

            if (IsEnemyCard())
            {
                // Giữ nguyên Hierarchy clone từ Enemy CardTemplate.
                EnsureCircleBadgeSprites();
                EnsurePrepPips();
                return;
            }

            EnsureEmbeddedHierarchy();
            EnsureElementBadgeExists(transform as RectTransform);
            if (elementBadgeRing != null)
            {
                ApplyCircleBadgeRing(
                    elementBadgeRing,
                    elementBadgeRing.color.a > 0.01f
                        ? elementBadgeRing.color
                        : HarmonyElementPalette.GetBadgeRingColor(HarmonyElement.Melody));
            }

            EnsurePrepPips();
            _prepPips?.SetLayoutMode(PrepPipsView.LayoutMode.SegmentStrip);
            BringElementBadgeToFront();
        }

        private bool IsEnemyCard() =>
            _useEnemyTemplateHierarchy
            || GetComponentInParent<EnemyStatusBarUIView>(true) != null;

        public void Bind(CombatUnit unit, UnitPresetSO preset)
        {
            Unsubscribe();

            _unit = unit;
            WireReferences();
            ApplyCardSkin(preset);
            ApplyElement(unit?.Stats.Element ?? HarmonyElement.Melody, preset);
            RefreshHp();
            RefreshPrep(animate: false);
            RefreshReduceS2BuffIcon();

            if (_unit != null)
            {
                _unit.OnHpChanged += HandleHpChanged;
                _unit.OnPrepChanged += HandlePrepChanged;
                _unit.OnPendingReduceS2Changed += HandlePendingReduceS2Changed;
            }
        }

        public void Unbind()
        {
            Unsubscribe();
            _unit = null;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_unit != null)
            {
                _unit.OnHpChanged -= HandleHpChanged;
                _unit.OnPrepChanged -= HandlePrepChanged;
                _unit.OnPendingReduceS2Changed -= HandlePendingReduceS2Changed;
            }
        }

        private void HandleHpChanged(CombatUnit unit)
        {
            RefreshHp();
            if (unit != null && unit.LastHpChange.ShouldShowFeedback)
            {
                PunchHealthBar(unit.LastHpChange.IsCritical);
            }
        }

        private void PunchHealthBar(bool isCritical)
        {
            var target = healthBarBg != null ? healthBarBg.rectTransform : healthBarFillRect;
            if (target == null)
            {
                return;
            }

            if (_barPunchRoutine != null)
            {
                StopCoroutine(_barPunchRoutine);
                target.localScale = _barPunchBaseScale;
            }
            else
            {
                _barPunchBaseScale = target.localScale;
            }

            _barPunchRoutine = StartCoroutine(BarPunchRoutine(target, isCritical));
        }

        private IEnumerator BarPunchRoutine(RectTransform target, bool isCritical)
        {
            var peak = _barPunchBaseScale * (isCritical ? 1.14f : 1.08f);
            const float up = 0.05f;
            const float down = 0.12f;
            var t = 0f;
            while (t < up)
            {
                t += Time.deltaTime;
                target.localScale = Vector3.Lerp(_barPunchBaseScale, peak, Mathf.Clamp01(t / up));
                yield return null;
            }

            t = 0f;
            while (t < down)
            {
                t += Time.deltaTime;
                target.localScale = Vector3.Lerp(peak, _barPunchBaseScale, Mathf.Clamp01(t / down));
                yield return null;
            }

            target.localScale = _barPunchBaseScale;
            _barPunchRoutine = null;
        }

        private void HandlePrepChanged(CombatUnit unit)
        {
            RefreshPrep(animate: true);
        }

        private void HandlePendingReduceS2Changed(CombatUnit unit)
        {
            RefreshReduceS2BuffIcon();
        }

        private void CacheClassicCardSize()
        {
            if (_authoredCardSizeCached)
            {
                return;
            }

            var rt = transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            var size = rt.sizeDelta;
            if (size.x > 1f && size.y > 1f)
            {
                _authoredCardSize = size;
                _authoredCardSizeCached = true;
            }
        }

        private void ApplyCardSkin(UnitPresetSO preset)
        {
            // Enemy: object/Rect lấy y nguyên từ Enemy CardTemplate — chỉ gán art + data.
            if (IsEnemyCard())
            {
                ApplyEnemyCardSkinFromTemplate(preset);
                return;
            }

            EnsureEmbeddedHierarchy();
            // Chỉ CardArt hiển thị hình — combat card art, fallback battleSprite.
            var cardArt = preset?.ResolveCombatCardSprite() ?? preset?.battleSprite;
            _embeddedSkin = cardArt != null;
            // Mọi thẻ dùng chung size CardTemplate (Ren grammar) — không scale theo sprite crop.
            ApplyUniformCardRootSize();
            SyncLayoutElementToAuthoredSize();

            if (cardArtImage != null)
            {
                cardArtImage.gameObject.SetActive(true);
                cardArtImage.sprite = cardArt;
                cardArtImage.color = cardArt != null ? Color.white : new Color(0.2f, 0.2f, 0.24f, 1f);
                cardArtImage.type = Image.Type.Simple;
                cardArtImage.preserveAspect = false;
                cardArtImage.raycastTarget = false;
                StretchFull(cardArtImage.rectTransform);
            }

            if (barStack != null)
            {
                barStack.gameObject.SetActive(true);
                // Chỉ đổi Y lúc load — giữ nguyên X / size / xoay / anchor từ CardTemplate.
                ApplyBarStackYFromPreset(preset);
            }

            // Khớp CardTemplate: BarStack trên CardArt trong Hierarchy; ElementBadge vẫn vẽ sau cùng.
            RestoreCardChildSiblingOrder();

            PlaceHealthBarInSlot(healthSlot);
            EnsurePrepPips();
            _prepPips?.LayoutIn(gaugeSlot);
            ApplyBadgeLayout();
            PlaceBuffForEmbedded();
            BringElementBadgeToFront();
        }

        /// <summary>
        /// Enemy clone: không tạo/xóa/reorder/Stretch object — chỉ bind sprite + bật node có sẵn.
        /// Duy nhất được chỉnh theo preset: HealthSlot Top (giữ chiều cao).
        /// </summary>
        private void ApplyEnemyCardSkinFromTemplate(UnitPresetSO preset)
        {
            var cardArt = preset?.ResolveCombatCardSprite() ?? preset?.battleSprite;
            _embeddedSkin = cardArt != null;
            ApplyUniformCardRootSize();
            SyncLayoutElementToAuthoredSize();

            if (cardArtImage != null)
            {
                cardArtImage.gameObject.SetActive(true);
                cardArtImage.sprite = cardArt;
                cardArtImage.color = cardArt != null ? Color.white : new Color(0.2f, 0.2f, 0.24f, 1f);
                cardArtImage.type = Image.Type.Simple;
                cardArtImage.preserveAspect = false;
                cardArtImage.raycastTarget = false;
                // Không StretchFull — giữ Rect CardArt từ Enemy CardTemplate.
            }

            if (barStack != null)
            {
                barStack.gameObject.SetActive(true);
            }

            // HealthBar đã nằm trong HealthSlot trên template → chỉ sync visual, không SetParent/Stretch.
            if (healthBarBg != null)
            {
                EnsureHealthBarVisuals();
                healthBarBg.color = new Color(1f, 1f, 1f, 0f);
            }

            EnsurePrepPips();
            ApplyHealthSlotTopFromPreset(preset);
            // Không LayoutIn / ApplyBadgeLayout / PlaceBuff / reorder sibling / BarStack Y.
        }

        /// <summary>
        /// Chỉ đổi Inspector Top của HealthSlot; giữ nguyên chiều cao (không thu nhỏ thanh máu).
        /// offsetMax.y = −Top; offsetMin.y chỉnh theo sizeDelta.y hiện có.
        /// </summary>
        private void ApplyHealthSlotTopFromPreset(UnitPresetSO preset)
        {
            if (healthSlot == null || preset == null || preset.healthSlotTop < 0f)
            {
                return;
            }

            var sizeY = healthSlot.sizeDelta.y;
            var max = healthSlot.offsetMax;
            var min = healthSlot.offsetMin;
            max.y = -preset.healthSlotTop;
            // Giữ sizeDelta.y → chiều cao slot không đổi, chỉ dịch theo Top.
            min.y = max.y - sizeY;
            healthSlot.offsetMax = max;
            healthSlot.offsetMin = min;
        }

        private void CopyChromeRectsFrom(PartyMemberCardView source)
        {
            if (source == null)
            {
                return;
            }

            // Chỉ Find — không WireReferences (tránh EnsureEmbeddedHierarchy đụng factory).
            var srcRoot = source.transform;
            var srcBar = source.barStack != null ? source.barStack : srcRoot.Find("BarStack") as RectTransform;
            var srcHealth = source.healthSlot != null
                ? source.healthSlot
                : srcRoot.Find("BarStack/HealthSlot") as RectTransform;
            var srcGauge = source.gaugeSlot != null
                ? source.gaugeSlot
                : srcRoot.Find("BarStack/GaugeSlot") as RectTransform;
            var srcArt = source.cardArtImage != null
                ? source.cardArtImage.rectTransform
                : srcRoot.Find("CardArt") as RectTransform;
            var srcBadge = source.elementBadgeRing != null
                ? source.elementBadgeRing.rectTransform
                : srcRoot.Find("ElementBadge") as RectTransform;
            var srcIcon = source.elementIcon != null
                ? source.elementIcon.rectTransform
                : srcRoot.Find("ElementBadge/ElementIcon") as RectTransform;
            var srcPrep = srcRoot.Find("BarStack/GaugeSlot/PrepPips") as RectTransform;

            if (barStack == null)
            {
                barStack = transform.Find("BarStack") as RectTransform;
            }

            if (healthSlot == null)
            {
                healthSlot = transform.Find("BarStack/HealthSlot") as RectTransform;
            }

            if (gaugeSlot == null)
            {
                gaugeSlot = transform.Find("BarStack/GaugeSlot") as RectTransform;
            }

            if (cardArtImage == null)
            {
                cardArtImage = transform.Find("CardArt")?.GetComponent<Image>();
            }

            if (elementBadgeRing == null)
            {
                elementBadgeRing = transform.Find("ElementBadge")?.GetComponent<Image>();
            }

            if (elementIcon == null)
            {
                elementIcon = transform.Find("ElementBadge/ElementIcon")?.GetComponent<Image>();
            }

            CopyRect(srcBar, barStack);
            CopyRect(srcHealth, healthSlot);
            CopyRect(srcGauge, gaugeSlot);
            CopyRect(srcArt, cardArtImage != null ? cardArtImage.rectTransform : null);
            CopyRect(srcBadge, elementBadgeRing != null ? elementBadgeRing.rectTransform : null);
            CopyRect(srcIcon, elementIcon != null ? elementIcon.rectTransform : null);
            CopyRect(srcPrep, transform.Find("BarStack/GaugeSlot/PrepPips") as RectTransform);

            // Sibling order: BarStack → CardArt → ElementBadge (như Enemy CardTemplate).
            if (barStack != null)
            {
                barStack.SetAsFirstSibling();
            }

            if (cardArtImage != null)
            {
                cardArtImage.rectTransform.SetSiblingIndex(barStack != null ? 1 : 0);
            }

            if (elementBadgeRing != null)
            {
                elementBadgeRing.transform.SetAsLastSibling();
            }
        }

        private static void CopyRect(RectTransform src, RectTransform dst)
        {
            if (src == null || dst == null)
            {
                return;
            }

            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.anchoredPosition = src.anchoredPosition;
            dst.sizeDelta = src.sizeDelta;
            dst.offsetMin = src.offsetMin;
            dst.offsetMax = src.offsetMax;
            dst.localRotation = src.localRotation;
            dst.localScale = src.localScale;
        }

        /// <summary>BarStack → CardArt → (buff) → ElementBadge — như Hierarchy CardTemplate.</summary>
        private void RestoreCardChildSiblingOrder()
        {
            if (barStack != null)
            {
                barStack.SetAsFirstSibling();
            }

            if (cardArtImage != null)
            {
                // Ngay sau BarStack.
                var artIndex = barStack != null ? 1 : 0;
                cardArtImage.rectTransform.SetSiblingIndex(artIndex);
            }
        }

        /// <summary>
        /// Load-time only (party): ghi đè <see cref="RectTransform.anchoredPosition"/>.y theo preset.
        /// </summary>
        private void ApplyBarStackYFromPreset(UnitPresetSO preset)
        {
            if (barStack == null || preset == null || preset.barStackAnchoredY < 0f)
            {
                return;
            }

            var pos = barStack.anchoredPosition;
            pos.y = preset.barStackAnchoredY;
            barStack.anchoredPosition = pos;
        }

        private void WireExistingBuffIconOnly()
        {
            DestroyLegacyReduceS2TextBadge();
            DestroyMisplacedBuffIcon();
            var existing = transform.Find("BuffReduceS2")?.GetComponent<Image>();
            if (existing != null)
            {
                _reduceS2BuffIcon = existing;
                ApplyReduceS2BuffVisual(_reduceS2BuffIcon);
            }
        }

        /// <summary>Ép root size về size đã cache từ CardTemplate — mọi unit cùng khung với Ren.</summary>
        private void ApplyUniformCardRootSize()
        {
            CacheClassicCardSize();
            var cardRt = transform as RectTransform;
            if (cardRt == null || !_authoredCardSizeCached)
            {
                return;
            }

            cardRt.sizeDelta = _authoredCardSize;
        }

        private void DestroyLegacyChrome()
        {
            DestroyUiObject(transform.Find("Border")?.gameObject);
            DestroyUiObject(transform.Find("Avatar")?.gameObject);
        }

        private void SyncLayoutElementToAuthoredSize()
        {
            var cardRt = transform as RectTransform;
            if (cardRt == null || !_authoredCardSizeCached)
            {
                return;
            }

            // Không đổi sizeDelta Hierarchy — chỉ sync LayoutElement cho row layout.
            var layoutElement = cardRt.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.preferredWidth = _authoredCardSize.x;
                layoutElement.preferredHeight = _authoredCardSize.y;
            }
        }

        /// <summary>
        /// Chỉ tạo node thiếu. Node đã có trong Hierarchy → giữ nguyên Rect (không Apply* hằng số).
        /// </summary>
        private void EnsureEmbeddedHierarchy()
        {
            var cardRt = transform as RectTransform;
            if (cardRt == null)
            {
                return;
            }

            if (cardArtImage == null)
            {
                var existing = cardRt.Find("CardArt")?.GetComponent<Image>();
                if (existing == null)
                {
                    var go = new GameObject("CardArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    var rt = go.GetComponent<RectTransform>();
                    rt.SetParent(cardRt, false);
                    StretchFull(rt);
                    existing = go.GetComponent<Image>();
                    existing.raycastTarget = false;
                    // Fill khung CardTemplate như Ren — không preserveAspect theo crop sprite.
                    existing.preserveAspect = false;
                    go.SetActive(false);
                }

                cardArtImage = existing;
            }

            if (barStack == null)
            {
                barStack = cardRt.Find("BarStack") as RectTransform;
                if (barStack == null)
                {
                    var go = new GameObject("BarStack", typeof(RectTransform));
                    barStack = go.GetComponent<RectTransform>();
                    barStack.SetParent(cardRt, false);
                    go.SetActive(false);
                    PartyCardLayout.ApplyEmbeddedBarStackRect(barStack);
                }
            }

            var createdHealthSlot = false;
            if (healthSlot == null)
            {
                healthSlot = barStack.Find("HealthSlot") as RectTransform;
                if (healthSlot == null)
                {
                    var go = new GameObject("HealthSlot", typeof(RectTransform));
                    healthSlot = go.GetComponent<RectTransform>();
                    healthSlot.SetParent(barStack, false);
                    createdHealthSlot = true;
                }
            }

            var createdGaugeSlot = false;
            if (gaugeSlot == null)
            {
                gaugeSlot = barStack.Find("GaugeSlot") as RectTransform;
                if (gaugeSlot == null)
                {
                    var go = new GameObject("GaugeSlot", typeof(RectTransform));
                    gaugeSlot = go.GetComponent<RectTransform>();
                    gaugeSlot.SetParent(barStack, false);
                    createdGaugeSlot = true;
                }
            }

            if (createdHealthSlot || createdGaugeSlot)
            {
                PartyCardLayout.ApplyEmbeddedHealthSlotRect(
                    createdHealthSlot ? healthSlot : null,
                    createdGaugeSlot ? gaugeSlot : null);
            }

            EnsureElementBadgeExists(cardRt);

            // Không bao giờ ApplyEmbeddedBarStackRect lên BarStack đã có trong Hierarchy.
        }

        private void EnsureElementBadgeExists(RectTransform cardRt)
        {
            if (cardRt == null)
            {
                return;
            }

            if (elementBadgeRing == null)
            {
                elementBadgeRing = cardRt.Find("ElementBadge")?.GetComponent<Image>();
            }

            if (elementBadgeRing == null)
            {
                var go = new GameObject("ElementBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(cardRt, false);
                PartyCardLayout.ApplyElementBadgeRect(rt, enemySide: IsEnemyCard());
                elementBadgeRing = go.GetComponent<Image>();
                elementBadgeRing.sprite = UiCircleSpriteUtil.Circle;
                elementBadgeRing.raycastTarget = false;
            }

            if (elementIcon == null)
            {
                elementIcon = elementBadgeRing.transform.Find("ElementIcon")?.GetComponent<Image>();
            }

            if (elementIcon == null)
            {
                var iconGo = new GameObject("ElementIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.SetParent(elementBadgeRing.transform, false);
                PartyCardLayout.ApplyElementIconRect(iconRt);
                elementIcon = iconGo.GetComponent<Image>();
                elementIcon.sprite = UiCircleSpriteUtil.Circle;
                elementIcon.preserveAspect = true;
                elementIcon.raycastTarget = false;
            }
        }

        private void BringElementBadgeToFront()
        {
            if (elementBadgeRing == null)
            {
                return;
            }

            elementBadgeRing.gameObject.SetActive(true);
            elementBadgeRing.enabled = true;
            // Enemy: giữ sibling order của CardTemplate (BarStack → CardArt → ElementBadge).
            if (!IsEnemyCard())
            {
                elementBadgeRing.transform.SetAsLastSibling();
            }
        }

        private void PlaceHealthBarInSlot(RectTransform slot)
        {
            if (healthBarBg == null || slot == null)
            {
                return;
            }

            var bgRt = healthBarBg.rectTransform;
            // Đã nằm đúng slot Hierarchy → không kéo/reset Rect.
            if (bgRt.parent == slot)
            {
                EnsureHealthBarVisuals();
                healthBarBg.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            bgRt.SetParent(slot, false);
            StretchFull(bgRt);
            EnsureHealthBarVisuals();
            healthBarBg.color = new Color(1f, 1f, 1f, 0f);
        }

        private static void StretchFull(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        private void EnsurePrepPips()
        {
            var root = transform as RectTransform;
            _prepPips = PrepPipsView.EnsureOn(root);
        }

        private void EnsureReduceS2BuffIcon()
        {
            DestroyLegacyReduceS2TextBadge();
            DestroyMisplacedBuffIcon();

            var cardRt = transform as RectTransform;
            if (cardRt == null)
            {
                return;
            }

            var existing = cardRt.Find("BuffReduceS2")?.GetComponent<Image>();
            if (existing != null)
            {
                _reduceS2BuffIcon = existing;
                PlaceBuffForEmbedded();
                ApplyReduceS2BuffVisual(_reduceS2BuffIcon);
                return;
            }

            var go = new GameObject("BuffReduceS2", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(cardRt, false);
            _reduceS2BuffIcon = go.GetComponent<Image>();
            _reduceS2BuffIcon.raycastTarget = false;
            _reduceS2BuffIcon.preserveAspect = true;
            PlaceBuffForEmbedded();
            ApplyReduceS2BuffVisual(_reduceS2BuffIcon);
            go.SetActive(false);
        }

        private void PlaceBuffForEmbedded()
        {
            if (_reduceS2BuffIcon == null)
            {
                return;
            }

            var rt = _reduceS2BuffIcon.rectTransform;
            // Hierarchy đã author BuffReduceS2 → giữ nguyên Rect / sibling order.
            if (RectSizeUtil.IsAuthored(rt))
            {
                return;
            }

            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-4f, -34f);
            rt.sizeDelta = new Vector2(24f, 24f);
            rt.localRotation = Quaternion.identity;
        }

        private void DestroyMisplacedBuffIcon()
        {
            var healthRt = healthBarBg != null
                ? healthBarBg.rectTransform
                : transform.Find("HealthBarBg") as RectTransform
                  ?? transform.Find("BarStack/HealthSlot/HealthBarBg") as RectTransform;
            var underHealth = healthRt != null ? healthRt.Find("BuffReduceS2") : null;
            if (underHealth != null)
            {
                DestroyUiObject(underHealth.gameObject);
            }
        }

        private void DestroyLegacyReduceS2TextBadge()
        {
            var legacy = transform.Find("ReduceS2Badge");
            if (legacy != null)
            {
                DestroyUiObject(legacy.gameObject);
            }
        }

        private static void DestroyUiObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(go);
            }
            else
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void ApplyReduceS2BuffVisual(Image image)
        {
            if (image == null)
            {
                return;
            }

            var sprite = Resources.Load<Sprite>("UI/Combat/Buffs/buff_reduce_s2_v1");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                return;
            }

            image.sprite = UiCircleSpriteUtil.Circle;
            image.color = new Color(1f, 0.78f, 0.28f, 0.95f);
        }

        private void ApplyBadgeLayout()
        {
            var badgeRect = elementBadgeRing != null
                ? elementBadgeRing.rectTransform
                : transform.Find("ElementBadge") as RectTransform;

            if (badgeRect == null)
            {
                return;
            }

            // Hierarchy đã có ElementBadge → giữ nguyên Rect (kể cả parent). Không SetParent lại.
            if (RectSizeUtil.IsAuthored(badgeRect))
            {
                ApplyElementIconInset();
                return;
            }

            badgeRect.SetParent(transform, false);
            PartyCardLayout.ApplyElementBadgeRect(badgeRect, enemySide: IsEnemyCard());
            ApplyElementIconInset();
        }

        private void ApplyElementIconInset()
        {
            var iconRect = elementIcon != null
                ? elementIcon.rectTransform
                : transform.Find("ElementBadge/ElementIcon") as RectTransform;
            PartyCardLayout.ApplyElementIconRect(iconRect);
        }

        private void ApplyElement(HarmonyElement element, UnitPresetSO preset)
        {
            if (!IsEnemyCard())
            {
                EnsureElementBadgeExists(transform as RectTransform);
            }

            var statBlock = preset?.statBlock;
            // Vòng ngoài = màu hệ (Nhịp / Giai điệu / Hòa âm).
            var ringColor = HarmonyElementPalette.GetBadgeRingColor(element);
            var icon = HarmonyElementPalette.ResolveElementIcon(element, statBlock);

            if (elementBadgeRing != null)
            {
                ApplyCircleBadgeRing(elementBadgeRing, ringColor, preserveRect: IsEnemyCard());
            }

            if (elementIcon != null)
            {
                elementIcon.gameObject.SetActive(true);
                elementIcon.enabled = true;
                elementIcon.sprite = icon != null ? icon : UiCircleSpriteUtil.Circle;
                elementIcon.color = icon != null ? Color.white : HarmonyElementPalette.GetBadgeFill(element);
                elementIcon.preserveAspect = true;
                elementIcon.type = Image.Type.Simple;
                elementIcon.raycastTarget = false;
            }

            BringElementBadgeToFront();
        }

        /// <summary>Ép badge hệ thành hình tròn + màu ring.</summary>
        private static void ApplyCircleBadgeRing(Image ring, Color color, bool preserveRect = false)
        {
            if (ring == null)
            {
                return;
            }

            ring.gameObject.SetActive(true);
            ring.enabled = true;
            ring.sprite = UiCircleSpriteUtil.Circle;
            ring.type = Image.Type.Simple;
            ring.preserveAspect = true;
            ring.color = color;
            ring.raycastTarget = false;

            if (preserveRect)
            {
                return;
            }

            var rt = ring.rectTransform;
            if (rt != null && RectSizeUtil.IsAuthored(rt))
            {
                var side = Mathf.Max(1f, Mathf.Min(
                    rt.sizeDelta.x > 1f ? rt.sizeDelta.x : PartyCardLayout.EmbeddedBadgeSize,
                    rt.sizeDelta.y > 1f ? rt.sizeDelta.y : PartyCardLayout.EmbeddedBadgeSize));
                if (Mathf.Abs(rt.sizeDelta.x - rt.sizeDelta.y) > 0.5f)
                {
                    rt.sizeDelta = new Vector2(side, side);
                }
            }
        }

        private void RefreshHp()
        {
            if (_unit == null)
            {
                return;
            }

            var ratio = Mathf.Clamp01((float)_unit.CurrentHp / Mathf.Max(1, _unit.Stats.MaxHp));

            if (healthBarFillRect != null)
            {
                healthBarFillRect.anchorMin = new Vector2(0f, 0f);
                healthBarFillRect.anchorMax = new Vector2(ratio, 1f);
                healthBarFillRect.offsetMin = Vector2.zero;
                healthBarFillRect.offsetMax = Vector2.zero;
            }

            if (cardArtImage != null)
            {
                var alpha = _unit.IsAlive ? 1f : 0.35f;
                var c = cardArtImage.color;
                cardArtImage.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        private void RefreshPrep(bool animate)
        {
            if (_prepPips == null)
            {
                EnsurePrepPips();
            }

            _prepPips?.SetPrep(_unit != null ? _unit.Prep : 0, animate);
        }

        private void RefreshReduceS2BuffIcon()
        {
            if (_reduceS2BuffIcon == null)
            {
                if (IsEnemyCard())
                {
                    WireExistingBuffIconOnly();
                }
                else
                {
                    EnsureReduceS2BuffIcon();
                }
            }

            if (_reduceS2BuffIcon == null)
            {
                return;
            }

            var show = _unit != null && _unit.PendingReduceS2 > 0;
            _reduceS2BuffIcon.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            if (!IsEnemyCard())
            {
                PlaceBuffForEmbedded();
            }
            ApplyReduceS2BuffVisual(_reduceS2BuffIcon);
        }

        private void EnsureHealthBarVisuals()
        {
            var white = UiCircleSpriteUtil.White;

            if (healthBarBg != null)
            {
                healthBarBg.sprite = white;
                healthBarBg.type = Image.Type.Simple;
                healthBarBg.color = HealthTrackColor;
                healthBarBg.raycastTarget = false;
            }

            if (healthBarFill != null)
            {
                healthBarFill.sprite = white;
                healthBarFill.type = Image.Type.Simple;
                healthBarFill.color = HealthFillColor;
                healthBarFill.raycastTarget = false;
            }

            if (healthBarFillRect != null)
            {
                healthBarFillRect.pivot = new Vector2(0f, 0.5f);
            }
        }

        private void EnsureCircleBadgeSprites()
        {
            if (elementBadgeRing != null)
            {
                ApplyCircleBadgeRing(
                    elementBadgeRing,
                    elementBadgeRing.color.a > 0.01f
                        ? elementBadgeRing.color
                        : HarmonyElementPalette.GetBadgeRingColor(HarmonyElement.Melody),
                    preserveRect: IsEnemyCard());
            }

            if (elementIcon != null && elementIcon.sprite == null)
            {
                elementIcon.sprite = UiCircleSpriteUtil.Circle;
            }
        }
    }
}
