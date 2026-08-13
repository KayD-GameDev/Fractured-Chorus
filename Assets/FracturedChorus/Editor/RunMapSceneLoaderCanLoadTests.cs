using FracturedChorus.RunMap;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class RunMapSceneLoaderCanLoadTests
    {
        [Test]
        public void CanLoad_Empty_IsFalse()
        {
            Assert.IsFalse(RunMapSceneLoader.CanLoad(""));
            Assert.IsFalse(RunMapSceneLoader.CanLoad("   "));
            Assert.IsFalse(RunMapSceneLoader.CanLoad(null));
        }

        [Test]
        public void CanLoad_KnownScenes_IsTrue()
        {
            Assert.IsTrue(RunMapSceneLoader.CanLoad(RunMapSceneCatalog.MainMenuStartGame));
            Assert.IsTrue(RunMapSceneLoader.CanLoad(RunMapSceneCatalog.PrologueVN));
            Assert.IsTrue(RunMapSceneLoader.CanLoad(RunMapSceneCatalog.CombatPrototype));
        }

        [Test]
        public void CanLoad_Unknown_IsFalse()
        {
            Assert.IsFalse(RunMapSceneLoader.CanLoad("DefinitelyMissingScene_XYZ"));
        }
    }
}
