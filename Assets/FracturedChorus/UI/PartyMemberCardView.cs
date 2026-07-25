using System.Collections;
using FracturedChorus.Combat.Damage;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Một thẻ party — avatar, bar máu, badge hệ tròn góc phải. Clone từ CardTemplate lúc Play.
    /// </summary>
    public class PartyMemberCardView : MonoBehaviour
    {
        private static readonly Color HealthFillColor = new Color(0.18f, 0.92f, 0.28f, 1f);
        private static readonly Color HealthTrackColor = new Color(0.08f, 0.08f, 0.1f, 0.95f);

        [SerializeField] private Image borderImage;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image healthBarBg;
        [SerializeField] private Image healthBarFill;
        [SerializeField] private RectTransform healthBarFillRect;
        [SerializeField] private Image elementBadgeRing;
        [SerializeField] private Image elementIcon;

        private CombatUnit _unit;
        private PrepPipsView _prepPips;
        private Image _reduceS2BuffIcon;
        private Coroutine _barPunchRoutine;
        private Vector3 _barPunchBaseScale = Vector3.one;

        public CombatUnit BoundUnit => _unit;

        public void WireReferences()
        {
            if (borderImage == null)
            {
                borderImage = transform.Find("Border")?.GetComponent<Image>();
            }

            if (avatarImage == null)
            {
                avatarImage = transform.Find("Avatar")?.GetComponent<Image>();
            }

            if (healthBarBg == null)
            {
                healthBarBg = transform.Find("HealthBarBg")?.GetComponent<Image>();
            }

            if (healthBarFill == null)
            {
                healthBarFill = transform.Find("HealthBarBg/HealthBarFill")?.GetComponent<Image>();
            }

            if (healthBarFillRect == null && healthBarFill != null)
            {
                healthBarFillRect = healthBarFill.rectTransform;
            }

            if (healthBarFillRect == null)
            {
                healthBarFillRect = transform.Find("HealthBarBg/HealthBarFill") as RectTransform;
            }

            if (elementBadgeRing == null)
            {
                elementBadgeRing = transform.Find("ElementBadge")?.GetComponent<Image>();
            }

            if (elementIcon == null)
            {
                elementIcon = transform.Find("ElementBadge/ElementIcon")?.GetComponent<Image>();
            }

            EnsureHealthBarVisuals();
            EnsureCircleBadgeSprites();
            ApplyBadgeLayout();
            EnsurePrepPips();
            EnsureReduceS2BuffIcon();
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
                PlaceBuffAboveHealthBar(_reduceS2BuffIcon.rectTransform);
                ApplyReduceS2BuffVisual(_reduceS2BuffIcon);
                return;
            }

            var go = new GameObject("BuffReduceS2", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(cardRt, false);
            PlaceBuffAboveHealthBar(rt);
            _reduceS2BuffIcon = go.GetComponent<Image>();
            _reduceS2BuffIcon.raycastTarget = false;
            _reduceS2BuffIcon.preserveAspect = true;
            ApplyReduceS2BuffVisual(_reduceS2BuffIcon);
            go.SetActive(false);
        }

        private static void PlaceBuffAboveHealthBar(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(4f, 26f);
            rt.sizeDelta = new Vector2(28f, 28f);
            rt.SetAsLastSibling();
        }

        private void DestroyMisplacedBuffIcon()
        {
            var healthRt = healthBarBg != null
                ? healthBarBg.rectTransform
                : transform.Find("HealthBarBg") as RectTransform;
            var underHealth = healthRt != null ? healthRt.Find("BuffReduceS2") : null;
            if (underHealth != null)
            {
                DestroyUiObject(underHealth.gameObject);
            }

            var avatarRt = avatarImage != null
                ? avatarImage.rectTransform
                : transform.Find("Avatar") as RectTransform;
            var underAvatar = avatarRt != null ? avatarRt.Find("BuffReduceS2") : null;
            if (underAvatar != null)
            {
                DestroyUiObject(underAvatar.gameObject);
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

            // Scene đã set kích thước/vị trí badge (ví dụ 35×35 tại -6,-6) → tôn trọng, KHÔNG ép hằng số.
            if (RectSizeUtil.IsAuthored(badgeRect))
            {
                return;
            }

            // Fallback: chỉ khi CardTemplate trong scene chưa dựng badge.
            PartyCardLayout.ApplyElementBadgeRect(badgeRect);

            var iconRect = elementIcon != null
                ? elementIcon.rectTransform
                : transform.Find("ElementBadge/ElementIcon") as RectTransform;
            PartyCardLayout.ApplyElementIconRect(iconRect);
        }

        public void Bind(CombatUnit unit, UnitPresetSO preset)
        {
            Unsubscribe();

            _unit = unit;
            WireReferences();
            ApplyPortrait(preset);
            ApplyElement(unit?.Stats.Element ?? HarmonyElement.Melody, preset?.statBlock);
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

        private void ApplyPortrait(UnitPresetSO preset)
        {
            if (avatarImage == null)
            {
                return;
            }

            var sprite = preset?.ResolvePortraitSprite();
            avatarImage.sprite = sprite;
            avatarImage.color = sprite != null ? Color.white : new Color(0.35f, 0.35f, 0.42f, 1f);
            avatarImage.preserveAspect = true;
        }

        private void ApplyElement(HarmonyElement element, UnitStatBlockSO statBlock)
        {
            if (borderImage != null)
            {
                borderImage.color = HarmonyElementPalette.GetBorderColor(element);
            }

            var icon = HarmonyElementPalette.ResolveElementIcon(element, statBlock);

            if (elementBadgeRing != null)
            {
                elementBadgeRing.enabled = true;
                elementBadgeRing.sprite = UiCircleSpriteUtil.Circle;
                elementBadgeRing.color = HarmonyElementPalette.GetBadgeRingColor(element);
            }

            if (elementIcon != null)
            {
                var hasArtIcon = statBlock?.elementBadgeIcon != null && icon != null;
                elementIcon.sprite = icon != null ? icon : UiCircleSpriteUtil.Circle;
                elementIcon.color = hasArtIcon ? Color.white : HarmonyElementPalette.GetBadgeFill(element);
                elementIcon.preserveAspect = true;
                // Hình học icon lấy từ scene (ElementIcon trong CardTemplate); chỉ fallback khi badge chưa authored.
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

            if (avatarImage != null)
            {
                var alpha = _unit.IsAlive ? 1f : 0.35f;
                var c = avatarImage.color;
                avatarImage.color = new Color(c.r, c.g, c.b, alpha);
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
                EnsureReduceS2BuffIcon();
            }

            if (_reduceS2BuffIcon == null)
            {
                return;
            }

            var show = _unit != null && _unit.PendingReduceS2 > 0;
            _reduceS2BuffIcon.gameObject.SetActive(show);
            if (show)
            {
                PlaceBuffAboveHealthBar(_reduceS2BuffIcon.rectTransform);
                ApplyReduceS2BuffVisual(_reduceS2BuffIcon);
            }
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
            var circle = UiCircleSpriteUtil.Circle;
            if (elementBadgeRing != null)
            {
                if (elementBadgeRing.sprite == null)
                {
                    elementBadgeRing.sprite = circle;
                }

                elementBadgeRing.enabled = true;
            }

            if (elementIcon != null && elementIcon.sprite == null)
            {
                elementIcon.sprite = circle;
            }
        }
    }
}
