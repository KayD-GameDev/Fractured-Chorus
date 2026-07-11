using UnityEngine;

namespace FracturedChorus.Hub
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class TownMapSfxController : MonoBehaviour
    {
        [SerializeField] private AudioClip selectClip;
        [SerializeField] private AudioClip confirmClip;
        [SerializeField] private AudioClip openPanelClip;
        [SerializeField] private AudioClip closePanelClip;
        [SerializeField] [Range(0.1f, 2f)] private float volume = 1f;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.volume = volume;
        }

        public void Configure(AudioClip select, AudioClip confirm, AudioClip openPanel, AudioClip closePanel, float sfxVolume = 1f)
        {
            selectClip = select;
            confirmClip = confirm;
            openPanelClip = openPanel;
            closePanelClip = closePanel;
            volume = sfxVolume;
            if (_source != null)
            {
                _source.volume = volume;
            }
        }

        public void PlaySelect() => Play(selectClip);

        public void PlayConfirm() => Play(confirmClip);

        public void PlayOpenPanel() => Play(openPanelClip);

        public void PlayClosePanel() => Play(closePanelClip);

        private void Play(AudioClip clip)
        {
            if (clip == null || _source == null)
            {
                return;
            }

            if (!clip.preloadAudioData)
            {
                clip.LoadAudioData();
            }

            _source.PlayOneShot(clip, volume);
        }
    }
}
