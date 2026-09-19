using FracturedChorus.Hub;
using FracturedChorus.RunMap;
using UnityEngine;

namespace FracturedChorus.Menu
{
    public static class MainMenuConfigLaunch
    {
        private static bool s_bootConfigOnly;
        private static string s_returnScene;
        private static bool s_returnOpenStatusMenu;
        private static bool s_returnOpenSystemSubmenu;

        public static bool BootConfigOnly => s_bootConfigOnly;

        public static bool HasReturnTarget => !string.IsNullOrWhiteSpace(s_returnScene);

        public static void OpenFromCampusHub()
        {
            s_bootConfigOnly = true;
            s_returnScene = RunMapSceneCatalog.CampusHub;
            s_returnOpenStatusMenu = true;
            s_returnOpenSystemSubmenu = true;

            if (!RunMapSceneLoader.LoadByName(RunMapSceneCatalog.MainMenuStartGame))
            {
                Clear();
                Debug.LogError("[MainMenuConfig] Failed to load MainMenuStartGame for config.");
            }
        }

        public static bool ConsumeBootConfigOnly()
        {
            if (!s_bootConfigOnly)
            {
                return false;
            }

            s_bootConfigOnly = false;
            return true;
        }

        public static bool TryConsumeReturn(out string sceneName, out bool openStatusMenu, out bool openSystemSubmenu)
        {
            sceneName = s_returnScene;
            openStatusMenu = s_returnOpenStatusMenu;
            openSystemSubmenu = s_returnOpenSystemSubmenu;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            s_returnScene = null;
            s_returnOpenStatusMenu = false;
            s_returnOpenSystemSubmenu = false;
            return true;
        }

        public static void ApplyReturnFlags(string sceneName, bool openStatusMenu, bool openSystemSubmenu)
        {
            var toCampus = sceneName == RunMapSceneCatalog.CampusHub;
            TownMapView.OpenStatusMenuOnNextShow = openStatusMenu && toCampus;
            TownMapView.OpenSystemSubmenuOnNextShow = openSystemSubmenu && toCampus;
        }

        public static void Clear()
        {
            s_bootConfigOnly = false;
            s_returnScene = null;
            s_returnOpenStatusMenu = false;
            s_returnOpenSystemSubmenu = false;
        }
    }
}
