using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Timeline;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class BeatSegmentView : MonoBehaviour
    {
        [SerializeField] private int beatIndex;
        [SerializeField] private Image background;
        [SerializeField] private Image glow;
        [SerializeField] private Image portrait;
        [SerializeField] private Text actionLabel;
        [SerializeField] private Image phaseDividerLine;
        [SerializeField] private float scanScaleBoost = 1.14f;
        [SerializeField] private float scanFadeInDuration = 0.08f;
        [SerializeField] private float scanFadeOutDuration = 0.35f;

        private RectTransform _rect;
        private float _scanIntensity;
        private float _targetIntensity;
        private Color _glowBaseColor = Color.white;
        private Color _backgroundBaseColor = Color.white;

        public int DisplayBeatIndex => beatIndex;

        public void SetDisplayBeatIndex(int index)
        {
            beatIndex = index;
        }

        [System.Obsolete("Use SetDisplayBeatIndex")]
        public void SetBeatIndex(int index)
        {
            SetDisplayBeatIndex(index);
        }

        public void WireReferences()
        {
            if (_rect == null)
            {
                _rect = transform as RectTransform;
            }

            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (glow == null)
            {
                glow = transform.Find("Glow")?.GetComponent<Image>();
            }

            if (portrait == null)
            {
                portrait = transform.Find("Portrait")?.GetComponent<Image>();
            }

            if (actionLabel == null)
            {
                actionLabel = transform.Find("ActionLabel")?.GetComponent<Text>();
            }

            if (phaseDividerLine == null)
            {
                phaseDividerLine = transform.Find("PhaseDivider")?.GetComponent<Image>();
            }

            if (_rect != null)
            {
                _rect.pivot = new Vector2(0.5f, 0f);
            }

            UpdatePhaseDivider();
            CaptureLayoutBaseline();
        }

        public void CaptureLayoutBaseline()
        {
            if (_rect == null)
            {
                _rect = transform as RectTransform;
            }

            if (glow != null)
            {
                _glowBaseColor = glow.color;
            }

            if (background != null)
            {
                _backgroundBaseColor = background.color;
            }
        }

        public void SetScanHighlighted(bool highlighted)
        {
            SetScanIntensity(highlighted ? 1f : 0f);
        }

        public void SetScanIntensity(float intensity)
        {
            _targetIntensity = Mathf.Clamp01(intensity);
        }

        public void ResetScanHighlight()
        {
            _scanIntensity = 0f;
            _targetIntensity = 0f;
            ApplyScanVisual(0f);
        }

        private void Update()
        {
            if (Mathf.Approximately(_scanIntensity, _targetIntensity))
            {
                return;
            }

            var duration = _targetIntensity > _scanIntensity ? scanFadeInDuration : scanFadeOutDuration;
            var step = duration > 0f ? Time.deltaTime / duration : 1f;
            _scanIntensity = Mathf.MoveTowards(_scanIntensity, _targetIntensity, step);
            ApplyScanVisual(_scanIntensity);
        }

        private void ApplyScanVisual(float intensity)
        {
            if (_rect == null)
            {
                return;
            }

            _rect.localScale = Vector3.one * Mathf.Lerp(1f, scanScaleBoost, intensity);

            if (glow != null)
            {
                var peakAlpha = Mathf.Max(_glowBaseColor.a, 0.55f);
                var glowAlpha = Mathf.Lerp(_glowBaseColor.a, peakAlpha, intensity);
                glow.color = new Color(_glowBaseColor.r, _glowBaseColor.g, _glowBaseColor.b, glowAlpha);
            }

            if (background != null)
            {
                var c = _backgroundBaseColor;
                background.color = new Color(
                    Mathf.Min(1f, c.r + 0.1f * intensity),
                    Mathf.Min(1f, c.g + 0.1f * intensity),
                    Mathf.Min(1f, c.b + 0.1f * intensity),
                    c.a);
            }
        }

        public void UpdatePhaseDivider()
        {
            if (phaseDividerLine != null)
            {
                phaseDividerLine.gameObject.SetActive(TimelineConstants.IsPhaseDividerAfter(beatIndex));
            }
        }

        public void SetEmpty()
        {
            WireReferences();
            if (background != null)
            {
                background.color = new Color(0.12f, 0.12f, 0.18f, 0.45f);
                _backgroundBaseColor = background.color;
            }

            if (glow != null)
            {
                glow.color = new Color(1f, 1f, 1f, 0.05f);
                _glowBaseColor = glow.color;
            }

            if (portrait != null)
            {
                portrait.color = new Color(0.3f, 0.3f, 0.35f, 0.5f);
            }

            if (actionLabel != null)
            {
                actionLabel.text = string.Empty;
            }
        }

        public void SetEntry(AgendaEntry entry, bool isTelegraph = false)
        {
            SetSlot(entry, isTelegraph);
        }

        public void SetSlot(AgendaEntry playerEntry, EnemyTelegraph enemyTelegraph)
        {
            WireReferences();

            var hasPlayer = playerEntry?.Skill != null;
            var hasEnemy = enemyTelegraph?.Skill != null;

            if (!hasPlayer && !hasEnemy)
            {
                SetEmpty();
                return;
            }

            if (background != null)
            {
                background.color = hasEnemy
                    ? new Color(0.28f, 0.1f, 0.1f, 0.95f)
                    : new Color(0.18f, 0.16f, 0.24f, 0.95f);
                _backgroundBaseColor = background.color;
            }

            if (hasPlayer)
            {
                if (glow != null)
                {
                    glow.color = GetGlowColor(playerEntry.Skill.glowType);
                    _glowBaseColor = glow.color;
                }

                if (portrait != null)
                {
                    portrait.color = playerEntry.Unit?.PlaceholderColor ?? Color.gray;
                }

                if (actionLabel != null)
                {
                    actionLabel.text = hasEnemy
                        ? $"{playerEntry.Skill.displayName.ToUpperInvariant()} | EN"
                        : playerEntry.Skill.displayName.ToUpperInvariant();
                }
            }
            else
            {
                SetSlot(new AgendaEntry(enemyTelegraph.Unit, enemyTelegraph.Skill, enemyTelegraph.BeatIndex), true);
            }
        }

        private void SetSlot(AgendaEntry entry, bool isTelegraph)
        {
            WireReferences();
            if (entry?.Skill == null)
            {
                SetEmpty();
                return;
            }

            if (background != null)
            {
                background.color = isTelegraph
                    ? new Color(0.28f, 0.1f, 0.1f, 0.95f)
                    : new Color(0.18f, 0.16f, 0.24f, 0.95f);
                _backgroundBaseColor = background.color;
            }

            if (glow != null)
            {
                glow.color = GetGlowColor(entry.Skill.glowType);
                _glowBaseColor = glow.color;
            }

            if (portrait != null)
            {
                portrait.color = entry.Unit?.PlaceholderColor ?? Color.gray;
            }

            if (actionLabel != null)
            {
                actionLabel.text = entry.Skill.displayName.ToUpperInvariant();
            }
        }

        private static Color GetGlowColor(ActionGlowType glowType)
        {
            return glowType switch
            {
                ActionGlowType.Rush => new Color(0.2f, 0.5f, 1f, 0.45f),
                ActionGlowType.Support => new Color(0.2f, 0.9f, 0.4f, 0.4f),
                ActionGlowType.Guard => new Color(0.9f, 0.8f, 0.2f, 0.4f),
                _ => new Color(1f, 0.25f, 0.15f, 0.45f)
            };
        }
    }
}
