#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class TimelineRebuildMenu
    {
        [MenuItem("Fractured Chorus/Rebuild Timeline + Skill Panel (Hierarchy)")]
        public static void RebuildUiHierarchy()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Fractured Chorus", "Không tìm thấy Canvas trong scene.", "OK");
                return;
            }

            var timeline = TimelineHierarchyBuilder.BuildTimeline(canvas.transform);
            var skillPanel = TimelineHierarchyBuilder.BuildSkillPanel(canvas.transform);

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                SetRef(bootstrap, "timelineView", timeline);
                SetRef(bootstrap, "skillPanelView", skillPanel);
            }

            var controller = Object.FindAnyObjectByType<CombatController>();
            if (controller != null)
            {
                SetRef(controller, "timelineView", timeline);
                SetRef(controller, "skillPanelView", skillPanel);
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = timeline.gameObject;
            Debug.Log("[Fractured Chorus] Rebuilt BeatTimelineUI (128 slots) + SkillPanelUI in Hierarchy. Save scene.");
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
