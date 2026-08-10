using System.Collections.Generic;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using UnityEngine;
using UnityEngine.UI;
using FracturedChorus.UI;

namespace FracturedChorus.UI
{
    public class BossNoteClusterView : MonoBehaviour
    {
        private static readonly Color NumberColor = Color.white;
        private static readonly Color OutlinePurple = FcColorTokens.WithAlpha(FcColorTokens.Semantic.ElementMelody, 1f);
        private static readonly Color OutlineBlue = FcColorTokens.WithAlpha(FcColorTokens.Brand.CyanNeonBody, 1f);
        private static readonly Color OutlineRed = FcColorTokens.WithAlpha(FcColorTokens.Semantic.ElementRhythm, 1f);

        private readonly List<GameObject> _spawned = new();
        private readonly List<GameObject> _livingNoteRoots = new();
        private readonly Dictionary<int, RectTransform> _numberSlots = new();
        private RectTransform _layer;
        private TimelineNoteVisualCatalog _catalog;
        private System.Func<int, float> _contentXForBeat;
        private float _noteYFromBottom;
        private BossNoteNumberLayout _layout = new();
        private bool _editShellsPurgedForPlay;
        private bool _simLayoutCaptured;
        private Vector2 _simNoteSize;
        private float _simNoteAlpha = 0.78f;
        private BossNoteShapeLayout[] _simShapeLayouts;

        public void Configure(
            RectTransform layer,
            TimelineNoteVisualCatalog catalog,
            System.Func<int, float> contentXForBeat,
            float noteYFromBottom,
            BossNoteNumberLayout layout = null,
            System.Func<int, float> beatWidthForBeat = null)
        {
            _layer = layer;
            _catalog = catalog;
            _contentXForBeat = contentXForBeat;
            _noteYFromBottom = noteYFromBottom;
            _layout = layout ?? new BossNoteNumberLayout();
            if (_layout.variantNudges == null || _layout.variantNudges.Length != 5)
            {
                _layout.variantNudges = new Vector2[5];
            }

            _layout.EnsureSingleHeadNormByVariant();
        }

        public void Clear()
        {
            foreach (var go in _spawned)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _spawned.Clear();
            _livingNoteRoots.Clear();
            _numberSlots.Clear();
        }

        public Vector2 GetPerfectMarkSizeForBeat(int beatIndex, bool preview)
        {
            return ResolvePerfectMarkSize(preview);
        }

        public bool TryAttachPerfectPreview(int beatIndex, Sprite sprite, out Image image)
        {
            image = null;
            if (sprite == null ||
                !_numberSlots.TryGetValue(beatIndex, out var slot) ||
                slot == null)
            {
                return false;
            }

            var text = slot.GetComponent<Text>();
            if (text != null)
            {
                text.enabled = false;
            }

            image = SpawnPerfectOnSlot(slot, beatIndex, sprite, preview: true);
            return image != null;
        }

        public void EndPerfectPreview()
        {
            foreach (var slot in _numberSlots.Values)
            {
                if (slot == null)
                {
                    continue;
                }

                var text = slot.GetComponent<Text>();
                if (text != null && !string.IsNullOrEmpty(text.text))
                {
                    text.enabled = true;
                }
            }
        }

        public void Rebuild(BeatTimelineEngine timeline, float viewportHeight)
        {
            Clear();

            if (_layer == null || _contentXForBeat == null)
            {
                return;
            }

            _catalog?.EnsureDefaultsLoaded();
            var authored = ResolveAuthoredSpecsForRebuild();
            var clusters = BossNoteClusterBuilder.Build(timeline, authored);
            if (clusters == null || clusters.Count == 0)
            {
                return;
            }

            var y = _noteYFromBottom;
            if (viewportHeight > 1f)
            {
                y = Mathf.Clamp(_noteYFromBottom, 0f, viewportHeight);
            }

            foreach (var cluster in clusters)
            {
                // Double notes: one note per beat (each with its own RailAnchor), not one beamed glyph.
                if (cluster.Kind == BossNoteGlyphKind.Beamed)
                {
                    SpawnSingle(cluster.Left, y);
                    SpawnSingle(cluster.Right, y);
                }
                else
                {
                    SpawnSingle(cluster.Left, y);
                }
            }

            BringLivingNotesToFront();
        }

        /// <summary>
        /// Edit mode: keep NoteSimulator + optional seed shells.
        /// Play: capture simulator layout, hide simulator, purge other seeds — telegraphs only.
        /// </summary>
        private List<AuthoredBossNoteSpec> ResolveAuthoredSpecsForRebuild()
        {
            if (!Application.isPlaying)
            {
                _editShellsPurgedForPlay = false;
                _simLayoutCaptured = false;
                SetEditModeNoteShellsActive(true);
                var simEdit = BossNoteSimulator.FindInLayer(_layer);
                if (simEdit != null)
                {
                    simEdit.gameObject.SetActive(true);
                }

                return CollectAuthoredSpecs();
            }

            if (!_editShellsPurgedForPlay)
            {
                CaptureSimulatorLayoutForPlay();
                DestroyEditModeNoteShells();
                _editShellsPurgedForPlay = true;
            }

            return null;
        }

        private void CaptureSimulatorLayoutForPlay()
        {
            var sim = BossNoteSimulator.FindInLayer(_layer);
            if (sim == null || !sim.TryCapturePlayLayout(out var size, out var alpha, out var layouts))
            {
                _simLayoutCaptured = false;
                _simShapeLayouts = null;
                return;
            }

            _simNoteSize = size;
            _simNoteAlpha = alpha;
            _simShapeLayouts = layouts;
            _simLayoutCaptured = true;
            sim.SyncLayoutToCatalog();
            // Keep alive (hidden) so layout stays available; never destroy NoteSimulator.
            sim.gameObject.SetActive(false);
        }

        private void SetEditModeNoteShellsActive(bool active)
        {
            if (_layer == null)
            {
                return;
            }

            for (var i = 0; i < _layer.childCount; i++)
            {
                var child = _layer.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (IsSimulatorObject(child))
                {
                    child.gameObject.SetActive(active);
                    continue;
                }

                var name = child.name;
                if (name.StartsWith("NoteSingle_") || name.StartsWith("NoteBeamed_"))
                {
                    child.gameObject.SetActive(active);
                }
            }
        }

        private void DestroyEditModeNoteShells()
        {
            if (_layer == null)
            {
                return;
            }

            for (var i = _layer.childCount - 1; i >= 0; i--)
            {
                var child = _layer.GetChild(i);
                if (child == null || IsSimulatorObject(child))
                {
                    continue;
                }

                var name = child.name;
                if (!name.StartsWith("NoteSingle_") && !name.StartsWith("NoteBeamed_"))
                {
                    continue;
                }

                if (_spawned.Contains(child.gameObject))
                {
                    continue;
                }

                Destroy(child.gameObject);
            }
        }

        private static bool IsSimulatorObject(Transform child)
        {
            if (child == null)
            {
                return false;
            }

            return child.GetComponent<BossNoteSimulator>() != null
                   || child.name == BossNoteSimulator.ObjectName;
        }

        private Vector2 ResolveSingleNoteSize()
        {
            if (_simLayoutCaptured && _simNoteSize.x > 1f && _simNoteSize.y > 1f)
            {
                return _simNoteSize;
            }

            return _catalog != null
                ? _catalog.ResolveSingleNoteSize()
                : _simNoteSize.x > 1f ? _simNoteSize : new Vector2(52.95f, 67.24f);
        }

        private BossNoteShapeLayout ResolveShapeLayout(int variantIndex, Vector2 size, Sprite sprite)
        {
            if (_simLayoutCaptured && _simShapeLayouts != null && _simShapeLayouts.Length > 0)
            {
                var i = Mathf.Clamp(variantIndex, 0, _simShapeLayouts.Length - 1);
                var saved = _simShapeLayouts[i];
                if (saved.HasData)
                {
                    if (saved.knobSize.x < 0.5f && saved.knobSize.y < 0.5f)
                    {
                        return BossNoteShapeLayout.FromLegacyNoteSpace(
                            saved.railAnchorLocal,
                            saved.noteNumLocal);
                    }

                    return saved;
                }
            }

            var pin = FittedLocalFromNorm(_layout.ResolveSingleHeadNorm(variantIndex), size, sprite);
            return BossNoteShapeLayout.FromKnob(
                pin,
                new Vector2(24f, 24f),
                Vector2.zero,
                Vector2.zero);
        }

        private List<AuthoredBossNoteSpec> CollectAuthoredSpecs()
        {
            var list = new List<AuthoredBossNoteSpec>();
            if (_layer == null)
            {
                return list;
            }

            var authored = _layer.GetComponentsInChildren<BossNoteAuthoring>(true);
            for (var i = 0; i < authored.Length; i++)
            {
                var a = authored[i];
                if (a == null)
                {
                    continue;
                }

                list.Add(new AuthoredBossNoteSpec(a.BeatIndex, a.RemainingHits, a.DisplayTier));
            }

            return list;
        }

        private void BringLivingNotesToFront()
        {
            foreach (var root in _livingNoteRoots)
            {
                if (root != null)
                {
                    root.transform.SetAsLastSibling();
                }
            }
        }

        private void SpawnSingle(BossNoteHead head, float y)
        {
            var x = _contentXForBeat(head.BeatIndex);
            var size = ResolveSingleNoteSize();
            var w = size.x;
            var h = size.y;

            if (head.IsCleared)
            {
                var markSide = ResolvePerfectMarkSize(preview: false).x;
                var anchor = Mathf.Max(markSide, 24f);
                var noteImg = CreateImage(
                    $"NoteSingle_{head.BeatIndex}",
                    null,
                    new Vector2(x, y),
                    new Vector2(anchor, anchor));
                noteImg.enabled = false;
                noteImg.color = new Color(1f, 1f, 1f, 0f);

                var slot = CreateNumberSlot(
                    head.BeatIndex,
                    noteImg.rectTransform,
                    Vector2.zero,
                    anchor / 1.45f,
                    BossNoteNumberRole.Single,
                    Vector2.zero,
                    head.VariantIndex);
                SpawnPerfectOnSlot(
                    slot,
                    head.BeatIndex,
                    _catalog != null ? _catalog.CoverPerfect : null,
                    preview: false);
                return;
            }

            var sprite = _catalog != null
                ? _catalog.MusicSingle(head.VariantIndex, head.DisplayTier)
                : null;
            var shape = ResolveShapeLayout(head.VariantIndex, size, sprite);
            var pin = shape.PinInNoteSpace;

            // Pin like FeetAnchor: notePos + (Knob + RailAnchor) = (beatX, railY).
            var living = CreateImage(
                $"NoteSingle_{head.BeatIndex}",
                sprite,
                new Vector2(x - pin.x, y - pin.y),
                new Vector2(w, h));
            ApplyNoteAlpha(living);

            var knob = BossNoteSimulator.EnsureKnobOn(living.rectTransform, shape);
            _livingNoteRoots.Add(living.gameObject);

            var numSlot = CreateNumberSlot(
                head.BeatIndex,
                knob != null ? knob : living.rectTransform,
                shape.noteNumLocal,
                w * _layout.numberSizeFactor,
                BossNoteNumberRole.Single,
                shape.railAnchorLocal,
                head.VariantIndex);
            FillNumberText(numSlot, head);
        }

        private void ApplyNoteAlpha(Image image)
        {
            if (image == null)
            {
                return;
            }

            var a = _simLayoutCaptured && _simNoteAlpha > 0.01f
                ? _simNoteAlpha
                : (_catalog != null && _catalog.NoteAlpha > 0.01f ? _catalog.NoteAlpha : 0.78f);
            var c = image.color;
            c.a = a;
            image.color = c;
        }

        private Vector2 ResolvePerfectMarkSize(bool preview)
        {
            var side = _layout != null ? Mathf.Max(12f, _layout.perfectMarkFixedPx) : 24f;

            if (preview)
            {
                var previewScale = _layout != null
                    ? Mathf.Clamp(_layout.perfectPreviewScale, 1f, 1.2f)
                    : 1.1f;
                side *= previewScale;
            }

            return new Vector2(side, side);
        }

        private RectTransform CreateNumberSlot(
            int beatIndex,
            RectTransform noteParent,
            Vector2 localPos,
            float fontSize,
            BossNoteNumberRole role,
            Vector2 baseLocalPos,
            int variantIndex)
        {
            var go = new GameObject($"NoteNum_{beatIndex}", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(noteParent != null ? noteParent : _layer, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var box = Mathf.Max(36f, fontSize * 1.45f);
            rt.sizeDelta = new Vector2(box, box);
            rt.anchoredPosition = localPos;
            rt.SetAsLastSibling();

            var handle = go.AddComponent<BossNoteNumberHandle>();
            handle.Role = role;
            handle.VariantIndex = variantIndex;
            handle.BaseLocalPos = baseLocalPos;

            _numberSlots[beatIndex] = rt;
            _spawned.Add(go);
            return rt;
        }

        private void FillNumberText(RectTransform slot, BossNoteHead head)
        {
            if (slot == null)
            {
                return;
            }

            var text = slot.gameObject.GetComponent<Text>();
            if (text == null)
            {
                text = slot.gameObject.AddComponent<Text>();
            }

            text.font = UiFontCatalog.Body;
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var fontSize = Mathf.RoundToInt(Mathf.Clamp(slot.sizeDelta.y * 0.72f, 16f, 36f));
            text.text = Mathf.Clamp(head.RemainingHits, 0, 9).ToString();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = NumberColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = slot.gameObject.GetComponent<Outline>();
            if (outline == null)
            {
                outline = slot.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = OutlineForTier(head.DisplayTier);
            outline.effectDistance = new Vector2(1.4f, -1.4f);
        }

        private Image SpawnPerfectOnSlot(
            RectTransform slot,
            int beatIndex,
            Sprite sprite,
            bool preview)
        {
            if (slot == null)
            {
                return null;
            }

            _catalog?.EnsureDefaultsLoaded();
            if (sprite == null)
            {
                sprite = _catalog != null ? _catalog.CoverPerfect : null;
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[BossNote] CoverPerfect missing @ beat {beatIndex}");
                return null;
            }

            var size = ResolvePerfectMarkSize(preview);
            var go = new GameObject(
                preview ? $"NotePerfectPreview_{beatIndex}" : $"NotePerfect_{beatIndex}",
                typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(slot, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.enabled = true;
            img.color = Color.white;

            _spawned.Add(go);
            return img;
        }

        private static Vector2 FittedLocalFromNorm(Vector2 normFromCenter, Vector2 rectSize, Sprite sprite)
        {
            if (sprite == null || rectSize.y < 0.01f)
            {
                return new Vector2(normFromCenter.x * rectSize.x, normFromCenter.y * rectSize.y);
            }

            var sprAspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            var rectAspect = rectSize.x / rectSize.y;
            float drawW;
            float drawH;
            if (rectAspect > sprAspect)
            {
                drawH = rectSize.y;
                drawW = drawH * sprAspect;
            }
            else
            {
                drawW = rectSize.x;
                drawH = drawW / sprAspect;
            }

            return new Vector2(normFromCenter.x * drawW, normFromCenter.y * drawH);
        }

        private Image CreateImage(string name, Sprite sprite, Vector2 anchored, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_layer, false);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            if (sprite == null)
            {
                img.color = FcColorTokens.WithAlpha(FcColorTokens.Brand.MagentaAccent, 0.9f);
            }

            _spawned.Add(go);
            return img;
        }

        private static Color OutlineForTier(BossNoteTier tier) =>
            tier switch
            {
                BossNoteTier.Purple => OutlinePurple,
                BossNoteTier.Blue => OutlineBlue,
                _ => OutlineRed
            };
    }
}
