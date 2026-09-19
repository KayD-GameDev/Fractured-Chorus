using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.VFX
{
    public static class ButterflyTransitionHierarchy
    {
        public const string RootName = "FC_ButterflyTransition";
        public const string BackgroundName = "Background";
        public const string ButterflyRootName = "ButterflyRoot";
        public const string ButterflySpriteName = "ButterflySprite";
        public const string ButterflyGlowName = "ButterflyGlow";
        public const string TrailOriginName = "TrailOrigin";
        public const string VfxName = "VFX";
        public const string StarDustName = "StarDust";
        public const string SmallFragmentsName = "SmallFragments";
        public const string PrismFragmentsName = "PrismFragments";
        public const string MusicFragmentsName = "MusicFragments";
        public const string WaveformTrailsName = "WaveformTrails";
        public const string AmbientFarName = "AmbientFar";
        public const string AmbientNearName = "AmbientNear";
        public const string FadeOverlayName = "FadeOverlay";

        public static void EnsureMissing(ButterflyTransitionController controller)
        {
            if (controller == null)
            {
                return;
            }

            var root = controller.transform as RectTransform;
            if (root == null)
            {
                return;
            }

            if (controller.GetComponent<Canvas>() == null)
            {
                var canvas = controller.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 80;
            }

            if (controller.GetComponent<CanvasScaler>() == null)
            {
                var scaler = controller.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (controller.GetComponent<GraphicRaycaster>() == null)
            {
                controller.gameObject.AddComponent<GraphicRaycaster>();
            }

            if (controller.GetComponent<CanvasGroup>() == null)
            {
                controller.gameObject.AddComponent<CanvasGroup>();
            }

            GetOrCreateImage(root, BackgroundName, true, new Color(0.043f, 0.071f, 0.125f, 1f), true);
            var butterflyRoot = GetOrCreateRect(root, ButterflyRootName, false, new Vector2(240f, 240f));
            GetOrCreateImage(butterflyRoot, ButterflyGlowName, false, new Color(0.55f, 0.85f, 1f, 0.35f), false, new Vector2(320f, 320f));
            GetOrCreateImage(butterflyRoot, ButterflySpriteName, false, Color.white, false, new Vector2(240f, 240f));
            GetOrCreateRect(butterflyRoot, TrailOriginName, false, new Vector2(8f, 8f), new Vector2(-28f, 0f));
            var vfx = GetOrCreateRect(root, VfxName, true, Vector2.zero);
            GetOrCreateRect(vfx, StarDustName, true, Vector2.zero);
            GetOrCreateRect(vfx, SmallFragmentsName, true, Vector2.zero);
            GetOrCreateRect(vfx, PrismFragmentsName, true, Vector2.zero);
            GetOrCreateRect(vfx, MusicFragmentsName, true, Vector2.zero);
            var waves = GetOrCreateRect(vfx, WaveformTrailsName, true, Vector2.zero);
            EnsureWaveGraphic(waves, "Wave_0", 120f, new Color(0.55f, 0.9f, 1f, 0.28f));
            EnsureWaveGraphic(waves, "Wave_1", 78f, new Color(0.72f, 0.95f, 1f, 0.7f));
            EnsureWaveGraphic(waves, "Wave_2", 44f, new Color(0.88f, 0.97f, 1f, 1f));
            GetOrCreateRect(vfx, AmbientFarName, true, Vector2.zero);
            GetOrCreateRect(vfx, AmbientNearName, true, Vector2.zero);
            var fade = GetOrCreateImage(root, FadeOverlayName, true, new Color(0.031f, 0.051f, 0.094f, 1f), false);
            if (fade.GetComponent<CanvasGroup>() == null)
            {
                fade.gameObject.AddComponent<CanvasGroup>();
            }
        }

        private static Image GetOrCreateImage(
            Transform parent,
            string name,
            bool stretch,
            Color color,
            bool raycast,
            Vector2? centeredSize = null)
        {
            var created = parent.Find(name) == null;
            var rect = GetOrCreateRect(parent, name, stretch, centeredSize ?? Vector2.zero);
            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
                created = true;
            }

            if (created)
            {
                image.color = color;
                image.raycastTarget = raycast;
                image.preserveAspect = !stretch;
            }

            return image;
        }

        private static void EnsureWaveGraphic(RectTransform parent, string name, float width, Color color)
        {
            var rect = GetOrCreateRect(parent, name, true, Vector2.zero);
            if (rect.GetComponent<CanvasRenderer>() == null)
            {
                rect.gameObject.AddComponent<CanvasRenderer>();
            }

            if (!rect.TryGetComponent(out ButterflyWaveformTrail wave))
            {
                wave = rect.gameObject.AddComponent<ButterflyWaveformTrail>();
                wave.color = color;
                wave.raycastTarget = false;
                wave.BindVisual(null, width);
            }
        }

        private static RectTransform GetOrCreateRect(
            Transform parent,
            string name,
            bool stretch,
            Vector2 centeredSize,
            Vector2? anchored = null)
        {
            var found = parent.Find(name) as RectTransform;
            if (found != null)
            {
                return found;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = centeredSize;
                rect.anchoredPosition = anchored ?? Vector2.zero;
            }

            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        public static void EmbedUnderUiCanvas(RectTransform root, Transform parentCanvas)
        {
            if (root == null || parentCanvas == null)
            {
                return;
            }

            DestroyComponent(root.GetComponent<Canvas>());
            DestroyComponent(root.GetComponent<CanvasScaler>());
            DestroyComponent(root.GetComponent<GraphicRaycaster>());

            root.SetParent(parentCanvas, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = Vector2.zero;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;
            root.localRotation = Quaternion.identity;

            var background = root.Find(BackgroundName)?.GetComponent<Image>();
            if (background != null)
            {
                background.raycastTarget = false;
            }

            var fade = root.Find(FadeOverlayName)?.GetComponent<Image>();
            if (fade != null)
            {
                fade.raycastTarget = false;
            }
        }

        private static void DestroyComponent(Object component)
        {
            if (component == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(component);
                return;
            }
#endif
            Object.Destroy(component);
        }
    }
}
