#if UNITY_EDITOR
using FracturedChorus.Combat.Presentation;
using FracturedChorus.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class SkillVfxUnitAnchorSetup
    {
        private const string EnemyPoolFolder = "Assets/FracturedChorus/Prefabs/Enemy Pool";

        [MenuItem("Fractured Chorus/VFX/Ensure Projectile + ReceiveDmg On Prefabs")]
        public static void EnsureOnPrefabs()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPoolFolder });
            var count = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var views = root.GetComponentsInChildren<UnitView>(true);
                    if (views.Length == 0)
                    {
                        continue;
                    }

                    for (var v = 0; v < views.Length; v++)
                    {
                        EnsureAnchors(views[v]);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count += views.Length;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SkillVfx] Projectile + ReceiveDmg trên {count} unit prefab.");
        }

        [MenuItem("Fractured Chorus/VFX/Ensure Projectile + ReceiveDmg On Scene Units")]
        public static void EnsureOnSceneUnits()
        {
            var views = Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include);
            for (var i = 0; i < views.Length; i++)
            {
                if (views[i] == null)
                {
                    continue;
                }

                Undo.RecordObject(views[i].gameObject, "Ensure VFX Anchors");
                EnsureAnchors(views[i]);
                EditorUtility.SetDirty(views[i]);
            }

            if (views.Length > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(views[0].gameObject.scene);
            }

            Debug.Log($"[SkillVfx] Projectile + ReceiveDmg trên {views.Length} unit scene.");
        }

        public static void EnsureAnchors(UnitView view)
        {
            if (view == null)
            {
                return;
            }

            view.EnsureReceiveDmg();
            view.EnsureProjectile();
            view.DestroyLegacyVfxPreviewChildren();
            UnitSpriteSimulator.EnsureOn(view);
            SkillVfxSimulator.EnsureOn(view);
        }
    }
}
#endif
