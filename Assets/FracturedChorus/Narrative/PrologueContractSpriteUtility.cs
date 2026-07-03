using UnityEngine;

namespace FracturedChorus.Narrative
{
    public static class PrologueContractSpriteUtility
    {
        public static Sprite LoadPrimarySprite()
        {
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(PrologueContractLayout.ContractSpritePath);
            Sprite best = null;
            foreach (var asset in assets)
            {
                if (asset is not Sprite candidate)
                {
                    continue;
                }

                if (best == null || candidate.rect.width > best.rect.width)
                {
                    best = candidate;
                }
            }

            return best;
#else
            return null;
#endif
        }
    }
}
