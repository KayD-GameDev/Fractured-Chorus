using FracturedChorus.UI.Loading;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tests
{
    public class LoadingScreenPrefabTests
    {
        private const string Path = "Assets/FracturedChorus/Resources/UI/LoadingScreen.prefab";

        [Test]
        public void Prefab_HasControllerViewAndBar()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<LoadingScreenController>());
            Assert.IsNotNull(prefab.GetComponent<LoadingScreenView>());
            var canvas = prefab.GetComponentInChildren<Canvas>(true);
            Assert.IsNotNull(canvas);
            Assert.AreEqual(500, canvas.sortingOrder);
            Assert.IsNotNull(prefab.GetComponentInChildren<CanvasGroup>(true));
            var fill = Find(prefab.transform, "Fill");
            Assert.IsNotNull(fill);
            Assert.AreEqual(Image.Type.Sliced, fill.GetComponent<Image>().type);
            Assert.IsTrue(Find(prefab.transform, "SkyFill").gameObject.activeSelf);
            Assert.IsFalse(Find(prefab.transform, "Clouds").gameObject.activeSelf);
            Assert.IsFalse(Find(prefab.transform, "Clef").gameObject.activeSelf);
            Assert.IsFalse(Find(prefab.transform, "Floor").gameObject.activeSelf);
            Assert.IsTrue(Find(prefab.transform, "UiGroup").gameObject.activeSelf);
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }
    }
}
