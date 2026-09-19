using FracturedChorus.RunMap;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FracturedChorus.Hub
{
    public sealed class CampusBgmPlayer : MonoBehaviour
    {
        public const string ClipResourcePath = "Audio/Music/Neon_Golden_Hour";
        public const float Volume = 0.65f;
        public const string CharacterBuildScene = "CharacterBuild";

        private static CampusBgmPlayer _instance;
        private AudioSource _source;
        private bool _subscribed;

        public static void Play()
        {
            Ensure().StartLoop();
        }

        public static bool IsCampusMusicScene(string sceneName)
        {
            return sceneName == RunMapSceneCatalog.CampusHub
                || sceneName == CharacterBuildScene;
        }

        private static CampusBgmPlayer Ensure()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var existing = Object.FindAnyObjectByType<CampusBgmPlayer>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            var go = new GameObject("CampusBgm");
            Object.DontDestroyOnLoad(go);
            return go.AddComponent<CampusBgmPlayer>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            Object.DontDestroyOnLoad(gameObject);
            _source = GetComponent<AudioSource>();
            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
            }

            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = Volume;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            if (_subscribed)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsCampusMusicScene(scene.name))
            {
                StopLoop();
            }
        }

        private void StartLoop()
        {
            try
            {
                var clip = Resources.Load<AudioClip>(ClipResourcePath);
                if (clip == null)
                {
                    Debug.LogError("[Fractured Chorus] Campus BGM missing: Resources/Audio/Music/Neon_Golden_Hour");
                    return;
                }

                if (_source.clip == clip && _source.isPlaying)
                {
                    return;
                }

                _source.playOnAwake = false;
                _source.loop = true;
                _source.spatialBlend = 0f;
                _source.volume = Volume;
                _source.clip = clip;
                _source.Play();
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Failed to play campus BGM: {error}");
            }
        }

        private void StopLoop()
        {
            if (_source != null && _source.isPlaying)
            {
                _source.Stop();
            }
        }
    }
}
