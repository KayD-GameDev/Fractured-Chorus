#if UNITY_EDITOR
using FracturedChorus.Data;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class PartyTimelineSkillNoteSetup
    {
        [MenuItem("Fractured Chorus/Rebind Party Timeline Skill Notes")]
        public static void RebindAll()
        {
            RebindCharacter(
                "Coda",
                "coda_skill_note_main_v1.png",
                "coda_skill_note_wait_v1.png",
                "coda_skill_note_ult_v1.png",
                "Assets/FracturedChorus/Resources/Skills/Coda_basic.asset",
                "Assets/FracturedChorus/Resources/Skills/Coda_skill.asset",
                "Assets/FracturedChorus/Resources/Skills/Coda_ult.asset");

            RebindCharacter(
                "Charlotte",
                "charlotte_skill_note_main_v1.png",
                "charlotte_skill_note_wait_v1.png",
                "charlotte_skill_note_ult_v1.png",
                "Assets/FracturedChorus/Resources/Skills/Charlott_basic.asset",
                "Assets/FracturedChorus/Resources/Skills/Charlott_skill.asset",
                "Assets/FracturedChorus/Resources/Skills/Charlott_ult.asset");

            RebindCharacter(
                "Ren",
                "ren_skill_note_main_v1.png",
                "ren_skill_note_wait_v1.png",
                "ren_skill_note_ult_v1.png",
                "Assets/FracturedChorus/Resources/Skills/ren_basic.asset",
                "Assets/FracturedChorus/Resources/Skills/ren_skill.asset",
                "Assets/FracturedChorus/Resources/Skills/ren_ult.asset");

            AssetDatabase.SaveAssets();
            Debug.Log("[PartyTimelineNotes] Rebound Ren / Coda / Charlotte timeline skill notes.");
        }

        private static void RebindCharacter(
            string folder,
            string mainFile,
            string waitFile,
            string ultFile,
            string basicAsset,
            string skillAsset,
            string ultAsset)
        {
            var artRoot = $"Assets/FracturedChorus/Art/UI/Combat/Timeline/Skills/{folder}/";
            var resRoot = $"Assets/FracturedChorus/Resources/UI/Combat/Timeline/Skills/{folder}/";
            EnsureFolder(resRoot);
            foreach (var name in new[] { mainFile, waitFile, ultFile })
            {
                ForceSpriteImport(artRoot + name);
                AssetDatabase.CopyAsset(artRoot + name, resRoot + name);
                ForceSpriteImport(resRoot + name);
            }

            Bind(basicAsset, artRoot + mainFile, artRoot + waitFile);
            Bind(skillAsset, artRoot + mainFile, artRoot + waitFile);
            Bind(ultAsset, artRoot + ultFile, artRoot + waitFile);
        }

        private static void EnsureFolder(string resRoot)
        {
            var trim = resRoot.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(trim))
            {
                return;
            }

            var parts = trim.Split('/');
            var cur = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }

                cur = next;
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
                Debug.LogError($"[PartyTimelineNotes] Missing texture: {path}");
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
                Debug.LogError($"[PartyTimelineNotes] Missing skill: {skillAssetPath}");
                return;
            }

            if (active == null || standing == null)
            {
                Debug.LogError($"[PartyTimelineNotes] Missing sprites for {skillAssetPath}");
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
