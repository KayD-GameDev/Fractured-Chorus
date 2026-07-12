using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private VnCueResolver cueResolver;
        [SerializeField] private float ambienceVolume = 0.35f;
        [SerializeField] private float bgmVolume = 0.7f;

        public void Bind(VnCueResolver resolver)
        {
            cueResolver = resolver;
            EnsureSources();
        }

        public void PlayAmbience(string cueId, bool loop = true)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return;
            }

            EnsureSources();
            if (cueResolver == null || !cueResolver.TryGetClip(cueId, out var clip))
            {
                return;
            }

            if (ambienceSource.clip == clip && ambienceSource.isPlaying)
            {
                return;
            }

            ambienceSource.clip = clip;
            ambienceSource.loop = loop;
            ambienceSource.volume = ambienceVolume;
            ambienceSource.Play();
        }

        public void StopAmbience()
        {
            if (ambienceSource != null)
            {
                ambienceSource.Stop();
            }
        }

        public void PlayBgm(string cueId, bool loop = true, float pitch = 1f)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return;
            }

            if (cueId == VnAudioIds.StopBgm)
            {
                StopBgm();
                return;
            }

            EnsureSources();
            if (cueResolver == null || !cueResolver.TryGetClip(cueId, out var clip))
            {
                return;
            }

            var targetPitch = pitch > 0.01f ? pitch : 1f;
            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                bgmSource.pitch = targetPitch;
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = bgmVolume;
            bgmSource.pitch = targetPitch;
            bgmSource.Play();
        }

        public void SetBgmPitch(float pitch)
        {
            EnsureSources();
            if (bgmSource != null)
            {
                bgmSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            }
        }

        public void StopBgm()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.pitch = 1f;
            }
        }

        public void PlaySfx(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return;
            }

            EnsureSources();
            if (cueResolver == null || !cueResolver.TryGetClip(cueId, out var clip))
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }

        private void EnsureSources()
        {
            if (bgmSource == null)
            {
                bgmSource = CreateSource("VnBgm", true);
                bgmSource.volume = bgmVolume;
            }

            if (sfxSource == null)
            {
                sfxSource = CreateSource("VnSfx", false);
            }

            if (ambienceSource == null)
            {
                ambienceSource = CreateSource("VnAmbience", true);
                ambienceSource.volume = ambienceVolume;
            }
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
