using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class PartyUnitCardView : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image avatar;
        [SerializeField] private Image elementIcon;
        [SerializeField] private Image hpFill;
        [SerializeField] private Image hpBackground;
        [Tooltip("Giữ sprite Frame/Avatar/ElementIcon đã gán trong Hierarchy.")]
        [SerializeField] private bool preserveSceneImages = true;

        private CombatUnit _unit;

        public CombatUnit BoundUnit => _unit;

        public void WireReferences()
        {
            if (frame == null)
            {
                frame = transform.Find("Frame")?.GetComponent<Image>()
                    ?? GetComponent<Image>();
            }

            var avatarRoot = transform.Find("AvatarRoot");
            if (avatar == null && avatarRoot != null)
            {
                avatar = avatarRoot.Find("Avatar")?.GetComponent<Image>();
            }

            if (elementIcon == null && avatarRoot != null)
            {
                elementIcon = avatarRoot.Find("ElementIcon")?.GetComponent<Image>();
            }

            var hpBar = transform.Find("HpBar");
            if (hpBackground == null && hpBar != null)
            {
                hpBackground = hpBar.GetComponent<Image>();
            }

            if (hpFill == null && hpBar != null)
            {
                hpFill = hpBar.Find("Fill")?.GetComponent<Image>();
            }
        }

        public void Bind(CombatUnit unit, UnitPresetSO preset, Sprite portraitOverride = null)
        {
            Unbind();

            _unit = unit;
            if (_unit == null)
            {
                SetEmpty();
                return;
            }

            WireReferences();
            ApplyPresetVisuals(preset, portraitOverride);
            RefreshHp();
            _unit.OnHpChanged += HandleHpChanged;
        }

        public void ApplyPresentation(CombatUnit unit, PartyCardPresentation presentation)
        {
            Unbind();

            _unit = unit;
            if (_unit == null || presentation == null)
            {
                SetEmpty();
                return;
            }

            WireReferences();

            if (avatar != null)
            {
                if (presentation.Avatar != null)
                {
                    avatar.sprite = presentation.Avatar;
                    avatar.color = Color.white;
                }
                else if (presentation.Preset != null)
                {
                    avatar.sprite = null;
                    avatar.color = presentation.Preset.placeholderColor;
                }
                else
                {
                    avatar.sprite = null;
                    avatar.color = _unit.PlaceholderColor;
                }
            }

            SetElementIcon(presentation.ElementIcon);
            RefreshHp();
            _unit.OnHpChanged += HandleHpChanged;
        }

        public void Unbind()
        {
            if (_unit != null)
            {
                _unit.OnHpChanged -= HandleHpChanged;
                _unit = null;
            }
        }

        public void RefreshHp()
        {
            if (_unit == null || hpFill == null)
            {
                return;
            }

            var max = Mathf.Max(1, _unit.Stats.MaxHp);
            hpFill.fillAmount = Mathf.Clamp01((float)_unit.CurrentHp / max);
        }

        public void SetElementIcon(Sprite icon)
        {
            WireReferences();
            if (elementIcon == null)
            {
                return;
            }

            elementIcon.sprite = icon;
            elementIcon.enabled = icon != null;
        }

        private void ApplyPresetVisuals(UnitPresetSO preset, Sprite portraitOverride = null)
        {
            if (avatar != null)
            {
                var hasSceneAvatar = preserveSceneImages && avatar.sprite != null;
                if (!hasSceneAvatar)
                {
                    var sprite = portraitOverride ?? preset?.battleSprite;
                    if (sprite != null)
                    {
                        avatar.sprite = sprite;
                        avatar.color = Color.white;
                    }
                    else if (_unit != null)
                    {
                        avatar.sprite = null;
                        avatar.color = _unit.PlaceholderColor;
                    }
                }
            }

            var hasSceneElement = preserveSceneImages && elementIcon != null && elementIcon.sprite != null;
            if (!hasSceneElement)
            {
                SetElementIcon(preset?.elementIcon);
            }
        }

        private void HandleHpChanged(CombatUnit unit)
        {
            if (unit != _unit)
            {
                return;
            }

            RefreshHp();
        }

        private void SetEmpty()
        {
            WireReferences();
            if (avatar != null)
            {
                avatar.sprite = null;
                avatar.color = new Color(0.2f, 0.2f, 0.25f, 0.6f);
            }

            SetElementIcon(null);

            if (hpFill != null)
            {
                hpFill.fillAmount = 0f;
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}
