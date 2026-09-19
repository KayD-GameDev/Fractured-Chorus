using UnityEngine;

namespace FracturedChorus.Hub
{
    internal static class BondsPackSprites
    {
        private const string PackRoot = "Assets/FracturedChorus/Art/UI/Bonds/Pack/";

        private static Sprite _menuNormal;
        private static Sprite _menuSelected;
        private static Sprite _episodeRowNormal;
        private static Sprite _episodeRowSelected;
        private static Sprite _panelSocialStats;
        private static Sprite _panelCharacterInfo;

        public static Sprite MenuNormal => Load(ref _menuNormal, "02_Menu_Normal.png");
        public static Sprite MenuSelected => Load(ref _menuSelected, "03_Menu_Selected.png");
        public static Sprite EpisodeRowNormal => Load(ref _episodeRowNormal, "06_Row_Episode_Normal.png");
        public static Sprite EpisodeRowSelected => Load(ref _episodeRowSelected, "07_Row_Episode_Selected.png");
        public static Sprite PanelSocialStats => Load(ref _panelSocialStats, "04_Panel_SocialStats.png");
        public static Sprite PanelCharacterInfo => Load(ref _panelCharacterInfo, "10_Panel_CharacterInfo.png");

        private static Sprite Load(ref Sprite cache, string fileName)
        {
            if (cache != null)
            {
                return cache;
            }

#if UNITY_EDITOR
            cache = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(PackRoot + fileName);
#endif
            return cache;
        }
    }
}
