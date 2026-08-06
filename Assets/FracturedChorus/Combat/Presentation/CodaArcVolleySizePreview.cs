using FracturedChorus.Combat.Grid;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [ExecuteAlways]
    public class CodaArcVolleySizePreview : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Coda/";

        [Header("Anchors")]
        [SerializeField] private Transform caster;
        [SerializeField] private Transform target;
        [SerializeField] private bool useEncounterStage = true;
        [SerializeField] private bool autoSaveSceneOnApply = true;
        [SerializeField] private int stageRow = 1;
        [SerializeField] private int stageColumn;
        [SerializeField] private float sideGap = HexBoardLayout.DefaultSideGap;

        [Header("Skill 3 — Arc volley")]
        [SerializeField] [Range(-2f, 3f)] private float castBackX = 0.35f;
        [SerializeField] [Range(0.2f, 2.5f)] private float aimHeight = 0.8f;
        [SerializeField] [Range(0.2f, 1.5f)] private float chargeSeconds = 0.95f;
        [SerializeField] [Range(0.8f, 5f)] private float chargeWorldSize = 3.05f;
        [SerializeField] [Range(0.5f, 6f)] private float boltWorldSize = 3.05f;
        [SerializeField] [Range(0.4f, 6f)] private float arcSpreadY = 2.85f;
        [SerializeField] [Range(0.3f, 5f)] private float controlBulge = 2.55f;
        [SerializeField] [Range(0.6f, 6f)] private float impactWorldSize = 4.2f;
        [SerializeField] [Range(3, 8)] private int boltCount = 5;
        [SerializeField] private float boltFacingOffsetDegrees = 180f;
        [SerializeField] private bool invertArcNormal;
        [SerializeField] private int sortingOrder = 47;

        private SpriteRenderer _charge;
        private SpriteRenderer _impact;
        private readonly SpriteRenderer[] _bolts = new SpriteRenderer[8];
        private Material _additive;
        private float _animTime;

        public float CastBackX => castBackX;
        public float AimHeight => aimHeight;
        public float ChargeSeconds => Mathf.Max(0.12f, chargeSeconds);
        public float ChargeWorldSize => Mathf.Max(0.4f, chargeWorldSize);
        public float BoltWorldSize => Mathf.Max(0.3f, boltWorldSize);
        public float ArcSpreadY => Mathf.Max(0.2f, arcSpreadY);
        public float ControlBulge => Mathf.Max(0.2f, controlBulge);
        public float ImpactWorldSize => Mathf.Max(0.4f, impactWorldSize);

        private void OnEnable() => RefreshVisual(true);
        private void OnDisable() => DisposeAdditive();

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
            float chargeSize,
            float boltSize,
            float spread,
            float bulge,
            float impactSize)
        {
            castBackX = castBack;
            aimHeight = height;
            chargeWorldSize = Mathf.Max(0.4f, chargeSize);
            boltWorldSize = Mathf.Max(0.3f, boltSize);
            arcSpreadY = Mathf.Max(0.2f, spread);
            controlBulge = Mathf.Max(0.2f, bulge);
            impactWorldSize = Mathf.Max(0.4f, impactSize);
            RefreshVisual(true);
        }

        [ContextMenu("Save To Coda Skill 3")]
        public void SaveToChoreographer()
        {
            var choreo = FindAnyObjectByType<CodaSkillChoreographer>();
            if (choreo == null)
            {
                var host = GameObject.Find("CombatRoot") ?? gameObject;
                choreo = host.GetComponent<CodaSkillChoreographer>()
                         ?? host.AddComponent<CodaSkillChoreographer>();
            }

            choreo.ApplySkill3Tuning(
                CastBackX,
                AimHeight,
                ChargeSeconds,
                ChargeWorldSize,
                BoltWorldSize,
                ArcSpreadY,
                ControlBulge,
                ImpactWorldSize,
                boltFacingOffsetDegrees,
                invertArcNormal);

#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(choreo, "Save Coda Skill 3 Tuning");
            UnityEditor.EditorUtility.SetDirty(choreo);
            UnityEditor.EditorUtility.SetDirty(choreo.gameObject);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(choreo.gameObject.scene);
                if (autoSaveSceneOnApply)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(choreo.gameObject.scene);
                }
            }
#endif
            Debug.Log(
                $"[CodaSkill3] Saved castBack={CastBackX:F2} aimH={AimHeight:F2} charge={ChargeWorldSize:F2} bolt={BoltWorldSize:F2}");
        }

        public void RefreshVisual(bool forceRebuild)
        {
            EnsureVisual(forceRebuild);
            ApplyLayout();
        }

        private void EnsureVisual(bool forceRebuild)
        {
            EnsureAdditive();
            _charge = ResolveChild("Charge", ref forceRebuild);
            if (_charge != null && (_charge.sprite == null || forceRebuild))
            {
                _charge.sprite = LoadSprite("coda_vfx_beam_charge_v1");
                ApplyMat(_charge);
            }

            _impact = ResolveChild("Impact", ref forceRebuild);
            if (_impact != null && (_impact.sprite == null || forceRebuild))
            {
                _impact.sprite = LoadSprite("coda_vfx_arc_impact_v1")
                                 ?? LoadSprite("coda_vfx_star_hit_v1");
                ApplyMat(_impact);
            }

            var boltSprite = LoadSprite("coda_vfx_arc_bolt_v1")
                             ?? LoadSprite("coda_vfx_crescent_slash_v1");
            for (var i = 0; i < _bolts.Length; i++)
            {
                _bolts[i] = ResolveChild("Bolt_" + i, ref forceRebuild);
                if (_bolts[i] != null && (_bolts[i].sprite == null || forceRebuild))
                {
                    _bolts[i].sprite = boltSprite;
                    ApplyMat(_bolts[i]);
                }
            }
        }

        private void ApplyLayout()
        {
            ResolveAims(out var from, out var through);
            transform.position = Vector3.zero;

            if (_charge != null && _charge.sprite != null)
            {
                var pulse = 1f + Mathf.Sin(_animTime * 10f) * 0.06f;
                _charge.enabled = true;
                _charge.transform.position = from;
                _charge.transform.localRotation = Quaternion.Euler(0f, 0f, -_animTime * 50f);
                Fit(_charge, ChargeWorldSize * pulse);
                _charge.color = new Color(0.75f, 0.95f, 1f, 0.95f);
                _charge.sortingOrder = sortingOrder + 1;
            }

            if (_impact != null && _impact.sprite != null)
            {
                var pulse = 1f + Mathf.Sin(_animTime * 12f) * 0.07f;
                _impact.enabled = true;
                _impact.transform.position = through;
                _impact.transform.localRotation = Quaternion.Euler(0f, 0f, _animTime * 35f);
                Fit(_impact, ImpactWorldSize * pulse);
                _impact.color = new Color(1f, 0.9f, 1f, 0.9f);
                _impact.sortingOrder = sortingOrder + 2;
            }

            var count = Mathf.Clamp(boltCount, 3, 8);
            var flightT = Mathf.Repeat(_animTime * 0.55f, 1f);
            for (var i = 0; i < _bolts.Length; i++)
            {
                var sr = _bolts[i];
                if (sr == null)
                {
                    continue;
                }

                if (i >= count || sr.sprite == null)
                {
                    sr.enabled = false;
                    continue;
                }

                ResolveArc(from, through, i, count, out var start, out var control, out var end);
                var t = Mathf.Repeat(flightT + i * 0.12f, 1f);
                var pos = QuadBezier(start, control, end, t);
                var next = QuadBezier(start, control, end, Mathf.Min(1f, t + 0.02f));
                var delta = next - pos;
                var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + boltFacingOffsetDegrees;
                sr.enabled = true;
                sr.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, angle));
                Fit(sr, BoltWorldSize);
                sr.color = Color.white;
                sr.sortingOrder = sortingOrder;
            }
        }

        private void ResolveArc(
            Vector3 from,
            Vector3 through,
            int index,
            int count,
            out Vector3 start,
            out Vector3 control,
            out Vector3 end)
        {
            var dir = through - from;
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.right;
            }

            dir.Normalize();
            var normal = new Vector3(-dir.y, dir.x, 0f);
            if (invertArcNormal)
            {
                normal = -normal;
            }

            var u = count <= 1 ? 0.5f : index / (float)(count - 1);
            var signed = Mathf.Lerp(1f, -1f, u);
            var spread = ArcSpreadY * signed;
            var bulge = ControlBulge * (0.7f + 0.55f * (1f - Mathf.Abs(signed)));
            start = from + normal * spread * 0.35f;
            end = through;
            var mid = Vector3.Lerp(start, end, 0.4f);
            var side = signed >= 0f ? 1f : -1f;
            control = mid + normal * (bulge * side);
            if (Mathf.Abs(signed) < 0.08f)
            {
                control = mid + Vector3.up * bulge * 0.9f;
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

            var c = caster != null ? caster.position : transform.position;
            var t = target != null ? target.position : c + Vector3.right * 6f;
            casterFeet = ResolveUnitFeet(caster, c);
            targetFeet = ResolveUnitFeet(target, t);
        }

        private void ResolveAims(out Vector3 from, out Vector3 through)
        {
            if (useEncounterStage)
            {
                ResolveFeet(out var casterFeet, out var targetFeet);
                var dir = Mathf.Sign(targetFeet.x - casterFeet.x);
                if (Mathf.Approximately(dir, 0f))
                {
                    dir = 1f;
                }

                from = new Vector3(casterFeet.x - dir * castBackX, casterFeet.y + aimHeight, casterFeet.z);
                through = new Vector3(targetFeet.x, targetFeet.y + aimHeight, targetFeet.z);
                return;
            }

            from = ResolveBodyCenter(caster, aimHeight);
            through = ResolveBodyCenter(target, aimHeight);
            var d = Mathf.Sign(through.x - from.x);
            if (Mathf.Approximately(d, 0f))
            {
                d = 1f;
            }

            from = new Vector3(from.x - d * castBackX, from.y, from.z);
        }

        private static Vector3 ResolveBodyCenter(Transform tr, float heightFallback)
        {
            if (tr == null)
            {
                return Vector3.up * heightFallback;
            }

            var sr = tr.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                var b = sr.bounds;
                return new Vector3(b.center.x, b.center.y, tr.position.z);
            }

            var view = tr.GetComponent<UnitView>();
            if (view != null)
            {
                return view.FeetWorldPosition + Vector3.up * heightFallback;
            }

            return tr.position + Vector3.up * heightFallback;
        }

        private static Vector3 ResolveUnitFeet(Transform tr, Vector3 fallback)
        {
            if (tr == null)
            {
                return fallback;
            }

            var view = tr.GetComponent<UnitView>();
            return view != null ? view.FeetWorldPosition : tr.position;
        }

        private static Vector3 QuadBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
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

        private void ApplyMat(SpriteRenderer sr)
        {
            if (sr != null && _additive != null)
            {
                sr.sharedMaterial = _additive;
            }
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
                name = "CodaArcVolleyPreviewAdditive",
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
