using FracturedChorus.UI.Loading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Tests
{
    public class LoadingScreenControllerBusyTests
    {
        [TearDown]
        public void TearDown()
        {
            if (LoadingScreenController.Instance != null)
            {
                Object.DestroyImmediate(LoadingScreenController.Instance.gameObject);
            }
        }

        [Test]
        public void BeginLoad_Empty_ReturnsFalse()
        {
            var controller = LoadingScreenController.Ensure();

            Assert.IsFalse(controller.BeginLoad(" ", LoadSceneMode.Single));
            Assert.IsFalse(LoadingScreenController.IsBusy);
        }

        [Test]
        public void BeginLoad_Unknown_ReturnsFalse()
        {
            var controller = LoadingScreenController.Ensure();

            Assert.IsFalse(controller.BeginLoad("DefinitelyMissingScene_XYZ", LoadSceneMode.Single));
            Assert.IsFalse(LoadingScreenController.IsBusy);
        }
    }
}
