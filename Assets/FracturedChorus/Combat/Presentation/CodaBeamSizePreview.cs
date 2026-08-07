using FracturedChorus.Combat.Grid;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [ExecuteAlways]
    public class CodaBeamSizePreview : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Coda/";

        [Header("Anchors")]
        [SerializeField] private Transform caster;
        [SerializeField] private Transform target;
        [SerializeField] private bool useEncounterStage = true;
        [SerializeField] private int stageRow = 1;
        [SerializeField] private int stageColumn;
        [SerializeField] private float sideGap = HexBoardLayout.DefaultSideGap;
        [SerializeField] [Range(-4f, 4f)] private float castBackX = 0.55f;
        [SerializeField] [Range(0.1f, 2.5f)] private float aimHeight = 0.78f;

        [Header("Beam")]
        [SerializeField] [Range(0.3f, 5f)] private float beamThickness = 2.65f;
        [SerializeField] [Range(2f, 60f)] private float piercePast = 28f;
        [SerializeField] private bool pierceThroughMap = true;
        [SerializeField] [Range(0.4f, 5f)] private float chargeWorldSize = 1.85f;
        [SerializeField] [Range(0.6f, 6f)] private float impactWorldSize = 2.9f;
        [SerializeField] private int sortingOrder = 46;
        [SerializeField] private bool showCharge = true;
        [SerializeField] private bool showImpact = true;

        private SpriteRenderer _charge;
        private SpriteRenderer _beam;
        private SpriteRenderer _impact;
        private Material _additive;
        private float _animTime;

        public float CastBackX => castBackX;
        public float AimHeight => aimHeight;
        public float BeamThickness => Mathf.Max(0.2f, beamThickness);
        public float PiercePast => Mathf.Max(1f, piercePast);
        public bool PierceThroughMap => pierceThroughMap;
        public float ChargeWorldSize => Mathf.Max(0.3f, chargeWorldSize);
        public float ImpactWorldSize => Mathf.Max(0.4f, impactWorldSize);

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
            float castBack,
            float height,
            float thickness,
            float pierce,
            bool throughMap,
            float chargeSize,
            float impactSize)
        {
            castBackX = castBack;
            aimHeight = height;
            beamThickness = Mathf.Max(0.2f, thickness);
            piercePast = Mathf.Max(1f, pierce);
            pierceThroughMap = throughMap;
            chargeWorldSize = Mathf.Max(0.3f, chargeSize);
            impactWorldSize = Mathf.Max(0.4f, impactSize);
            RefreshVisual(true);
        }

        [ContextMenu("Save To Coda Skill 2")]
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

            choreo.ApplySkill2Tuning(
                CastBackX,
                AimHeight,
                BeamThickness,
                PiercePast,
                PierceThroughMap,
                ChargeWorldSize,
                ImpactWorldSize);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(choreo);
            UnityEditor.EditorUtility.SetDirty(choreo.gameObject);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(choreo.gameObject.scene);
            }
#endif
            Debug.Log(
                $"[CodaSkill2] Saved castBack={CastBackX:F2} thickness={BeamThickness:F2} pierce={PiercePast:F1} throughMap={PierceThroughMap}");
        }

        public void RefreshVisual(bool forceRebuild)
        {
            EnsureVisual(forceRebuild);
            ApplyLayout();
        }

        private void EnsureVisual(bool forceRebuild)
        {
            EnsureAdditive();

            if (showCharge)
            {
                _charge = ResolveChild("Charge", ref forceRebuild);
                if (_charge != null && (_charge.sprite == null || forceRebuild))
                {
                    _charge.sprite = LoadSprite("coda_vfx_beam_charge_v1")
                                     ?? LoadSprite("coda_vfx_impact_v1");
                    if (_additive != null)
                    {
                        _charge.sharedMaterial = _additive;
                    }
                }
            }
            else if (_charge != null)
            {
                _charge.enabled = false;
            }

            _beam = ResolveChild("Beam", ref forceRebuild);
            if (_beam != null && (_beam.sprite == null || forceRebuild))
            {
                _beam.sprite = LoadSprite("coda_vfx_beam_v1");
                if (_additive != null)
                {
                    _beam.sharedMaterial = _additive;
                }
            }

            if (showImpact)
            {
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
            }
            else if (_impact != null)
            {
                _impact.enabled = false;
            }
        }

        private void ApplyLayout()
        {
            ResolveAims(out var from, out var through);
            transform.position = Vector3.zero;
            transform.localScale = Vector3.one;

            if (_charge != null && showCharge && _charge.sprite != null)
            {
                var pulse = 1f + Mathf.Sin(_animTime * 10f) * 0.06f;
                _charge.enabled = true;
                _charge.transform.position = from;
                _charge.transform.localRotation = Quaternion.Euler(0f, 0f, -_animTime * 40f);
                Fit(_charge, ChargeWorldSize * pulse);
                _charge.color = new Color(0.75f, 0.95f, 1f, 0.95f);
                _charge.sortingOrder = sortingOrder + 1;
            }

            if (_beam != null && _beam.sprite != null)
            {
                var dir = through - from;
                dir.z = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                {
                    dir = Vector3.right;
                }

                dir.Normalize();
                var end = ResolvePierceEnd(from, through, dir);
                var mid = (from + end) * 0.5f;
                var delta = end - from;
                var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                var length = Vector2.Distance(new Vector2(from.x, from.y), new Vector2(end.x, end.y));
                var native = _beam.sprite.bounds.size;
                var sx = native.x > 0.001f ? length / native.x : 1f;
                var sy = native.y > 0.001f ? BeamThickness / native.y : 1f;
                var flicker = 1f + Mathf.Sin(_animTime * 28f) * 0.05f;
                _beam.enabled = true;
                _beam.transform.SetPositionAndRotation(mid, Quaternion.Euler(0f, 0f, angle));
                _beam.transform.localScale = new Vector3(sx, sy * flicker, 1f);
                _beam.color = Color.white;
                _beam.sortingOrder = sortingOrder;
            }

            if (_impact != null && showImpact && _impact.sprite != null)
            {
                var pulse = 1f + Mathf.Sin(_animTime * 14f) * 0.08f;
                _impact.enabled = true;
                _impact.transform.position = through;
                _impact.transform.localRotation = Quaternion.Euler(0f, 0f, _animTime * 35f);
                Fit(_impact, ImpactWorldSize * pulse);
                _impact.color = new Color(1f, 0.9f, 1f, 0.95f);
                _impact.sortingOrder = sortingOrder + 2;
            }
        }

        private void ResolveAims(out Vector3 from, out Vector3 through)
        {
            ResolveFeet(out var casterFeet, out var targetFeet);
            var dir = Mathf.Sign(targetFeet.x - casterFeet.x);
            if (Mathf.Approximately(dir, 0f))
            {
                dir = 1f;
            }

            from = new Vector3(casterFeet.x - dir * castBackX, casterFeet.y + aimHeight, casterFeet.z);
            through = new Vector3(targetFeet.x, targetFeet.y + aimHeight, targetFeet.z);
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
            casterFeet = new Vector3(ResolveFeetX(caster, casterPos.x), ResolveFeetY(caster, casterPos.y), casterPos.z);
            targetFeet = new Vector3(ResolveFeetX(target, targetPos.x), ResolveFeetY(target, targetPos.y), targetPos.z);
        }

        private Vector3 ResolvePierceEnd(Vector3 from, Vector3 through, Vector3 dir)
        {
            var minPast = PiercePast;
            if (!pierceThroughMap)
            {
                return through + dir * minPast;
            }

            var cam = Camera.main;
#if UNITY_EDITOR
            if (cam == null)
            {
                var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cams != null && cams.Length > 0)
                {
                    cam = cams[0];
                }
            }
#endif
            if (cam != null && cam.orthographic)
            {
                var halfW = cam.orthographicSize * Mathf.Max(0.01f, cam.aspect);
                var centerX = cam.transform.position.x;
                var edgeX = dir.x >= 0f ? centerX + halfW + 1.5f : centerX - halfW - 1.5f;
                var dx = edgeX - from.x;
                if (Mathf.Abs(dir.x) > 0.001f && Mathf.Sign(dx) == Mathf.Sign(dir.x))
                {
                    var dist = Mathf.Abs(dx / dir.x);
                    dist = Mathf.Max(dist, Vector2.Distance(from, through) + minPast * 0.35f);
                    return from + dir * dist;
                }
            }

            var fallback = Vector2.Distance(new Vector2(from.x, from.y), new Vector2(through.x, through.y))
                           + minPast;
            return from + dir * Mathf.Max(minPast, fallback);
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

        private static float ResolveFeetX(Transform t, float fallback)
        {
            if (t == null)
            {
                return fallback;
            }

            var view = t.GetComponent<UnitView>();
            return view != null ? view.FeetWorldPosition.x : t.position.x;
        }

        private static float ResolveFeetY(Transform t, float fallback)
        {
            if (t == null)
            {
                return fallback;
            }

            var view = t.GetComponent<UnitView>();
            return view != null ? view.FeetWorldPosition.y : t.position.y;
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
                name = "CodaBeamPreviewAdditive",
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
