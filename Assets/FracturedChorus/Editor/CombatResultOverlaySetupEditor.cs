#if UNITY_EDITOR
using FracturedChorus.Combat.Core;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class CombatResultOverlaySetupEditor
    {
        private const string VictoryPath = "Assets/FracturedChorus/Art/UI/Combat/Result/combat_result_victory_v1.png";
        private const string DefeatPath = "Assets/FracturedChorus/Art/UI/Combat/Result/combat_result_defeat_v1.png";
        private const string ContinuePath = "Assets/FracturedChorus/Art/UI/Combat/Result/combat_btn_continue_v1.png";
        private const string RetryPath = "Assets/FracturedChorus/Art/UI/Combat/Result/combat_btn_retry_v1.png";

        [MenuItem("Fractured Chorus/Setup Combat Result Overlay (Current Scene)")]
        public static void SetupResultOverlay()
        {
            var canvas = CombatUiHierarchy.ResolveCombatCanvasTransform();
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] CombatCanvas not found. Open CombatPrototype first.");
                return;
            }

            var existing = canvas.GetComponentInChildren<CombatResultOverlayUIView>(true);
            CombatResultOverlayUIView view;
            if (existing != null)
            {
                view = existing;
                Undo.RecordObject(view.gameObject, "Refresh Combat Result Overlay");
            }
            else
            {
                var go = new GameObject("CombatResultOverlay", typeof(RectTransform), typeof(CombatResultOverlayUIView));
                Undo.RegisterCreatedObjectUndo(go, "Create Combat Result Overlay");
                go.transform.SetParent(canvas, false);
                view = go.GetComponent<CombatResultOverlayUIView>();
                view.BuildDefaultHierarchy();
            }

            view.WireSceneReferences();
            AssignSprites(view);
            view.ApplyDefaultSprites();

            var so = new SerializedObject(view);
            so.FindProperty("preserveSceneLayout").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            view.gameObject.SetActive(true);
            Selection.activeGameObject = view.gameObject;

            var controller = Object.FindAnyObjectByType<CombatController>();
            if (controller != null)
            {
                var cso = new SerializedObject(controller);
                cso.FindProperty("resultOverlay").objectReferenceValue = view;
                cso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }

            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log(
                "[Fractured Chorus] CombatResultOverlay sẵn sàng dưới CombatCanvas. " +
                "Chỉnh RectTransform Title / ContinueButton / RetryButton / Dimmer, rồi Save scene. " +
                "Tắt GameObject trước khi Play nếu muốn ẩn mặc định (runtime sẽ bật khi hết trận).");
        }

        [MenuItem("Fractured Chorus/Hide Combat Result Overlay")]
        public static void HideResultOverlay()
        {
            var view = Object.FindAnyObjectByType<CombatResultOverlayUIView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogWarning("[Fractured Chorus] Không thấy CombatResultOverlay.");
                return;
            }

            Undo.RecordObject(view.gameObject, "Hide Combat Result Overlay");
            view.gameObject.SetActive(false);
            EditorUtility.SetDirty(view.gameObject);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }

        [MenuItem("Fractured Chorus/Bring Combat Result Overlay To Front")]
        public static void BringResultOverlayToFront()
        {
            var view = Object.FindAnyObjectByType<CombatResultOverlayUIView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogWarning("[Fractured Chorus] Không thấy CombatResultOverlay.");
                return;
            }

            Undo.RecordObject(view.transform, "Bring Result Overlay To Front");
            Undo.RecordObject(view.gameObject, "Bring Result Overlay To Front");
            view.BringToFront();
            EditorUtility.SetDirty(view.gameObject);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Selection.activeGameObject = view.gameObject;
            Debug.Log("[Fractured Chorus] CombatResultOverlay → last sibling + Canvas sortOrder 500. Save scene.");
        }

        private static void AssignSprites(CombatResultOverlayUIView view)
        {
            var so = new SerializedObject(view);
            AssignSpriteProp(so, "victorySprite", VictoryPath);
            AssignSpriteProp(so, "defeatSprite", DefeatPath);
            AssignSpriteProp(so, "continueSprite", ContinuePath);
            AssignSpriteProp(so, "retrySprite", RetryPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSpriteProp(SerializedObject so, string prop, string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (tex != null)
                {
                    Debug.LogWarning(
                        $"[Fractured Chorus] '{assetPath}' chưa import Sprite. " +
                        "Inspector → Texture Type = Sprite (2D and UI) → Apply.");
                }

                return;
            }

            so.FindProperty(prop).objectReferenceValue = sprite;
        }
    }
}
#endif
