#if UNITY_EDITOR
using System.IO;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Editor
{
    [InitializeOnLoad]
    public static class CharlotteShieldPreviewEditor
    {
        private const string DomePreviewName = "CharlotteDomeRingSizePreview";
        private const string Skill1PreviewName = "CharlottePersonalShieldSizePreview";
        private const string Skill1SaveHideFlag = "Temp/charlotte-skill1-save-hide.flag";

        static CharlotteShieldPreviewEditor()
        {
            EditorApplication.delayCall += TrySkill1SaveHideFromFlag;
        }

        private static void TrySkill1SaveHideFromFlag()
        {
            if (!File.Exists(Skill1SaveHideFlag))
            {
                return;
            }

            try
            {
                File.Delete(Skill1SaveHideFlag);
            }
            catch
            {
            }

            SaveAndHideSkill1Preview();
        }

        [MenuItem("Fractured Chorus/VFX/Save & Hide Charlotte Skill 1 Personal Shield")]
        public static void SaveAndHideSkill1Preview()
        {
            var preview = Object.FindAnyObjectByType<CharlottePersonalShieldSizePreview>(
                FindObjectsInactive.Include);
            if (preview == null)
            {
                Debug.LogWarning("[CharlotteSkill1] Không tìm thấy preview để Save & Hide.");
                return;
            }

            preview.SaveToTuning();
            Undo.RecordObject(preview.gameObject, "Hide Charlotte Skill 1 Preview");
            preview.gameObject.SetActive(false);
            EditorUtility.SetDirty(preview.gameObject);
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
            Debug.Log(
                $"[CharlotteSkill1] Saved & hidden. size={preview.WorldSize:F2} height={preview.HeightOffset:F2} orbit={preview.OrbitRadius:F2}");
        }

        [MenuItem("Fractured Chorus/VFX/Preview Charlotte Skill 1 Personal Shield")]
        public static void SpawnSkill1Preview()
        {
            EnsureCombatSceneLoaded();
            var tuning = EnsureTuning();
            var preview = EnsureSkill1Preview(tuning);
            preview.gameObject.SetActive(true);
            preview.RefreshVisual(true);
            Selection.activeGameObject = preview.gameObject;
            EditorGUIUtility.PingObject(preview.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log(
                "[CharlotteSkill1] Preview sẵn sàng. Kéo mũi tên Y / vòng vàng (size) / vòng cyan (orbit) → Save.");
        }

        [MenuItem("Fractured Chorus/VFX/Save Charlotte Skill 1 Personal Shield From Preview")]
        public static void SaveSkill1FromPreview()
        {
            var preview = Object.FindAnyObjectByType<CharlottePersonalShieldSizePreview>();
            if (preview == null)
            {
                EditorUtility.DisplayDialog(
                    "Charlotte Skill 1",
                    "Chưa có preview. Chạy Preview Charlotte Skill 1 Personal Shield trước.",
                    "OK");
                return;
            }

            preview.SaveToTuning();
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
        }

        [MenuItem("Fractured Chorus/VFX/Preview Charlotte Skill 3 Dome (Edit Size)")]
        public static void SpawnPreview()
        {
            EnsureCombatSceneLoaded();
            var tuning = EnsureTuning();
            var preview = EnsureDomePreview(tuning);
            preview.gameObject.SetActive(true);
            preview.RefreshVisual(true);
            Selection.activeGameObject = preview.gameObject;
            EditorGUIUtility.PingObject(preview.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log(
                "[CharlotteDome] Preview sẵn sàng. Kéo vòng tròn vàng trong Scene, hoặc slider World Size → Save.");
        }

        [MenuItem("Fractured Chorus/VFX/Save Charlotte Skill 3 Dome Size From Preview")]
        public static void SaveFromPreview()
        {
            var preview = Object.FindAnyObjectByType<CharlotteDomeRingSizePreview>();
            if (preview == null)
            {
                EditorUtility.DisplayDialog(
                    "Charlotte Dome",
                    "Chưa có preview. Chạy Preview Charlotte Skill 3 Dome trước.",
                    "OK");
                return;
            }

            preview.SaveToTuning();
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
        }

        [MenuItem("Fractured Chorus/VFX/Clear Charlotte Shield Preview")]
        public static void ClearPreview()
        {
            var skill1 = Object.FindAnyObjectByType<CharlottePersonalShieldSizePreview>(
                FindObjectsInactive.Include);
            if (skill1 != null)
            {
                Undo.DestroyObjectImmediate(skill1.gameObject);
            }

            var dome = Object.FindAnyObjectByType<CharlotteDomeRingSizePreview>(
                FindObjectsInactive.Include);
            if (dome != null)
            {
                Undo.DestroyObjectImmediate(dome.gameObject);
            }

            var old = Object.FindAnyObjectByType<CharlotteShieldSizePreview>();
            if (old != null)
            {
                Undo.DestroyObjectImmediate(old.gameObject);
            }
        }

        private static void EnsureCombatSceneLoaded()
        {
            var active = SceneManager.GetActiveScene();
            if (active.name == "CombatPrototype" || active.name == "CombatTutorial")
            {
                return;
            }

            var path = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
        }

        private static CharlotteShieldTuning EnsureTuning()
        {
            var tuning = CharlotteShieldTuning.Resolve();
            if (tuning != null)
            {
                return tuning;
            }

            var root = GameObject.Find("CombatRoot");
            if (root == null)
            {
                root = new GameObject("CombatRoot");
                Undo.RegisterCreatedObjectUndo(root, "Create CombatRoot");
            }

            tuning = Undo.AddComponent<CharlotteShieldTuning>(root);
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(root.scene);
            return tuning;
        }

        private static CharlottePersonalShieldSizePreview EnsureSkill1Preview(CharlotteShieldTuning tuning)
        {
            var existing = Object.FindAnyObjectByType<CharlottePersonalShieldSizePreview>(
                FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.SetTuning(
                    tuning.PersonalWorldSize,
                    tuning.PersonalHeightOffset,
                    tuning.PersonalOrbitRadius);
                BindFollow(existing);
                return existing;
            }

            var go = new GameObject(Skill1PreviewName);
            Undo.RegisterCreatedObjectUndo(go, "Create Charlotte Skill 1 Preview");
            var root = GameObject.Find("CombatRoot");
            if (root != null)
            {
                go.transform.SetParent(root.transform, false);
            }

            var preview = go.AddComponent<CharlottePersonalShieldSizePreview>();
            preview.SetTuning(
                tuning.PersonalWorldSize,
                tuning.PersonalHeightOffset,
                tuning.PersonalOrbitRadius);
            BindFollow(preview);
            EditorSceneManager.MarkSceneDirty(go.scene);
            return preview;
        }

        private static CharlotteDomeRingSizePreview EnsureDomePreview(CharlotteShieldTuning tuning)
        {
            var existing = Object.FindAnyObjectByType<CharlotteDomeRingSizePreview>(
                FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.SetTuning(tuning.DomeWorldSize, tuning.DomeXOffset, tuning.DomeHeightOffset);
                BindFollow(existing);
                return existing;
            }

            var legacy = Object.FindAnyObjectByType<CharlotteShieldSizePreview>();
            if (legacy != null)
            {
                Undo.DestroyObjectImmediate(legacy.gameObject);
            }

            var go = new GameObject(DomePreviewName);
            Undo.RegisterCreatedObjectUndo(go, "Create Charlotte Dome Preview");
            var preview = go.AddComponent<CharlotteDomeRingSizePreview>();
            preview.SetTuning(tuning.DomeWorldSize, tuning.DomeXOffset, tuning.DomeHeightOffset);
            BindFollow(preview);
            EditorSceneManager.MarkSceneDirty(go.scene);
            return preview;
        }

        private static void BindFollow(CharlottePersonalShieldSizePreview preview)
        {
            var follow = ResolveCharlotteFollow();
            if (follow != null)
            {
                preview.BindFollow(follow);
            }
        }

        private static void BindFollow(CharlotteDomeRingSizePreview preview)
        {
            var follow = ResolveCharlotteFollow();
            if (follow != null)
            {
                preview.BindFollow(follow);
            }
        }

        private static Transform ResolveCharlotteFollow()
        {
            // Unity 6.4+: FindObjectsInactive overload — FindObjectsSortMode is obsolete.
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Exclude))
            {
                if (CharlotteCounterShieldView.IsCharlotteUnit(view.Unit, view))
                {
                    return view.transform;
                }
            }

            var named = GameObject.Find("Unit_Tank")
                        ?? GameObject.Find("Charlotte")
                        ?? GameObject.Find("Charlott");
            return named != null ? named.transform : null;
        }
    }

    [CustomEditor(typeof(CharlottePersonalShieldSizePreview))]
    public sealed class CharlottePersonalShieldSizePreviewInspector : UnityEditor.Editor
    {
        private SerializedProperty _worldSize;
        private SerializedProperty _heightOffset;
        private SerializedProperty _orbitRadius;

        private void OnEnable()
        {
            _worldSize = serializedObject.FindProperty("worldSize");
            _heightOffset = serializedObject.FindProperty("heightOffset");
            _orbitRadius = serializedObject.FindProperty("orbitRadius");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Scene: kéo mũi tên Y để chỉnh height · vòng vàng = World Size · vòng cyan = Orbit Radius.\n" +
                "Không dùng Scale tool.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(_worldSize, 0.4f, 6f, new GUIContent("World Size"));
            EditorGUILayout.Slider(_heightOffset, -1f, 3f, new GUIContent("Height Offset (Y)"));
            EditorGUILayout.Slider(_orbitRadius, 0.35f, 3.5f, new GUIContent("Orbit Radius"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                var preview = (CharlottePersonalShieldSizePreview)target;
                preview.RefreshVisual(true);
                EditorUtility.SetDirty(preview);
                SceneView.RepaintAll();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }

            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "worldSize",
                "heightOffset",
                "orbitRadius");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Save Size To Skill 1 Personal Shield", GUILayout.Height(28f)))
            {
                var preview = (CharlottePersonalShieldSizePreview)target;
                preview.SaveToTuning();
                EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            }
        }

        private void OnSceneGUI()
        {
            var preview = (CharlottePersonalShieldSizePreview)target;
            if (preview == null || !preview.isActiveAndEnabled)
            {
                return;
            }

            var center = preview.ResolveAnchorWorld();
            Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);

            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(center, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(preview, "Move Charlotte Skill 1 Preview");
                var followT = preview.Follow;
                var anchor = followT != null ? followT.position : preview.transform.position;
                var view = followT != null ? followT.GetComponent<UnitView>() : null;
                var feetY = view != null ? view.FeetWorldPosition.y : anchor.y;
                preview.SetHeightOffset(moved.y - feetY);
                EditorUtility.SetDirty(preview);
            }

            center = preview.ResolveAnchorWorld();
            EditorGUI.BeginChangeCheck();
            var radius = Handles.RadiusHandle(Quaternion.identity, center, preview.WorldSize * 0.5f);
            Handles.DrawWireDisc(center, Vector3.forward, preview.WorldSize * 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(preview, "Resize Charlotte Skill 1 Preview");
                preview.SetWorldSize(Mathf.Clamp(radius * 2f, 0.4f, 6f));
                EditorUtility.SetDirty(preview);
            }

            Handles.color = new Color(0.35f, 0.9f, 1f, 0.95f);
            EditorGUI.BeginChangeCheck();
            var orbit = Handles.RadiusHandle(Quaternion.identity, center, preview.OrbitRadius);
            Handles.DrawWireDisc(center, Vector3.forward, preview.OrbitRadius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(preview, "Orbit Charlotte Skill 1 Preview");
                preview.SetOrbitRadius(Mathf.Clamp(orbit, 0.35f, 3.5f));
                EditorUtility.SetDirty(preview);
            }
        }
    }

    [CustomEditor(typeof(CharlotteDomeRingSizePreview))]
    public sealed class CharlotteDomeRingSizePreviewInspector : UnityEditor.Editor
    {
        private SerializedProperty _worldSize;
        private SerializedProperty _xOffset;
        private SerializedProperty _heightOffset;
        private SerializedProperty _waveSizeScale;
        private SerializedProperty _showWaveOrbit;

        private void OnEnable()
        {
            _worldSize = serializedObject.FindProperty("worldSize");
            _xOffset = serializedObject.FindProperty("xOffset");
            _heightOffset = serializedObject.FindProperty("heightOffset");
            _waveSizeScale = serializedObject.FindProperty("waveSizeScale");
            _showWaveOrbit = serializedObject.FindProperty("showWaveOrbit");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Scene: kéo mũi tên (X/Y) để dịch vị trí · kéo vòng vàng để đổi size.\n" +
                "Không dùng Scale tool.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(_worldSize, 0.4f, 14f, new GUIContent("World Size"));
            EditorGUILayout.Slider(_xOffset, -4f, 4f, new GUIContent("X Offset"));
            EditorGUILayout.Slider(_heightOffset, -1f, 3f, new GUIContent("Height Offset (Y)"));
            EditorGUILayout.PropertyField(_showWaveOrbit);
            if (_showWaveOrbit.boolValue)
            {
                EditorGUILayout.Slider(_waveSizeScale, 0.2f, 2.5f, new GUIContent("Wave Size Scale"));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                var preview = (CharlotteDomeRingSizePreview)target;
                preview.RefreshVisual(true);
                EditorUtility.SetDirty(preview);
                SceneView.RepaintAll();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }

            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "worldSize",
                "xOffset",
                "heightOffset",
                "waveSizeScale",
                "showWaveOrbit");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Save Size To Skill 3 Dome", GUILayout.Height(28f)))
            {
                var preview = (CharlotteDomeRingSizePreview)target;
                preview.SaveToTuning();
                EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            }
        }

        private void OnSceneGUI()
        {
            var preview = (CharlotteDomeRingSizePreview)target;
            if (preview == null || !preview.isActiveAndEnabled)
            {
                return;
            }

            var center = preview.ResolveAnchorWorld();
            Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);

            EditorGUI.BeginChangeCheck();
            var moved = Handles.PositionHandle(center, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(preview, "Move Charlotte Dome Preview");
                var followT = preview.Follow;
                var anchor = followT != null ? followT.position : preview.transform.position;
                var view = followT != null ? followT.GetComponent<UnitView>() : null;
                var feetY = view != null ? view.FeetWorldPosition.y : anchor.y;
                preview.SetOffsets(moved.x - anchor.x, moved.y - feetY);
                EditorUtility.SetDirty(preview);
            }

            center = preview.ResolveAnchorWorld();
            EditorGUI.BeginChangeCheck();
            var radius = Handles.RadiusHandle(Quaternion.identity, center, preview.WorldSize * 0.5f);
            Handles.DrawWireDisc(center, Vector3.forward, preview.WorldSize * 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(preview, "Resize Charlotte Dome Preview");
                preview.SetWorldSize(Mathf.Clamp(radius * 2f, 0.4f, 14f));
                EditorUtility.SetDirty(preview);
            }
        }
    }

    [CustomEditor(typeof(CharlotteShieldTuning))]
    public sealed class CharlotteShieldTuningInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            var tuning = (CharlotteShieldTuning)target;
            var skill1 = Object.FindAnyObjectByType<CharlottePersonalShieldSizePreview>(
                FindObjectsInactive.Include);
            var dome = Object.FindAnyObjectByType<CharlotteDomeRingSizePreview>(
                FindObjectsInactive.Include);

            EditorGUILayout.Space(6f);
            if (skill1 == null)
            {
                if (GUILayout.Button("Show Skill 1 Personal Shield Preview", GUILayout.Height(26f)))
                {
                    CharlotteShieldPreviewEditor.SpawnSkill1Preview();
                }
            }
            else
            {
                if (!skill1.gameObject.activeSelf
                    && GUILayout.Button("Enable Skill 1 Preview", GUILayout.Height(26f)))
                {
                    skill1.gameObject.SetActive(true);
                }

                if (GUILayout.Button("Sync Skill 1 Preview From Tuning", GUILayout.Height(26f)))
                {
                    skill1.gameObject.SetActive(true);
                    skill1.SetTuning(
                        tuning.PersonalWorldSize,
                        tuning.PersonalHeightOffset,
                        tuning.PersonalOrbitRadius);
                    Selection.activeGameObject = skill1.gameObject;
                    SceneView.RepaintAll();
                }
            }

            if (dome == null)
            {
                if (GUILayout.Button("Show Dome Preview", GUILayout.Height(26f)))
                {
                    CharlotteShieldPreviewEditor.SpawnPreview();
                }
            }
            else
            {
                if (!dome.gameObject.activeSelf
                    && GUILayout.Button("Enable Dome Preview", GUILayout.Height(26f)))
                {
                    dome.gameObject.SetActive(true);
                }

                if (GUILayout.Button("Sync Dome Preview From Tuning", GUILayout.Height(26f)))
                {
                    dome.gameObject.SetActive(true);
                    dome.SetTuning(tuning.DomeWorldSize, tuning.DomeXOffset, tuning.DomeHeightOffset);
                    Selection.activeGameObject = dome.gameObject;
                    SceneView.RepaintAll();
                }
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                if (skill1 != null)
                {
                    skill1.SetTuning(
                        tuning.PersonalWorldSize,
                        tuning.PersonalHeightOffset,
                        tuning.PersonalOrbitRadius);
                }

                if (dome != null)
                {
                    dome.SetTuning(tuning.DomeWorldSize, tuning.DomeXOffset, tuning.DomeHeightOffset);
                }

                SceneView.RepaintAll();
            }
        }
    }
}
#endif
