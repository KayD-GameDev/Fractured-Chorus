using FracturedChorus.Combat.Presentation;
using FracturedChorus.Combat.Timeline;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.UI
{
    /// <summary>
    /// Hierarchy: NoteSimulator / Knob / (RailAnchor, NoteNum).
    /// Knob tunes belly space; RailAnchor + NoteNum are children (centered for pin + digit).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public sealed class BossNoteSimulator : MonoBehaviour
    {
        public const string ObjectName = "NoteSimulator";
        public const string KnobName = "Knob";
        public const string LegacyKnobName = "Square";
        public const string RailAnchorName = "RailAnchor";
        public const string NoteNumName = "NoteNum";
        public const int ShapeCount = BossNoteClusterBuilder.SingleVariantCount;

        public enum NoteShapePreview
        {
            V0 = 0,
            V1 = 1,
            V2 = 2,
            V3 = 3,
            V4 = 4
        }

        [SerializeField] private BeatTimelineUIView timeline;
        [SerializeField] private NoteShapePreview shapePreview = NoteShapePreview.V0;
        [SerializeField] private BossNoteShapeLayout[] shapeLayouts = new BossNoteShapeLayout[ShapeCount];

        private bool _syncing;
        private int _loadedShape = -1;

        public NoteShapePreview ShapePreview => shapePreview;
        public RectTransform Rect => transform as RectTransform;
        public BossNoteShapeLayout[] ShapeLayouts => shapeLayouts;

        public Vector2 NoteSize
        {
            get
            {
                var rt = Rect;
                return rt != null ? rt.sizeDelta : Vector2.zero;
            }
        }

        public float NoteAlpha
        {
            get
            {
                var img = GetComponent<Image>();
                return img != null && img.color.a > 0.01f ? img.color.a : 0.78f;
            }
        }

        public Vector2 KnobLocal
        {
            get
            {
                var knob = FindKnob();
                return knob != null ? knob.anchoredPosition : Vector2.zero;
            }
        }

        public Vector2 KnobSize
        {
            get
            {
                var knob = FindKnob();
                return knob != null ? knob.sizeDelta : new Vector2(24f, 24f);
            }
        }

        /// <summary>RailAnchor relative to Knob.</summary>
        public Vector2 RailAnchorLocal
        {
            get
            {
                var anchor = FindRailAnchor();
                return anchor != null ? anchor.anchoredPosition : Vector2.zero;
            }
        }

        /// <summary>NoteNum relative to Knob.</summary>
        public Vector2 NoteNumLocal
        {
            get
            {
                var num = FindNoteNum();
                return num != null ? num.anchoredPosition : Vector2.zero;
            }
        }

        /// <summary>Pin in NoteSimulator space (= Knob + RailAnchor).</summary>
        public Vector2 PinInNoteSpace => KnobLocal + RailAnchorLocal;

        public bool TryCapturePlayLayout(
            out Vector2 size,
            out float alpha,
            out BossNoteShapeLayout[] layouts)
        {
            EnsureKnobHierarchy();
            EnsureShapeLayouts();
            SaveCurrentShapeLayout();
            size = NoteSize;
            alpha = NoteAlpha;
            layouts = shapeLayouts;
            return size.x > 1f && size.y > 1f;
        }

        public BossNoteShapeLayout GetLayoutForVariant(int variantIndex)
        {
            EnsureShapeLayouts();
            var i = Mathf.Clamp(variantIndex, 0, ShapeCount - 1);
            var layout = shapeLayouts[i];
            if (!layout.HasData)
            {
                return CaptureHandlesToLayout();
            }

            // Migrate legacy entries that only had rail/num in note space (knobSize default 0).
            if (layout.knobSize.x < 0.5f && layout.knobSize.y < 0.5f)
            {
                return BossNoteShapeLayout.FromLegacyNoteSpace(layout.railAnchorLocal, layout.noteNumLocal);
            }

            return layout;
        }

        public void SetShapePreview(NoteShapePreview preview)
        {
            if (_syncing)
            {
                return;
            }

            if (_loadedShape >= 0 && (int)shapePreview == _loadedShape)
            {
                SaveCurrentShapeLayout();
            }

            shapePreview = preview;
            ApplyShapePreview(loadSavedHandles: true);
        }

        public void ApplyShapePreview(bool loadSavedHandles)
        {
            if (_syncing)
            {
                return;
            }

            _syncing = true;
            try
            {
                var catalog = ResolveCatalog();
                if (catalog != null)
                {
                    catalog.EnsureDefaultsLoaded();
                    ApplySprite(catalog.MusicSingle((int)shapePreview, BossNoteTier.Red));
                }

                EnsureKnobHierarchy();
                if (loadSavedHandles)
                {
                    LoadShapeLayoutToHandles((int)shapePreview);
                }

                _loadedShape = (int)shapePreview;
            }
            finally
            {
                _syncing = false;
            }
        }

        public void SaveCurrentShapeLayout()
        {
            EnsureKnobHierarchy();
            EnsureShapeLayouts();
            var i = Mathf.Clamp((int)shapePreview, 0, ShapeCount - 1);
            var next = CaptureHandlesToLayout();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(this, "Save NoteSimulator Shape Layout");
            }
#endif
            shapeLayouts[i] = next;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif
            SyncLayoutToCatalog();
        }

        public void SyncLayoutToCatalog()
        {
            var size = NoteSize;
            if (size.x < 1f || size.y < 1f)
            {
                return;
            }

            var view = ResolveTimeline();
            if (view == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(view, "Sync NoteSimulator Layout");
            }
#endif
            view.ApplyBossNoteTemplateSettings(size, NoteAlpha);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(view);
            }
#endif
        }

        public void SnapRailAnchorToBossLine()
        {
            var view = ResolveTimeline();
            var rt = Rect;
            EnsureKnobHierarchy();
            if (view == null || rt == null)
            {
                return;
            }

            var pin = PinInNoteSpace;
            var railY = view.BossNoteRailAnchoredY;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(rt, "Snap NoteSimulator To Boss Line");
            }
#endif
            var pos = rt.anchoredPosition;
            pos.y = railY - pin.y;
            rt.anchoredPosition = pos;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(rt);
            }
#endif
            SaveCurrentShapeLayout();
        }

        /// <summary>
        /// Ensures NoteSimulator / Knob / (RailAnchor, NoteNum). Renames Square → Knob.
        /// </summary>
        public RectTransform EnsureKnobHierarchy()
        {
            var root = Rect;
            if (root == null)
            {
                return null;
            }

            var knob = FindKnob();
            if (knob == null)
            {
                var legacy = root.Find(LegacyKnobName) as RectTransform;
                if (legacy != null)
                {
                    knob = legacy;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        Undo.RecordObject(knob.gameObject, "Rename Square → Knob");
                    }
#endif
                    knob.gameObject.name = KnobName;
                }
            }

            if (knob == null)
            {
                var go = new GameObject(KnobName, typeof(RectTransform));
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RegisterCreatedObjectUndo(go, "Create Knob");
                }
#endif
                knob = go.GetComponent<RectTransform>();
                knob.SetParent(root, false);
                knob.anchorMin = new Vector2(0.5f, 0.5f);
                knob.anchorMax = new Vector2(0.5f, 0.5f);
                knob.pivot = new Vector2(0.5f, 0.5f);
                knob.sizeDelta = new Vector2(24f, 24f);
                knob.anchoredPosition = Vector2.zero;
            }

            if (knob.parent != root)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.SetTransformParent(knob, root, "Reparent Knob");
                }
                else
#endif
                {
                    knob.SetParent(root, true);
                }
            }

            // Center-ish anchors for knob as a movable space.
            knob.anchorMin = new Vector2(0.5f, 0.5f);
            knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.pivot = new Vector2(0.5f, 0.5f);
            knob.localScale = Vector3.one;
            if (knob.sizeDelta.x < 0.5f || knob.sizeDelta.y < 0.5f)
            {
                knob.sizeDelta = new Vector2(24f, 24f);
            }

            EnsureChildUnderKnob(knob, RailAnchorName, Vector2.zero, addRailComponent: true);
            EnsureChildUnderKnob(knob, NoteNumName, Vector2.zero, addRailComponent: false, ensureText: true);
            return knob;
        }

        public static RectTransform EnsureKnobOn(RectTransform noteParent, BossNoteShapeLayout layout)
        {
            if (noteParent == null)
            {
                return null;
            }

            var knob = noteParent.Find(KnobName) as RectTransform;
            if (knob == null)
            {
                var go = new GameObject(KnobName, typeof(RectTransform));
                knob = go.GetComponent<RectTransform>();
                knob.SetParent(noteParent, false);
            }

            knob.anchorMin = new Vector2(0.5f, 0.5f);
            knob.anchorMax = new Vector2(0.5f, 0.5f);
            knob.pivot = new Vector2(0.5f, 0.5f);
            knob.localScale = Vector3.one;
            knob.anchoredPosition = layout.knobLocal;
            knob.sizeDelta = layout.knobSize.x > 0.5f ? layout.knobSize : new Vector2(24f, 24f);

            EnsureRailAnchorOn(knob, RailAnchorName, layout.railAnchorLocal);
            return knob;
        }

        public static RectTransform EnsureRailAnchorOn(RectTransform parent, string anchorName, Vector2 localPos)
        {
            if (parent == null || string.IsNullOrEmpty(anchorName))
            {
                return null;
            }

            var existing = parent.Find(anchorName) as RectTransform;
            if (existing == null)
            {
                var go = new GameObject(anchorName, typeof(RectTransform));
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RegisterCreatedObjectUndo(go, "Create RailAnchor");
                }
#endif
                existing = go.GetComponent<RectTransform>();
                existing.SetParent(parent, false);
            }
            else if (existing.parent != parent)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.SetTransformParent(existing, parent, "Reparent RailAnchor under Knob");
                }
                else
#endif
                {
                    existing.SetParent(parent, false);
                }
            }

            existing.anchorMin = new Vector2(0.5f, 0.5f);
            existing.anchorMax = new Vector2(0.5f, 0.5f);
            existing.pivot = new Vector2(0.5f, 0.5f);
            existing.sizeDelta = Vector2.zero;
            existing.anchoredPosition = localPos;
            existing.localScale = Vector3.one;

            if (existing.GetComponent<BossNoteRailAnchor>() == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.AddComponent<BossNoteRailAnchor>(existing.gameObject);
                }
                else
#endif
                {
                    existing.gameObject.AddComponent<BossNoteRailAnchor>();
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(existing);
            }
#endif
            return existing;
        }

        public static BossNoteSimulator FindInLayer(RectTransform layer)
        {
            if (layer == null)
            {
                return null;
            }

            var direct = layer.Find(ObjectName)?.GetComponent<BossNoteSimulator>();
            return direct != null ? direct : layer.GetComponentInChildren<BossNoteSimulator>(true);
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += EditorDeferredSync;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (gameObject.name != ObjectName)
            {
                gameObject.name = ObjectName;
            }

            EnsureShapeLayouts();
            EditorApplication.delayCall += EditorDeferredSync;
        }

        private void EditorDeferredSync()
        {
            if (this == null)
            {
                return;
            }

            EnsureKnobHierarchy();
            ApplyShapePreview(loadSavedHandles: _loadedShape != (int)shapePreview);
            var i = Mathf.Clamp((int)shapePreview, 0, ShapeCount - 1);
            if (!shapeLayouts[i].HasData)
            {
                SaveCurrentShapeLayout();
            }
        }
#endif

        private BossNoteShapeLayout CaptureHandlesToLayout() =>
            BossNoteShapeLayout.FromKnob(KnobLocal, KnobSize, RailAnchorLocal, NoteNumLocal);

        private void LoadShapeLayoutToHandles(int variantIndex)
        {
            var layout = GetLayoutForVariant(variantIndex);
            if (!layout.HasData)
            {
                return;
            }

            var knob = EnsureKnobHierarchy();
            var rail = FindRailAnchor();
            var num = FindNoteNum();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (knob != null)
                {
                    Undo.RecordObject(knob, "Load Shape Knob");
                }

                if (rail != null)
                {
                    Undo.RecordObject(rail, "Load Shape RailAnchor");
                }

                if (num != null)
                {
                    Undo.RecordObject(num, "Load Shape NoteNum");
                }
            }
#endif
            if (knob != null)
            {
                knob.anchoredPosition = layout.knobLocal;
                if (layout.knobSize.x > 0.5f && layout.knobSize.y > 0.5f)
                {
                    knob.sizeDelta = layout.knobSize;
                }
            }

            if (rail != null)
            {
                rail.anchoredPosition = layout.railAnchorLocal;
            }

            if (num != null)
            {
                num.anchorMin = new Vector2(0.5f, 0.5f);
                num.anchorMax = new Vector2(0.5f, 0.5f);
                num.pivot = new Vector2(0.5f, 0.5f);
                num.anchoredPosition = layout.noteNumLocal;
            }
        }

        private RectTransform EnsureChildUnderKnob(
            RectTransform knob,
            string childName,
            Vector2 localPos,
            bool addRailComponent,
            bool ensureText = false)
        {
            var existing = FindDeepChild(Rect, childName) as RectTransform;
            if (existing == null)
            {
                var go = new GameObject(childName, typeof(RectTransform));
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
                }
#endif
                existing = go.GetComponent<RectTransform>();
                existing.SetParent(knob, false);
                existing.anchoredPosition = localPos;
            }
            else if (existing.parent != knob)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.SetTransformParent(existing, knob, "Reparent " + childName + " under Knob");
                }
                else
#endif
                {
                    existing.SetParent(knob, false);
                }
            }

            existing.anchorMin = new Vector2(0.5f, 0.5f);
            existing.anchorMax = new Vector2(0.5f, 0.5f);
            existing.pivot = new Vector2(0.5f, 0.5f);
            existing.localScale = Vector3.one;
            if (childName == RailAnchorName)
            {
                existing.sizeDelta = Vector2.zero;
            }

            if (addRailComponent && existing.GetComponent<BossNoteRailAnchor>() == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Undo.AddComponent<BossNoteRailAnchor>(existing.gameObject);
                }
                else
#endif
                {
                    existing.gameObject.AddComponent<BossNoteRailAnchor>();
                }
            }

            if (ensureText && existing.GetComponent<Text>() == null)
            {
                var text = existing.gameObject.AddComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                text.text = "3";
                if (existing.sizeDelta.sqrMagnitude < 1f)
                {
                    existing.sizeDelta = new Vector2(24f, 24f);
                }
            }

            return existing;
        }

        private void EnsureShapeLayouts()
        {
            if (shapeLayouts != null && shapeLayouts.Length == ShapeCount)
            {
                return;
            }

            var next = new BossNoteShapeLayout[ShapeCount];
            if (shapeLayouts != null)
            {
                for (var i = 0; i < Mathf.Min(shapeLayouts.Length, ShapeCount); i++)
                {
                    next[i] = shapeLayouts[i];
                }
            }

            shapeLayouts = next;
        }

        private RectTransform FindKnob()
        {
            var root = Rect;
            if (root == null)
            {
                return null;
            }

            var knob = root.Find(KnobName) as RectTransform;
            if (knob != null)
            {
                return knob;
            }

            return root.Find(LegacyKnobName) as RectTransform;
        }

        private RectTransform FindRailAnchor()
        {
            var knob = FindKnob();
            if (knob != null)
            {
                var underKnob = knob.Find(RailAnchorName) as RectTransform;
                if (underKnob != null)
                {
                    return underKnob;
                }
            }

            return FindDeepChild(Rect, RailAnchorName) as RectTransform;
        }

        private RectTransform FindNoteNum()
        {
            var knob = FindKnob();
            if (knob != null)
            {
                var underKnob = knob.Find(NoteNumName) as RectTransform;
                if (underKnob != null)
                {
                    return underKnob;
                }
            }

            return FindDeepChild(Rect, NoteNumName) as RectTransform;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindDeepChild(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private TimelineNoteVisualCatalog ResolveCatalog()
        {
            var view = ResolveTimeline();
            return view != null ? view.NoteVisuals : null;
        }

        private BeatTimelineUIView ResolveTimeline()
        {
            if (timeline != null)
            {
                return timeline;
            }

            timeline = GetComponentInParent<BeatTimelineUIView>();
            if (timeline == null)
            {
                timeline = FindAnyObjectByType<BeatTimelineUIView>();
            }

            return timeline;
        }

        private void ApplySprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            var img = GetComponent<Image>();
            if (img == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(img, "NoteSimulator Shape Preview");
            }
#endif
            img.sprite = sprite;
            img.preserveAspect = true;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(img);
            }
#endif
        }
    }
}
