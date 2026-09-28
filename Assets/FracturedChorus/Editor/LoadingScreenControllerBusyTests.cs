using System.Reflection;
using FracturedChorus.RunMap;
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

        [Test]
        public void BeginLoadOrChain_WhileBusy_QueuesCombatTutorial()
        {
            var controller = LoadingScreenController.Ensure();
            SetBusy(controller, true);

            Assert.IsTrue(controller.BeginLoadOrChain(RunMapSceneCatalog.CombatTutorial));
            Assert.AreEqual(RunMapSceneCatalog.CombatTutorial, ChainScene(controller));
        }

        [Test]
        public void BeginLoadOrChain_UnknownWhileBusy_DoesNotQueue()
        {
            var controller = LoadingScreenController.Ensure();
            SetBusy(controller, true);

            Assert.IsFalse(controller.BeginLoadOrChain("DefinitelyMissingScene_XYZ"));
            Assert.IsNull(ChainScene(controller));
        }

        private static void SetBusy(LoadingScreenController controller, bool busy)
        {
            typeof(LoadingScreenController)
                .GetField("_busy", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(controller, busy);
        }

        private static string ChainScene(LoadingScreenController controller)
        {
            return (string)typeof(LoadingScreenController)
                .GetField("_chainScene", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(controller);
        }
    }
}
