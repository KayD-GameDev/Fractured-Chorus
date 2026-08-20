using UnityEngine;

namespace FracturedChorus.Menu
{
    [ExecuteAlways]
    public sealed class MainMenuLayoutSandboxLayers : MonoBehaviour
    {
        [SerializeField] private GameObject attractLayer;
        [SerializeField] private GameObject mainMenuLayer;

        public GameObject AttractLayer => attractLayer;
        public GameObject MainMenuLayer => mainMenuLayer;

        public void Bind(GameObject attract, GameObject mainMenu)
        {
            attractLayer = attract;
            mainMenuLayer = mainMenu;
        }

        public void ShowAttract()
        {
            SetLayer(attractLayer, true);
            SetLayer(mainMenuLayer, false);
        }

        public void ShowMainMenu()
        {
            SetLayer(attractLayer, false);
            SetLayer(mainMenuLayer, true);
        }

        public void ShowBoth()
        {
            SetLayer(attractLayer, true);
            SetLayer(mainMenuLayer, true);
        }

        private static void SetLayer(GameObject layer, bool active)
        {
            if (layer != null && layer.activeSelf != active)
            {
                layer.SetActive(active);
            }
        }
    }
}
