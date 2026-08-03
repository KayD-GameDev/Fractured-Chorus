#if UNITY_EDITOR
using FracturedChorus.Data;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class RenTimelineSkillNoteSetup
    {
        private const string ArtRoot = "Assets/FracturedChorus/Art/UI/Combat/Timeline/Skills/Ren/";
        private const string ResRoot = "Assets/FracturedChorus/Resources/UI/Combat/Timeline/Skills/Ren/";

        private const string Main = "ren_skill_note_main_v1.png";
        private const string Wait = "ren_skill_note_wait_v1.png";
        private const string Ult = "ren_skill_note_ult_v1.png";

        private const string BasicAsset = "Assets/FracturedChorus/Resources/Skills/ren_basic.asset";
        private const string SkillAsset = "Assets/FracturedChorus/Resources/Skills/ren_skill.asset";
        private const string UltAsset = "Assets/FracturedChorus/Resources/Skills/ren_ult.asset";

        [MenuItem("Fractured Chorus/Rebind Ren Timeline Skill Notes")]
        public static void Rebind()
        {
            SyncArtToResources();
            ForceSpriteImport(ArtRoot + Main);
            ForceSpriteImport(ArtRoot + Wait);
            ForceSpriteImport(ArtRoot + Ult);
            ForceSpriteImport(ResRoot + Main);
            ForceSpriteImport(ResRoot + Wait);
            ForceSpriteImport(ResRoot + Ult);

            Bind(BasicAsset, ArtRoot + Main, ArtRoot + Wait);
            Bind(SkillAsset, ArtRoot + Main, ArtRoot + Wait);
            Bind(UltAsset, ArtRoot + Ult, ArtRoot + Wait);
            AssetDatabase.SaveAssets();
            Debug.Log("[RenTimelineNotes] Rebound ren_basic / ren_skill → main_v1, ren_ult → ult_v1.");
        }

        private static void SyncArtToResources()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Resources/UI/Combat/Timeline/Skills"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Resources/UI/Combat/Timeline"))
                {
                    AssetDatabase.CreateFolder("Assets/FracturedChorus/Resources/UI/Combat", "Timeline");
                }

                AssetDatabase.CreateFolder("Assets/FracturedChorus/Resources/UI/Combat/Timeline", "Skills");
            }

            if (!AssetDatabase.IsValidFolder(ResRoot.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Resources/UI/Combat/Timeline/Skills", "Ren");
            }

            foreach (var name in new[] { Main, Wait, Ult })
            {
                AssetDatabase.CopyAsset(ArtRoot + name, ResRoot + name);
            }
        }

        private static void ForceSpriteImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer == null)
            {
                Debug.LogError($"[RenTimelineNotes] Missing texture: {path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void Bind(string skillAssetPath, string activePath, string standingPath)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(skillAssetPath);
            var active = LoadFirstSprite(activePath);
            var standing = LoadFirstSprite(standingPath);
            if (skill == null)
            {
                Debug.LogError($"[RenTimelineNotes] Missing skill: {skillAssetPath}");
                return;
            }

            if (active == null || standing == null)
            {
                Debug.LogError($"[RenTimelineNotes] Missing sprites for {skillAssetPath}");
                return;
            }

            skill.timelineActiveSprite = active;
            skill.timelineStandingSprite = standing;
            EditorUtility.SetDirty(skill);
        }

        private static Sprite LoadFirstSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
