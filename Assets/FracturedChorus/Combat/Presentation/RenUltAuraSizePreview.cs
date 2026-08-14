using FracturedChorus.Combat.Grid;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [ExecuteAlways]
    public class RenUltAuraSizePreview : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Ren/";

        [Header("Anchors")]
        [SerializeField] private Transform follow;
        [SerializeField] private Transform target;
        [SerializeField] private bool useEncounterStage = true;
        [SerializeField] private int stageRow = 1;
        [SerializeField] private int stageColumn;
        [SerializeField] private float sideGap = HexBoardLayout.DefaultSideGap;
        [SerializeField] [Range(0.2f, 2f)] private float bodyHeight = 0.7f;
        [SerializeField] [Range(0.2f, 2f)] private float aimHeight = 0.55f;

        [Header("Skill 3 — Ult aura")]
        [SerializeField] [Range(0.8f, 8f)] private float auraWorldSize = 2.8f;
        [SerializeField] [Range(0.3f, 4f)] private float orbitRadius = 1.15f;
        [SerializeField] [Range(0.4f, 3f)] private float bulletHeadWorldSize = 1.275f;
        [SerializeField] private int sortingOrder = 38;
        [SerializeField] private bool showBullets = true;

        private SpriteRenderer _glow;
        private SpriteRenderer _wave;
        private SpriteRenderer _notes;
        private SpriteRenderer _bulletA;
        private SpriteRenderer _bulletB;
        private Material _additive;
        private float _animTime;

        public float AuraWorldSize => Mathf.Max(0.4f, auraWorldSize);
        public float OrbitRadius => Mathf.Max(0.2f, orbitRadius);
        public float BulletHeadWorldSize => Mathf.Max(0.3f, bulletHeadWorldSize);
        public float BodyHeight => bodyHeight;
        public float AimHeight => aimHeight;

        private void OnEnable()
        {
            RefreshVisual(true);
        }

        private void OnDisable()
        {
            DisposeAdditive();
        }

        private void LateUpdate()
        {
            _animTime += Application.isPlaying ? Time.deltaTime : 0.016f;
            RefreshVisual(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                RefreshVisual(true);
            }
        }
#endif

        public void Bind(Transform followTransform, Transform targetTransform)
        {
            follow = followTransform;
            target = targetTransform;
            RefreshVisual(true);
        }

        public void SetTuning(float worldSize, float orbit, float bulletSize, float body, float aim)
        {
            auraWorldSize = Mathf.Max(0.4f, worldSize);
            orbitRadius = Mathf.Max(0.2f, orbit);
            bulletHeadWorldSize = Mathf.Max(0.3f, bulletSize);
            bodyHeight = body;
            aimHeight = aim;
            RefreshVisual(true);
        }

        [ContextMenu("Save To Ren Skill 3")]
        public void SaveToChoreographer()
        {
            var choreo = FindAnyObjectByType<PlayerSkillShotChoreographer>();
            if (choreo == null)
            {
                var host = GameObject.Find("CombatRoot") ?? gameObject;
                choreo = host.GetComponent<PlayerSkillShotChoreographer>();
                if (choreo == null)
                {
                    choreo = host.AddComponent<PlayerSkillShotChoreographer>();
                }
            }

            choreo.ApplySkill3Tuning(AuraWorldSize, OrbitRadius, BulletHeadWorldSize, AimHeight);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(choreo);
            UnityEditor.EditorUtility.SetDirty(choreo.gameObject);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(choreo.gameObject.scene);
            }
#endif
            Debug.Log(
                $"[RenSkill3] Saved aura={AuraWorldSize:F2} orbit={OrbitRadius:F2} bullet={BulletHeadWorldSize:F2}");
        }

        public void RefreshVisual(bool forceRebuild)
        {
            EnsureVisual(forceRebuild);
            ApplyLayout();
        }

        private void EnsureVisual(bool forceRebuild)
        {
            EnsureAdditive();

            _glow = ResolveChild("Glow", ref forceRebuild);
            if (_glow != null && (_glow.sprite == null || forceRebuild))
            {
                _glow.sprite = LoadSprite("ren_ult_eerie_aura_glow_v1")
                               ?? LoadSprite("ren_ult_red_aura_glow_v1");
                if (_additive != null)
                {
                    _glow.sharedMaterial = _additive;
                }
            }

            _wave = ResolveChild("Wave", ref forceRebuild);
            if (_wave != null && (_wave.sprite == null || forceRebuild))
            {
                _wave.sprite = LoadSprite("ren_ult_eerie_waveforms_v1");
                if (_additive != null)
                {
                    _wave.sharedMaterial = _additive;
                }
            }

            _notes = ResolveChild("Notes", ref forceRebuild);
            if (_notes != null && (_notes.sprite == null || forceRebuild))
            {
                _notes.sprite = LoadSprite("ren_ult_eerie_notes_v1")
                                ?? LoadSprite("ren_ult_red_notes_v1");
                if (_additive != null)
                {
                    _notes.sharedMaterial = _additive;
                }
            }

            if (showBullets)
            {
                _bulletA = ResolveChild("BulletA", ref forceRebuild);
                _bulletB = ResolveChild("BulletB", ref forceRebuild);
                var head = LoadSprite("ren_ult_bullet_flight_v1")
                           ?? LoadSprite("ren_bullet_flight_01_v1")
                           ?? LoadSprite("ren_bullet_head_v1");
                if (_bulletA != null && (_bulletA.sprite == null || forceRebuild))
                {
                    _bulletA.sprite = head;
                    if (_additive != null)
                    {
                        _bulletA.sharedMaterial = _additive;
                    }
                }

                if (_bulletB != null && (_bulletB.sprite == null || forceRebuild))
                {
                    _bulletB.sprite = head;
                    if (_additive != null)
                    {
                        _bulletB.sharedMaterial = _additive;
                    }
                }
            }
        }

        private void ApplyLayout()
        {
            ResolveFeet(out var renFeet, out var bossFeet);
            var center = new Vector3(renFeet.x, renFeet.y + bodyHeight, renFeet.z);
            var from = new Vector3(renFeet.x, renFeet.y + aimHeight, renFeet.z);
            var to = new Vector3(bossFeet.x, bossFeet.y + aimHeight, bossFeet.z);
            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;

            if (_glow != null && _glow.sprite != null)
            {
                var pulse = 1f + Mathf.Sin(_animTime * 8f) * 0.05f;
                _glow.enabled = true;
                _glow.transform.position = center;
                _glow.transform.localRotation = Quaternion.Euler(0f, 0f, _animTime * 12f);
                Fit(_glow, AuraWorldSize * pulse);
                _glow.color = new Color(1f, 0.2f, 0.28f, 0.9f);
                _glow.sortingOrder = sortingOrder;
            }

            if (_wave != null && _wave.sprite != null)
            {
                var a = _animTime * 70f * Mathf.Deg2Rad;
                _wave.enabled = true;
                _wave.transform.position = center + new Vector3(Mathf.Cos(a) * OrbitRadius, Mathf.Sin(a) * OrbitRadius * 0.55f, 0f);
                _wave.transform.localRotation = Quaternion.Euler(0f, 0f, _animTime * 40f);
                Fit(_wave, AuraWorldSize * 0.42f);
                _wave.color = new Color(1f, 0.35f, 0.45f, 0.75f);
                _wave.sortingOrder = sortingOrder + 1;
            }

            if (_notes != null && _notes.sprite != null)
            {
                var a = (_animTime * 55f + 140f) * Mathf.Deg2Rad;
                _notes.enabled = true;
                _notes.transform.position = center + new Vector3(Mathf.Cos(a) * OrbitRadius * 0.85f, Mathf.Sin(a) * OrbitRadius * 0.5f, 0f);
                _notes.transform.localRotation = Quaternion.Euler(0f, 0f, -_animTime * 25f);
                Fit(_notes, AuraWorldSize * 0.38f);
                _notes.color = new Color(1f, 0.45f, 0.55f, 0.8f);
                _notes.sortingOrder = sortingOrder + 2;
            }

            if (!showBullets)
            {
                if (_bulletA != null)
                {
                    _bulletA.enabled = false;
                }

                if (_bulletB != null)
                {
                    _bulletB.enabled = false;
                }

                return;
            }

            PlaceBullet(_bulletA, from, to, 0.18f);
            PlaceBullet(_bulletB, from, to, -0.18f);
        }

        private void PlaceBullet(SpriteRenderer sr, Vector3 from, Vector3 to, float yOffset)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var mid = Vector3.Lerp(from, to, 0.45f) + Vector3.up * yOffset;
            var delta = to - from;
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            sr.enabled = true;
            sr.transform.SetPositionAndRotation(mid, Quaternion.Euler(0f, 0f, angle));
            Fit(sr, BulletHeadWorldSize);
            sr.color = Color.white;
            sr.sortingOrder = sortingOrder + 3;
        }

        private void ResolveFeet(out Vector3 renFeet, out Vector3 bossFeet)
        {
            if (useEncounterStage)
            {
                renFeet = HexBoardLayout.GetWorldPosition(
                    new GridPosition(GridSide.Player, stageRow, stageColumn), sideGap);
                bossFeet = HexBoardLayout.GetWorldPosition(
                    new GridPosition(GridSide.Enemy, stageRow, stageColumn), sideGap);
                return;
            }

            var renPos = follow != null ? follow.position : transform.position;
            var bossPos = target != null ? target.position : renPos + Vector3.right * 6f;
            renFeet = ResolveFeet(follow, renPos);
            bossFeet = ResolveFeet(target, bossPos);
        }

        private static Vector3 ResolveFeet(Transform t, Vector3 fallback)
        {
            if (t == null)
            {
                return fallback;
            }

            var view = t.GetComponent<UnitView>();
            return view != null ? view.FeetWorldPosition : t.position;
        }

        private SpriteRenderer ResolveChild(string childName, ref bool forceRebuild)
        {
            var child = transform.Find(childName);
            if (child == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(transform, false);
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

        private static void Fit(SpriteRenderer sr, float worldSize)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            var scale = native > 0.001f ? worldSize / native : 1f;
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
                name = "RenUltAuraPreviewAdditive",
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
