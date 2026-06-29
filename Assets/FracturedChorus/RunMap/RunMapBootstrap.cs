using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.RunMap
{
    /// <summary>Seed + MapTemplateSO settings for RunMapController boot.</summary>
    public class RunMapBootstrap : MonoBehaviour
    {
        [SerializeField] private MapTemplateSO template;
        [SerializeField] private int overrideSeed;
        [SerializeField] private bool useOverrideSeed;
        [SerializeField] private bool randomizeSeedOnPlay = true;

        public MapTemplateSO Template => template;

        public int ResolveSeed()
        {
            if (useOverrideSeed)
            {
                return overrideSeed;
            }

            if (template != null && !template.randomizeSeedOnPlay)
            {
                return template.defaultSeed;
            }

            if (randomizeSeedOnPlay || (template != null && template.randomizeSeedOnPlay))
            {
                return Random.Range(1, int.MaxValue);
            }

            return template != null ? template.defaultSeed : 42;
        }
    }
}
