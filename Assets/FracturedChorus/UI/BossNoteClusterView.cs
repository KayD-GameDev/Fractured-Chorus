using System.Collections.Generic;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class BossNoteClusterView : MonoBehaviour
    {
        private static readonly Color NumberColor = Color.white;
        private static readonly Color OutlinePurple = new Color(0.16f, 0.04f, 0.25f, 1f);
        private static readonly Color OutlineBlue = new Color(0.04f, 0.1f, 0.25f, 1f);
        private static readonly Color OutlineRed = new Color(0.25f, 0.06f, 0.09f, 1f);

        private readonly List<GameObject> _spawned = new();
        private readonly List<GameObject> _livingNoteRoots = new();
        private readonly Dictionary<int, RectTransform> _numberSlots = new();
        private RectTransform _layer;
        private TimelineNoteVisualCatalog _catalog;
        private System.Func<int, float> _contentXForBeat;
        private float _noteYFromBottom;
        private BossNoteNumberLayout _layout = new();

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
            if (_layer == null || timeline == null || _contentXForBeat == null)
            {
                return;
            }

            _catalog?.EnsureDefaultsLoaded();
            var clusters = BossNoteClusterBuilder.Build(timeline);

            var y = _noteYFromBottom;
            if (viewportHeight > 1f)
            {
                y = Mathf.Clamp(_noteYFromBottom, 0f, viewportHeight);
            }

            foreach (var cluster in clusters)
            {
                if (cluster.Kind == BossNoteGlyphKind.Beamed)
                {
                    SpawnBeamed(cluster, y);
                }
                else
                {
                    SpawnSingle(cluster.Left, y);
                }
            }

            BringLivingNotesToFront();
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
            var baseSize = _catalog != null ? Mathf.Max(40f, _catalog.NoteDisplaySize) : 48f;
            var scale = Mathf.Max(1f, _layout.singleScale);
            var w = baseSize * scale;
            var h = w * 1.28f;

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
            var norm = _layout.ResolveSingleHeadNorm(head.VariantIndex);
            var headLocal = FittedLocalFromNorm(norm, new Vector2(w, h), sprite);
            var nudge = _layout.numberNudgeSingle + _layout.ResolveVariantNudge(head.VariantIndex);
            var numberLocal = headLocal + nudge;

            var living = CreateImage(
                $"NoteSingle_{head.BeatIndex}",
                sprite,
                new Vector2(x, y),
                new Vector2(w, h));
            _livingNoteRoots.Add(living.gameObject);

            var numSlot = CreateNumberSlot(
                head.BeatIndex,
                living.rectTransform,
                numberLocal,
                w * _layout.numberSizeFactor,
                BossNoteNumberRole.Single,
                headLocal,
                head.VariantIndex);
            FillNumberText(numSlot, head);
        }

        private void SpawnBeamed(BossNoteCluster cluster, float y)
        {
            var left = cluster.Left;
            var right = cluster.Right;
            var x0 = _contentXForBeat(left.BeatIndex);
            var x1 = _contentXForBeat(right.BeatIndex);
            var mid = (x0 + x1) * 0.5f;
            var baseSize = _catalog != null ? Mathf.Max(40f, _catalog.NoteDisplaySize) : 48f;
            var height = baseSize * Mathf.Max(1f, _layout.beamedHeightScale);
            var width = Mathf.Abs(x1 - x0) + baseSize * 1.65f;
            var sprite = _catalog != null ? _catalog.MusicBeamedRedSprite() : null;
            var noteImg = CreateImage(
                $"NoteBeamed_{left.BeatIndex}_{right.BeatIndex}",
                sprite,
                new Vector2(mid, y),
                new Vector2(width, height));

            if (!left.IsCleared || !right.IsCleared)
            {
                _livingNoteRoots.Add(noteImg.gameObject);
            }

            var font = height * _layout.numberSizeFactor;
            var size = new Vector2(width, height);

            PlaceBeamedHead(left, noteImg.rectTransform, size, sprite, font, true);
            PlaceBeamedHead(right, noteImg.rectTransform, size, sprite, font, false);
        }

        private void PlaceBeamedHead(
            BossNoteHead head,
            RectTransform noteParent,
            Vector2 noteSize,
            Sprite sprite,
            float fontSize,
            bool isLeft)
        {
            var norm = isLeft ? _layout.beamedHeadNormLeft : _layout.beamedHeadNormRight;
            var role = isLeft ? BossNoteNumberRole.BeamedLeft : BossNoteNumberRole.BeamedRight;
            var sideNudge = isLeft ? _layout.numberNudgeBeamedLeft : _layout.numberNudgeBeamedRight;
            var baseLocal = FittedLocalFromNorm(norm, noteSize, sprite);
            var numberLocal = baseLocal + _layout.numberNudgeBeamed + sideNudge;

            var slot = CreateNumberSlot(
                head.BeatIndex,
                noteParent,
                numberLocal,
                fontSize,
                role,
                baseLocal,
                0);

            if (head.IsCleared)
            {
                SpawnPerfectOnSlot(
                    slot,
                    head.BeatIndex,
                    _catalog != null ? _catalog.CoverPerfect : null,
                    preview: false);
            }
            else
            {
                FillNumberText(slot, head);
            }
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

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                img.color = new Color(1f, 0.35f, 0.85f, 0.9f);
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
