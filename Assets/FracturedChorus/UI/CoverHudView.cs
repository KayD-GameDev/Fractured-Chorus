using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Cover;
using UnityEngine;
using UnityEngine.UI;
using FracturedChorus.UI;

namespace FracturedChorus.UI
{
    public class CoverHudView : MonoBehaviour
    {
        private const string DefaultButtonSpriteResourcePath = "UI/Combat/combat_btn_cover_v1";

        [Header("Scene edit — kéo RectTransform / gán sprite tại đây")]
        [SerializeField] private bool preserveSceneLayout = true;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private bool hideLabelWhenSpriteAssigned = true;
        [SerializeField] private float disabledSpriteAlpha = 0.45f;

        [Header("Wired children (auto nếu trống)")]
        [SerializeField] private RectTransform rootRect;
        [SerializeField] private Button coverButton;
        [SerializeField] private Image coverButtonImage;
        [SerializeField] private Text coverButtonLabel;
        [SerializeField] private CoverEnergyGaugeView energyGauge;
        [SerializeField] private Text statusLabel;

        [Header("Defaults khi tạo mới (preserveSceneLayout = off mới ghi đè)")]
        [SerializeField] private Vector2 defaultAnchoredPosition = new Vector2(-16f, -16f);
        [SerializeField] private Vector2 defaultSizeDelta = new Vector2(240f, 220f);

        private CombatSession _session;

        public Button CoverButton => coverButton;
        public Image CoverButtonImage => coverButtonImage;
        public Sprite ButtonSprite => buttonSprite;
        public CoverEnergyGaugeView EnergyGauge => energyGauge;

        private void Awake()
        {
            if (!UiEnabled)
            {
                HideVisuals();
                return;
            }

            EnsureBuilt();
        }

        public static bool UiEnabled { get; set; }

        public static void HideAll()
        {
            UiEnabled = false;
            var huds = Object.FindObjectsByType<CoverHudView>(FindObjectsInactive.Include);
            for (var i = 0; i < huds.Length; i++)
            {
                huds[i]?.HideVisuals();
            }

            var gauges = Object.FindObjectsByType<CoverEnergyGaugeView>(FindObjectsInactive.Include);
            for (var i = 0; i < gauges.Length; i++)
            {
                if (gauges[i] != null)
                {
                    gauges[i].gameObject.SetActive(false);
                }
            }
        }

        public void HideVisuals()
        {
            gameObject.SetActive(false);
        }

        public static CoverHudView EnsureOn(RectTransform parent)
        {
            if (!UiEnabled)
            {
                HideAll();
                return null;
            }

            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find("CoverHud")?.GetComponent<CoverHudView>();
            if (existing == null)
            {
                existing = Object.FindAnyObjectByType<CoverHudView>();
            }

            if (existing != null)
            {
                existing.AttachToParentIfNeeded(parent);
                existing.EnsureBuilt();
                existing.ApplyButtonVisual();
                return existing;
            }

            var go = new GameObject("CoverHud", typeof(RectTransform));
            var view = go.AddComponent<CoverHudView>();
            view.preserveSceneLayout = false;
            view.AttachToParentIfNeeded(parent);
            view.ApplyDefaultRootLayout();
            view.EnsureBuilt();
            view.preserveSceneLayout = true;
            view.ApplyButtonVisual();
            return view;
        }

        private void AttachToParentIfNeeded(RectTransform canvasParent)
        {
            var rt = transform as RectTransform;
            if (rt == null || canvasParent == null)
            {
                return;
            }

            if (rt.parent != canvasParent)
            {
                rt.SetParent(canvasParent, false);
            }

            rt.SetAsLastSibling();
            rootRect = rt;
        }

        private void ApplyDefaultRootLayout()
        {
            var rt = transform as RectTransform;
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = defaultAnchoredPosition;
            rt.sizeDelta = defaultSizeDelta;
        }

        public void EnsureBuilt()
        {
            if (!UiEnabled)
            {
                HideVisuals();
                return;
            }

            rootRect = transform as RectTransform;
            if (rootRect == null)
            {
                return;
            }

            if (!preserveSceneLayout)
            {
                ApplyDefaultRootLayout();
            }

            WireOrCreateButton();
            WireOrCreateEnergyGauge();
            WireOrCreateStatusLabel();
            DestroyLegacyChrome();
            ResolveSerializedRefs();
            ApplyButtonVisual();
        }

        private void DestroyLegacyChrome()
        {
            if (rootRect == null)
            {
                return;
            }

            DestroyChildIfPresent(rootRect, "GaugeBar");
            DestroyChildIfPresent(rootRect, "GaugeLabel");
            DestroyChildIfPresent(rootRect, "EnergyLabel");
            if (coverButton != null)
            {
                DestroyChildIfPresent(coverButton.transform, "EnergyLabel");
            }
        }

        private static void DestroyChildIfPresent(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(child.gameObject);
            }
            else
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private void WireOrCreateStatusLabel()
        {
            if (statusLabel != null)
            {
                return;
            }

            statusLabel = EnsureText(rootRect, "StatusLabel", new Vector2(0f, 0.86f), new Vector2(1f, 1f), 11);
        }

        private void WireOrCreateButton()
        {
            var btnT = rootRect.Find("CoverButton");
            if (btnT == null)
            {
                var btnGo = new GameObject("CoverButton", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(Button));
                btnT = btnGo.transform;
                btnT.SetParent(rootRect, false);
                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0f, 0f);
                btnRt.anchorMax = new Vector2(0.68f, 1f);
                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;
                coverButton = btnGo.GetComponent<Button>();
                coverButtonImage = btnGo.GetComponent<Image>();
            }
            else
            {
                if (coverButton == null)
                {
                    coverButton = btnT.GetComponent<Button>();
                }

                if (coverButtonImage == null)
                {
                    coverButtonImage = btnT.GetComponent<Image>();
                }

                if (!preserveSceneLayout)
                {
                    var btnRt = btnT as RectTransform;
                    if (btnRt != null)
                    {
                        btnRt.anchorMin = new Vector2(0f, 0f);
                        btnRt.anchorMax = new Vector2(0.68f, 1f);
                        btnRt.offsetMin = Vector2.zero;
                        btnRt.offsetMax = Vector2.zero;
                    }
                }
            }

            if (coverButton != null)
            {
                coverButton.onClick.RemoveListener(OnCoverClicked);
                coverButton.onClick.AddListener(OnCoverClicked);
            }

            if (coverButtonLabel == null)
            {
                coverButtonLabel = EnsureText(btnT as RectTransform, "Label", Vector2.zero, Vector2.one, 14);
                if (coverButtonLabel != null)
                {
                    coverButtonLabel.fontStyle = FontStyle.Bold;
                    coverButtonLabel.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        private void WireOrCreateEnergyGauge()
        {
            if (energyGauge == null)
            {
                energyGauge = transform.Find("CoverEnergyGauge")?.GetComponent<CoverEnergyGaugeView>();
            }

            if (energyGauge == null)
            {
                energyGauge = CoverEnergyGaugeView.EnsureOn(rootRect);
            }

            energyGauge?.EnsureBuilt();
        }

        private void ResolveSerializedRefs()
        {
            if (coverButton == null)
            {
                coverButton = transform.Find("CoverButton")?.GetComponent<Button>();
            }

            if (coverButtonImage == null && coverButton != null)
            {
                coverButtonImage = coverButton.GetComponent<Image>();
            }

            if (energyGauge == null)
            {
                energyGauge = transform.Find("CoverEnergyGauge")?.GetComponent<CoverEnergyGaugeView>();
            }

            if (statusLabel == null)
            {
                statusLabel = transform.Find("StatusLabel")?.GetComponent<Text>();
            }

            if (coverButtonLabel == null)
            {
                coverButtonLabel = transform.Find("CoverButton/Label")?.GetComponent<Text>();
            }
        }

        public void ApplyButtonVisual()
        {
            ResolveSerializedRefs();
            if (coverButtonImage == null)
            {
                return;
            }

            var sprite = buttonSprite;
            if (sprite == null)
            {
                try
                {
                    sprite = Resources.Load<Sprite>(DefaultButtonSpriteResourcePath);
                    if (sprite != null && buttonSprite == null)
                    {
                        buttonSprite = sprite;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[CoverHud] Failed to load default cover sprite: " + e);
                }
            }

            if (sprite != null)
            {
                coverButtonImage.sprite = sprite;
                coverButtonImage.type = Image.Type.Simple;
                coverButtonImage.preserveAspect = true;
                coverButtonImage.color = Color.white;
                if (hideLabelWhenSpriteAssigned && coverButtonLabel != null)
                {
                    coverButtonLabel.text = string.Empty;
                }
            }
            else
            {
                coverButtonImage.sprite = null;
                coverButtonImage.color = new Color(0.35f, 0.4f, 0.55f, 0.95f);
                if (coverButtonLabel != null)
                {
                    coverButtonLabel.text = "COVER";
                    coverButtonLabel.alignment = TextAnchor.MiddleCenter;
                    coverButtonLabel.fontStyle = FontStyle.Bold;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ResolveSerializedRefs();
            ApplyButtonVisual();
        }
#endif

        public void Bind(CombatSession session)
        {
            if (_session != null)
            {
                _session.OnPhaseChanged -= OnPhaseChanged;
                if (_session.Cover != null)
                {
                    _session.Cover.OnChanged -= Refresh;
                }
            }

            _session = session;
            EnsureBuilt();

            if (_session != null)
            {
                _session.OnPhaseChanged += OnPhaseChanged;
                _session.Cover.OnChanged += Refresh;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_session == null)
            {
                return;
            }

            _session.OnPhaseChanged -= OnPhaseChanged;
            if (_session.Cover != null)
            {
                _session.Cover.OnChanged -= Refresh;
            }
        }

        private void OnPhaseChanged(CombatPhase _) => Refresh();

        private void OnCoverClicked()
        {
            if (_session?.Cover == null)
            {
                return;
            }

            var renAlive = IsRenAlive(_session);
            if (!_session.AllowCoverActivate)
            {
                Debug.Log("[Cover] Button ignored — not in planning stop (AllowCoverActivate=false).");
                return;
            }

            if (!_session.Cover.TryActivate(renAlive))
            {
                Debug.Log(
                    $"[Cover] Cannot activate — gauge {_session.Cover.Gauge}/{CoverConstants.ActivateCost}" +
                    $" pending={_session.Cover.IsPending} active={_session.Cover.IsActive} renAlive={renAlive}");
                return;
            }

            Refresh();
        }

        public void Refresh()
        {
            EnsureBuilt();
            ApplyButtonVisual();
            if (_session?.Cover == null)
            {
                return;
            }

            var cover = _session.Cover;
            energyGauge?.SetGauge(cover.Gauge);

            if (statusLabel != null)
            {
                if (cover.IsActive)
                {
                    statusLabel.text = $"ACTIVE {cover.ActiveBeatsRemaining}";
                }
                else if (cover.IsPending)
                {
                    statusLabel.text = "PENDING";
                }
                else
                {
                    statusLabel.text = string.Empty;
                }

                statusLabel.alignment = TextAnchor.MiddleCenter;
            }

            if (coverButton != null)
            {
                var canPress = _session.AllowCoverActivate &&
                               cover.CanActivate(IsRenAlive(_session));
                coverButton.interactable = canPress;
                var cg = coverButton.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = coverButton.gameObject.AddComponent<CanvasGroup>();
                }

                cg.blocksRaycasts = true;
                cg.interactable = canPress;
                if (coverButtonImage != null && coverButtonImage.sprite != null)
                {
                    var c = coverButtonImage.color;
                    c.a = canPress ? 1f : disabledSpriteAlpha;
                    coverButtonImage.color = c;
                }
                else if (coverButtonLabel != null)
                {
                    coverButtonLabel.color = canPress ? Color.white : new Color(1f, 1f, 1f, disabledSpriteAlpha);
                }

                energyGauge?.SetInteractableVisual(canPress);
            }
        }

        private static bool IsRenAlive(CombatSession session)
        {
            if (session?.Grid == null)
            {
                return false;
            }

            foreach (var u in session.Grid.PlayerUnits)
            {
                if (u != null &&
                    u.IsAlive &&
                    string.Equals(u.DisplayName, CoverConstants.RenDisplayName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Text EnsureText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            int fontSize)
        {
            if (parent == null)
            {
                return null;
            }

            var t = parent.Find(name);
            Text text;
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                t = go.transform;
                t.SetParent(parent, false);
                text = go.GetComponent<Text>();
                text.font = UiFontCatalog.Body;
                if (text.font == null)
                {
                    text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                text.color = Color.white;
                text.raycastTarget = false;
            }
            else
            {
                text = t.GetComponent<Text>();
            }

            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            text.fontSize = fontSize;
            return text;
        }
    }
}
