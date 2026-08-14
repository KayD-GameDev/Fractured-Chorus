using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class LoadingScreenArtImportTests
    {
        private static readonly string[] Paths =
        {
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_clouds.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_notes_stars.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_skyline.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_buildings_signs.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_clef.png",
            "Assets/FracturedChorus/Art/UI/LoadingScreen/loading_floor.png"
        };

        private static readonly string[] RuntimeBackgrounds =
        {
            "Assets/FracturedChorus/Resources/UI/LoadingBg/loading_bg_01.png",
            "Assets/FracturedChorus/Resources/UI/LoadingBg/loading_bg_02.png",
            "Assets/FracturedChorus/Resources/UI/LoadingBg/loading_bg_03.png"
        };

        [Test]
        public void LayerPngs_ExistAsSprites()
        {
            foreach (var path in Paths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.IsNotNull(sprite, path);
            }
        }

        [Test]
        public void RuntimeBackgrounds_ExistAsSprites()
        {
            foreach (var path in RuntimeBackgrounds)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.IsNotNull(sprite, path);
            }
        }
    }
}
