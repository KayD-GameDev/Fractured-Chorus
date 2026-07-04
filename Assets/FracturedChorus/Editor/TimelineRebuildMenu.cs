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
                EditorUtility.DisplayDialog("Fractured Chorus", "ExecuteOverlayUI not found in scene.", "OK");
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
            var canvasTransform = CombatUiHierarchy.ResolveCombatCanvasTransform();
            if (canvasTransform == null)
            {
                EditorUtility.DisplayDialog("Fractured Chorus", "CombatCanvas not found in scene.", "OK");
                return;
            }

            var existing = canvasTransform.Find("ExecuteOverlayUI");
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
                executeOverlay = TimelineHierarchyBuilder.BuildExecuteOverlay(canvasTransform);
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

            EditorSceneManager.MarkSceneDirty(canvasTransform.gameObject.scene);
            Selection.activeGameObject = executeOverlay.gameObject;
            Debug.Log("[Fractured Chorus] ExecuteOverlayUI ready in Hierarchy. Adjust RectTransform in Inspector, then Save scene.");
        }

        [MenuItem("Fractured Chorus/Rebuild Timeline + Skill Panel (Hierarchy)")]
        public static void RebuildUiHierarchy()
        {
            var canvasTransform = CombatUiHierarchy.ResolveCombatCanvasTransform();
            if (canvasTransform == null)
            {
                EditorUtility.DisplayDialog("Fractured Chorus", "CombatCanvas not found in scene.", "OK");
                return;
            }

            var timeline = TimelineHierarchyBuilder.BuildTimeline(canvasTransform);
            var skillPanel = TimelineHierarchyBuilder.BuildSkillPanel(canvasTransform);
            var partyBar = TimelineHierarchyBuilder.BuildPartyStatusBar(canvasTransform);
            var executeOverlay = TimelineHierarchyBuilder.BuildExecuteOverlay(canvasTransform);

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                SetRef(bootstrap, "timelineView", timeline);
                SetRef(bootstrap, "skillPanelView", skillPanel);
                SetRef(bootstrap, "partyStatusBarView", partyBar);
                SetRef(bootstrap, "executeOverlay", executeOverlay);
            }

            var controller = Object.FindAnyObjectByType<CombatController>();
            if (controller != null)
            {
                SetRef(controller, "timelineView", timeline);
                SetRef(controller, "skillPanelView", skillPanel);
                SetRef(controller, "executeOverlay", executeOverlay);
            }

            EditorSceneManager.MarkSceneDirty(canvasTransform.gameObject.scene);
            Selection.activeGameObject = executeOverlay.gameObject;
            Debug.Log("[Fractured Chorus] Rebuilt BeatTimelineUI + SkillPanelUI + PartyStatusBarUI + ExecuteOverlayUI. Save scene.");
        }

        [MenuItem("Fractured Chorus/Upgrade Party Card Template (Hierarchy)")]
        public static void UpgradePartyCardTemplate()
        {
            CombatUiHierarchy.UpgradePartyCardTemplatesInScene();
        }

        [MenuItem("Fractured Chorus/Fix Party Status Bar (Move to CombatCanvas)")]
        public static void FixPartyStatusBarPlacement()
        {
            CombatUiHierarchy.FixPartyStatusBarPlacement();
        }

        [MenuItem("Fractured Chorus/Find Missing Scripts (Active Scene)")]
        public static void FindMissingScriptsInActiveScene()
        {
            CombatUiHierarchy.LogMissingScriptsInActiveScene();
        }

        [MenuItem("Fractured Chorus/Remove Missing Scripts (Active Scene)")]
        public static void RemoveMissingScriptsInActiveScene()
        {
            CombatUiHierarchy.RemoveMissingScriptsInActiveScene();
        }

        [MenuItem("Fractured Chorus/Add Party Status Bar (Hierarchy)")]
        public static void AddPartyStatusBarToScene()
        {
            CombatUiHierarchy.RenameBackgroundCanvasInScene();

            var canvasTransform = CombatUiHierarchy.ResolveCombatCanvasTransform();
            if (canvasTransform == null)
            {
                EditorUtility.DisplayDialog("Fractured Chorus", "CombatCanvas not found in scene.", "OK");
                return;
            }

            var partyBar = TimelineHierarchyBuilder.BuildPartyStatusBar(canvasTransform);
            CombatUiHierarchy.EnsurePartyCardsInHierarchy();
            ElementBadgeIconSetup.ApplyToStatBlocks();

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                SetRef(bootstrap, "partyStatusBarView", partyBar);
            }

            EditorSceneManager.MarkSceneDirty(canvasTransform.gameObject.scene);
            Selection.activeGameObject = partyBar.gameObject;
            Debug.Log("[Fractured Chorus] PartyStatusBarUI ready under CombatCanvas (top-left). Save scene.");
        }

        [MenuItem("Fractured Chorus/Setup Party Cards in Hierarchy")]
        public static void SetupPartyCardsInHierarchy()
        {
            CombatUiHierarchy.EnsurePartyCardsInHierarchy();
            ElementBadgeIconSetup.ApplyToStatBlocks();
        }

        [MenuItem("Fractured Chorus/Add Enemy Status Bar (Hierarchy)")]
        public static void AddEnemyStatusBarToScene()
        {
            CombatUiHierarchy.AddEnemyStatusBarToScene();
            ElementBadgeIconSetup.ApplyToStatBlocks();
        }

        [MenuItem("Fractured Chorus/Setup Enemy Cards in Hierarchy")]
        public static void SetupEnemyCardsInHierarchy()
        {
            CombatUiHierarchy.EnsureEnemyCardsInHierarchy();
            ElementBadgeIconSetup.ApplyToStatBlocks();
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
