#if UNITY_EDITOR
using FracturedChorus.Audio;
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Presentation;
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    /// <summary>
    /// Edit Preview trên CombatRoot.
    /// Khi thêm UI combat mới: thêm foldout + Ping/Select trong OnInspectorGUI (mục Edit Preview).
    /// </summary>
    [CustomEditor(typeof(CombatPrototypeBootstrap))]
    public class CombatPrototypeBootstrapEditor : UnityEditor.Editor
    {
        private static bool _showRawRefs = false;
        private static bool _foldDeployExecute = true;
        private static bool _foldPerfect = true;
        private static bool _foldCounterFeel = false;
        private static bool _foldTimeline = false;
        private static bool _foldSkillPanel = false;
        private static bool _foldPartyEnemy = false;
        private static bool _foldAudio = false;

        public override void OnInspectorGUI()
        {
            var bootstrap = (CombatPrototypeBootstrap)target;
            serializedObject.Update();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Edit Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Chỉnh layout/UI combat tại đây. Ping = highlight Hierarchy. " +
                "Thêm UI mới → cập nhật foldout trong CombatPrototypeBootstrapEditor.",
                MessageType.Info);

            DrawDeployExecute(bootstrap);
            DrawPerfect(bootstrap);
            DrawCounterFeel(bootstrap);
            DrawTimeline(bootstrap);
            DrawSkillPanel(bootstrap);
            DrawPartyEnemy(bootstrap);
            DrawAudio(bootstrap);

            EditorGUILayout.Space(10f);
            _showRawRefs = EditorGUILayout.Foldout(_showRawRefs, "Bootstrap refs (raw)", true);
            if (_showRawRefs)
            {
                DrawDefaultInspector();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawDeployExecute(CombatPrototypeBootstrap bootstrap)
        {
            _foldDeployExecute = EditorGUILayout.Foldout(_foldDeployExecute, "Deploy / Execute button", true);
            if (!_foldDeployExecute)
            {
                return;
            }

            EditorGUI.indentLevel++;
            var overlayProp = serializedObject.FindProperty("executeOverlay");
            EditorGUILayout.PropertyField(overlayProp, new GUIContent("Overlay"));

            var overlay = overlayProp.objectReferenceValue as CombatExecuteOverlayUIView;
            DrawPingRow(overlay != null ? overlay.gameObject : null, "Select ExecuteOverlayUI");

            if (overlay != null)
            {
                var so = new SerializedObject(overlay);
                so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("buttonSize"));
                EditorGUILayout.PropertyField(so.FindProperty("buttonAnchoredPosition"));
                EditorGUILayout.PropertyField(so.FindProperty("deploySprite"));
                EditorGUILayout.PropertyField(so.FindProperty("executeSprite"));
                EditorGUILayout.PropertyField(so.FindProperty("hideLabelWhenUsingSprites"));
                if (so.ApplyModifiedProperties())
                {
                    overlay.ApplyLayout();
                    overlay.WireReferences();
                    EditorUtility.SetDirty(overlay);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Chưa gán ExecuteOverlayUI.", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPerfect(CombatPrototypeBootstrap bootstrap)
        {
            _foldPerfect = EditorGUILayout.Foldout(_foldPerfect, "Perfect popup", true);
            if (!_foldPerfect)
            {
                return;
            }

            EditorGUI.indentLevel++;
            var driverProp = serializedObject.FindProperty("counterPresentation");
            EditorGUILayout.PropertyField(driverProp, new GUIContent("Counter Presentation"));

            var driver = driverProp.objectReferenceValue as CounterPresentationDriver;
            DrawPingRow(driver != null ? driver.gameObject : null, "Select CounterPresentationDriver");

            if (driver != null)
            {
                var so = new SerializedObject(driver);
                so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("perfectChipSize"), new GUIContent("Chip Size"));
                EditorGUILayout.PropertyField(so.FindProperty("perfectChipDuration"), new GUIContent("Duration"));
                if (so.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(driver);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Gán / Add CounterPresentationDriver trên CombatRoot.", MessageType.Warning);
            }

            EditorGUILayout.LabelField("Sprite", "Resources/UI/Combat/combat_perfect_popup_v1");
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCounterFeel(CombatPrototypeBootstrap bootstrap)
        {
            _foldCounterFeel = EditorGUILayout.Foldout(_foldCounterFeel, "Counter feel (dense notes)", true);
            if (!_foldCounterFeel)
            {
                return;
            }

            EditorGUI.indentLevel++;
            var driver = serializedObject.FindProperty("counterPresentation").objectReferenceValue as CounterPresentationDriver;
            if (driver != null)
            {
                var so = new SerializedObject(driver);
                so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("restartGapSec"));
                EditorGUILayout.PropertyField(so.FindProperty("burstWindowSec"));
                EditorGUILayout.PropertyField(so.FindProperty("burstCount"));
                so.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("Cần CounterPresentationDriver.", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawTimeline(CombatPrototypeBootstrap bootstrap)
        {
            _foldTimeline = EditorGUILayout.Foldout(_foldTimeline, "Beat Timeline", true);
            if (!_foldTimeline)
            {
                return;
            }

            EditorGUI.indentLevel++;
            var prop = serializedObject.FindProperty("timelineView");
            EditorGUILayout.PropertyField(prop);
            var view = prop.objectReferenceValue as BeatTimelineUIView;
            DrawPingRow(view != null ? view.gameObject : null, "Select BeatTimelineUI");

            if (view != null)
            {
                var so = new SerializedObject(view);
                so.Update();
                var catalog = so.FindProperty("noteVisuals");
                if (catalog != null)
                {
                    EditorGUILayout.LabelField("Note visuals", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NoteRed"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NoteBlue"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NotePurple"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("DropGhostValid"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("DropGhostInvalid"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("CoverPerfect"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("CoverMiss"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("BeatFrameEmpty"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("BeatFrameImpact"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("BeatFrameWindup"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NoteDisplaySize"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NoteDisplayWidth"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NoteDisplayHeight"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NoteAlpha"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("CoverPerfectAlpha"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NoteRedSizeScale"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NoteBlueSizeScale"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("NotePurpleSizeScale"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("GhostDisplaySize"));
                    EditorGUILayout.PropertyField(catalog.FindPropertyRelative("CoverDisplaySize"));
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Band layout (Approach A)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(so.FindProperty("noteBandNormalizedY"), new GUIContent("Note Band Y (legacy)"));
                EditorGUILayout.PropertyField(so.FindProperty("bossNoteRailAnchoredY"), new GUIContent("Note Rail Y (BorderTop)"));
                EditorGUILayout.PropertyField(so.FindProperty("laneGapBelowRail"), new GUIContent("Lane Gap Below Rail"));
                EditorGUILayout.PropertyField(so.FindProperty("laneBandMinNormalizedY"), new GUIContent("Lane Band Min Y"));
                EditorGUILayout.PropertyField(so.FindProperty("laneBandMaxNormalizedY"), new GUIContent("Lane Band Max Y (legacy)"));
                EditorGUILayout.PropertyField(so.FindProperty("bossTrackFrameBorderTop"), new GUIContent("Boss Border Top (Holo)"));
                EditorGUILayout.PropertyField(so.FindProperty("bossTrackFrameBorderThickness"), new GUIContent("Boss Track Border Thickness"));
                EditorGUILayout.PropertyField(so.FindProperty("timelineStaffBackground"), new GUIContent("Staff Background"));
                EditorGUILayout.PropertyField(so.FindProperty("timelineStaffBackgroundAlpha"), new GUIContent("Staff BG Alpha"));

                if (so.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(view);
                }

                EditorGUILayout.Space(4f);
                if (GUILayout.Button("Seed Lane Preview (Hierarchy)"))
                {
                    if (Application.isPlaying)
                    {
                        EditorUtility.DisplayDialog(
                            "Fractured Chorus",
                            "Exit Play Mode trước khi seed Hierarchy preview.",
                            "OK");
                    }
                    else if (TimelineHierarchyBuilder.SeedTimelineLanePreview(view))
                    {
                        EditorUtility.SetDirty(view);
                    }
                }

                EditorGUILayout.HelpBox(
                    "Staff BG = thanh nhạc hologram. Note rail = BossTrackFrame/BorderTop (bụng nốt neo vào đây).\n" +
                    "Seed từ UnitPresetSO: Lane_*/LaneAvatar_* + BorderTop + Beat_1 + NoteSingle_1 (Remaining Hits). Play bind preset, giữ Y scene.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Chưa gán BeatTimelineUI.", MessageType.Warning);
            }

            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSkillPanel(CombatPrototypeBootstrap bootstrap)
        {
            _foldSkillPanel = EditorGUILayout.Foldout(_foldSkillPanel, "Skill Panel", true);
            if (!_foldSkillPanel)
            {
                return;
            }

            EditorGUI.indentLevel++;
            var prop = serializedObject.FindProperty("skillPanelView");
            EditorGUILayout.PropertyField(prop);
            var view = prop.objectReferenceValue as SkillPanelUIView;
            DrawPingRow(view != null ? view.gameObject : null, "Select SkillPanelUI");
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPartyEnemy(CombatPrototypeBootstrap bootstrap)
        {
            _foldPartyEnemy = EditorGUILayout.Foldout(_foldPartyEnemy, "Party / Enemy status", true);
            if (!_foldPartyEnemy)
            {
                return;
            }

            EditorGUI.indentLevel++;
            var partyProp = serializedObject.FindProperty("partyStatusBarView");
            var enemyProp = serializedObject.FindProperty("enemyStatusBarView");
            EditorGUILayout.PropertyField(partyProp);
            var party = partyProp.objectReferenceValue as PartyStatusBarUIView;
            DrawPingRow(party != null ? party.gameObject : null, "Select PartyStatusBarUI");

            EditorGUILayout.PropertyField(enemyProp);
            var enemy = enemyProp.objectReferenceValue as EnemyStatusBarUIView;
            DrawPingRow(enemy != null ? enemy.gameObject : null, "Select EnemyStatusBarUI");
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAudio(CombatPrototypeBootstrap bootstrap)
        {
            _foldAudio = EditorGUILayout.Foldout(_foldAudio, "Music / SFX", true);
            if (!_foldAudio)
            {
                return;
            }

            EditorGUI.indentLevel++;
            var musicProp = serializedObject.FindProperty("musicController");
            var sfxProp = serializedObject.FindProperty("combatSfxController");
            EditorGUILayout.PropertyField(musicProp);
            var music = musicProp.objectReferenceValue as CombatMusicController;
            DrawPingRow(music != null ? music.gameObject : null, "Select CombatMusic");

            EditorGUILayout.PropertyField(sfxProp);
            var sfx = sfxProp.objectReferenceValue as CombatSfxController;
            DrawPingRow(sfx != null ? sfx.gameObject : null, "Select CombatSfx");
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawPingRow(GameObject go, string selectLabel)
        {
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(go == null))
            {
                if (GUILayout.Button("Ping", GUILayout.Width(48f)))
                {
                    EditorGUIUtility.PingObject(go);
                }

                if (GUILayout.Button(selectLabel))
                {
                    Selection.activeGameObject = go;
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
