#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class CoverHudSetupEditor
    {
        private const string HologramArtPath =
            "Assets/FracturedChorus/Art/UI/Combat/Cover/combat_btn_cover_hologram_v1.png";
        private const string ResourcesCoverPath =
            "Assets/FracturedChorus/Resources/UI/Combat/combat_btn_cover_v1.png";

        [MenuItem("Fractured Chorus/Setup Cover HUD (Hierarchy)")]
        public static void SetupCoverHudInHierarchy()
        {
            SyncHologramSpriteToResources();

            var canvasRt = ResolveCanvasRoot();
            if (canvasRt == null)
            {
                Debug.LogError("[CoverHud] No Canvas / Combat UI root found. Open CombatPrototype scene.");
                return;
            }

            var hud = Object.FindAnyObjectByType<CoverHudView>();
            if (hud == null)
            {
                var go = new GameObject("CoverHud", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Create CoverHud");
                hud = Undo.AddComponent<CoverHudView>(go);
            }

            var so = new SerializedObject(hud);
            so.FindProperty("preserveSceneLayout").boolValue = true;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ResourcesCoverPath);
            if (sprite == null)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HologramArtPath);
            }

            if (sprite != null)
            {
                so.FindProperty("buttonSprite").objectReferenceValue = sprite;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            hud.EnsureBuilt();
            hud.ApplyButtonVisual();
            ForceBuildEnergyGauge(hud);

            var rt = hud.transform as RectTransform;
            if (rt != null && rt.parent != canvasRt)
            {
                Undo.SetTransformParent(rt, canvasRt, "Parent CoverHud");
            }

            Selection.activeGameObject = hud.EnergyGauge != null
                ? hud.EnergyGauge.gameObject
                : hud.gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(
                "[CoverHud] Ready. CoverEnergyGauge nằm bên phải CoverHud (ngoài nút). Save scene.");
        }

        [MenuItem("Fractured Chorus/Cover Pips/Keep 1 Pip Template (Scene)")]
        public static void StripCoverPipsToTemplate()
        {
            var gauge = ResolveCoverGauge();
            if (gauge == null)
            {
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(gauge.gameObject, "Keep 1 Cover Pip Template");
            var removed = gauge.StripToTemplatePip();
            EditorUtility.SetDirty(gauge);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            var template = gauge.transform.Find("Pips/Pip_0");
            Selection.activeGameObject = template != null ? template.gameObject : gauge.gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);
            Debug.Log(
                $"[CoverHud] Scene giữ 1 pip template (Pip_0). Removed={removed}. " +
                "Play sẽ clone đủ 10. Chỉnh RectTransform Pip_0 + pipStepY trên CoverEnergyGauge.");
        }

        [MenuItem("Fractured Chorus/Cover Pips/Preview Spawn 10 (Editor)")]
        public static void PreviewSpawnTenCoverPips()
        {
            var gauge = ResolveCoverGauge();
            if (gauge == null)
            {
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(gauge.gameObject, "Preview 10 Cover Pips");
            var gso = new SerializedObject(gauge);
            gso.FindProperty("preserveSceneLayout").boolValue = true;
            var pip = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/FracturedChorus/Art/UI/Combat/Cover/cover_energy_pip_hologram_v1.png");
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/FracturedChorus/Art/UI/Combat/Cover/cover_energy_gauge_frame_hologram_v1.png");
            if (pip != null)
            {
                gso.FindProperty("pipSprite").objectReferenceValue = pip;
            }

            if (frame != null)
            {
                gso.FindProperty("frameSprite").objectReferenceValue = frame;
            }

            gso.ApplyModifiedPropertiesWithoutUndo();

            var created = gauge.CreateHandEditPips(resetLayout: true);
            EditorUtility.SetDirty(gauge);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            var pipsRoot = gauge.transform.Find("Pips");
            Selection.activeGameObject = pipsRoot != null ? pipsRoot.gameObject : gauge.gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);
            Debug.Log(
                $"[CoverHud] Preview spawn {created} pips trong Editor. " +
                "Trước khi ship scene: chạy Keep 1 Pip Template.");
        }

        private static CoverEnergyGaugeView ResolveCoverGauge()
        {
            var hud = Object.FindAnyObjectByType<CoverHudView>();
            var gauge = hud != null
                ? hud.EnergyGauge
                : Object.FindAnyObjectByType<CoverEnergyGaugeView>();
            if (gauge == null && hud != null)
            {
                ForceBuildEnergyGauge(hud);
                gauge = hud.EnergyGauge;
            }

            if (gauge == null)
            {
                Debug.LogError("[CoverHud] Không thấy CoverEnergyGauge. Chạy Setup Cover HUD trước.");
            }

            return gauge;
        }

        [MenuItem("Fractured Chorus/Build Cover Energy Gauge (Scene)")]
        public static void BuildCoverEnergyGaugeMenu()
        {
            var hud = Object.FindAnyObjectByType<CoverHudView>();
            if (hud == null)
            {
                Debug.LogError("[CoverHud] Không thấy CoverHudView. Mở CombatPrototype rồi Setup Cover HUD trước.");
                return;
            }

            ForceBuildEnergyGauge(hud);
            Selection.activeGameObject = hud.EnergyGauge != null
                ? hud.EnergyGauge.gameObject
                : hud.gameObject;
            EditorGUIUtility.PingObject(Selection.activeGameObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[CoverHud] CoverEnergyGauge built — kiểm tra Hierarchy / Scene view.");
        }

        private static void ForceBuildEnergyGauge(CoverHudView hud)
        {
            if (hud == null)
            {
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(hud.gameObject, "Build Cover Energy Gauge");
            var so = new SerializedObject(hud);
            so.FindProperty("preserveSceneLayout").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            hud.EnsureBuilt();
            var gauge = hud.EnergyGauge ?? hud.GetComponentInChildren<CoverEnergyGaugeView>(true);
            if (gauge == null)
            {
                Debug.LogError(
                    "[CoverHud] CoverEnergyGauge chưa có trong scene. " +
                    "Không auto-spawn layout — hãy giữ object CoverEnergyGauge hiện có.");
                return;
            }

            so.FindProperty("energyGauge").objectReferenceValue = gauge;
            so.ApplyModifiedPropertiesWithoutUndo();

            var gso = new SerializedObject(gauge);
            gso.FindProperty("preserveSceneLayout").boolValue = true;
            var pip = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/FracturedChorus/Art/UI/Combat/Cover/cover_energy_pip_hologram_v1.png");
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/FracturedChorus/Art/UI/Combat/Cover/cover_energy_gauge_frame_hologram_v1.png");
            if (pip != null)
            {
                gso.FindProperty("pipSprite").objectReferenceValue = pip;
            }

            if (frame != null)
            {
                gso.FindProperty("frameSprite").objectReferenceValue = frame;
            }

            gso.ApplyModifiedPropertiesWithoutUndo();
            gauge.EnsureBuilt();
            EditorUtility.SetDirty(gauge);

            EditorUtility.SetDirty(hud);
        }

        [MenuItem("Fractured Chorus/Sync Cover Button Sprite (Hologram → Resources)")]
        public static void SyncHologramSpriteToResources()
        {
            if (!System.IO.File.Exists(HologramArtPath) &&
                !System.IO.File.Exists(ToFullPath(HologramArtPath)))
            {
                Debug.LogWarning("[CoverHud] Hologram source missing: " + HologramArtPath);
                EnsureCoverImport(ResourcesCoverPath);
                return;
            }

            var srcFull = ToFullPath(HologramArtPath);
            var dstFull = ToFullPath(ResourcesCoverPath);
            var dstDir = System.IO.Path.GetDirectoryName(dstFull);
            if (!string.IsNullOrEmpty(dstDir) && !System.IO.Directory.Exists(dstDir))
            {
                System.IO.Directory.CreateDirectory(dstDir);
            }

            if (System.IO.File.Exists(srcFull))
            {
                System.IO.File.Copy(srcFull, dstFull, true);
                AssetDatabase.ImportAsset(ResourcesCoverPath, ImportAssetOptions.ForceUpdate);
            }

            EnsureCoverImport(ResourcesCoverPath);
            EnsureCoverImport(HologramArtPath);
            CombatButtonSpriteImportSettings.EnsureImportSettingsMenu();
            Debug.Log("[CoverHud] Synced hologram → Resources/UI/Combat/combat_btn_cover_v1.png");
        }

        private static void EnsureCoverImport(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

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

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                dirty = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static RectTransform ResolveCanvasRoot()
        {
            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                var party = bootstrap.GetComponentInChildren<PartyStatusBarUIView>(true);
                if (party != null && party.transform.parent is RectTransform partyParent)
                {
                    return partyParent;
                }

                var timeline = bootstrap.GetComponentInChildren<BeatTimelineUIView>(true);
                if (timeline != null && timeline.transform.parent is RectTransform tlParent)
                {
                    return tlParent;
                }
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            return canvas != null ? canvas.transform as RectTransform : null;
        }

        private static string ToFullPath(string assetPath)
        {
            var projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            return System.IO.Path.Combine(projectRoot, assetPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }
    }

    [CustomEditor(typeof(CoverHudView))]
    public class CoverHudViewInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var view = (CoverHudView)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Cover button edit", EditorStyles.boldLabel);

            if (GUILayout.Button("Apply Button Visual (sprite → Image)"))
            {
                Undo.RecordObject(view, "Apply Cover Button Visual");
                view.EnsureBuilt();
                view.ApplyButtonVisual();
                EditorUtility.SetDirty(view);
                if (view.CoverButtonImage != null)
                {
                    EditorUtility.SetDirty(view.CoverButtonImage);
                }
            }

            if (GUILayout.Button("Select CoverButton in Hierarchy"))
            {
                if (view.CoverButton != null)
                {
                    Selection.activeGameObject = view.CoverButton.gameObject;
                    EditorGUIUtility.PingObject(view.CoverButton.gameObject);
                }
                else
                {
                    view.EnsureBuilt();
                    if (view.CoverButton != null)
                    {
                        Selection.activeGameObject = view.CoverButton.gameObject;
                    }
                }
            }

            if (GUILayout.Button("Sync Hologram Sprite → Resources"))
            {
                CoverHudSetupEditor.SyncHologramSpriteToResources();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/FracturedChorus/Resources/UI/Combat/combat_btn_cover_v1.png");
                if (sprite != null)
                {
                    var so = serializedObject;
                    so.FindProperty("buttonSprite").objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    view.ApplyButtonVisual();
                    EditorUtility.SetDirty(view);
                }
            }

            if (GUILayout.Button("Build Cover Energy Gauge (frame + 10 pips)"))
            {
                CoverHudSetupEditor.BuildCoverEnergyGaugeMenu();
            }

            if (GUILayout.Button("Keep 1 Pip Template (Scene)"))
            {
                CoverHudSetupEditor.StripCoverPipsToTemplate();
            }

            if (GUILayout.Button("Preview Spawn 10 Pips (Editor)"))
            {
                CoverHudSetupEditor.PreviewSpawnTenCoverPips();
            }

            if (GUILayout.Button("Relayout Spawned Pips From Pip_0"))
            {
                var gauge = view.EnergyGauge;
                if (gauge == null)
                {
                    gauge = view.GetComponentInChildren<CoverEnergyGaugeView>(true);
                }

                if (gauge != null)
                {
                    Undo.RegisterFullObjectHierarchyUndo(gauge.gameObject, "Relayout Cover Pips");
                    gauge.RelayoutPipsFromPip0();
                    EditorUtility.SetDirty(gauge);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    Selection.activeGameObject = gauge.gameObject;
                }
            }

            EditorGUILayout.HelpBox(
                "Scene chỉ cần Pip_0 (template). Play sẽ clone đủ 10 theo pipStepY.\n" +
                "Trước khi save ship: Keep 1 Pip Template. Preview Spawn chỉ để xem layout Editor.",
                MessageType.Info);
        }
    }
}
#endif
