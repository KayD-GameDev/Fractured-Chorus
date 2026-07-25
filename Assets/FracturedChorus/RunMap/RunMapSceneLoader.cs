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
        private const string RunMapScenePath = "Assets/FracturedChorus/Scenes/RunMapPrototype.unity";

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
                $"Thêm {CombatScenePath} vào File → Build Settings.");
            return false;
        }

        public static bool LoadCombatPrototype() => LoadByName(RunMapSceneCatalog.CombatPrototype);

        public static bool LoadRunMapPrototype() => LoadByName(RunMapSceneCatalog.RunMapPrototype);

        private static string ResolveScenePath(string sceneName)
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

            if (sceneName == RunMapSceneCatalog.RunMapPrototype)
            {
                return RunMapScenePath;
            }

            return $"Assets/FracturedChorus/Scenes/{sceneName}.unity";
        }
    }
}
