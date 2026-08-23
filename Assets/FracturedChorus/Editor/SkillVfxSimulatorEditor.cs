#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Editor
{
    public static class SkillVfxSimulatorMenu
    {
        [MenuItem("Fractured Chorus/VFX/Open Skill Simulator")]
        public static void OpenSimulator()
        {
            EnsureCombatSceneLoaded();
            AddToAllUnits();
            var sim = FindPreferredSimulator();
            if (sim == null)
            {
                sim = EnsureStandaloneSimulator();
            }

            sim.SetPreviewLive(true);
            sim.gameObject.SetActive(true);
            Selection.activeGameObject = sim.gameObject;
            EditorGUIUtility.PingObject(sim.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log(
                "[SkillVfx] Simulator trên unit đã sẵn sàng. Bật Preview Live, kéo A/B và scale → Save To Profile.");
        }

        [MenuItem("Fractured Chorus/VFX/Add Skill Simulator To All Units")]
        public static void AddToAllUnits()
        {
            var views = Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include);
            var added = 0;
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (view == null)
                {
                    continue;
                }

                Undo.RecordObject(view.gameObject, "Add Skill VFX Simulator");
                if (SkillVfxSimulator.EnsureOn(view) != null)
                {
                    view.EnsureProjectile();
                    view.EnsureReceiveDmg();
                    added++;
                }

                EditorUtility.SetDirty(view.gameObject);
            }

            if (views.Length > 0)
            {
                EditorSceneManager.MarkSceneDirty(views[0].gameObject.scene);
            }

            Debug.Log($"[SkillVfx] SkillVfxSimulator trên {added} unit.");
        }

        [MenuItem("Fractured Chorus/VFX/Save Skill Simulator To Profile")]
        public static void SaveFromSimulator()
        {
            var sim = Object.FindAnyObjectByType<SkillVfxSimulator>(FindObjectsInactive.Include);
            if (sim == null)
            {
                EditorUtility.DisplayDialog(
                    "Skill VFX",
                    "Chưa có Skill Simulator. Chạy Open Skill Simulator trước.",
                    "OK");
                return;
            }

            if (sim.Profile == null)
            {
                EditorUtility.DisplayDialog(
                    "Skill VFX",
                    "Skill chưa có SkillVfxProfileSO. Gán profile hoặc bấm Create Profile For Skill.",
                    "OK");
                return;
            }

            Undo.RecordObject(sim.Profile, "Save Skill VFX Profile");
            sim.SaveToProfile();
            EditorSceneManager.MarkSceneDirty(sim.gameObject.scene);
            EditorSceneManager.SaveScene(sim.gameObject.scene);
        }

        [MenuItem("Fractured Chorus/VFX/Save & Hide Skill Simulator")]
        public static void SaveAndHide()
        {
            var sim = Object.FindAnyObjectByType<SkillVfxSimulator>(FindObjectsInactive.Include);
            if (sim == null)
            {
                Debug.LogWarning("[SkillVfx] Không tìm thấy simulator để Save & Hide.");
                return;
            }

            if (sim.Profile != null)
            {
                Undo.RecordObject(sim.Profile, "Save Skill VFX Profile");
                sim.SaveToProfile();
            }

            Undo.RecordObject(sim.gameObject, "Hide Skill VFX Simulator");
            sim.gameObject.SetActive(false);
            EditorUtility.SetDirty(sim.gameObject);
            EditorSceneManager.MarkSceneDirty(sim.gameObject.scene);
            EditorSceneManager.SaveScene(sim.gameObject.scene);
        }

        [MenuItem("Fractured Chorus/VFX/Clear Skill Simulator")]
        public static void ClearSimulator()
        {
            var sim = Object.FindAnyObjectByType<SkillVfxSimulator>(FindObjectsInactive.Include);
            if (sim != null)
            {
                Undo.DestroyObjectImmediate(sim.gameObject);
            }
        }

        internal static SkillVfxSimulator FindPreferredSimulator()
        {
            var despair = ResolveDespair();
            if (despair != null)
            {
                var onDespair = despair.GetComponent<SkillVfxSimulator>();
                if (onDespair != null)
                {
                    return onDespair;
                }
            }

            return Object.FindAnyObjectByType<SkillVfxSimulator>(FindObjectsInactive.Include);
        }

        internal static SkillVfxSimulator EnsureStandaloneSimulator()
        {
            var existing = Object.FindAnyObjectByType<SkillVfxSimulator>(FindObjectsInactive.Include);
            if (existing != null)
            {
                BindDefaults(existing);
                existing.SetPreviewLive(true);
                return existing;
            }

            var root = GameObject.Find("CombatRoot");
            if (root == null)
            {
                root = new GameObject("CombatRoot");
                Undo.RegisterCreatedObjectUndo(root, "Create CombatRoot");
            }

            var go = new GameObject(SkillVfxSimulator.PreviewName);
            Undo.RegisterCreatedObjectUndo(go, "Create Skill VFX Simulator");
            go.transform.SetParent(root.transform, false);
            var sim = go.AddComponent<SkillVfxSimulator>();
            BindDefaults(sim);
            sim.SetPreviewLive(true);
            return sim;
        }

        private static void BindDefaults(SkillVfxSimulator sim)
        {
            var skill = sim.Skill
                        ?? Resources.Load<SkillDefinitionSO>("Skills/boss_despair_core");
            var caster = sim.Caster ?? ResolveDespair();
            var target = ResolveParty();
            sim.SetPreviewLive(true);
            sim.Bind(skill, caster, target);
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

        private static UnitView ResolveDespair()
        {
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Exclude))
            {
                var id = view.Unit != null ? view.Unit.UnitId ?? string.Empty : string.Empty;
                var key = view.DemoUnitKey ?? string.Empty;
                var n = view.name ?? string.Empty;
                if (id == "boss_despair"
                    || key == "boss_despair"
                    || n.IndexOf("Despair", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Knight", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return view;
                }
            }

            var named = GameObject.Find("Unit_Knight of Despair") ?? GameObject.Find("Unit_Boss");
            return named != null ? named.GetComponent<UnitView>() : null;
        }

        private static UnitView ResolveParty()
        {
            foreach (var view in Object.FindObjectsByType<UnitView>(FindObjectsInactive.Exclude))
            {
                if (view.Side == FracturedChorus.Combat.Grid.GridSide.Player)
                {
                    return view;
                }
            }

            var named = GameObject.Find("Unit_Ren") ?? GameObject.Find("Unit_Charlotte");
            return named != null ? named.GetComponent<UnitView>() : null;
        }
    }

    [CustomEditor(typeof(SkillVfxSimulator))]
    public sealed class SkillVfxSimulatorEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var sim = (SkillVfxSimulator)target;
            var profile = sim.Profile;
            if (profile == null)
            {
                return;
            }

            if (!sim.ShowPattern && !sim.ShowSprite)
            {
                return;
            }

            Handles.color = new Color(0.35f, 0.85f, 1f, 0.95f);
            Handles.Label(sim.FromWorld + Vector3.up * 0.15f, "A VFX");
            EditorGUI.BeginChangeCheck();
            var nextA = Handles.PositionHandle(sim.FromWorld, sim.CurrentProjectileWorldRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sim, "Move VFX A");
                if (profile != null)
                {
                    Undo.RecordObject(profile, "Move VFX A");
                }

                sim.SetVfxFromWorld(nextA);
                EditorUtility.SetDirty(sim);
                if (profile != null)
                {
                    EditorUtility.SetDirty(profile);
                }
            }

            EditorGUI.BeginChangeCheck();
            var nextRot = Handles.RotationHandle(sim.CurrentProjectileWorldRotation, sim.FromWorld);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sim, "Rotate VFX Projectile");
                if (profile != null)
                {
                    Undo.RecordObject(profile, "Rotate VFX Projectile");
                }

                sim.SetProjectileWorldRotation(nextRot);
                EditorUtility.SetDirty(sim);
                if (profile != null)
                {
                    EditorUtility.SetDirty(profile);
                }
            }

            var opponents = sim.Opponents;
            if (opponents == null || opponents.Count == 0)
            {
                Handles.DrawLine(sim.FromWorld, sim.ToWorld);
                Handles.Label(sim.ToWorld + Vector3.up * 0.15f, "B VFX");
                DrawToHandle(sim, profile, null, sim.ToWorld);
                return;
            }

            for (var i = 0; i < opponents.Count; i++)
            {
                var view = opponents[i];
                if (view == null)
                {
                    continue;
                }

                var to = SkillVfxAnchorResolver.ResolveAnchorDestination(view) + sim.ToOffset;
                Handles.DrawLine(sim.FromWorld, to);
                Handles.Label(to + Vector3.up * 0.15f, "B " + view.name);
                DrawToHandle(sim, profile, view, to);
            }
        }

        private static void DrawToHandle(
            SkillVfxSimulator sim,
            SkillVfxProfileSO profile,
            UnitView target,
            Vector3 world)
        {
            EditorGUI.BeginChangeCheck();
            var next = Handles.PositionHandle(world, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            Undo.RecordObject(sim, "Move VFX B");
            if (profile != null)
            {
                Undo.RecordObject(profile, "Move VFX B");
            }

            sim.SetVfxToWorld(target, next);
            EditorUtility.SetDirty(sim);
            if (profile != null)
            {
                EditorUtility.SetDirty(profile);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var sim = (SkillVfxSimulator)target;
            EditorGUILayout.HelpBox(
                "Show Sprite = projectile đứng tại A.\n" +
                "Show Impact = impact trên ReceiveDmg của mọi target (Knight đang ẩn impact).\n" +
                "Play = bay A→B. Tới B = ẩn projectile và đánh dấu contact.\n" +
                "A = Projectile. B = ReceiveDmg từng target.",
                MessageType.Info);

            DrawSkillTabs(sim);
            if (sim.PreviewLive)
            {
                EditorGUILayout.HelpBox(sim.PoseBindingLabel, MessageType.None);
            }

            DrawPlayPauseButtons(sim);
            if (sim.Profile != null)
            {
                DrawVisibilityToggles(sim);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("skill"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("profileOverride"));

            DrawHierarchyRefs(sim);
            DrawDefaultInspectorMinusKnown();
            serializedObject.ApplyModifiedProperties();

            var profile = sim.Profile;
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Pattern", EditorStyles.boldLabel);
            DrawKindPatternTicks(sim, profile);
            profile = sim.Profile;
            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Skill chưa có VFX profile. Tick một loại Pattern hoặc bấm Create Profile For Skill.",
                    MessageType.Warning);
                if (sim.Skill != null && GUILayout.Button("Create Profile For Skill", GUILayout.Height(26f)))
                {
                    CreateProfileForSkill(sim);
                }

                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Active Profile", profile, typeof(SkillVfxProfileSO), false);
            }

            EditorGUILayout.HelpBox(KindHelp(profile.kind, profile.HasPattern), MessageType.None);
            if (profile.hideImpactSprite)
            {
                EditorGUILayout.HelpBox(
                    sim.ArrivedAtB
                        ? "Impact ẩn. Projectile đã tới B — contact."
                        : "Impact ẩn. Contact khi projectile chạy tới B.",
                    MessageType.None);
            }

            EditorGUI.BeginChangeCheck();
            var sizeLabel = profile.kind == SkillVfxKind.Hit ? "Impact World Size" : "Projectile World Size";
            var nextSize = EditorGUILayout.FloatField(sizeLabel, sim.CurrentProjectileWorldSize);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(profile, "Set Skill VFX Size");
                Undo.RecordObject(sim, "Set Skill VFX Size");
                sim.SetUniformProjectileScale(nextSize);
                EditorUtility.SetDirty(profile);
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            var nextEuler = EditorGUILayout.Vector3Field("Projectile Rotation", sim.ProjectileEuler);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(profile, "Set Projectile Rotation");
                Undo.RecordObject(sim, "Set Projectile Rotation");
                sim.SetProjectileEuler(nextEuler);
                EditorUtility.SetDirty(profile);
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Vector3Field("Saved From Offset", sim.FromOffset);
                EditorGUILayout.Vector3Field("Saved To Offset", sim.ToOffset);
            }

            EditorGUILayout.HelpBox(
                $"Kind {profile.kind}  pattern={(profile.HasPattern ? "on" : "off")}\n" +
                $"A {sim.FromWorld}  →  B {sim.ToWorld}",
                MessageType.None);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Save To Profile", GUILayout.Height(28f)))
            {
                Undo.RecordObject(profile, "Save Skill VFX Profile");
                sim.SaveToProfile();
                EditorSceneManager.MarkSceneDirty(sim.gameObject.scene);
            }

            if (GUILayout.Button("Save & Hide", GUILayout.Height(26f)))
            {
                SkillVfxSimulatorMenu.SaveAndHide();
            }
        }

        private void DrawPlayPauseButtons(SkillVfxSimulator sim)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var icon = sim.IsPlaying ? PauseIcon() : PlayIcon();
            if (GUILayout.Button(icon, GUILayout.Width(40f), GUILayout.Height(28f)))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(sim, "Toggle Skill VFX Play");
                sim.TogglePlayPause();
                serializedObject.Update();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static GUIContent PlayIcon()
        {
            var content = EditorGUIUtility.IconContent("PlayButton");
            if (content == null || content.image == null)
            {
                content = EditorGUIUtility.IconContent("Animation.Play");
            }

            return new GUIContent(content.image, "Play");
        }

        private static GUIContent PauseIcon()
        {
            var content = EditorGUIUtility.IconContent("PauseButton");
            if (content == null || content.image == null)
            {
                content = EditorGUIUtility.IconContent("PauseButton On");
            }

            return new GUIContent(content.image, "Pause");
        }

        private void DrawVisibilityToggles(SkillVfxSimulator sim)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var nextSprite = EditorGUILayout.Toggle("Show Sprite", sim.ShowSprite);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(sim, "Toggle VFX Sprite");
                sim.SetShowSprite(nextSprite);
                serializedObject.Update();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            var nextImpact = EditorGUILayout.Toggle("Show Impact", sim.ShowImpact);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(sim, "Toggle VFX Impact");
                sim.SetShowImpact(nextImpact);
                serializedObject.Update();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }

            EditorGUI.BeginChangeCheck();
            var nextPattern = EditorGUILayout.Toggle("Show Pattern", sim.ShowPattern);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(sim, "Toggle VFX Pattern");
                sim.SetShowPattern(nextPattern);
                serializedObject.Update();
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }
        }

        private static void DrawSkillTabs(SkillVfxSimulator sim)
        {
            var tabs = sim.CollectPreviewSkills();
            if (tabs.Length == 0)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Skill Tabs", EditorStyles.boldLabel);
            var labels = new string[tabs.Length];
            for (var i = 0; i < tabs.Length; i++)
            {
                labels[i] = SkillVfxSimulator.SkillTabLabel(tabs[i]);
            }

            var current = sim.CurrentSkillTabIndex;
            var next = GUILayout.Toolbar(current, labels);
            if (next != current && next >= 0 && next < tabs.Length)
            {
                Undo.RecordObject(sim, "Switch Skill VFX Tab");
                sim.SetSkill(tabs[next]);
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
            }
        }

        private void DrawHierarchyRefs(SkillVfxSimulator sim)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Hierarchy", EditorStyles.boldLabel);
            var hosted = sim.GetComponent<UnitView>() != null;

            using (new EditorGUI.DisabledScope(hosted))
            {
                EditorGUI.BeginChangeCheck();
                var nextCaster = (UnitView)EditorGUILayout.ObjectField(
                    "Caster", sim.Caster, typeof(UnitView), true);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(sim, "Assign Caster");
                    sim.AssignCaster(nextCaster);
                    EditorUtility.SetDirty(sim);
                    sim.RefreshVisual(true);
                    SceneView.RepaintAll();
                }
            }

            EditorGUI.BeginChangeCheck();
            var nextA = (UnitProjectileAnchor)EditorGUILayout.ObjectField(
                "A Projectile", sim.ProjectileA, typeof(UnitProjectileAnchor), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(sim, "Assign Projectile A");
                sim.AssignProjectileA(nextA);
                EditorUtility.SetDirty(sim);
                sim.RefreshVisual(true);
                SceneView.RepaintAll();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("targets"), true);

            if (GUILayout.Button("Fill Opposite Side", GUILayout.Height(22f)))
            {
                Undo.RecordObject(sim, "Fill Opposite VFX Targets");
                sim.FillOppositeTargets();
                EditorUtility.SetDirty(sim);
                sim.RefreshVisual(true);
                SceneView.RepaintAll();
            }

            var opponents = sim.Opponents;
            if (opponents != null)
            {
                for (var i = 0; i < opponents.Count; i++)
                {
                    var view = opponents[i];
                    if (view == null)
                    {
                        continue;
                    }

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(
                            "B " + view.name,
                            view.ReceiveDmg,
                            typeof(UnitReceiveDmgAnchor),
                            true);
                    }
                }
            }
        }

        private void DrawKindPatternTicks(SkillVfxSimulator sim, SkillVfxProfileSO profile)
        {
            var kinds = new[]
            {
                SkillVfxKind.Projectile,
                SkillVfxKind.Buff,
                SkillVfxKind.SpellBurst,
                SkillVfxKind.Hit
            };

            for (var i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i];
                var on = profile != null && profile.kind == kind && profile.HasPattern;
                EditorGUI.BeginChangeCheck();
                var next = EditorGUILayout.ToggleLeft(kind + " Pattern", on);
                if (!EditorGUI.EndChangeCheck() || next == on)
                {
                    continue;
                }

                if (next && profile == null)
                {
                    profile = CreateProfileForSkill(sim);
                }

                if (profile == null)
                {
                    continue;
                }

                Undo.RecordObject(profile, "Set Skill VFX Pattern");
                Undo.RecordObject(sim, "Set Skill VFX Pattern");
                sim.SetKindHasPattern(kind, next);
                EditorUtility.SetDirty(profile);
                EditorUtility.SetDirty(sim);
                SceneView.RepaintAll();
                profile = sim.Profile;
            }
        }

        private static string KindHelp(SkillVfxKind kind, bool hasPattern)
        {
            if (!hasPattern)
            {
                return "Không pattern: sprite đứng tại vị trí đã lưu (A cho Buff/Projectile/SpellBurst, B cho Hit).";
            }

            return kind switch
            {
                SkillVfxKind.Projectile => "Bay từ A (Projectile + offset) tới B (ReceiveDmg + offset).",
                SkillVfxKind.Buff => "Đứng/fade tại A Projectile (mặc định), không bay.",
                SkillVfxKind.SpellBurst => "Sinh tại A, bung/bay tới B.",
                SkillVfxKind.Hit =>
                    "Đánh tại B ReceiveDmg khi đạt mốc clip (arriveAtB). Không bay. Impact trống thì không lấy sprite projectile.",
                _ => string.Empty
            };
        }

        private void DrawDefaultInspectorMinusKnown()
        {
            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "skill",
                "profileOverride",
                "previewLive",
                "previewPaused",
                "hideSprite",
                "hidePattern",
                "showImpact",
                "caster",
                "projectileA",
                "targets");
        }

        private static SkillVfxProfileSO CreateProfileForSkill(SkillVfxSimulator sim)
        {
            var skill = sim.Skill;
            if (skill == null)
            {
                return null;
            }

            var folder = "Assets/FracturedChorus/Resources/Skills/Vfx";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Resources/Skills", "Vfx");
            }

            var id = string.IsNullOrEmpty(skill.skillId) ? skill.name : skill.skillId;
            var path = $"{folder}/SkillVfx_{id}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<SkillVfxProfileSO>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<SkillVfxProfileSO>();
                profile.kind = SkillVfxKind.Projectile;
                profile.fromAnchor = SkillVfxAnchor.CasterHead;
                profile.toAnchor = SkillVfxAnchor.TargetBody;
                AssetDatabase.CreateAsset(profile, path);
            }

            Undo.RecordObject(skill, "Assign Skill VFX Profile");
            skill.vfxProfile = profile;
            EditorUtility.SetDirty(skill);
            EditorUtility.SetDirty(profile);
            sim.RefreshVisual(true);
            return profile;
        }
    }
}
#endif
