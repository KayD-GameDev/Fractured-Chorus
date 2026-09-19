using FracturedChorus.Hub;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Menu
{
    public static class TitleScreenUiGuard
    {
        public static void SuppressStrayResonanceDiveButtons()
        {
            var buttons = Object.FindObjectsByType<ResonanceDiveButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (button == null)
                {
                    continue;
                }

                if (button.GetComponentInParent<MetaStatusMenuUI>(true) != null)
                {
                    continue;
                }

                button.SetListening(false);
                button.SetMenuVisible(false, 0f);
            }
        }
    }
}
