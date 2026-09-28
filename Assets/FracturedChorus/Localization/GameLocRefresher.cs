using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FracturedChorus.Localization
{
    public sealed class GameLocRefresher : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject(nameof(GameLocRefresher));
            DontDestroyOnLoad(host);
            host.AddComponent<GameLocRefresher>();
        }

        private void OnEnable()
        {
            GameLoc.Changed += Refresh;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Refresh();
        }

        private void OnDisable()
        {
            GameLoc.Changed -= Refresh;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Refresh();
        }

        private static void Refresh()
        {
            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
            for (var i = 0; i < texts.Length; i++)
            {
                var text = texts[i];
                if (text == null)
                {
                    continue;
                }

                var binding = text.GetComponent<GameLocBinding>();
                if (binding != null && binding.skip)
                {
                    continue;
                }

                if (binding == null)
                {
                    if (!GameLoc.HasPhrase(text.text))
                    {
                        continue;
                    }

                    binding = text.gameObject.AddComponent<GameLocBinding>();
                    binding.source = text.text;
                }

                GameLoc.ApplyBinding(text, binding);
            }
        }
    }
}
