using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.Hub
{
    public sealed class SocialStatsOverlayUI : MonoBehaviour
    {
        public readonly struct BuildResult
        {
            public BuildResult(SocialStatsOverlayUI overlay)
            {
                Overlay = overlay;
            }

            public SocialStatsOverlayUI Overlay { get; }
        }

        private static readonly Color Cyan = new Color(0f, 0.831f, 1f, 1f);
        private static readonly Color NavyDim = new Color(0.02f, 0.04f, 0.12f, 0.75f);
        private static readonly Color WatermarkColor = new Color(0.7f, 0.95f, 1f, 0.06f);

        private static readonly Vector2[] NodeOffsets =
        {
            new Vector2(-400f, 20f),
            new Vector2(-230f, 190f),
            new Vector2(0f, 250f),
            new Vector2(230f, 190f),
            new Vector2(400f, 20f)
        };

        private static readonly string[] NodeNames =
        {
            "Node_Resonance",
            "Node_Cadence",
            "Node_Pulse",
            "Node_Harmony",
            "Node_Rhythm"
        };

        [SerializeField] private GameObject root;
        [SerializeField] private SocialStatsRadarGraphic radar;
        [SerializeField] private SocialStatsNodeView[] nodes = new SocialStatsNodeView[5];
        [SerializeField] private Image heroBust;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text subtitleLabel;
        [SerializeField] private Text watermarkLabel;
        [SerializeField] private Text footerLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private TownMapSfxController sfx;

        private GameMetaState _state;
        private bool _wired;

        public bool IsOpen => root != null && root.activeSelf;

        public void BindSfx(TownMapSfxController controller)
        {
            sfx = controller;
        }

        public void Show(GameMetaState state)
        {
            _state = state;
            EnsureRuntimeBindings();
            Wire();
            if (root != null)
            {
                root.SetActive(true);
            }

            sfx?.PlayOpenPanel();
            Refresh();
        }

        public void Hide()
        {
            if (IsOpen)
            {
                sfx?.PlayClosePanel();
            }

            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (TownMapInput.CancelPressed())
            {
                Hide();
            }
        }

        public static BuildResult Build(Transform parent)
        {
            var existing = parent.Find("SocialStatsOverlay");
            if (existing != null)
            {
                var overlay = existing.GetComponent<SocialStatsOverlayUI>()
                              ?? existing.gameObject.AddComponent<SocialStatsOverlayUI>();
                overlay.EnsureRuntimeBindings();
                overlay.Rewire();
                return new BuildResult(overlay);
            }

            return new BuildResult(CreateHierarchy(parent));
        }

        public void EnsureRuntimeBindings()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (radar == null)
            {
                var radarTf = FindTransform(transform, "ChartRoot/Radar", "Radar");
                if (radarTf != null)
                {
                    radar = radarTf.GetComponent<SocialStatsRadarGraphic>();
                }
            }

            if (heroBust == null)
            {
                var heroTf = FindTransform(transform, "HeroBust");
                if (heroTf != null)
                {
                    heroBust = heroTf.GetComponent<Image>();
                }
            }

            if (titleLabel == null)
            {
                var titleTf = FindTransform(transform, "TitleBlock/Title", "Title");
                if (titleTf != null)
                {
                    titleLabel = titleTf.GetComponent<Text>();
                }
            }

            if (subtitleLabel == null)
            {
                var subtitleTf = FindTransform(transform, "TitleBlock/Subtitle", "Subtitle");
                if (subtitleTf != null)
                {
                    subtitleLabel = subtitleTf.GetComponent<Text>();
                }
            }

            if (watermarkLabel == null)
            {
                var watermarkTf = FindTransform(transform, "Watermark");
                if (watermarkTf != null)
                {
                    watermarkLabel = watermarkTf.GetComponent<Text>();
                }
            }

            if (footerLabel == null)
            {
                var footerTf = FindTransform(transform, "FooterEsc");
                if (footerTf != null)
                {
                    footerLabel = footerTf.GetComponent<Text>();
                }
            }

            if (closeButton == null)
            {
                closeButton = FindButton(transform, "CloseButton");
            }

            if (nodes == null || nodes.Length != 5)
            {
                nodes = new SocialStatsNodeView[5];
            }

            for (var i = 0; i < 5; i++)
            {
                if (nodes[i] != null)
                {
                    continue;
                }

                var nodeTf = FindTransform(transform, $"NodesRoot/{NodeNames[i]}", NodeNames[i]);
                if (nodeTf != null)
                {
                    nodes[i] = nodeTf.GetComponent<SocialStatsNodeView>()
                               ?? nodeTf.gameObject.AddComponent<SocialStatsNodeView>();
                }
            }
        }

        public void Rewire()
        {
            _wired = false;
            Wire();
        }

        private void Wire()
        {
            if (_wired)
            {
                return;
            }

            if (root == null)
            {
                root = gameObject;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            _wired = true;
        }

        private void Refresh()
        {
            if (_state == null)
            {
                return;
            }

            var ranks = new int[5];
            for (var i = 0; i < 5; i++)
            {
                var stat = SocialStatPresentation.OrderedStats[i];
                ranks[i] = _state.SocialStats.GetRank(stat);
                if (nodes != null && i < nodes.Length)
                {
                    nodes[i]?.Bind(stat, ranks[i], null);
                }
            }

            radar?.SetRanks(ranks);

            if (heroBust != null && heroBust.sprite == null)
            {
                var sprite = LoadHeroBustSprite();
                if (sprite != null)
                {
                    heroBust.sprite = sprite;
                    heroBust.enabled = true;
                    heroBust.preserveAspect = true;
                }
            }
        }

        private static SocialStatsOverlayUI CreateHierarchy(Transform parent)
        {
            var rootGo = new GameObject("SocialStatsOverlay", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            Stretch(rootGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dim = CreateImage(rootGo.transform, "DimBackdrop", null);
            Stretch(dim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            dim.color = NavyDim;
            dim.raycastTarget = true;

            var watermark = CreateText(rootGo.transform, "Watermark", "RESONANCE FIELD", 96, TextAnchor.MiddleCenter);
            Stretch(watermark.rectTransform, new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);
            watermark.color = WatermarkColor;
            watermark.fontStyle = FontStyle.Bold;
            watermark.rectTransform.localEulerAngles = new Vector3(0f, 0f, -28f);

            var titleBlock = new GameObject("TitleBlock", typeof(RectTransform));
            titleBlock.transform.SetParent(rootGo.transform, false);
            Stretch(titleBlock.GetComponent<RectTransform>(), new Vector2(0.04f, 0.82f), new Vector2(0.55f, 0.98f), Vector2.zero, Vector2.zero);

            var title = CreateText(titleBlock.transform, "Title", "SOCIAL STATS", 42, TextAnchor.LowerLeft);
            Stretch(title.rectTransform, new Vector2(0f, 0.35f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;

            var subtitle = CreateText(titleBlock.transform, "Subtitle", "共鳴フィールド", 20, TextAnchor.UpperLeft);
            Stretch(subtitle.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.4f), Vector2.zero, Vector2.zero);
            subtitle.color = Cyan;

            var chartRoot = new GameObject("ChartRoot", typeof(RectTransform));
            chartRoot.transform.SetParent(rootGo.transform, false);
            Stretch(chartRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var radarGo = new GameObject("Radar", typeof(RectTransform), typeof(CanvasRenderer), typeof(SocialStatsRadarGraphic));
            radarGo.transform.SetParent(chartRoot.transform, false);
            var radarRect = radarGo.GetComponent<RectTransform>();
            radarRect.anchorMin = new Vector2(0.5f, 0.58f);
            radarRect.anchorMax = new Vector2(0.5f, 0.58f);
            radarRect.pivot = new Vector2(0.5f, 0.5f);
            radarRect.sizeDelta = new Vector2(600f, 600f);
            radarRect.anchoredPosition = Vector2.zero;
            var radarGraphic = radarGo.GetComponent<SocialStatsRadarGraphic>();
            radarGraphic.raycastTarget = false;
            radarGraphic.color = Color.white;

            var centerGlyph = CreateImage(chartRoot.transform, "CenterGlyph", null);
            var centerRect = centerGlyph.rectTransform;
            centerRect.anchorMin = new Vector2(0.5f, 0.58f);
            centerRect.anchorMax = new Vector2(0.5f, 0.58f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);
            centerRect.sizeDelta = new Vector2(48f, 48f);
            centerRect.anchoredPosition = Vector2.zero;
            centerGlyph.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
            centerGlyph.raycastTarget = false;

            var nodesRoot = new GameObject("NodesRoot", typeof(RectTransform));
            nodesRoot.transform.SetParent(rootGo.transform, false);
            Stretch(nodesRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var nodeViews = new SocialStatsNodeView[5];
            for (var i = 0; i < 5; i++)
            {
                nodeViews[i] = CreateNode(nodesRoot.transform, NodeNames[i], NodeOffsets[i]);
            }

            var hero = CreateImage(rootGo.transform, "HeroBust", LoadHeroBustSprite());
            var heroRect = hero.rectTransform;
            heroRect.anchorMin = new Vector2(0.5f, 0f);
            heroRect.anchorMax = new Vector2(0.5f, 0f);
            heroRect.pivot = new Vector2(0.5f, 0f);
            heroRect.sizeDelta = new Vector2(380f, 420f);
            heroRect.anchoredPosition = new Vector2(0f, 0f);
            hero.preserveAspect = true;
            hero.raycastTarget = false;
            if (hero.sprite == null)
            {
                hero.enabled = false;
            }

            var footer = CreateText(rootGo.transform, "FooterEsc", "[Esc] Back", 22, TextAnchor.MiddleRight);
            Stretch(footer.rectTransform, new Vector2(0.72f, 0.02f), new Vector2(0.97f, 0.08f), Vector2.zero, Vector2.zero);
            footer.color = Cyan;
            footer.fontStyle = FontStyle.Bold;

            var overlay = rootGo.AddComponent<SocialStatsOverlayUI>();
            overlay.root = rootGo;
            overlay.radar = radarGraphic;
            overlay.nodes = nodeViews;
            overlay.heroBust = hero;
            overlay.titleLabel = title;
            overlay.subtitleLabel = subtitle;
            overlay.watermarkLabel = watermark;
            overlay.footerLabel = footer;
            overlay.Rewire();
            rootGo.SetActive(false);
            return overlay;
        }

        private static SocialStatsNodeView CreateNode(Transform parent, string name, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.58f);
            rect.anchorMax = new Vector2(0.5f, 0.58f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 140f);
            rect.anchoredPosition = anchoredPosition;

            var icon = CreateImage(go.transform, "Icon", null);
            Stretch(icon.rectTransform, new Vector2(0f, 0.55f), new Vector2(0.28f, 1f), Vector2.zero, Vector2.zero);
            icon.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
            icon.raycastTarget = false;
            icon.enabled = false;

            var nameLabel = CreateText(go.transform, "Name", name.Replace("Node_", string.Empty), 22, TextAnchor.MiddleLeft);
            Stretch(nameLabel.rectTransform, new Vector2(0.3f, 0.65f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            nameLabel.fontStyle = FontStyle.Bold;
            nameLabel.color = Color.white;

            var rankLabel = CreateText(go.transform, "Rank", "Rank 1", 18, TextAnchor.MiddleLeft);
            Stretch(rankLabel.rectTransform, new Vector2(0.3f, 0.38f), new Vector2(1f, 0.68f), Vector2.zero, Vector2.zero);
            rankLabel.color = Cyan;

            var flavorLabel = CreateText(go.transform, "Flavor", string.Empty, 13, TextAnchor.UpperLeft);
            Stretch(flavorLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.4f), Vector2.zero, Vector2.zero);
            flavorLabel.color = new Color(1f, 1f, 1f, 0.78f);
            flavorLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            flavorLabel.verticalOverflow = VerticalWrapMode.Truncate;

            var view = go.AddComponent<SocialStatsNodeView>();
            view.AssignRefs(icon, nameLabel, rankLabel, flavorLabel);
            return view;
        }

        private static Sprite LoadHeroBustSprite()
        {
            var fromResources = Resources.Load<Sprite>("UI/SocialStats/ren_resonance_bust_v1");
            if (fromResources != null)
            {
                return fromResources;
            }

            var all = Resources.LoadAll<Sprite>("UI/SocialStats/ren_resonance_bust_v1");
            if (all != null && all.Length > 0)
            {
                return all[0];
            }

#if UNITY_EDITOR
            const string artPath = "Assets/FracturedChorus/Art/Characters/Ren/VnBust/ren_bust_neutral_v1.png";
            var importer = AssetImporter.GetAtPath(artPath) as TextureImporter;
            if (importer != null)
            {
                var dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }

            var editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            if (editorSprite != null)
            {
                return editorSprite;
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(artPath))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }
#endif
            return null;
        }

        private static Button FindButton(Transform root, params string[] paths)
        {
            var tf = FindTransform(root, paths);
            if (tf == null)
            {
                return null;
            }

            var button = tf.GetComponent<Button>();
            if (button != null)
            {
                return button;
            }

            var image = tf.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            button = tf.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Transform FindTransform(Transform root, params string[] paths)
        {
            foreach (var path in paths)
            {
                var found = root.Find(path);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
