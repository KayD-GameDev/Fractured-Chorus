#if UNITY_EDITOR
using FracturedChorus.Data;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    /// <summary>
    /// Re-binds Charlott (tank) skill icons after PNG rename/reimport.
    /// </summary>
    public static class CharlottSkillIconSetup
    {
        private const string BasicPath = "Assets/FracturedChorus/Art/UI/Skills/Charlotte/Charlott_BasicAttack.png";
        private const string SkillPath = "Assets/FracturedChorus/Art/UI/Skills/Charlotte/Charlott_Skill.png";
        private const string UltPath = "Assets/FracturedChorus/Art/UI/Skills/Charlotte/Charlott_Ult.png";
        private const string BasicAsset = "Assets/FracturedChorus/Resources/Skills/tank_basic.asset";
        private const string SkillAsset = "Assets/FracturedChorus/Resources/Skills/tank_skill.asset";
        private const string UltAsset = "Assets/FracturedChorus/Resources/Skills/tank_ult.asset";

        [MenuItem("Fractured Chorus/Rebind Charlott Skill Icons")]
        public static void RebindIcons()
        {
            AssetDatabase.ImportAsset(BasicPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(SkillPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(UltPath, ImportAssetOptions.ForceUpdate);

            Bind(BasicAsset, BasicPath);
            Bind(SkillAsset, SkillPath);
            Bind(UltAsset, UltPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[CharlottSkillIcons] Rebound tank_basic / tank_skill / tank_ult icons.");
        }

        private static void Bind(string skillAssetPath, string spritePath)
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(skillAssetPath);
            var sprite = LoadFirstSprite(spritePath);
            if (skill == null)
            {
                Debug.LogError($"[CharlottSkillIcons] Missing skill asset: {skillAssetPath}");
                return;
            }

            if (sprite == null)
            {
                Debug.LogError($"[CharlottSkillIcons] Missing sprite: {spritePath}");
                return;
            }

            skill.icon = sprite;
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
