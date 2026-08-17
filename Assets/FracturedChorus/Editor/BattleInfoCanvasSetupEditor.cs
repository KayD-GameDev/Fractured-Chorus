#if UNITY_EDITOR
using FracturedChorus.Combat.Core;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class BattleInfoCanvasSetupEditor
    {
        private const string PlanningPath =
            "Assets/FracturedChorus/Resources/UI/Combat/Banners/combat_banner_planning_phase_v1.png";
        private const string BattleStartPath =
            "Assets/FracturedChorus/Resources/UI/Combat/Banners/combat_banner_battle_start_v1.png";

        [MenuItem("Fractured Chorus/Ensure Battle Info Canvas")]
        public static void EnsureBattleInfoCanvas()
        {
            var view = Object.FindAnyObjectByType<CombatPhaseBannerView>(FindObjectsInactive.Include);
            if (view == null)
            {
                var go = new GameObject(
                    CombatPhaseBannerView.ObjectName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(CanvasGroup),
                    typeof(CombatPhaseBannerView));
                Undo.RegisterCreatedObjectUndo(go, "Create Battle Info Canvas");
                view = go.GetComponent<CombatPhaseBannerView>();
            }
            else
            {
                Undo.RecordObject(view.gameObject, "Refresh Battle Info Canvas");
                if (view.gameObject.name != CombatPhaseBannerView.ObjectName)
                {
                    view.gameObject.name = CombatPhaseBannerView.ObjectName;
                }
            }

            view.EnsureBuilt();
            AssignSprites(view);
            WireController(view);

            view.gameObject.SetActive(true);
            Selection.activeGameObject = view.gameObject;
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log(
                "[Fractured Chorus] BattleInfo canvas ready. Swap Planning / Battle Start sprites on Inspector, then Save scene.");
        }

        private static void AssignSprites(CombatPhaseBannerView view)
        {
            var so = new SerializedObject(view);
            AssignSpriteProp(so, "planningSprite", PlanningPath);
            AssignSpriteProp(so, "battleStartSprite", BattleStartPath);
            var banner = view.transform.Find(CombatPhaseBannerView.BannerChildName);
            if (banner != null)
            {
                so.FindProperty("bannerImage").objectReferenceValue = banner.GetComponent<Image>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSpriteProp(SerializedObject so, string prop, string assetPath)
        {
            var field = so.FindProperty(prop);
            if (field == null)
            {
                return;
            }

            if (field.objectReferenceValue != null)
            {
                return;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[Fractured Chorus] Missing sprite at '{assetPath}'.");
                return;
            }

            field.objectReferenceValue = sprite;
        }

        private static void WireController(CombatPhaseBannerView view)
        {
            var controller = Object.FindAnyObjectByType<CombatController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                return;
            }

            var cso = new SerializedObject(controller);
            var prop = cso.FindProperty("phaseBanner");
            if (prop == null)
            {
                return;
            }

            prop.objectReferenceValue = view;
            cso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }
    }
}
#endif
