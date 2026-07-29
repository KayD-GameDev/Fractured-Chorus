using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public sealed class SceneUiFontBootstrap : MonoBehaviour
    {
        [SerializeField] private bool includeInactive = true;

        private void Awake()
        {
            UiFontCatalog.ApplyHierarchy(transform, includeInactive);
        }
    }
}
