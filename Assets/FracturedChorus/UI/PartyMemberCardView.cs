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

            if (_unit != null)
            {
                _unit.OnHpChanged += HandleHpChanged;
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
            }
        }

        private void HandleHpChanged(CombatUnit unit)
        {
            RefreshHp();
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
