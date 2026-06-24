#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Core;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class TimelineRebuildMenu
    {
        [MenuItem("Fractured Chorus/Fix Execute Overlay (Remove Missing Scripts)")]
        public static void FixExecuteOverlayMissingScripts()
        {
            var overlayGo = GameObject.Find("CombatCanvas/ExecuteOverlayUI") ?? GameObject.Find("ExecuteOverlayUI");
            if (overlayGo == null)
            {
                EditorUtility.DisplayDialog("Fractured Chorus", "Không tìm thấy ExecuteOverlayUI trong scene.", "OK");
                return;
            }

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(overlayGo);

            var buttonGo = overlayGo.transform.Find("ExecuteButton")?.gameObject;
            if (buttonGo != null)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(buttonGo);
            }

            var views = overlayGo.GetComponents<CombatExecuteOverlayUIView>();
            for (var i = views.Length - 1; i > 0; i--)
            {
                Undo.DestroyObjectImmediate(views[i]);
            }

            var view = overlayGo.GetComponent<CombatExecuteOverlayUIView>();
            if (view == null)
            {
                view = Undo.AddComponent<CombatExecuteOverlayUIView>(overlayGo);
            }

            var button = overlayGo.transform.Find("ExecuteButton")?.GetComponent<Button>();
            if (button == null && buttonGo != null)
            {
                var image = buttonGo.GetComponent<Image>() ?? Undo.AddComponent<Image>(buttonGo);
                image.color = new Color(0.35f, 0.15f, 0.55f, 0.95f);
                image.raycastTarget = true;
                button = Undo.AddComponent<Button>(buttonGo);
                button.targetGraphic = image;
            }

            var label = button != null ? button.GetComponentInChildren<Text>(true) : null;

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                SetRef(bootstrap, "executeOverlay", view);
            }

            var controller = Object.FindAnyObjectByType<CombatController>();
            if (controller != null)
            {
                SetRef(controller, "executeOverlay", view);
            }

            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("executeButton").objectReferenceValue = button;
            viewSo.FindProperty("labelText").objectReferenceValue = label;
            viewSo.FindProperty("combatController").objectReferenceValue = controller;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            if (button != null && controller != null)
            {
                for (var i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                {
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
                }

                UnityEventTools.AddPersistentListener(button.onClick, controller.StartRound);
                EditorUtility.SetDirty(button);
            }

            view.WireReferences();

            EditorSceneManager.MarkSceneDirty(overlayGo.scene);
            Selection.activeGameObject = overlayGo;
            Debug.Log("[Fractured Chorus] ExecuteOverlayUI fixed. Save scene (Ctrl+S).");
        }

        [MenuItem("Fractured Chorus/Add Execute Overlay (Hierarchy)")]
        public static void AddExecuteOverlayToScene()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Fractured Chorus", "Không tìm thấy Canvas trong scene.", "OK");
                return;
            }

            var existing = canvas.transform.Find("ExecuteOverlayUI");
            CombatExecuteOverlayUIView executeOverlay;
            if (existing != null)
            {
                executeOverlay = existing.GetComponent<CombatExecuteOverlayUIView>();
                if (executeOverlay == null)
                {
                    executeOverlay = Undo.AddComponent<CombatExecuteOverlayUIView>(existing.gameObject);
                }
            }
            else
            {
                executeOverlay = TimelineHierarchyBuilder.BuildExecuteOverlay(canvas.transform);
            }

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                SetRef(bootstrap, "executeOverlay", executeOverlay);
            }

            var controller = Object.FindAnyObjectByType<CombatController>();
            if (controller != null)
            {
                SetRef(controller, "executeOverlay", executeOverlay);
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = executeOverlay.gameObject;
            Debug.Log("[Fractured Chorus] ExecuteOverlayUI ready in Hierarchy. Adjust RectTransform in Inspector, then Save scene.");
        }

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
            var executeOverlay = TimelineHierarchyBuilder.BuildExecuteOverlay(canvas.transform);

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                SetRef(bootstrap, "timelineView", timeline);
                SetRef(bootstrap, "skillPanelView", skillPanel);
                SetRef(bootstrap, "executeOverlay", executeOverlay);
            }

            var controller = Object.FindAnyObjectByType<CombatController>();
            if (controller != null)
            {
                SetRef(controller, "timelineView", timeline);
                SetRef(controller, "skillPanelView", skillPanel);
                SetRef(controller, "executeOverlay", executeOverlay);
            }

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Selection.activeGameObject = executeOverlay.gameObject;
            Debug.Log("[Fractured Chorus] Rebuilt BeatTimelineUI + SkillPanelUI + ExecuteOverlayUI. Save scene.");
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
