using FracturedChorus.Combat.Cover;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    /// <summary>
    /// Cover energy gauge: scene chỉ cần 1 pip template (Pip_0 / PipTemplate).
    /// Khi vào Play (hoặc EnsureBuilt), clone đủ <see cref="CoverConstants.GaugeCap"/> pips.
    /// </summary>
    public class CoverEnergyGaugeView : MonoBehaviour
    {
        private const string DefaultPipResourcePath = "UI/Combat/Cover/cover_energy_pip_hologram_v1";
        private const string DefaultFrameResourcePath = "UI/Combat/Cover/cover_energy_gauge_frame_hologram_v1";
        private const string TemplateName = "PipTemplate";
        private const string PipNamePrefix = "Pip_";

        [SerializeField] private bool preserveSceneLayout = true;
        [SerializeField] private Sprite pipSprite;
        [SerializeField] private Sprite frameSprite;
        [SerializeField] private Image frameImage;
        [SerializeField] private RectTransform pipsRoot;
        [Tooltip("Pip mẫu trên scene (để trống = Pip_0 / PipTemplate / Image con đầu).")]
        [SerializeField] private RectTransform pipTemplate;
        [Tooltip("Khoảng cách Y giữa các pip (Pip_0 → Pip_1). Dương = đi lên. 0 = auto từ size template.")]
        [SerializeField] private float pipStepY;
        [SerializeField] private Color pipOnColor = Color.white;
        [SerializeField] private Color pipOffColor = new Color(1f, 1f, 1f, 0.28f);
        [SerializeField] private float disabledAlpha = 0.45f;

        private readonly Image[] _pips = new Image[CoverConstants.GaugeCap];
        private int _displayed;
        private CanvasGroup _canvasGroup;
        private bool _wired;
        private bool _spawned;

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
            }

            BindPipsRoot();
            EnsureTemplatePip();
        }

        public void EnsureBuilt()
        {
            if (_wired && _spawned)
            {
                ApplyVisual(_displayed);
                return;
            }

            ResolveSprites();
            BindFrame();
            BindPipsRoot();
            SpawnPipsFromTemplate();
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

        /// <summary>Editor: giữ 1 pip template trên scene (xóa Pip_1..n).</summary>
        public int StripToTemplatePip()
        {
            BindPipsRoot();
            var template = ResolveTemplatePip(createIfMissing: true);
            if (template == null || pipsRoot == null)
            {
                return 0;
            }

            EnsureTemplateIdentity(template);
            var removed = 0;
            for (var i = pipsRoot.childCount - 1; i >= 0; i--)
            {
                var child = pipsRoot.GetChild(i);
                if (child == template)
                {
                    continue;
                }

                DestroyImmediate(child.gameObject);
                removed++;
            }

            pipTemplate = template;
            _spawned = false;
            _wired = false;
            for (var i = 0; i < _pips.Length; i++)
            {
                _pips[i] = null;
            }

            return removed;
        }

        public int CreateHandEditPips(bool resetLayout)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return EditorPreviewSpawnPips(resetLayout);
            }
#endif
            SpawnPipsFromTemplate();
            return CoverConstants.GaugeCap;
        }

        public void RelayoutPipsFromPip0()
        {
            if (!_spawned)
            {
                SpawnPipsFromTemplate();
                return;
            }

            var template = _pips[0] != null ? _pips[0].rectTransform : ResolveTemplatePip(false);
            if (template == null)
            {
                return;
            }

            var step = ResolveStepY(template);
            var origin = template.anchoredPosition;
            var size = template.sizeDelta;
            for (var i = 0; i < _pips.Length; i++)
            {
                if (_pips[i] == null)
                {
                    continue;
                }

                var rt = _pips[i].rectTransform;
                rt.anchorMin = template.anchorMin;
                rt.anchorMax = template.anchorMax;
                rt.pivot = template.pivot;
                rt.sizeDelta = size;
                rt.localScale = template.localScale;
                rt.anchoredPosition = new Vector2(origin.x, origin.y + i * step);
            }
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
                if (preserveSceneLayout)
                {
                    Debug.LogWarning("[CoverEnergyGauge] Frame missing in scene — not creating (preserve hand layout).");
                    return;
                }

                BuildRuntimeFallbackHierarchy();
                frameT = transform.Find("Frame");
                if (frameT == null)
                {
                    return;
                }
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
                if (preserveSceneLayout)
                {
                    Debug.LogWarning("[CoverEnergyGauge] Pips root missing in scene — not creating.");
                    return;
                }

                BuildRuntimeFallbackHierarchy();
                pipsRoot = transform.Find("Pips") as RectTransform;
            }
        }

        private RectTransform ResolveTemplatePip(bool createIfMissing)
        {
            if (pipTemplate != null)
            {
                return pipTemplate;
            }

            if (pipsRoot == null)
            {
                return null;
            }

            var named = pipsRoot.Find(TemplateName) as RectTransform
                        ?? pipsRoot.Find($"{PipNamePrefix}0") as RectTransform;
            if (named != null)
            {
                pipTemplate = named;
                return named;
            }

            for (var i = 0; i < pipsRoot.childCount; i++)
            {
                var child = pipsRoot.GetChild(i) as RectTransform;
                if (child == null || child.GetComponent<Image>() == null)
                {
                    continue;
                }

                pipTemplate = child;
                return child;
            }

            if (!createIfMissing)
            {
                return null;
            }

            return EnsureTemplatePip();
        }

        private RectTransform EnsureTemplatePip()
        {
            BindPipsRoot();
            if (pipsRoot == null)
            {
                return null;
            }

            var existing = ResolveTemplatePip(createIfMissing: false);
            if (existing != null)
            {
                EnsureTemplateIdentity(existing);
                return existing;
            }

            ResolveSprites();
            var go = new GameObject($"{PipNamePrefix}0", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(pipsRoot, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(46f, 47f);
            rt.anchoredPosition = new Vector2(0.1f, -141.2f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            if (pipSprite != null)
            {
                img.sprite = pipSprite;
            }

            img.color = pipOffColor;
            pipTemplate = rt;
            return rt;
        }

        private static void EnsureTemplateIdentity(RectTransform template)
        {
            if (template != null && template.name != $"{PipNamePrefix}0" && template.name != TemplateName)
            {
                template.name = $"{PipNamePrefix}0";
            }
            else if (template != null && template.name == TemplateName)
            {
                template.name = $"{PipNamePrefix}0";
            }
        }

        private float ResolveStepY(RectTransform template)
        {
            if (Mathf.Abs(pipStepY) > 0.01f)
            {
                return pipStepY;
            }

            if (pipsRoot != null)
            {
                var pip1 = pipsRoot.Find($"{PipNamePrefix}1") as RectTransform;
                if (pip1 != null && pip1 != template)
                {
                    var measured = pip1.anchoredPosition.y - template.anchoredPosition.y;
                    if (Mathf.Abs(measured) > 0.01f)
                    {
                        return measured;
                    }
                }
            }

            // Scene CombatPrototype hiện ~13.8 giữa các pip (Pip_0 đáy → lên).
            var fromSize = Mathf.Max(12f, template.sizeDelta.y * 0.29f);
            return fromSize;
        }

        private void SpawnPipsFromTemplate()
        {
            if (_spawned)
            {
                return;
            }

            BindPipsRoot();
            var template = ResolveTemplatePip(createIfMissing: true);
            if (template == null || pipsRoot == null)
            {
                Debug.LogWarning("[CoverEnergyGauge] No pip template — cannot spawn gauge pips.");
                return;
            }

            EnsureTemplateIdentity(template);
            var step = ResolveStepY(template);
            if (Mathf.Abs(pipStepY) <= 0.01f)
            {
                pipStepY = step;
            }

            var origin = template.anchoredPosition;
            var size = template.sizeDelta;
            var anchorMin = template.anchorMin;
            var anchorMax = template.anchorMax;
            var pivot = template.pivot;
            var scale = template.localScale;
            var templateImg = template.GetComponent<Image>();

            // Xóa pip cũ (trừ template) rồi clone đủ GaugeCap.
            for (var i = pipsRoot.childCount - 1; i >= 0; i--)
            {
                var child = pipsRoot.GetChild(i);
                if (child == template)
                {
                    continue;
                }

                DestroyImmediate(child.gameObject);
            }

            for (var i = 0; i < CoverConstants.GaugeCap; i++)
            {
                RectTransform pipRt;
                Image img;
                if (i == 0)
                {
                    pipRt = template;
                    img = templateImg;
                }
                else
                {
                    var clone = Instantiate(template.gameObject, pipsRoot);
                    clone.name = $"{PipNamePrefix}{i}";
                    pipRt = clone.GetComponent<RectTransform>();
                    img = clone.GetComponent<Image>();
                }

                pipRt.name = $"{PipNamePrefix}{i}";
                pipRt.SetSiblingIndex(i);
                pipRt.anchorMin = anchorMin;
                pipRt.anchorMax = anchorMax;
                pipRt.pivot = pivot;
                pipRt.localScale = scale;
                pipRt.sizeDelta = size;
                pipRt.anchoredPosition = new Vector2(origin.x, origin.y + i * step);

                if (img == null)
                {
                    img = pipRt.gameObject.AddComponent<Image>();
                }

                if (pipSprite != null)
                {
                    img.sprite = pipSprite;
                    img.type = Image.Type.Simple;
                    img.preserveAspect = true;
                }

                img.raycastTarget = false;
                img.enabled = true;
                _pips[i] = img;
            }

            pipTemplate = template;
            _spawned = true;
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
        private int EditorPreviewSpawnPips(bool resetLayout)
        {
            preserveSceneLayout = true;
            ResolveSprites();
            BindPipsRoot();
            EnsureTemplatePip();
            _spawned = false;
            SpawnPipsFromTemplate();
            if (resetLayout)
            {
                RelayoutPipsFromPip0();
            }

            ApplyVisual(CoverConstants.GaugeCap);
            return CoverConstants.GaugeCap;
        }
#endif
    }
}
