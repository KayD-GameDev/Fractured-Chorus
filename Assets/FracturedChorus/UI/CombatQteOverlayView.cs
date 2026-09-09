using FracturedChorus.Combat.Qte;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class CombatQteOverlayView : MonoBehaviour
    {
        public const int OverlaySortOrder = 560;

        [Header("Scene refs — chỉnh tay trong Hierarchy")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform ringRoot;
        [SerializeField] private Image innerRing;
        [SerializeField] private Image outerRing;
        [SerializeField] private Image prompt;
        [SerializeField] private Image gradeChip;
        [SerializeField] private Image dimmer;

        private Vector3 _outerBaseScale = Vector3.one;

        public Image InnerRing => innerRing;
        public Image OuterRing => outerRing;
        public Image Prompt => prompt;
        public Image GradeChip => gradeChip;

        public static CombatQteOverlayView EnsureCreated()
        {
            var existing = FindAnyObjectByType<CombatQteOverlayView>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.WireSceneReferences();
                return existing;
            }

            var go = new GameObject(
                "CombatQteOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(CombatQteOverlayView));
            var view = go.GetComponent<CombatQteOverlayView>();
            view.BuildDefaultHierarchy();
            view.Hide();
            return view;
        }

        public void WireSceneReferences()
        {
            if (overlayCanvas == null)
            {
                overlayCanvas = GetComponent<Canvas>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (dimmer == null)
            {
                dimmer = FindChildImage("Dimmer");
            }

            if (ringRoot == null)
            {
                var ring = transform.Find("RingRoot") as RectTransform;
                ringRoot = ring;
            }

            if (innerRing == null)
            {
                innerRing = FindChildImage("InnerRing");
            }

            if (outerRing == null)
            {
                outerRing = FindChildImage("OuterRing");
            }

            if (prompt == null)
            {
                prompt = FindChildImage("Prompt");
            }

            if (gradeChip == null)
            {
                gradeChip = FindChildImage("GradeChip");
            }
        }

        public void BuildDefaultHierarchy()
        {
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            overlayCanvas = GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                overlayCanvas = gameObject.AddComponent<Canvas>();
            }

            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = OverlaySortOrder;
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            dimmer = EnsureImage("Dimmer", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dimmer.color = new Color(0.02f, 0.04f, 0.12f, 0.28f);
            dimmer.raycastTarget = true;

            ringRoot = EnsureRect("RingRoot", transform);
            ringRoot.anchorMin = ringRoot.anchorMax = new Vector2(0.5f, 0.52f);
            ringRoot.pivot = new Vector2(0.5f, 0.5f);
            ringRoot.anchoredPosition = Vector2.zero;
            ringRoot.sizeDelta = new Vector2(256f, 256f);

            outerRing = EnsureImage("OuterRing", ringRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            outerRing.preserveAspect = true;
            outerRing.raycastTarget = false;
            outerRing.color = Color.white;

            innerRing = EnsureImage("InnerRing", ringRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            innerRing.preserveAspect = true;
            innerRing.raycastTarget = false;
            innerRing.color = Color.white;

            prompt = EnsureImage("Prompt", ringRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(168f, 72f));
            prompt.preserveAspect = true;
            prompt.raycastTarget = false;

            gradeChip = EnsureImage("GradeChip", transform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(320f, 96f));
            gradeChip.preserveAspect = true;
            gradeChip.raycastTarget = false;
            gradeChip.enabled = false;

            _outerBaseScale = Vector3.one;
            Hide();
        }

        public void ApplyProfile(CombatQteProfileSO profile)
        {
            WireSceneReferences();
            if (profile == null)
            {
                return;
            }

            Assign(innerRing, profile.innerRing);
            Assign(outerRing, profile.outerRing);
            Assign(prompt, profile.prompt);
        }

        public void ShowPrompt(CombatQteProfileSO profile)
        {
            if (this == null)
            {
                return;
            }

            WireSceneReferences();
            ApplyProfile(profile);
            if (gradeChip != null)
            {
                gradeChip.enabled = false;
            }

            if (outerRing != null)
            {
                _outerBaseScale = Vector3.one;
                outerRing.rectTransform.localScale = Vector3.one * (profile != null ? profile.outerStartScale : 1.65f);
            }

            SetVisible(true);
        }

        public void SetOuterScale(float scale)
        {
            if (this == null || outerRing == null)
            {
                return;
            }

            outerRing.rectTransform.localScale = _outerBaseScale * scale;
        }

        public void ShowGrade(Sprite sprite)
        {
            if (this == null || gradeChip == null)
            {
                return;
            }

            gradeChip.sprite = sprite;
            gradeChip.enabled = sprite != null;
            gradeChip.color = Color.white;
        }

        public void Hide()
        {
            if (this == null)
            {
                return;
            }

            SetVisible(false);
            if (gradeChip != null)
            {
                gradeChip.enabled = false;
            }
        }

        private void SetVisible(bool visible)
        {
            // Unity fake-null: destroyed overlay during Abort/OnDisable.
            if (this == null)
            {
                return;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            // Keep the GO active. SetActive(false) + C# ?. on EncounterDirector.Abort
            // throws MissingReferenceException and can stop the duel before damage.
            if (visible && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }
        }

        private Image FindChildImage(string childName)
        {
            var child = transform.Find(childName);
            if (child == null && ringRoot != null)
            {
                child = ringRoot.Find(childName);
            }

            if (child == null)
            {
                foreach (var img in GetComponentsInChildren<Image>(true))
                {
                    if (img != null && img.gameObject.name == childName)
                    {
                        return img;
                    }
                }
            }

            return child != null ? child.GetComponent<Image>() : null;
        }

        private static RectTransform EnsureRect(string name, Transform parent)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image EnsureImage(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 size)
        {
            var existing = parent.Find(name);
            Image image;
            if (existing != null)
            {
                image = existing.GetComponent<Image>();
                if (image == null)
                {
                    image = existing.gameObject.AddComponent<Image>();
                }
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }

            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            if (anchorMin == Vector2.zero && anchorMax == Vector2.one)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = size;
            }

            return image;
        }

        private static void Assign(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
            image.color = Color.white;
        }
    }
}
