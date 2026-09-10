using UnityEngine;

namespace FracturedChorus.Hub.CharacterBuild
{
    public sealed class CharacterBuildPortraitLayersView : MonoBehaviour
    {
        [SerializeField] private GameObject[] layers;

        public void SetActiveIndex(int index)
        {
            if (layers == null)
            {
                return;
            }

            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] != null)
                {
                    layers[i].SetActive(i == index);
                }
            }
        }
    }
}
