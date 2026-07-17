using FracturedChorus.Combat.Cover;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class CoverEnergyGaugeView : MonoBehaviour
    {
        private const string DefaultPipResourcePath = "UI/Combat/Cover/cover_energy_pip_hologram_v1";
        private const string DefaultFrameResourcePath = "UI/Combat/Cover/cover_energy_gauge_frame_hologram_v1";

        [SerializeField] private bool preserveSceneLayout = true;
        [SerializeField] private Sprite pipSprite;
        [SerializeField] private Sprite frameSprite;
        [SerializeField] private Image frameImage;
        [SerializeField] private RectTransform pipsRoot;
        [SerializeField] private Color pipOnColor = Color.white;
        [SerializeField] private Color pipOffColor = new Color(1f, 1f, 1f, 0.28f);
        [SerializeField] private float disabledAlpha = 0.45f;

        private readonly Image[] _pips = new Image[CoverConstants.GaugeCap];
        private int _displayed;
        private CanvasGroup _canvasGroup;
        private bool _wired;

        public static CoverEnergyGaugeView EnsureOn(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find("CoverEnergyGauge")?.GetComponent<CoverEnergyGaugeView>();
            if (existing != null)
            {
                existing.EnsureBuilt();
                return existing;
            }

            // Fallback when scene lost CoverEnergyGauge (e.g. merge kept local timeline polish).
            // Prefer Hierarchy authoring via Fractured Chorus → Setup Cover HUD.
            var go = new GameObject("CoverEnergyGauge", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(8f, 0f);
            rt.sizeDelta = new Vector2(72f, 0f);

            var view = go.AddComponent<CoverEnergyGaugeView>();
            view.preserveSceneLayout = true;
            view.BuildRuntimeFallbackHierarchy();
            view.EnsureBuilt();
            return view;
        }

        /// <summary>Minimal Frame + Pips when scene object was not authored.</summary>
        private void BuildRuntimeFallbackHierarchy()
        {
            ResolveSprites();

            if (transform.Find("Frame") == null)
            {
                var frameGo = new GameObject("Frame", typeof(RectTransform));
                var frameRt = frameGo.GetComponent<RectTransform>();
                frameRt.SetParent(transform, false);
                frameRt.anchorMin = Vector2.zero;
                frameRt.anchorMax = Vector2.one;
                frameRt.offsetMin = Vector2.zero;
                frameRt.offsetMax = Vector2.zero;
                var img = frameGo.AddComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
                if (frameSprite != null)
                {
                    img.sprite = frameSprite;
                    img.color = Color.white;
                }
                else
                {
                    img.color = new Color(0.15f, 0.18f, 0.24f, 0.85f);
                }

                frameImage = img;
            }

            if (transform.Find("Pips") == null)
            {
                var pipsGo = new GameObject("Pips", typeof(RectTransform));
                pipsRoot = pipsGo.GetComponent<RectTransform>();
                pipsRoot.SetParent(transform, false);
                pipsRoot.anchorMin = Vector2.zero;
                pipsRoot.anchorMax = Vector2.one;
                pipsRoot.offsetMin = new Vector2(8f, 8f);
                pipsRoot.offsetMax = new Vector2(-8f, -8f);

                const float pipSize = 10f;
                const float gap = 4f;
                var total = CoverConstants.GaugeCap * pipSize + (CoverConstants.GaugeCap - 1) * gap;
                var startY = total * 0.5f - pipSize * 0.5f;
                for (var i = 0; i < CoverConstants.GaugeCap; i++)
                {
                    var pipGo = new GameObject($"Pip_{i}", typeof(RectTransform));
                    var pipRt = pipGo.GetComponent<RectTransform>();
                    pipRt.SetParent(pipsRoot, false);
                    pipRt.anchorMin = new Vector2(0.5f, 0.5f);
                    pipRt.anchorMax = new Vector2(0.5f, 0.5f);
                    pipRt.pivot = new Vector2(0.5f, 0.5f);
                    pipRt.sizeDelta = new Vector2(pipSize, pipSize);
                    pipRt.anchoredPosition = new Vector2(0f, startY - i * (pipSize + gap));
                    var pipImg = pipGo.AddComponent<Image>();
                    pipImg.raycastTarget = false;
                    pipImg.preserveAspect = true;
                    if (pipSprite != null)
                    {
                        pipImg.sprite = pipSprite;
                    }

                    pipImg.color = pipOffColor;
                }
            }
        }

        public void EnsureBuilt()
        {
            if (_wired)
            {
                BindPipImages();
                return;
            }

            ResolveSprites();
            BindFrame();
            BindPipsRoot();
            BindPipImages();
            _wired = true;
            ApplyVisual(_displayed);
        }

        public void SetGauge(int gauge)
        {
            EnsureBuilt();
            _displayed = Mathf.Clamp(gauge, 0, CoverConstants.GaugeCap);
            ApplyVisual(_displayed);
        }

        public void SetInteractableVisual(bool canPress)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _canvasGroup.alpha = canPress ? 1f : disabledAlpha;
        }

        public RectTransform EnsurePipsRoot()
        {
            BindPipsRoot();
            return pipsRoot;
        }

        public int CreateHandEditPips(bool resetLayout)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return EditorCreateOrFillPips(resetLayout);
            }
#endif
            Debug.LogWarning("[CoverEnergyGauge] CreateHandEditPips is editor-only. Scene pips are authoritative at runtime.");
            BindPipImages();
            return 0;
        }

        public void RelayoutPipsFromPip0()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorRelayoutFromPip0AndPip1();
                return;
            }
#endif
            Debug.LogWarning("[CoverEnergyGauge] Relayout is editor-only. Runtime keeps scene pip positions.");
        }

        private void ResolveSprites()
        {
            if (pipSprite == null)
            {
                pipSprite = Resources.Load<Sprite>(DefaultPipResourcePath);
            }

            if (frameSprite == null)
            {
                frameSprite = Resources.Load<Sprite>(DefaultFrameResourcePath);
            }

#if UNITY_EDITOR
            if (pipSprite == null)
            {
                pipSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/FracturedChorus/Art/UI/Combat/Cover/cover_energy_pip_hologram_v1.png");
            }

            if (frameSprite == null)
            {
                frameSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/FracturedChorus/Art/UI/Combat/Cover/cover_energy_gauge_frame_hologram_v1.png");
            }
#endif
        }

        private void BindFrame()
        {
            var frameT = transform.Find("Frame");
            if (frameT == null)
            {
                Debug.LogWarning("[CoverEnergyGauge] Frame missing in scene — not creating (preserve hand layout).");
                return;
            }

            if (frameImage == null)
            {
                frameImage = frameT.GetComponent<Image>();
            }

            if (frameImage == null)
            {
                return;
            }

            if (frameSprite != null)
            {
                frameImage.sprite = frameSprite;
                frameImage.type = Image.Type.Simple;
                frameImage.preserveAspect = true;
                frameImage.color = Color.white;
            }

            frameImage.raycastTarget = false;
        }

        private void BindPipsRoot()
        {
            if (pipsRoot == null)
            {
                pipsRoot = transform.Find("Pips") as RectTransform;
            }

            if (pipsRoot == null)
            {
                Debug.LogWarning("[CoverEnergyGauge] Pips root missing in scene — not creating.");
            }
        }

        private void BindPipImages()
        {
            if (pipsRoot == null)
            {
                BindPipsRoot();
            }

            if (pipsRoot == null)
            {
                return;
            }

            for (var i = 0; i < _pips.Length; i++)
            {
                var pipT = pipsRoot.Find($"Pip_{i}");
                if (pipT == null)
                {
                    if (_pips[i] == null)
                    {
                        Debug.LogWarning($"[CoverEnergyGauge] Pip_{i} missing in scene — skip (no runtime spawn).");
                    }

                    _pips[i] = null;
                    continue;
                }

                var img = pipT.GetComponent<Image>();
                if (img == null)
                {
                    _pips[i] = null;
                    continue;
                }

                if (pipSprite != null && img.sprite != pipSprite)
                {
                    img.sprite = pipSprite;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                }

                img.raycastTarget = false;
                img.enabled = true;
                _pips[i] = img;
            }
        }

        private void ApplyVisual(int gauge)
        {
            for (var i = 0; i < _pips.Length; i++)
            {
                var pip = _pips[i];
                if (pip == null)
                {
                    continue;
                }

                pip.color = i < gauge ? pipOnColor : pipOffColor;
            }
        }

#if UNITY_EDITOR
        private int EditorCreateOrFillPips(bool resetLayout)
        {
            preserveSceneLayout = true;
            ResolveSprites();
            BindPipsRoot();
            if (pipsRoot == null)
            {
                var go = new GameObject("Pips", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                pipsRoot = go.GetComponent<RectTransform>();
                pipsRoot.anchorMin = Vector2.zero;
                pipsRoot.anchorMax = Vector2.one;
                pipsRoot.offsetMin = Vector2.zero;
                pipsRoot.offsetMax = Vector2.zero;
            }

            var ref0 = pipsRoot.Find("Pip_0") as RectTransform;
            var ref1 = pipsRoot.Find("Pip_1") as RectTransform;
            if (ref0 == null)
            {
                Debug.LogError("[CoverEnergyGauge] Place Pip_0 in scene first, then run Create/Relayout.");
                return 0;
            }

            var size = ref0.sizeDelta;
            var x = ref0.anchoredPosition.x;
            var y0 = ref0.anchoredPosition.y;
            var step = ref1 != null
                ? ref1.anchoredPosition.y - y0
                : Mathf.Max(12f, size.y * 0.3f);

            var created = 0;
            for (var i = 0; i < CoverConstants.GaugeCap; i++)
            {
                var pipRt = pipsRoot.Find($"Pip_{i}") as RectTransform;
                var isNew = pipRt == null;
                if (isNew)
                {
                    var pipGo = new GameObject($"Pip_{i}", typeof(RectTransform), typeof(CanvasRenderer),
                        typeof(Image));
                    pipRt = pipGo.GetComponent<RectTransform>();
                    pipRt.SetParent(pipsRoot, false);
                    created++;
                }

                var img = pipRt.GetComponent<Image>();
                img.raycastTarget = false;
                if (pipSprite != null)
                {
                    img.sprite = pipSprite;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                }

                img.color = Color.white;
                pipRt.SetSiblingIndex(i);
                _pips[i] = img;

                if (i <= 1)
                {
                    continue;
                }

                if (resetLayout || isNew)
                {
                    pipRt.anchorMin = new Vector2(0.5f, 0.5f);
                    pipRt.anchorMax = new Vector2(0.5f, 0.5f);
                    pipRt.pivot = new Vector2(0.5f, 0.5f);
                    pipRt.sizeDelta = size;
                    pipRt.anchoredPosition = new Vector2(x, y0 + i * step);
                }
            }

            _wired = false;
            BindPipImages();
            ApplyVisual(CoverConstants.GaugeCap);
            return created;
        }

        private void EditorRelayoutFromPip0AndPip1()
        {
            BindPipsRoot();
            var ref0 = pipsRoot != null ? pipsRoot.Find("Pip_0") as RectTransform : null;
            var ref1 = pipsRoot != null ? pipsRoot.Find("Pip_1") as RectTransform : null;
            if (ref0 == null || ref1 == null)
            {
                Debug.LogError("[CoverEnergyGauge] Need Pip_0 and Pip_1 placed before relayout.");
                return;
            }

            var size = ref0.sizeDelta;
            var x = ref0.anchoredPosition.x;
            var y0 = ref0.anchoredPosition.y;
            var step = ref1.anchoredPosition.y - y0;

            for (var i = 0; i < CoverConstants.GaugeCap; i++)
            {
                var pipRt = pipsRoot.Find($"Pip_{i}") as RectTransform;
                if (pipRt == null)
                {
                    var pipGo = new GameObject($"Pip_{i}", typeof(RectTransform), typeof(CanvasRenderer),
                        typeof(Image));
                    pipRt = pipGo.GetComponent<RectTransform>();
                    pipRt.SetParent(pipsRoot, false);
                    var imgNew = pipGo.GetComponent<Image>();
                    imgNew.raycastTarget = false;
                    if (pipSprite != null)
                    {
                        imgNew.sprite = pipSprite;
                        imgNew.type = Image.Type.Simple;
                        imgNew.preserveAspect = true;
                    }
                }

                if (i == 0 || i == 1)
                {
                    continue;
                }

                pipRt.anchorMin = new Vector2(0.5f, 0.5f);
                pipRt.anchorMax = new Vector2(0.5f, 0.5f);
                pipRt.pivot = new Vector2(0.5f, 0.5f);
                pipRt.sizeDelta = size;
                pipRt.anchoredPosition = new Vector2(x, y0 + i * step);
            }

            _wired = false;
            BindPipImages();
            ApplyVisual(_displayed);
        }
#endif
    }
}
