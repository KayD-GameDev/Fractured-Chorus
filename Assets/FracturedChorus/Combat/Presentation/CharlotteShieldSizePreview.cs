using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [ExecuteAlways]
    public class CharlotteShieldSizePreview : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Charlotte/";

        [Header("Tune — chỉnh rồi Save To Tuning")]
        [SerializeField] private float worldSize = 3.8f;
        [SerializeField] private float forwardOffset = 1.35f;
        [SerializeField] private float heightOffset = 0.95f;
        [SerializeField] private float forwardSign = 1f;
        [SerializeField] private Transform follow;
        [SerializeField] private Color tint = new Color(1f, 0.82f, 0.28f, 0.95f);
        [SerializeField] private int sortingOrder = 80;

        private SpriteRenderer _shield;
        private Material _additive;

        public float WorldSize => Mathf.Max(0.2f, worldSize);
        public float ForwardOffset => forwardOffset;
        public float HeightOffset => heightOffset;

        private void OnEnable()
        {
            EnsureVisual();
            ApplyLayout();
        }

        private void OnDisable()
        {
            if (_additive != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_additive);
                }
                else
                {
                    DestroyImmediate(_additive);
                }

                _additive = null;
            }
        }

        private void LateUpdate()
        {
            ApplyLayout();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            worldSize = Mathf.Max(0.2f, worldSize);
            if (isActiveAndEnabled)
            {
                EnsureVisual();
                ApplyLayout();
            }
        }
#endif

        [ContextMenu("Save Size To CharlotteShieldTuning")]
        public void SaveToTuning()
        {
            var tuning = CharlotteShieldTuning.Resolve();
            if (tuning == null)
            {
                var host = GameObject.Find("CombatRoot");
                if (host == null)
                {
                    host = gameObject;
                }

                tuning = host.GetComponent<CharlotteShieldTuning>();
                if (tuning == null)
                {
                    tuning = host.AddComponent<CharlotteShieldTuning>();
                }
            }

            tuning.Apply(WorldSize, ForwardOffset, HeightOffset);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(tuning);
            UnityEditor.EditorUtility.SetDirty(tuning.gameObject);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tuning.gameObject.scene);
            }
#endif
            Debug.Log(
                $"[CharlotteShield] Saved tuning worldSize={WorldSize:F2} forward={ForwardOffset:F2} height={HeightOffset:F2}");
        }

        public void BindFollow(Transform target, float faceSign = 1f)
        {
            follow = target;
            forwardSign = Mathf.Approximately(faceSign, 0f) ? 1f : Mathf.Sign(faceSign);
            ApplyLayout();
        }

        public void SetTuning(float size, float forward, float height)
        {
            worldSize = Mathf.Max(0.2f, size);
            forwardOffset = forward;
            heightOffset = height;
            ApplyLayout();
        }

        private void EnsureVisual()
        {
            if (_shield != null && _shield.sprite != null)
            {
                return;
            }

            var child = transform.Find("Shield");
            if (child == null)
            {
                var go = new GameObject("Shield");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }

            _shield = child.GetComponent<SpriteRenderer>();
            if (_shield == null)
            {
                _shield = child.gameObject.AddComponent<SpriteRenderer>();
            }

            _shield.sprite = LoadSprite("charlotte_vfx_rho_shield_hold_2layer_v3")
                             ?? LoadSprite("charlotte_vfx_rho_shield_hold_v2")
                             ?? LoadSprite("charlotte_vfx_counter_shield_v1");
            _shield.sortingOrder = sortingOrder;
            _shield.color = tint;
            if (_additive == null)
            {
                var shader = Shader.Find("FracturedChorus/VFX/RenBulletAdditive")
                             ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _additive = new Material(shader)
                    {
                        name = "CharlotteShieldPreviewAdditive",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
            }

            if (_additive != null)
            {
                _shield.sharedMaterial = _additive;
            }
        }

        private void ApplyLayout()
        {
            EnsureVisual();
            if (_shield == null || _shield.sprite == null)
            {
                return;
            }

            var anchor = follow != null ? follow.position : transform.position;
            var feetY = follow != null ? ResolveFeetY(follow) : anchor.y;
            var world = new Vector3(
                anchor.x + forwardOffset * forwardSign,
                feetY + heightOffset,
                anchor.z);
            transform.position = world;

            var native = Mathf.Max(_shield.sprite.bounds.size.x, _shield.sprite.bounds.size.y);
            var scale = native > 0.001f ? WorldSize / native : 1f;
            var faceBoss = forwardSign >= 0f ? -1f : 1f;
            _shield.transform.localScale = new Vector3(scale * faceBoss, scale, 1f);
            _shield.color = tint;
            _shield.sortingOrder = sortingOrder;
        }

        private static float ResolveFeetY(Transform target)
        {
            var view = target.GetComponent<UnitView>();
            if (view != null)
            {
                return view.FeetWorldPosition.y;
            }

            return target.position.y;
        }

        private static Sprite LoadSprite(string fileName)
        {
            var path = ResourceRoot + fileName;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                return null;
            }

            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
    }
}
