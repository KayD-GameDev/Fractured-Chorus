using FracturedChorus.Combat.Grid;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [ExecuteAlways]
    public class CodaStarHitSizePreview : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Coda/";

        [Header("Anchors")]
        [SerializeField] private Transform caster;
        [SerializeField] private Transform target;
        [SerializeField] private bool useEncounterStage = true;
        [SerializeField] private int stageRow = 1;
        [SerializeField] private int stageColumn;
        [SerializeField] private float sideGap = HexBoardLayout.DefaultSideGap;

        [Header("Skill 1 — Star hit")]
        [SerializeField] [Range(0.5f, 6f)] private float standoffX = 2.45f;
        [SerializeField] [Range(0.1f, 2.5f)] private float contactHeight = 0.8f;
        [SerializeField] [Range(0.6f, 6f)] private float hitWorldSize = 2.9f;
        [SerializeField] [Range(0.1f, 1.5f)] private float debrisWorldSize = 0.45f;
        [SerializeField] [Range(5, 18)] private int debrisCount = 12;
        [SerializeField] private int sortingOrder = 46;
        [SerializeField] private bool showStandoffMarker = true;
        [SerializeField] private bool showDebris = true;

        private SpriteRenderer _impact;
        private SpriteRenderer _standoff;
        private readonly SpriteRenderer[] _debris = new SpriteRenderer[18];
        private Sprite[] _debrisSprites;
        private Material _additive;
        private float _animTime;

        public float StandoffX => Mathf.Max(0.2f, standoffX);
        public float ContactHeight => contactHeight;
        public float HitWorldSize => Mathf.Max(0.4f, hitWorldSize);
        public float DebrisWorldSize => Mathf.Max(0.1f, debrisWorldSize);
        public int DebrisCount => Mathf.Clamp(debrisCount, 5, 18);

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

        public void Bind(Transform casterTransform, Transform targetTransform)
        {
            caster = casterTransform;
            target = targetTransform;
            RefreshVisual(true);
        }

        public void SetTuning(
            float standoff,
            float height,
            float hitSize,
            float debrisSize,
            int debris)
        {
            standoffX = Mathf.Max(0.2f, standoff);
            contactHeight = height;
            hitWorldSize = Mathf.Max(0.4f, hitSize);
            debrisWorldSize = Mathf.Max(0.1f, debrisSize);
            debrisCount = Mathf.Clamp(debris, 5, 18);
            RefreshVisual(true);
        }

        [ContextMenu("Save To Coda Skill 1")]
        public void SaveToChoreographer()
        {
            var choreo = FindAnyObjectByType<CodaSkillChoreographer>();
            if (choreo == null)
            {
                var host = GameObject.Find("CombatRoot") ?? gameObject;
                choreo = host.GetComponent<CodaSkillChoreographer>();
                if (choreo == null)
                {
                    choreo = host.AddComponent<CodaSkillChoreographer>();
                }
            }

            choreo.ApplySkill1Tuning(
                StandoffX,
                ContactHeight,
                HitWorldSize,
                DebrisWorldSize,
                DebrisCount);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(choreo);
            UnityEditor.EditorUtility.SetDirty(choreo.gameObject);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(choreo.gameObject.scene);
            }
#endif
            Debug.Log(
                $"[CodaSkill1] Saved standoff={StandoffX:F2} height={ContactHeight:F2} hit={HitWorldSize:F2} debris={DebrisWorldSize:F2} x{DebrisCount}");
        }

        public void RefreshVisual(bool forceRebuild)
        {
            EnsureVisual(forceRebuild);
            ApplyLayout();
        }

        private void EnsureVisual(bool forceRebuild)
        {
            EnsureAdditive();
            EnsureDebrisSprites();

            _impact = ResolveChild("Impact", ref forceRebuild);
            if (_impact != null && (_impact.sprite == null || forceRebuild))
            {
                _impact.sprite = LoadSprite("coda_vfx_star_hit_v1")
                                 ?? LoadSprite("coda_vfx_impact_v1");
                if (_additive != null)
                {
                    _impact.sharedMaterial = _additive;
                }
            }

            if (showStandoffMarker)
            {
                _standoff = ResolveChild("Standoff", ref forceRebuild);
                if (_standoff != null && (_standoff.sprite == null || forceRebuild))
                {
                    _standoff.sprite = LoadSprite("coda_vfx_beam_charge_v1")
                                       ?? LoadSprite("coda_vfx_impact_v1");
                    if (_additive != null)
                    {
                        _standoff.sharedMaterial = _additive;
                    }
                }
            }
            else if (_standoff != null)
            {
                _standoff.enabled = false;
            }

            if (showDebris)
            {
                for (var i = 0; i < _debris.Length; i++)
                {
                    var child = ResolveChild($"Debris_{i}", ref forceRebuild);
                    _debris[i] = child;
                    if (child == null)
                    {
                        continue;
                    }

                    if (child.sprite == null || forceRebuild)
                    {
                        child.sprite = PickDebrisSprite(i);
                        if (_additive != null)
                        {
                            child.sharedMaterial = _additive;
                        }
                    }
                }
            }
        }

        private void ApplyLayout()
        {
            ResolveFeet(out var casterFeet, out var targetFeet);
            var dir = Mathf.Sign(targetFeet.x - casterFeet.x);
            if (Mathf.Approximately(dir, 0f))
            {
                dir = 1f;
            }

            var strikeFeet = new Vector3(targetFeet.x - dir * StandoffX, targetFeet.y, casterFeet.z);
            var contact = new Vector3(targetFeet.x, targetFeet.y + ContactHeight, targetFeet.z);

            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;

            if (_impact != null && _impact.sprite != null)
            {
                var pulse = 1f + Mathf.Sin(_animTime * 12f) * 0.06f;
                _impact.enabled = true;
                _impact.transform.position = contact;
                _impact.transform.localRotation = Quaternion.Euler(0f, 0f, _animTime * 28f);
                Fit(_impact, HitWorldSize * pulse);
                _impact.color = new Color(1f, 0.95f, 1f, 0.95f);
                _impact.sortingOrder = sortingOrder;
            }

            if (_standoff != null && showStandoffMarker && _standoff.sprite != null)
            {
                _standoff.enabled = true;
                _standoff.transform.position = strikeFeet + Vector3.up * ContactHeight;
                _standoff.transform.localRotation = Quaternion.Euler(0f, 0f, -_animTime * 50f);
                Fit(_standoff, 0.85f);
                _standoff.color = new Color(0.55f, 0.9f, 1f, 0.55f);
                _standoff.sortingOrder = sortingOrder - 1;
            }

            if (!showDebris)
            {
                for (var i = 0; i < _debris.Length; i++)
                {
                    if (_debris[i] != null)
                    {
                        _debris[i].enabled = false;
                    }
                }

                return;
            }

            var count = DebrisCount;
            for (var i = 0; i < _debris.Length; i++)
            {
                var sr = _debris[i];
                if (sr == null)
                {
                    continue;
                }

                if (i >= count || sr.sprite == null)
                {
                    sr.enabled = false;
                    continue;
                }

                var t = i / (float)Mathf.Max(1, count - 1);
                var angle = (i * 47.3f + _animTime * 18f) * Mathf.Deg2Rad;
                var radius = Mathf.Lerp(0.35f, 1.35f, t) * (0.85f + HitWorldSize * 0.12f);
                var offset = new Vector3(Mathf.Cos(angle) * radius * dir, Mathf.Sin(angle) * radius * 0.65f, 0f);
                sr.enabled = true;
                sr.transform.position = contact + offset;
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, _animTime * 40f + i * 20f);
                Fit(sr, DebrisWorldSize * (0.85f + 0.2f * Mathf.Sin(_animTime * 8f + i)));
                sr.color = new Color(1f, 0.95f, 1f, 0.85f);
                sr.sortingOrder = sortingOrder + 1;
            }
        }

        private void ResolveFeet(out Vector3 casterFeet, out Vector3 targetFeet)
        {
            if (useEncounterStage)
            {
                casterFeet = HexBoardLayout.GetWorldPosition(
                    new GridPosition(GridSide.Player, stageRow, stageColumn), sideGap);
                targetFeet = HexBoardLayout.GetWorldPosition(
                    new GridPosition(GridSide.Enemy, stageRow, stageColumn), sideGap);
                return;
            }

            var casterPos = caster != null ? caster.position : transform.position;
            var targetPos = target != null ? target.position : casterPos + Vector3.right * 6f;
            casterFeet = ResolveFeet(caster, casterPos);
            targetFeet = ResolveFeet(target, targetPos);
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

        private void EnsureDebrisSprites()
        {
            if (_debrisSprites != null && _debrisSprites.Length > 0)
            {
                return;
            }

            var path = ResourceRoot + "coda_vfx_star_debris_v1";
            var sliced = Resources.LoadAll<Sprite>(path);
            if (sliced != null && sliced.Length > 1)
            {
                _debrisSprites = sliced;
                return;
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                var fromSheet = CodaStarHitView.SliceStarSheet(tex);
                if (fromSheet != null && fromSheet.Length > 0)
                {
                    _debrisSprites = fromSheet;
                    return;
                }
            }

            var single = Resources.Load<Sprite>(path) ?? LoadSprite("coda_vfx_star_hit_v1");
            _debrisSprites = single != null ? new[] { single } : null;
        }

        private Sprite PickDebrisSprite(int index)
        {
            if (_debrisSprites == null || _debrisSprites.Length == 0)
            {
                return null;
            }

            return _debrisSprites[index % _debrisSprites.Length];
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
                name = "CodaStarHitPreviewAdditive",
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
