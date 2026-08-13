using FracturedChorus.UI.Loading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tests
{
    public class LoadingScreenViewTests
    {
        [Test]
        public void SetProgress_SetsFillAndPercent()
        {
            var go = new GameObject("LoadingScreenViewTest");
            var view = go.AddComponent<LoadingScreenView>();
            view.BuildForTests();
            view.SetProgress(0.75f);
            Assert.AreEqual(0.75f, view.FillAmount, 0.001f);
            Assert.AreEqual("75%", view.PercentText);
            Assert.IsTrue(view.PercentVisible);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetProgress_HidesPercentNearZero()
        {
            var go = new GameObject("LoadingScreenViewTestZero");
            var view = go.AddComponent<LoadingScreenView>();
            view.BuildForTests();
            view.SetProgress(0f);
            Assert.IsFalse(view.PercentVisible);
            Object.DestroyImmediate(go);
        }
    }
}
