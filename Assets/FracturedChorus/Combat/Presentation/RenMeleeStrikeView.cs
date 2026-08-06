using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class RenMeleeStrikeSettings
    {
        public Sprite Arc;
        public Sprite Impact;
        public Material AdditiveMaterial;
        public float ArcSeconds = 0.14f;
        public float ImpactSeconds = 0.18f;
        public float ArcWorldSize = 2.325f;
        public float ImpactWorldSize = 2.025f;
        public int SortingOrder = 40;
    }

    public class RenMeleeStrikeView : MonoBehaviour
    {
        private SpriteRenderer _arc;
        private SpriteRenderer _impact;
        private RenMeleeStrikeSettings _settings;

        public static RenMeleeStrikeView Spawn(
            Vector3 from,
            Vector3 to,
            RenMeleeStrikeSettings settings,
            Transform parent = null)
        {
            if (settings == null || (settings.Arc == null && settings.Impact == null))
            {
                return null;
            }

            var go = new GameObject("RenMeleeStrike");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<RenMeleeStrikeView>();
            view._settings = settings;
            view.Build();
            view.StartCoroutine(view.PlayRoutine(from, to));
            return view;
        }

        private void Build()
        {
            _arc = CreateChildRenderer("Arc", _settings.Arc, additive: true);
            _impact = CreateChildRenderer("Impact", _settings.Impact, additive: true);
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
            sr.sortingOrder = _settings.SortingOrder;
            sr.color = Color.white;
            if (additive && _settings.AdditiveMaterial != null)
            {
                sr.sharedMaterial = _settings.AdditiveMaterial;
            }

            return sr;
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
            FitSprite(_arc, _settings.ArcWorldSize);
            if (_arc != null)
            {
                _arc.transform.localPosition = Vector3.zero;
            }

            var arcSeconds = Mathf.Max(0.01f, _settings.ArcSeconds);
            var elapsed = 0f;
            while (elapsed < arcSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / arcSeconds);
                var eased = 1f - (1f - t) * (1f - t);
                transform.position = Vector3.Lerp(from, to, Mathf.Lerp(0.25f, 0.9f, eased));
                if (_arc != null)
                {
                    FitSprite(_arc, _settings.ArcWorldSize * Mathf.Lerp(0.55f, 1.15f, eased));
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

            if (_impact != null && _impact.sprite != null)
            {
                _impact.enabled = true;
                _impact.transform.SetPositionAndRotation(to, Quaternion.identity);
                FitSprite(_impact, _settings.ImpactWorldSize);
                var impactSeconds = Mathf.Max(0.01f, _settings.ImpactSeconds);
                var impactElapsed = 0f;
                while (impactElapsed < impactSeconds)
                {
                    impactElapsed += Time.deltaTime;
                    var u = Mathf.Clamp01(impactElapsed / impactSeconds);
                    FitSprite(_impact, _settings.ImpactWorldSize * Mathf.Lerp(0.75f, 1.3f, u));
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

            var native = Mathf.Max(
                0.001f,
                Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y));
            var scale = worldSize / native;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
