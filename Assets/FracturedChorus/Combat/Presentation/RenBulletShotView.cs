using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class RenBulletShotView : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Ren/";
        private const float DefaultTravelSeconds = 0.28f;
        private const float ImpactSeconds = 0.16f;
        private const float HeadWorldSize = 1.275f;
        private const float TrailHeight = 0.825f;
        private const int SortingOrder = 40;

        private static Sprite s_head;
        private static Sprite s_trail;
        private static Sprite s_impact;
        private static Material s_additiveMat;
        private static bool s_loaded;

        private SpriteRenderer _trail;
        private SpriteRenderer _head;
        private SpriteRenderer _impact;
        private Coroutine _routine;

        public static RenBulletShotView Spawn(Vector3 from, Vector3 to, Transform parent = null)
        {
            EnsureSprites();
            var go = new GameObject("RenBulletShot");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<RenBulletShotView>();
            view.Build();
            view._routine = view.StartCoroutine(view.PlayRoutine(from, to));
            return view;
        }

        private void Build()
        {
            _trail = CreateChildRenderer("Trail", s_trail, additive: true);
            _head = CreateChildRenderer("Head", s_head, additive: false);
            _impact = CreateChildRenderer("Impact", s_impact, additive: true);
            if (_impact != null)
            {
                _impact.enabled = false;
            }
        }

        private SpriteRenderer CreateChildRenderer(string name, Sprite sprite, bool additive)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = SortingOrder;
            sr.color = Color.white;
            if (additive)
            {
                EnsureAdditiveMaterial();
                if (s_additiveMat != null)
                {
                    sr.sharedMaterial = s_additiveMat;
                }
            }

            return sr;
        }

        private static void EnsureSprites()
        {
            if (s_loaded)
            {
                return;
            }

            s_head = Resources.Load<Sprite>(ResourceRoot + "ren_bullet_head_v1");
            s_trail = Resources.Load<Sprite>(ResourceRoot + "ren_bullet_trail_v1");
            s_impact = Resources.Load<Sprite>(ResourceRoot + "ren_bullet_impact_v1");
            EnsureAdditiveMaterial();
            s_loaded = true;
        }

        private static void EnsureAdditiveMaterial()
        {
            if (s_additiveMat != null)
            {
                return;
            }

            var shader = Shader.Find("FracturedChorus/VFX/RenBulletAdditive")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            s_additiveMat = new Material(shader)
            {
                name = "RenBulletAdditive",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private IEnumerator PlayRoutine(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            delta.z = 0f;
            var distance = delta.magnitude;
            if (distance < 0.05f)
            {
                distance = 0.05f;
                delta = Vector3.right * distance;
            }

            var dir = delta / distance;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.SetPositionAndRotation(from, Quaternion.Euler(0f, 0f, angle));

            FitHead();
            FitTrail(0f);
            if (_impact != null)
            {
                _impact.transform.localPosition = Vector3.zero;
                FitImpact();
            }

            var elapsed = 0f;
            while (elapsed < DefaultTravelSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / DefaultTravelSeconds);
                var eased = 1f - (1f - t) * (1f - t);
                var pos = Vector3.Lerp(from, to, eased);
                transform.position = pos;

                var traveled = Vector3.Distance(from, pos);
                FitTrail(traveled);
                yield return null;
            }

            transform.position = to;
            FitTrail(distance);

            if (_head != null)
            {
                _head.enabled = false;
            }

            if (_trail != null)
            {
                _trail.enabled = false;
            }

            if (_impact != null)
            {
                _impact.enabled = true;
                _impact.transform.position = to;
                var impactElapsed = 0f;
                while (impactElapsed < ImpactSeconds)
                {
                    impactElapsed += Time.deltaTime;
                    var u = Mathf.Clamp01(impactElapsed / ImpactSeconds);
                    var scale = Mathf.Lerp(0.7f, 1.25f, u);
                    var alpha = 1f - u;
                    _impact.transform.localScale = Vector3.one * scale;
                    var c = _impact.color;
                    _impact.color = new Color(c.r, c.g, c.b, alpha);
                    yield return null;
                }
            }

            Destroy(gameObject);
        }

        private void FitHead()
        {
            if (_head == null || _head.sprite == null)
            {
                return;
            }

            var sprite = _head.sprite;
            var worldH = sprite.bounds.size.y;
            var scale = worldH > 0.001f ? HeadWorldSize / worldH : 1f;
            _head.transform.localScale = new Vector3(scale, scale, 1f);
            _head.transform.localPosition = Vector3.zero;
        }

        private void FitTrail(float traveled)
        {
            if (_trail == null || _trail.sprite == null)
            {
                return;
            }

            var length = Mathf.Max(0.15f, traveled);
            var sprite = _trail.sprite;
            var nativeW = Mathf.Max(0.001f, sprite.bounds.size.x);
            var nativeH = Mathf.Max(0.001f, sprite.bounds.size.y);
            var scaleX = length / nativeW;
            var scaleY = TrailHeight / nativeH;
            _trail.drawMode = SpriteDrawMode.Simple;
            _trail.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            _trail.transform.localPosition = new Vector3(-length * 0.5f, 0f, 0f);
            _trail.sortingOrder = SortingOrder - 1;
        }

        private void FitImpact()
        {
            if (_impact == null || _impact.sprite == null)
            {
                return;
            }

            var sprite = _impact.sprite;
            var worldH = sprite.bounds.size.y;
            var scale = worldH > 0.001f ? HeadWorldSize * 1.1f / worldH : 1f;
            _impact.transform.localScale = Vector3.one * scale;
            _impact.sortingOrder = SortingOrder + 1;
        }
    }
}
