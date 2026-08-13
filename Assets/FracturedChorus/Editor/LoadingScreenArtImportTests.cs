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

        [Test]
        public void LayerPngs_ExistAsSprites()
        {
            foreach (var path in Paths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Assert.IsNotNull(sprite, path);
            }
        }
    }
}
