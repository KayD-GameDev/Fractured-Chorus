using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [ExecuteAlways]
    public class CharlotteDomeRingSizePreview : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Charlotte/";

        [Header("Skill 3 Dome — chỉnh World Size (không dùng Scale tool)")]
        [SerializeField] [Range(0.4f, 14f)] private float worldSize = 2.9f;
        [SerializeField] [Range(-4f, 4f)] private float xOffset = 0f;
        [SerializeField] [Range(-1f, 3f)] private float heightOffset = 0.75f;
        [SerializeField] private Transform follow;
        [SerializeField] private Color tint = new Color(1f, 0.85f, 0.3f, 0.92f);
        [SerializeField] private int sortingOrder = 42;

        [Header("Wave orbit preview")]
        [SerializeField] private bool showWaveOrbit = true;
        [SerializeField] [Range(0.2f, 2.5f)] private float waveSizeScale = 1.18f;
        [SerializeField] [Range(1f, 30f)] private float waveFps = 24f;

        private SpriteRenderer _ring;
        private SpriteRenderer _wave;
        private Sprite[] _waveFrames;
        private Material _additive;
        private float _animTime;

        public float WorldSize => Mathf.Max(0.2f, worldSize);
        public float XOffset => xOffset;
        public float HeightOffset => heightOffset;
        public Transform Follow => follow;

        private void OnEnable()
        {
            RefreshVisual(forceRebuild: true);
        }

        private void OnDisable()
        {
            DisposeAdditive();
        }

        private void LateUpdate()
        {
            var dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            _animTime += dt;
            RefreshVisual(forceRebuild: false);
            AnimateWave();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            worldSize = Mathf.Max(0.2f, worldSize);
            waveSizeScale = Mathf.Max(0.2f, waveSizeScale);
            if (isActiveAndEnabled)
            {
                RefreshVisual(forceRebuild: true);
            }
        }
#endif

        [ContextMenu("Save Size To Charlotte Skill 3 Dome")]
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

            tuning.ApplyDome(WorldSize, XOffset, HeightOffset);

            var choreo = FindAnyObjectByType<CharlotteSkillChoreographer>();
            choreo?.ApplyDomeTuning(WorldSize, XOffset, HeightOffset);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(tuning);
            UnityEditor.EditorUtility.SetDirty(tuning.gameObject);
            if (choreo != null)
            {
                UnityEditor.EditorUtility.SetDirty(choreo);
            }

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tuning.gameObject.scene);
            }
#endif
            Debug.Log(
                $"[CharlotteDome] Saved Skill3 dome worldSize={WorldSize:F2} x={XOffset:F2} height={HeightOffset:F2}");
        }

        public void BindFollow(Transform target)
        {
            follow = target;
            RefreshVisual(forceRebuild: false);
        }

        public void SetTuning(float size, float x, float height)
        {
            worldSize = Mathf.Max(0.2f, size);
            xOffset = x;
            heightOffset = height;
            RefreshVisual(forceRebuild: true);
        }

        public void SetWorldSize(float size)
        {
            worldSize = Mathf.Max(0.2f, size);
            RefreshVisual(forceRebuild: true);
        }

        public void SetOffsets(float x, float height)
        {
            xOffset = x;
            heightOffset = height;
            RefreshVisual(forceRebuild: true);
        }

        public Vector3 ResolveAnchorWorld()
        {
            var anchor = follow != null ? follow.position : transform.position;
            var feetY = follow != null ? ResolveFeetY(follow) : anchor.y;
            return new Vector3(anchor.x + xOffset, feetY + heightOffset, anchor.z);
        }

        public void RefreshVisual(bool forceRebuild)
        {
            EnsureVisual(forceRebuild);
            ApplyLayout();
        }

        private void EnsureVisual(bool forceRebuild)
        {
            EnsureAdditive();

            _ring = ResolveChildRenderer("Ring", ref forceRebuild);
            if (_ring != null && (_ring.sprite == null || forceRebuild))
            {
                _ring.sprite = LoadSprite("charlotte_vfx_dome_ring_v1");
                if (_additive != null)
                {
                    _ring.sharedMaterial = _additive;
                }
            }

            _waveFrames ??= new[]
            {
                LoadSprite("charlotte_vfx_dome_wave_orbit_f1"),
                LoadSprite("charlotte_vfx_dome_wave_orbit_f2"),
                LoadSprite("charlotte_vfx_dome_wave_orbit_f3"),
                LoadSprite("charlotte_vfx_dome_wave_orbit_f4")
            };

            if (showWaveOrbit)
            {
                _wave = ResolveChildRenderer("WaveOrbit", ref forceRebuild);
                if (_wave != null)
                {
                    if (_wave.sprite == null && _waveFrames[0] != null)
                    {
                        _wave.sprite = _waveFrames[0];
                    }

                    if (_additive != null)
                    {
                        _wave.sharedMaterial = _additive;
                    }

                    _wave.enabled = true;
                }
            }
            else if (_wave != null)
            {
                _wave.enabled = false;
            }
        }

        private SpriteRenderer ResolveChildRenderer(string childName, ref bool forceRebuild)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                child = go.transform;
                forceRebuild = true;
            }

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = child.gameObject.AddComponent<SpriteRenderer>();
                forceRebuild = true;
            }

            return sr;
        }

        private void ApplyLayout()
        {
            if (_ring == null || _ring.sprite == null)
            {
                return;
            }

            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.position = ResolveAnchorWorld();

            FitWorld(_ring, WorldSize);
            _ring.color = tint;
            _ring.sortingOrder = sortingOrder;
            _ring.enabled = true;

            if (_wave != null && _wave.enabled && _wave.sprite != null)
            {
                var pulse = 1f + Mathf.Sin(_animTime * 7.5f) * 0.06f;
                FitWorld(_wave, WorldSize * Mathf.Max(0.2f, waveSizeScale) * pulse);
                _wave.color = new Color(1f, 0.95f, 0.7f, 0.85f + 0.15f * Mathf.Abs(Mathf.Sin(_animTime * 3.6f)));
                _wave.sortingOrder = sortingOrder + 2;
                _wave.transform.localPosition = Vector3.zero;
                _wave.transform.localRotation = Quaternion.identity;
            }
        }

        private void AnimateWave()
        {
            if (_wave == null || !_wave.enabled || _waveFrames == null || _waveFrames.Length == 0)
            {
                return;
            }

            var index = Mathf.FloorToInt(_animTime * Mathf.Max(1f, waveFps)) % _waveFrames.Length;
            if (index < 0)
            {
                index += _waveFrames.Length;
            }

            var sprite = _waveFrames[index];
            if (sprite != null && _wave.sprite != sprite)
            {
                _wave.sprite = sprite;
            }
        }

        private static void FitWorld(SpriteRenderer sr, float worldSize)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            sr.transform.localPosition = Vector3.zero;
            sr.transform.localRotation = Quaternion.identity;

            var native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            if (native < 0.001f)
            {
                return;
            }

            var parent = sr.transform.parent;
            var parentScale = 1f;
            if (parent != null)
            {
                parentScale = Mathf.Max(
                    Mathf.Abs(parent.lossyScale.x),
                    Mathf.Abs(parent.lossyScale.y));
                if (parentScale < 0.0001f)
                {
                    parentScale = 1f;
                }
            }

            var local = worldSize / (native * parentScale);
            sr.transform.localScale = new Vector3(local, local, 1f);
        }

        private void EnsureAdditive()
        {
            if (_additive != null)
            {
                return;
            }

            var shader = Shader.Find("FracturedChorus/VFX/RenBulletAdditive")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            _additive = new Material(shader)
            {
                name = "CharlotteDomePreviewAdditive",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void DisposeAdditive()
        {
            if (_additive == null)
            {
                return;
            }

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

        private static float ResolveFeetY(Transform target)
        {
            var view = target.GetComponent<UnitView>();
            return view != null ? view.FeetWorldPosition.y : target.position.y;
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
