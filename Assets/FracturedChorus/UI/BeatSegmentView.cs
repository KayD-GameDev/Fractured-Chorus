using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Combat.Presentation;
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
        [SerializeField] private Image beatFrame;
        [SerializeField] private Image noteTier;
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
        private TimelineNoteVisualCatalog _noteVisuals;
        private Sprite _defaultPortraitSprite;
        private bool _capturedDefaultPortrait;
        private float _noteBandNormalizedY = 0.72f;
        private Image _beatFrameVisual;
        private Image _noteTierVisual;
        private bool _suppressActiveImpactGlyph;

        public int DisplayBeatIndex => beatIndex;

        public void SetNoteVisualCatalog(TimelineNoteVisualCatalog catalog)
        {
            _noteVisuals = catalog;
        }

        public void SetSuppressActiveImpactGlyph(bool suppress)
        {
            _suppressActiveImpactGlyph = suppress;
        }

        public void SetNoteBandNormalizedY(float normalizedYFromBottom)
        {
            _noteBandNormalizedY = Mathf.Clamp01(normalizedYFromBottom);
        }

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

            var staleMask = GetComponent<RectMask2D>();
            if (staleMask != null)
            {
                Destroy(staleMask);
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

            if (noteTier == null)
            {
                noteTier = transform.Find("NoteTier")?.GetComponent<Image>();
            }

            _noteTierVisual = noteTier != null ? noteTier : portrait;

            if (_noteTierVisual != null && !_capturedDefaultPortrait)
            {
                _defaultPortraitSprite = _noteTierVisual.sprite;
                _capturedDefaultPortrait = true;
            }

            if (beatFrame == null)
            {
                beatFrame = transform.Find("BeatFrame")?.GetComponent<Image>();
            }

            if (beatFrame != null)
            {
                _beatFrameVisual = beatFrame;
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
                _rect.localScale = Vector3.one;
            }

            ResetScanHighlight();
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

            if (glow != null && glow.enabled)
            {
                var peakAlpha = Mathf.Max(_glowBaseColor.a, 0.55f);
                var glowAlpha = Mathf.Lerp(_glowBaseColor.a, peakAlpha, intensity);
                glow.color = new Color(_glowBaseColor.r, _glowBaseColor.g, _glowBaseColor.b, glowAlpha);
            }

            if (_beatFrameVisual != null && _beatFrameVisual.enabled)
            {
                var c = _backgroundBaseColor;
                _beatFrameVisual.color = new Color(
                    Mathf.Min(1f, c.r + 0.1f * intensity),
                    Mathf.Min(1f, c.g + 0.1f * intensity),
                    Mathf.Min(1f, c.b + 0.1f * intensity),
                    c.a);
            }
            else if (background != null)
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

        public void ClearEnemyVisualOnly()
        {
            WireReferences();
            var note = NoteImage;
            if (note != null)
            {
                note.sprite = _defaultPortraitSprite;
                note.color = new Color(0.2f, 0.2f, 0.24f, 0.1f);
                ApplyPortraitLayout(22f);
            }

            if (actionLabel != null)
            {
                actionLabel.text = string.Empty;
            }

            ApplyBeatFrame(hasTelegraph: false, isWindup: false);
        }

        public void SetEmpty()
        {
            WireReferences();
            ApplyBeatFrame(hasTelegraph: false, isWindup: false);

            if (glow != null)
            {
                glow.enabled = true;
                glow.color = new Color(1f, 1f, 1f, 0.05f);
                _glowBaseColor = glow.color;
            }

            var note = NoteImage;
            if (note != null)
            {
                note.sprite = _defaultPortraitSprite;
                note.color = new Color(0.2f, 0.2f, 0.24f, 0.1f);
                ApplyPortraitLayout(22f);
            }

            if (actionLabel != null)
            {
                actionLabel.text = string.Empty;
            }
        }

        public void SetEntry(AgendaEntry entry, bool isTelegraph = false)
        {
            if (!isTelegraph || entry?.Skill == null)
            {
                SetEmpty();
                return;
            }

            SetTelegraphSlot(new EnemyTelegraph
            {
                Unit = entry.Unit,
                Skill = entry.Skill,
                BeatIndex = entry.BeatIndex,
                IsWindupOnly = false
            }, remainingHits: -1);
        }

        public void SetSlot(AgendaEntry playerEntry, EnemyTelegraph enemyTelegraph)
        {
            SetSlot(playerEntry, enemyTelegraph, remainingHits: -1);
        }

        /// <param name="remainingHits">
        /// Hits left to cancel (−1 = use telegraph.HitsRequired / NoteTier as-authored).
        /// 0 shows cover-perfect icon.
        /// </param>
        public void SetSlot(AgendaEntry playerEntry, EnemyTelegraph enemyTelegraph, int remainingHits)
        {
            WireReferences();

            var hasEnemy = enemyTelegraph?.Skill != null;

            if (!hasEnemy)
            {
                SetEmpty();
                return;
            }

            SetTelegraphSlot(enemyTelegraph, remainingHits);
        }

        private Image NoteImage => _noteTierVisual != null ? _noteTierVisual : portrait;

        private void SetTelegraphSlot(EnemyTelegraph telegraph, int remainingHits)
        {
            WireReferences();
            var skill = telegraph.Skill;
            if (skill == null)
            {
                SetEmpty();
                return;
            }

            var isWindup = telegraph.IsWindupOnly;
            ApplyBeatFrame(hasTelegraph: true, isWindup: isWindup);

            if (glow != null)
            {
                var glowColor = isWindup
                    ? new Color(1f, 0.25f, 0.15f, 0.2f)
                    : new Color(1f, 1f, 1f, 0.08f);
                glow.color = glowColor;
                _glowBaseColor = glow.color;
            }

            var note = NoteImage;
            if (note != null)
            {
                note.gameObject.SetActive(true);
                if (isWindup)
                {
                    note.sprite = _defaultPortraitSprite;
                    note.color = new Color(0.85f, 0.2f, 0.2f, 0.75f);
                    ApplyPortraitLayout(22f);
                }
                else
                {
                    var hits = remainingHits >= 0
                        ? remainingHits
                        : (telegraph.HitsRequired > 0 ? telegraph.HitsRequired : (int)telegraph.NoteTier);
                    ApplyImpactNoteVisual(hits);
                }
            }

            if (actionLabel != null)
            {
                if (isWindup)
                {
                    actionLabel.text = "◆ ↑";
                }
                else
                {
                    var hits = remainingHits >= 0
                        ? remainingHits
                        : (telegraph.HitsRequired > 0 ? telegraph.HitsRequired : (int)telegraph.NoteTier);
                    if (hits <= 0)
                    {
                        actionLabel.text = "◆ PERFECT";
                    }
                    else if (CombatCounterResolver.TryGetDisplayTier(hits, out var displayTier) && hits > 1)
                    {
                        actionLabel.text = $"◆ {GetNoteLabel(displayTier)} · {hits}";
                    }
                    else
                    {
                        actionLabel.text = $"◆ {skill.displayName.ToUpperInvariant()}";
                    }
                }
            }
        }

        private void ApplyBeatFrame(bool hasTelegraph, bool isWindup)
        {
            if (!hasTelegraph)
            {
                if (_beatFrameVisual != null)
                {
                    _beatFrameVisual.enabled = false;
                }

                if (background != null)
                {
                    background.sprite = null;
                    background.type = Image.Type.Simple;
                    background.color = new Color(0.1f, 0.11f, 0.16f, 0.22f);
                    background.raycastTarget = true;
                    background.enabled = true;
                    _backgroundBaseColor = background.color;
                }

                return;
            }

            EnsureBeatFrameVisual();

            if (background != null)
            {
                background.sprite = null;
                background.type = Image.Type.Simple;
                background.color = new Color(0.1f, 0.1f, 0.14f, 0.28f);
                background.raycastTarget = true;
            }

            var sprite = _noteVisuals?.BeatFrame(true, isWindup);
            if (sprite != null && _beatFrameVisual != null)
            {
                StretchInset(_beatFrameVisual.rectTransform, 1.5f);
                _beatFrameVisual.enabled = true;
                _beatFrameVisual.sprite = sprite;
                _beatFrameVisual.type = Image.Type.Simple;
                _beatFrameVisual.preserveAspect = false;
                _beatFrameVisual.fillCenter = true;
                _beatFrameVisual.raycastTarget = false;
                _beatFrameVisual.color = new Color(1f, 1f, 1f, 0.55f);
                _backgroundBaseColor = _beatFrameVisual.color;

                if (glow != null)
                {
                    glow.enabled = false;
                }

                return;
            }

            if (_beatFrameVisual != null)
            {
                _beatFrameVisual.enabled = false;
            }

            if (glow != null)
            {
                glow.enabled = true;
            }

            if (background != null)
            {
                background.color = isWindup
                    ? new Color(0.35f, 0.14f, 0.14f, 0.75f)
                    : new Color(0.12f, 0.12f, 0.18f, 0.85f);
                _backgroundBaseColor = background.color;
            }
        }

        private void EnsureBeatFrameVisual()
        {
            var staleMask = GetComponent<RectMask2D>();
            if (staleMask != null)
            {
                Destroy(staleMask);
            }

            if (_beatFrameVisual == null)
            {
                if (beatFrame != null)
                {
                    _beatFrameVisual = beatFrame;
                }
                else
                {
                    var existing = transform.Find("BeatFrame")?.GetComponent<Image>();
                    if (existing != null)
                    {
                        _beatFrameVisual = existing;
                        beatFrame = existing;
                    }
                    else
                    {
                        var go = new GameObject("BeatFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                        var rt = go.GetComponent<RectTransform>();
                        rt.SetParent(transform, false);
                        _beatFrameVisual = go.GetComponent<Image>();
                        _beatFrameVisual.raycastTarget = false;
                        beatFrame = _beatFrameVisual;
                    }
                }
            }

            StretchInset(_beatFrameVisual.rectTransform, 1.5f);
            if (background != null)
            {
                background.transform.SetAsFirstSibling();
                _beatFrameVisual.transform.SetSiblingIndex(1);
            }
            else
            {
                _beatFrameVisual.transform.SetAsFirstSibling();
            }
        }

        private static void StretchInset(RectTransform rt, float insetPx)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = new Vector2(insetPx, insetPx);
            rt.offsetMax = new Vector2(-insetPx, -insetPx);
            rt.localScale = Vector3.one;
        }

        private void ApplyImpactNoteVisual(int remainingHits)
        {
            var note = NoteImage;
            if (note == null)
            {
                return;
            }

            var noteAlpha = 0.78f;
            if (_noteVisuals != null && _noteVisuals.NoteAlpha > 0.01f)
            {
                noteAlpha = Mathf.Clamp01(_noteVisuals.NoteAlpha);
            }

            if (_suppressActiveImpactGlyph)
            {
                HideImpactGlyph(note);
                if (portrait != null && portrait != note)
                {
                    HideImpactGlyph(portrait);
                }

                if (noteTier != null && noteTier != note && noteTier != portrait)
                {
                    HideImpactGlyph(noteTier);
                }

                return;
            }

            if (remainingHits <= 0)
            {
                note.enabled = true;
                var cover = _noteVisuals?.CoverPerfect;
                var coverSize = _noteVisuals != null ? _noteVisuals.CoverDisplaySize : 56f;
                var coverAlpha = _noteVisuals != null ? _noteVisuals.CoverPerfectAlpha : 1f;
                if (cover != null)
                {
                    note.sprite = cover;
                    note.color = new Color(1f, 1f, 1f, Mathf.Clamp01(coverAlpha));
                    note.preserveAspect = true;
                }
                else
                {
                    note.sprite = _defaultPortraitSprite;
                    note.color = new Color(0.95f, 0.85f, 0.35f, noteAlpha);
                }

                ApplyPortraitLayout(coverSize);
                return;
            }

            note.enabled = true;
            if (!CombatCounterResolver.TryGetDisplayTier(remainingHits, out var tier))
            {
                tier = BossNoteTier.Red;
            }

            ApplyImpactNotePortrait(tier);
        }

        private static void HideImpactGlyph(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.enabled = false;
            image.sprite = null;
            image.color = new Color(1f, 1f, 1f, 0f);
            if (image.gameObject.activeSelf)
            {
                image.gameObject.SetActive(false);
            }
        }

        private void ApplyImpactNotePortrait(BossNoteTier tier)
        {
            var note = NoteImage;
            if (note == null)
            {
                return;
            }

            var sprite = _noteVisuals?.NoteForTier(tier);
            var baseSize = _noteVisuals != null ? _noteVisuals.NoteDisplaySize : 40f;
            var scale = _noteVisuals != null ? _noteVisuals.NoteSizeScaleForTier(tier) : 1f;
            var size = baseSize * scale;

            var noteAlpha = 0.78f;
            if (_noteVisuals != null && _noteVisuals.NoteAlpha > 0.01f)
            {
                noteAlpha = Mathf.Clamp01(_noteVisuals.NoteAlpha);
            }
            if (sprite != null)
            {
                note.sprite = sprite;
                note.color = new Color(1f, 1f, 1f, noteAlpha);
                note.preserveAspect = true;
            }
            else
            {
                note.sprite = _defaultPortraitSprite;
                var tint = BossNoteTierColors.ForTier(tier);
                note.color = new Color(tint.r, tint.g, tint.b, noteAlpha);
            }

            ApplyPortraitLayout(size);
        }

        private void ApplyPortraitLayout(float size)
        {
            var note = NoteImage;
            if (note?.rectTransform == null)
            {
                return;
            }

            var rt = note.rectTransform;
            var y = _noteBandNormalizedY;
            rt.anchorMin = new Vector2(0.5f, y);
            rt.anchorMax = new Vector2(0.5f, y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
        }

        private static string GetNoteLabel(BossNoteTier tier)
        {
            return tier switch
            {
                BossNoteTier.Purple => "TÍM",
                BossNoteTier.Blue => "LÁ",
                _ => "ĐỎ"
            };
        }
    }
}
