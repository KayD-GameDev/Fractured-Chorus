### Task 2: RunMapSceneLoader.CanLoad

**Files:**
- Modify: `Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs`
- Modify: `Assets/FracturedChorus/Editor/LoadingProgressTests.cs` (thêm tests CanLoad)

**Interfaces:**
- Consumes: existing `ResolveScenePath` (đổi thành `public static` hoặc giữ private + `CanLoad` public).
- Produces: `RunMapSceneLoader.CanLoad(string sceneName)` — `true` nếu build index ≥ 0 hoặc `CanStreamedLevelBeLoaded`. Không load scene. Tên rỗng → `false`.

- [ ] **Step 1: Add failing tests**

```csharp
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
```

Đặt class này trong cùng file `LoadingProgressTests.cs` hoặc file `RunMapSceneLoaderCanLoadTests.cs`. Prefer file mới: `Assets/FracturedChorus/Editor/RunMapSceneLoaderCanLoadTests.cs`.

- [ ] **Step 2: Run — FAIL (`CanLoad` missing)**

- [ ] **Step 3: Implement CanLoad — chưa đổi LoadByName sang async**

Replace `RunMapSceneLoader` body methods:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.RunMap
{
    public static class RunMapSceneLoader
    {
        private const string MainMenuStartGameScenePath = "Assets/FracturedChorus/Scenes/MainMenuStartGame.unity";
        private const string PrologueVNScenePath = "Assets/FracturedChorus/Scenes/PrologueVN.unity";
        private const string OpeningInvestigationScenePath = "Assets/FracturedChorus/Scenes/OpeningInvestigation.unity";
        private const string CampusHubScenePath = "Assets/FracturedChorus/Scenes/CampusHub.unity";
        private const string CombatScenePath = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
        private const string CombatTutorialScenePath = "Assets/FracturedChorus/Scenes/CombatTutorial.unity";
        private const string RunMapScenePath = "Assets/FracturedChorus/Scenes/RunMapPrototype.unity";

        public static bool CanLoad(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            var buildIndex = SceneUtility.GetBuildIndexByScenePath(ResolveScenePath(sceneName));
            if (buildIndex >= 0)
            {
                return true;
            }

            return Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public static bool LoadByName(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[Fractured Chorus] RunMapSceneLoader: scene name rỗng.");
                return false;
            }

            var buildIndex = SceneUtility.GetBuildIndexByScenePath(ResolveScenePath(sceneName));
            if (buildIndex >= 0)
            {
                Debug.Log($"[Fractured Chorus] Load scene index {buildIndex} ({sceneName}).");
                SceneManager.LoadScene(buildIndex, mode);
                return true;
            }

            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.Log($"[Fractured Chorus] Load scene by name: {sceneName}.");
                SceneManager.LoadScene(sceneName, mode);
                return true;
            }

            Debug.LogError(
                $"[Fractured Chorus] Không load được scene '{sceneName}'. " +
                $"Thêm scene vào File → Build Settings.");
            return false;
        }

        public static bool LoadCombatPrototype() => LoadByName(RunMapSceneCatalog.CombatPrototype);

        public static bool LoadCombatTutorial() => LoadByName(RunMapSceneCatalog.CombatTutorial);

        public static bool LoadRunMapPrototype() => LoadByName(RunMapSceneCatalog.RunMapPrototype);

        public static string ResolveScenePath(string sceneName)
        {
            if (sceneName == RunMapSceneCatalog.MainMenuStartGame)
            {
                return MainMenuStartGameScenePath;
            }

            if (sceneName == RunMapSceneCatalog.PrologueVN)
            {
                return PrologueVNScenePath;
            }

            if (sceneName == RunMapSceneCatalog.OpeningInvestigation)
            {
                return OpeningInvestigationScenePath;
            }

            if (sceneName == RunMapSceneCatalog.CampusHub)
            {
                return CampusHubScenePath;
            }

            if (sceneName == RunMapSceneCatalog.CombatPrototype)
            {
                return CombatScenePath;
            }

            if (sceneName == RunMapSceneCatalog.CombatTutorial)
            {
                return CombatTutorialScenePath;
            }

            if (sceneName == RunMapSceneCatalog.RunMapPrototype)
            {
                return RunMapScenePath;
            }

            return $"Assets/FracturedChorus/Scenes/{sceneName}.unity";
        }
    }
}
```

`ResolveScenePath` đổi `public` để test/builder không cần duplicate path. FlowerShopWork vẫn đi nhánh fallback `Scenes/{name}.unity`.

- [ ] **Step 4: Tests PASS** (`CanLoad_KnownScenes` phụ thuộc Build Settings — nếu fail, thêm scene vào Build Settings trước, không fake test).

- [ ] **Step 5: Commit**

```
git add Assets/FracturedChorus/RunMap/RunMapSceneLoader.cs Assets/FracturedChorus/Editor/RunMapSceneLoaderCanLoadTests.cs Assets/FracturedChorus/Editor/RunMapSceneLoaderCanLoadTests.cs.meta
git commit -m "Expose scene load checks without starting a load."
```

---

