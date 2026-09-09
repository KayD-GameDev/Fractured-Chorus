using UnityEngine;

namespace FracturedChorus.Meta
{
    /// <summary>
    /// Cộng dồn thời gian chơi thực tế vào session đang mở. Tự dựng lúc vào Play và sống xuyên scene,
    /// nên không scene nào phải tự nhớ gắn component.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaytimeTracker : MonoBehaviour
    {
        private static PlaytimeTracker s_instance;

        public static PlaytimeTracker Instance => s_instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_instance != null)
            {
                return;
            }

            var host = new GameObject("[Fractured Chorus] PlaytimeTracker");
            s_instance = host.AddComponent<PlaytimeTracker>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
        }

        private void Update()
        {
            // Chỉ tính khi đang thực sự chơi một file save — thời gian ngồi ở main menu không tính.
            if (!GameMetaSession.HasSession)
            {
                return;
            }

            GameMetaSession.Current.Playtime.Add(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}
