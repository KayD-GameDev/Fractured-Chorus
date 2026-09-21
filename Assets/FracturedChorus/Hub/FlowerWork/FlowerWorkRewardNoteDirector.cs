using System;
using System.Collections;
using FracturedChorus.Hub;
using FracturedChorus.Meta;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub.FlowerWork
{
    public sealed class FlowerWorkRewardNoteDirector : MonoBehaviour
    {
        public const string NoteResourcePath = "UI/FlowerWork/note_resonance";
        public const float CrossBgSeconds = 0.72f;
        public const float ToNodeSeconds = 0.55f;
        public const float HoldOverlaySeconds = 0.85f;
        public const float NoteSize = 112f;

        [SerializeField] private Sprite noteSprite;
        [SerializeField] private RectTransform overlayRoot;
        [SerializeField] private Image noteImage;

        private Image _note;
        private Material _additive;
        private SocialStatsOverlayUI _overlay;

        public IEnumerator PlayResonanceReward(VnRuntimeController runtime, GameMetaState state, Action applyResonance)
        {
            UiEscapeGate.Push(this);
            Image note = null;
            var applied = false;

            var canvas = ResolveCanvas(runtime);
            if (canvas == null)
            {
                Debug.LogError("[FlowerWork] Reward note missing canvas.");
                applyResonance?.Invoke();
                UiEscapeGate.Pop(this);
                yield break;
            }

            ShowRen(runtime);
            note = SpawnNote(canvas.transform);
            var start = ResolveOrigin(runtime, canvas);
            var mid = ScreenToCanvas(canvas, new Vector2(Screen.width * 0.62f, Screen.height * 0.58f));
            Place(note.rectTransform, start);
            yield return MoveArc(note.rectTransform, start, mid, CrossBgSeconds);

            var overlay = EnsureOverlay(canvas.transform);
            overlay.Show(state, allowCancel: false);
            note.transform.SetAsLastSibling();

            var node = overlay.GetNodeRect(SocialStatType.Resonance);
            var target = node != null
                ? WorldToCanvas(canvas, node.TransformPoint(node.rect.center))
                : mid + new Vector2(-220f, 40f);
            yield return MoveEaseOut(note.rectTransform, mid, target, ToNodeSeconds);
            Punch(note.rectTransform);

            applyResonance?.Invoke();
            applied = true;
            overlay.RefreshFromState();
            yield return new WaitForSeconds(HoldOverlaySeconds);
            overlay.Hide();

            FinishReward(note, applied, applyResonance);
        }

        private void FinishReward(Image note, bool applied, Action applyResonance)
        {
            if (!applied)
            {
                applyResonance?.Invoke();
            }

            ReleaseNoteVisual(note);
            UiEscapeGate.Pop(this);
        }

        private void ReleaseNoteVisual(Image note)
        {
            if (note == null)
            {
                return;
            }

            if (noteImage != null && note == noteImage)
            {
                note.gameObject.SetActive(false);
            }
            else
            {
                Destroy(note.gameObject);
            }

            _note = null;
        }

        private static Canvas ResolveCanvas(VnRuntimeController runtime)
        {
            if (runtime != null && runtime.BackgroundImage != null)
            {
                return runtime.BackgroundImage.canvas;
            }

            return null;
        }

        private static void ShowRen(VnRuntimeController runtime)
        {
            if (runtime?.SpeakerCatalog == null || runtime.PortraitView == null)
            {
                return;
            }

            if (runtime.SpeakerCatalog.TryGet(VnSpeakerIds.Ren, out var ren))
            {
                runtime.PortraitView.Show(ren, "smile");
            }
        }

        private Image SpawnNote(Transform canvas)
        {
            if (noteImage != null)
            {
                _note = noteImage;
                noteImage.gameObject.SetActive(true);
                return noteImage;
            }

            if (_note != null)
            {
                _note.gameObject.SetActive(true);
                return _note;
            }

            var go = new GameObject("FlowerRewardNote", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(NoteSize, NoteSize);

            var image = go.GetComponent<Image>();
            image.sprite = noteSprite != null ? noteSprite : Resources.Load<Sprite>(NoteResourcePath);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.material = ResolveAdditive();
            _note = image;
            return image;
        }

        private Material ResolveAdditive()
        {
            if (_additive != null)
            {
                return _additive;
            }

            var shader = Shader.Find("FracturedChorus/UI/Additive");
            if (shader == null)
            {
                return null;
            }

            _additive = new Material(shader)
            {
                name = "FlowerRewardNoteAdditive_Runtime"
            };
            return _additive;
        }

        private SocialStatsOverlayUI EnsureOverlay(Transform canvas)
        {
            if (_overlay != null)
            {
                return _overlay;
            }

            if (overlayRoot != null)
            {
                _overlay = overlayRoot.GetComponent<SocialStatsOverlayUI>()
                           ?? SocialStatsOverlayUI.Build(overlayRoot).Overlay;
                return _overlay;
            }

            _overlay = SocialStatsOverlayUI.Build(canvas).Overlay;
            return _overlay;
        }

        private static Vector2 ResolveOrigin(VnRuntimeController runtime, Canvas canvas)
        {
            var left = runtime != null && runtime.PortraitView != null ? runtime.PortraitView.LeftRoot : null;
            if (left != null && left.gameObject.activeInHierarchy)
            {
                return WorldToCanvas(canvas, left.TransformPoint(new Vector3(left.rect.width * 0.55f, left.rect.height * 0.62f)));
            }

            return ScreenToCanvas(canvas, new Vector2(Screen.width * 0.22f, Screen.height * 0.42f));
        }

        private static Vector2 ScreenToCanvas(Canvas canvas, Vector2 screen)
        {
            var canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return screen;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            return local;
        }

        private static Vector2 WorldToCanvas(Canvas canvas, Vector3 world)
        {
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            return ScreenToCanvas(canvas, screen);
        }

        private static void Place(RectTransform rect, Vector2 local)
        {
            rect.anchoredPosition = local;
            rect.localScale = Vector3.one;
        }

        private static IEnumerator MoveArc(RectTransform rect, Vector2 from, Vector2 to, float seconds)
        {
            var lift = new Vector2(0f, Mathf.Abs(to.x - from.x) * 0.22f);
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var eased = t * t * (3f - 2f * t);
                var pos = Vector2.Lerp(from, to, eased);
                pos += lift * Mathf.Sin(t * Mathf.PI);
                rect.anchoredPosition = pos;
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(18f, -12f, eased));
                yield return null;
            }

            rect.anchoredPosition = to;
        }

        private static IEnumerator MoveEaseOut(RectTransform rect, Vector2 from, Vector2 to, float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                rect.anchoredPosition = Vector2.Lerp(from, to, eased);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.72f, eased);
                yield return null;
            }

            rect.anchoredPosition = to;
        }

        private static void Punch(RectTransform rect)
        {
            rect.localScale = Vector3.one * 1.15f;
        }

        private void OnDestroy()
        {
            UiEscapeGate.Pop(this);
            if (_additive != null)
            {
                Destroy(_additive);
            }
        }
    }
}
