using UnityEngine;

namespace FracturedChorus.Audio
{
    public class CombatSfxController : MonoBehaviour
    {
        [SerializeField] private AudioSource perfectCounterSource;
        [SerializeField] private AudioClip perfectCounterClip;
        [SerializeField] private float perfectCounterVolume = 1f;

        private void Awake()
        {
            EnsurePerfectCounterSource();
            TryAssignDefaultClip();
        }

        public void PlayPerfectCounter()
        {
            if (perfectCounterClip == null)
            {
                Debug.LogWarning("[CombatSfx] perfectCounterClip not assigned — run 'Fractured Chorus/Wire Combat Music'.");
                return;
            }

            EnsurePerfectCounterSource();
            perfectCounterSource.Stop();
            perfectCounterSource.clip = perfectCounterClip;
            perfectCounterSource.volume = perfectCounterVolume;
            perfectCounterSource.time = 0f;
            perfectCounterSource.Play();
        }

        private void EnsurePerfectCounterSource()
        {
            if (perfectCounterSource != null)
            {
                return;
            }

            var existing = transform.Find("PerfectCounterSfx");
            if (existing != null)
            {
                perfectCounterSource = existing.GetComponent<AudioSource>();
                if (perfectCounterSource != null)
                {
                    return;
                }
            }

            var go = new GameObject("PerfectCounterSfx");
            go.transform.SetParent(transform, false);
            perfectCounterSource = go.AddComponent<AudioSource>();
            perfectCounterSource.playOnAwake = false;
            perfectCounterSource.loop = false;
            perfectCounterSource.spatialBlend = 0f;
            perfectCounterSource.bypassReverbZones = true;
            perfectCounterSource.priority = 0;
        }

        private void TryAssignDefaultClip()
        {
#if UNITY_EDITOR
            if (perfectCounterClip == null)
            {
                perfectCounterClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/FracturedChorus/Audio/SFX/Combat_PerfectCounter.wav");
            }
#endif
        }
    }
}
