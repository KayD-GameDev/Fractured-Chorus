using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Menu
{
    public sealed class TitleAttractCrystalField : MonoBehaviour
    {
        [SerializeField] private Sprite[] shards;
        [SerializeField] private int count = 18;
        [SerializeField] private Vector2 sizeRange = new Vector2(36f, 92f);
        [SerializeField] private Vector2 speedRange = new Vector2(18f, 46f);
        [SerializeField] private Vector2 spinRange = new Vector2(-22f, 22f);

        private RectTransform _root;
        private Shard[] _items;

        private struct Shard
        {
            public RectTransform Rect;
            public CanvasGroup Group;
            public Vector2 Velocity;
            public float Spin;
            public float Phase;
            public float MinAlpha;
            public float MaxAlpha;
        }

        public void Bind(Sprite[] boundShards, int shardCount)
        {
            shards = boundShards;
            if (shardCount > 0)
            {
                count = shardCount;
            }

            if (Application.isPlaying)
            {
                Rebuild();
            }
        }

        private void Awake()
        {
            _root = transform as RectTransform;
        }

        private void OnEnable()
        {
            if (_items == null || _items.Length == 0)
            {
                Rebuild();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || _items == null)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i];
                if (item.Rect == null)
                {
                    continue;
                }

                var pos = item.Rect.anchoredPosition + item.Velocity * dt;
                if (pos.x > 1100f)
                {
                    pos.x = -1100f;
                }
                else if (pos.x < -1100f)
                {
                    pos.x = 1100f;
                }

                if (pos.y > 700f)
                {
                    pos.y = -700f;
                }
                else if (pos.y < -700f)
                {
                    pos.y = 700f;
                }

                item.Rect.anchoredPosition = pos;
                item.Rect.Rotate(0f, 0f, item.Spin * dt);
                item.Phase += dt * 0.7f;
                if (item.Group != null)
                {
                    var t = (Mathf.Sin(item.Phase) + 1f) * 0.5f;
                    item.Group.alpha = Mathf.Lerp(item.MinAlpha, item.MaxAlpha, t);
                }

                _items[i] = item;
            }
        }

        private void Rebuild()
        {
            if (shards == null || shards.Length == 0)
            {
                return;
            }

            Sprite first = null;
            for (var i = 0; i < shards.Length; i++)
            {
                if (shards[i] != null)
                {
                    first = shards[i];
                    break;
                }
            }

            if (first == null)
            {
                return;
            }

            _root = transform as RectTransform;
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            _items = new Shard[count];
            for (var i = 0; i < count; i++)
            {
                var go = new GameObject($"Crystal_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                go.transform.SetParent(transform, false);
                var rect = go.GetComponent<RectTransform>();
                var image = go.GetComponent<Image>();
                var group = go.GetComponent<CanvasGroup>();
                var sprite = shards[i % shards.Length];
                if (sprite == null)
                {
                    sprite = first;
                }
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.color = Color.white;
                group.blocksRaycasts = false;
                group.interactable = false;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                var size = Random.Range(sizeRange.x, sizeRange.y);
                var aspect = 1f;
                if (sprite != null && sprite.rect.height > 1f)
                {
                    aspect = sprite.rect.width / sprite.rect.height;
                }

                rect.sizeDelta = new Vector2(size * aspect, size);
                rect.anchoredPosition = new Vector2(Random.Range(-920f, 920f), Random.Range(-480f, 480f));
                rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                var angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                var speed = Random.Range(speedRange.x, speedRange.y);
                _items[i] = new Shard
                {
                    Rect = rect,
                    Group = group,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    Spin = Random.Range(spinRange.x, spinRange.y),
                    Phase = Random.Range(0f, Mathf.PI * 2f),
                    MinAlpha = Random.Range(0.22f, 0.4f),
                    MaxAlpha = Random.Range(0.55f, 0.88f)
                };
            }
        }
    }
}
