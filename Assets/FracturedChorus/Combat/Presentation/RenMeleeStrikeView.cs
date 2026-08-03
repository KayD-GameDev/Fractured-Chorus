using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class RenMeleeStrikeView : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Ren/";
        private const float ArcSeconds = 0.14f;
        private const float ImpactSeconds = 0.18f;
        private const float ArcWorldSize = 2.325f;
        private const float ImpactWorldSize = 2.025f;
        private const int SortingOrder = 40;

        private static Sprite s_arc;
        private static Sprite s_impact;
        private static Material s_additiveMat;
        private static bool s_loaded;

        private SpriteRenderer _arc;
        private SpriteRenderer _impact;

        public static RenMeleeStrikeView Spawn(Vector3 from, Vector3 to, Transform parent = null)
        {
            EnsureSprites();
            var go = new GameObject("RenMeleeStrike");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<RenMeleeStrikeView>();
            view.Build();
            view.StartCoroutine(view.PlayRoutine(from, to));
            return view;
        }

        private void Build()
        {
            _arc = CreateChildRenderer("Arc", s_arc, additive: true);
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

            s_arc = Resources.Load<Sprite>(ResourceRoot + "ren_melee_arc_v1");
            s_impact = Resources.Load<Sprite>(ResourceRoot + "ren_melee_impact_v1");
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
                name = "RenMeleeAdditive",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private IEnumerator PlayRoutine(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            delta.z = 0f;
            if (delta.sqrMagnitude < 0.0001f)
            {
                delta = Vector3.right;
            }

            var dir = delta.normalized;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var mid = Vector3.Lerp(from, to, 0.55f);

            transform.SetPositionAndRotation(mid, Quaternion.Euler(0f, 0f, angle));
            FitSprite(_arc, ArcWorldSize);
            if (_arc != null)
            {
                _arc.transform.localPosition = Vector3.zero;
            }

            var elapsed = 0f;
            while (elapsed < ArcSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / ArcSeconds);
                var eased = 1f - (1f - t) * (1f - t);
                transform.position = Vector3.Lerp(from, to, Mathf.Lerp(0.25f, 0.9f, eased));
                if (_arc != null)
                {
                    FitSprite(_arc, ArcWorldSize * Mathf.Lerp(0.55f, 1.15f, eased));
                    var alpha = t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
                    var c = _arc.color;
                    _arc.color = new Color(c.r, c.g, c.b, alpha);
                }

                yield return null;
            }

            if (_arc != null)
            {
                _arc.enabled = false;
            }

            if (_impact != null)
            {
                _impact.enabled = true;
                _impact.transform.SetPositionAndRotation(to, Quaternion.identity);
                FitSprite(_impact, ImpactWorldSize);
                var impactElapsed = 0f;
                while (impactElapsed < ImpactSeconds)
                {
                    impactElapsed += Time.deltaTime;
                    var u = Mathf.Clamp01(impactElapsed / ImpactSeconds);
                    FitSprite(_impact, ImpactWorldSize * Mathf.Lerp(0.75f, 1.3f, u));
                    var c = _impact.color;
                    _impact.color = new Color(c.r, c.g, c.b, 1f - u);
                    yield return null;
                }
            }

            Destroy(gameObject);
        }

        private static void FitSprite(SpriteRenderer sr, float worldSize)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var native = Mathf.Max(0.001f, sr.sprite.bounds.size.y);
            var scale = worldSize / native;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
