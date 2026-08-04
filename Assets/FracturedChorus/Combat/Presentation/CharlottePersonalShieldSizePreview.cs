using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [ExecuteAlways]
    public class CharlottePersonalShieldSizePreview : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Charlotte/";
        private const int WaveCount = 6;
        private const int NoteCount = 8;

        [Header("Skill 1 — Personal orbit (không dùng Scale tool)")]
        [SerializeField] [Range(0.4f, 6f)] private float worldSize = 3.54f;
        [SerializeField] [Range(-1f, 3f)] private float heightOffset = 0.39f;
        [SerializeField] [Range(0.35f, 3.5f)] private float orbitRadius = 0.93f;
        [SerializeField] private Transform follow;
        [SerializeField] private Color tint = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private int sortingOrder = 43;
        [SerializeField] [Range(1f, 30f)] private float waveFps = 12f;

        private SpriteRenderer _halo;
        private SpriteRenderer[] _waves;
        private SpriteRenderer[] _notes;
        private float[] _waveAngles;
        private float[] _waveRadii;
        private float[] _waveSpin;
        private float[] _noteAngles;
        private float[] _noteRadii;
        private float[] _noteBob;
        private Sprite[] _waveFrames;
        private Sprite[] _noteSprites;
        private Material _additive;
        private float _animTime;
        private float _pulse;
        private bool _built;

        public float WorldSize => Mathf.Max(0.2f, worldSize);
        public float HeightOffset => heightOffset;
        public float OrbitRadius => Mathf.Max(0.35f, orbitRadius);
        public Transform Follow => follow;

        private void OnEnable()
        {
            RefreshVisual(true);
        }

        private void OnDisable()
        {
            DisposeAdditive();
            _built = false;
        }

        private void LateUpdate()
        {
            var dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            _animTime += dt;
            _pulse += dt * 5.2f;
            RefreshVisual(false);
            AnimateOrbit(dt);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            worldSize = Mathf.Max(0.2f, worldSize);
            orbitRadius = Mathf.Max(0.35f, orbitRadius);
            if (isActiveAndEnabled)
            {
                RefreshVisual(true);
            }
        }
#endif

        [ContextMenu("Save Size To Charlotte Skill 1 Personal Shield")]
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

            tuning.ApplyPersonal(WorldSize, HeightOffset, OrbitRadius);

            var choreo = FindAnyObjectByType<CharlotteSkillChoreographer>();
            choreo?.ApplyPersonalShieldTuning(WorldSize, HeightOffset, OrbitRadius);

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
                $"[CharlotteSkill1] Saved personal shield worldSize={WorldSize:F2} height={HeightOffset:F2} orbit={OrbitRadius:F2}");
        }

        public void BindFollow(Transform target)
        {
            follow = target;
            RefreshVisual(false);
        }

        public void SetTuning(float size, float height, float orbit)
        {
            worldSize = Mathf.Max(0.2f, size);
            heightOffset = height;
            orbitRadius = Mathf.Max(0.35f, orbit);
            RefreshVisual(true);
        }

        public void SetWorldSize(float size)
        {
            worldSize = Mathf.Max(0.2f, size);
            RefreshVisual(true);
        }

        public void SetHeightOffset(float height)
        {
            heightOffset = height;
            RefreshVisual(false);
        }

        public void SetOrbitRadius(float radius)
        {
            orbitRadius = Mathf.Max(0.35f, radius);
            RefreshVisual(true);
        }

        public Vector3 ResolveAnchorWorld()
        {
            var anchor = follow != null ? follow.position : transform.position;
            var feetY = follow != null ? ResolveFeetY(follow) : anchor.y;
            return new Vector3(anchor.x, feetY + heightOffset, anchor.z);
        }

        public void RefreshVisual(bool forceRebuild)
        {
            EnsureVisual(forceRebuild);
            ApplyLayout();
        }

        private void EnsureVisual(bool forceRebuild)
        {
            EnsureAdditive();
            EnsureSprites();

            if (forceRebuild || !_built)
            {
                ClearChildren();
                BuildRenderers();
                _built = true;
            }
        }

        private void EnsureSprites()
        {
            _waveFrames ??= new[]
            {
                LoadSprite("charlotte_vfx_dome_wave_orbit_f1"),
                LoadSprite("charlotte_vfx_dome_wave_orbit_f2"),
                LoadSprite("charlotte_vfx_dome_wave_orbit_f3"),
                LoadSprite("charlotte_vfx_dome_wave_orbit_f4")
            };

            if (_noteSprites == null || _noteSprites.Length == 0)
            {
                _noteSprites = LoadNoteSprites();
            }
        }

        private void BuildRenderers()
        {
            var haloSprite = PickWave(0) ?? PickNote(0);
            if (haloSprite != null)
            {
                _halo = CreateRenderer("Halo", haloSprite, sortingOrder);
            }

            _waves = new SpriteRenderer[WaveCount];
            _waveAngles = new float[WaveCount];
            _waveRadii = new float[WaveCount];
            _waveSpin = new float[WaveCount];
            for (var i = 0; i < WaveCount; i++)
            {
                _waves[i] = CreateRenderer("Wave" + i, PickWave(i), sortingOrder + 1);
                _waveAngles[i] = (i / (float)WaveCount) * Mathf.PI * 2f;
                _waveRadii[i] = OrbitRadius * (i % 2 == 0 ? 1f : 0.78f);
                _waveSpin[i] = (i % 2 == 0 ? 1f : -1f) * (70f + i * 8f);
            }

            _notes = new SpriteRenderer[NoteCount];
            _noteAngles = new float[NoteCount];
            _noteRadii = new float[NoteCount];
            _noteBob = new float[NoteCount];
            for (var i = 0; i < NoteCount; i++)
            {
                _notes[i] = CreateRenderer("Note" + i, PickNote(i), sortingOrder + 2);
                _noteAngles[i] = (i / (float)NoteCount) * Mathf.PI * 2f + 0.35f;
                _noteRadii[i] = OrbitRadius * (0.75f + (i % 3) * 0.12f);
                _noteBob[i] = i * 0.7f;
            }
        }

        private void ApplyLayout()
        {
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            transform.position = ResolveAnchorWorld();

            var pulse = 1f + Mathf.Sin(_pulse * 1.4f) * 0.08f;
            if (_halo != null && _halo.sprite != null)
            {
                Fit(_halo, WorldSize * 0.85f * pulse);
                var t = tint;
                _halo.color = new Color(t.r, t.g, t.b, 0.55f);
                _halo.sortingOrder = sortingOrder;
            }

            if (_waves != null)
            {
                for (var i = 0; i < _waves.Length; i++)
                {
                    if (_waves[i] == null)
                    {
                        continue;
                    }

                    _waveRadii[i] = OrbitRadius * (i % 2 == 0 ? 1f : 0.78f);
                    Fit(_waves[i], WorldSize * 0.42f * pulse);
                    var t = tint;
                    _waves[i].color = new Color(t.r, t.g, t.b, 0.85f);
                    _waves[i].sortingOrder = sortingOrder + 1;
                }
            }

            if (_notes != null)
            {
                for (var i = 0; i < _notes.Length; i++)
                {
                    if (_notes[i] == null)
                    {
                        continue;
                    }

                    _noteRadii[i] = OrbitRadius * (0.75f + (i % 3) * 0.12f);
                    Fit(_notes[i], WorldSize * 0.18f);
                    var t = tint;
                    _notes[i].color = new Color(1f, t.g, t.b * 0.7f, 0.9f);
                    _notes[i].sortingOrder = sortingOrder + 2;
                }
            }
        }

        private void AnimateOrbit(float dt)
        {
            AdvanceWaveFrames();
            var pulse = 1f + Mathf.Sin(_pulse * 1.4f) * 0.08f;

            if (_halo != null)
            {
                _halo.transform.localRotation = Quaternion.Euler(0f, 0f, -_pulse * 35f);
            }

            if (_waves != null)
            {
                for (var i = 0; i < _waves.Length; i++)
                {
                    var wave = _waves[i];
                    if (wave == null)
                    {
                        continue;
                    }

                    _waveAngles[i] += dt * (_waveSpin[i] * Mathf.Deg2Rad);
                    var r = _waveRadii[i] * pulse;
                    wave.transform.localPosition = new Vector3(
                        Mathf.Cos(_waveAngles[i]) * r,
                        Mathf.Sin(_waveAngles[i]) * r * 0.55f,
                        0f);
                    wave.transform.localRotation = Quaternion.Euler(0f, 0f, _waveAngles[i] * Mathf.Rad2Deg + 90f);
                }
            }

            if (_notes != null)
            {
                for (var i = 0; i < _notes.Length; i++)
                {
                    var note = _notes[i];
                    if (note == null)
                    {
                        continue;
                    }

                    _noteAngles[i] += dt * 1.8f * (i % 2 == 0 ? 1f : -1.15f);
                    _noteBob[i] += dt * 5f;
                    var r = _noteRadii[i] * (0.95f + 0.08f * Mathf.Sin(_noteBob[i]));
                    var bobY = Mathf.Sin(_noteBob[i]) * 0.08f;
                    note.transform.localPosition = new Vector3(
                        Mathf.Cos(_noteAngles[i]) * r,
                        Mathf.Sin(_noteAngles[i]) * r * 0.6f + bobY,
                        0f);
                    note.transform.localRotation = Quaternion.Euler(0f, 0f, _noteAngles[i] * Mathf.Rad2Deg);
                }
            }
        }

        private void AdvanceWaveFrames()
        {
            if (_waveFrames == null || _waveFrames.Length == 0)
            {
                return;
            }

            var index = Mathf.FloorToInt(_animTime * Mathf.Max(1f, waveFps)) % _waveFrames.Length;
            if (index < 0)
            {
                index += _waveFrames.Length;
            }

            var frame = _waveFrames[index];
            if (frame == null)
            {
                return;
            }

            if (_halo != null && _halo.sprite != frame)
            {
                _halo.sprite = frame;
            }

            if (_waves == null)
            {
                return;
            }

            for (var i = 0; i < _waves.Length; i++)
            {
                if (_waves[i] != null && _waves[i].sprite != frame)
                {
                    _waves[i].sprite = frame;
                }
            }
        }

        private SpriteRenderer CreateRenderer(string childName, Sprite sprite, int order)
        {
            if (sprite == null)
            {
                return null;
            }

            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (_additive != null)
            {
                sr.sharedMaterial = _additive;
            }

            return sr;
        }

        private void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            _halo = null;
            _waves = null;
            _notes = null;
        }

        private Sprite PickWave(int index)
        {
            if (_waveFrames == null || _waveFrames.Length == 0)
            {
                return null;
            }

            return _waveFrames[index % _waveFrames.Length];
        }

        private Sprite PickNote(int index)
        {
            if (_noteSprites == null || _noteSprites.Length == 0)
            {
                return PickWave(index);
            }

            return _noteSprites[index % _noteSprites.Length];
        }

        private static void Fit(SpriteRenderer sr, float size)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            var scale = native > 0.001f ? size / native : 1f;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
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
                name = "CharlottePersonalShieldPreviewAdditive",
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

        private static Sprite[] LoadNoteSprites()
        {
            var scatter = LoadSprite("charlotte_vfx_note_scatter_v1");
            var renNotes = Resources.LoadAll<Sprite>("VFX/Combat/Ren/ren_ult_eerie_notes_v1");
            if (renNotes != null && renNotes.Length > 1)
            {
                return renNotes;
            }

            var renSingle = Resources.Load<Sprite>("VFX/Combat/Ren/ren_ult_eerie_notes_v1")
                            ?? Resources.Load<Sprite>("VFX/Combat/Ren/ren_ult_red_notes_v1");
            if (scatter != null && renSingle != null)
            {
                return new[] { scatter, renSingle };
            }

            if (scatter != null)
            {
                return new[] { scatter };
            }

            return renSingle != null ? new[] { renSingle } : null;
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
