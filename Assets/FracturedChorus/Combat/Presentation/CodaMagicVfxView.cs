using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CodaMagicVfxView : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Coda/";
        private const float SlashSeconds = 0.22f;
        private const float BeamSeconds = 0.26f;
        private const float ImpactSeconds = 0.18f;
        private const float TripleGapSeconds = 0.08f;
        private const float SlashWorldSize = 2.55f;
        private const float BeamHeight = 0.825f;
        private const float ImpactWorldSize = 1.875f;
        private const int SortingOrder = 80;

        private static Sprite s_slash;
        private static Sprite s_beam;
        private static Sprite s_impact;
        private static Material s_additiveMat;
        private static bool s_loaded;

        public static void SpawnCrescentSlash(Vector3 from, Vector3 to, Transform parent = null)
        {
            if (!EnsureSprites())
            {
                Debug.LogWarning("[CodaVFX] Missing crescent/impact sprites under Resources/VFX/Combat/Coda/");
                return;
            }

            var go = CreateRoot("CodaCrescentSlashVfx", parent);
            var view = go.AddComponent<CodaMagicVfxView>();
            view.StartCoroutine(view.PlaySlashRoutine(from, to));
        }

        public static void SpawnBeam(Vector3 from, Vector3 to, Transform parent = null)
        {
            if (!EnsureSprites())
            {
                Debug.LogWarning("[CodaVFX] Missing beam/impact sprites under Resources/VFX/Combat/Coda/");
                return;
            }

            var go = CreateRoot("CodaBeamVfx", parent);
            var view = go.AddComponent<CodaMagicVfxView>();
            view.StartCoroutine(view.PlayBeamRoutine(from, to, yOffset: 0f));
        }

        public static void SpawnTripleBeam(Vector3 from, Vector3 to, Transform parent = null)
        {
            if (!EnsureSprites())
            {
                Debug.LogWarning("[CodaVFX] Missing beam/impact sprites under Resources/VFX/Combat/Coda/");
                return;
            }

            var go = CreateRoot("CodaTripleBeamVfx", parent);
            var view = go.AddComponent<CodaMagicVfxView>();
            view.StartCoroutine(view.PlayTripleBeamRoutine(from, to));
        }

        private static GameObject CreateRoot(string name, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(null, false);
            }

            return go;
        }

        private static bool EnsureSprites()
        {
            if (s_loaded && s_slash != null && s_beam != null && s_impact != null)
            {
                return true;
            }

            s_slash = LoadSprite("coda_vfx_crescent_slash_v1");
            s_beam = LoadSprite("coda_vfx_beam_v1");
            s_impact = LoadSprite("coda_vfx_impact_v1");
            EnsureAdditiveMaterial();
            s_loaded = s_slash != null || s_beam != null || s_impact != null;
            return s_slash != null || s_beam != null || s_impact != null;
        }

        private static Sprite LoadSprite(string fileName)
        {
            var sprite = Resources.Load<Sprite>(ResourceRoot + fileName);
            if (sprite != null)
            {
                return sprite;
            }

            var tex = Resources.Load<Texture2D>(ResourceRoot + fileName);
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
                name = "CodaMagicAdditive",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = SortingOrder;
            sr.color = Color.white;
            if (s_additiveMat != null)
            {
                sr.sharedMaterial = s_additiveMat;
            }

            return sr;
        }

        private IEnumerator PlaySlashRoutine(Vector3 from, Vector3 to)
        {
            var delta = Flatten(to - from);
            var dir = delta.normalized;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            var slash = CreateRenderer("Slash", s_slash);
            FitSquare(slash, SlashWorldSize * 0.55f);

            var elapsed = 0f;
            while (elapsed < SlashSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / SlashSeconds);
                var eased = 1f - (1f - t) * (1f - t);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(from, to, Mathf.Lerp(0.2f, 0.92f, eased)),
                    Quaternion.Euler(0f, 0f, angle));
                FitSquare(slash, SlashWorldSize * Mathf.Lerp(0.55f, 1.2f, eased));
                SetAlpha(slash, t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f);
                yield return null;
            }

            if (slash != null)
            {
                slash.enabled = false;
            }

            yield return PlayImpactAt(to);
            Destroy(gameObject);
        }

        private IEnumerator PlayBeamRoutine(Vector3 from, Vector3 to, float yOffset)
        {
            var start = from + Vector3.up * yOffset;
            var end = to + Vector3.up * yOffset * 0.35f;
            var delta = Flatten(end - start);
            var distance = Mathf.Max(0.2f, delta.magnitude);
            var dir = delta / distance;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var beam = CreateRenderer("Beam", s_beam);
            var impact = CreateRenderer("Impact", s_impact);
            impact.enabled = false;

            transform.SetPositionAndRotation(start, Quaternion.Euler(0f, 0f, angle));
            FitBeam(beam, 0.15f);

            var elapsed = 0f;
            while (elapsed < BeamSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / BeamSeconds);
                var eased = 1f - (1f - t) * (1f - t);
                var length = Mathf.Lerp(0.2f, distance, eased);
                transform.position = start + dir * (length * 0.5f);
                FitBeam(beam, length);
                SetAlpha(beam, t < 0.75f ? 1f : 1f - (t - 0.75f) / 0.25f);
                yield return null;
            }

            if (beam != null)
            {
                beam.enabled = false;
            }

            yield return PlayImpactRenderer(impact, end);
            Destroy(gameObject);
        }

        private IEnumerator PlayTripleBeamRoutine(Vector3 from, Vector3 to)
        {
            var offsets = new[] { 0.28f, 0f, -0.28f };
            for (var i = 0; i < offsets.Length; i++)
            {
                var child = CreateRoot("CodaBeamPulse", null);
                var pulse = child.AddComponent<CodaMagicVfxView>();
                pulse.StartCoroutine(pulse.PlayBeamRoutine(from, to, offsets[i]));
                if (i < offsets.Length - 1)
                {
                    yield return new WaitForSeconds(TripleGapSeconds);
                }
            }

            yield return new WaitForSeconds(BeamSeconds + ImpactSeconds + 0.05f);
            Destroy(gameObject);
        }

        private IEnumerator PlayImpactAt(Vector3 point)
        {
            var impact = CreateRenderer("Impact", s_impact);
            yield return PlayImpactRenderer(impact, point);
        }

        private IEnumerator PlayImpactRenderer(SpriteRenderer impact, Vector3 point)
        {
            if (impact == null || impact.sprite == null)
            {
                yield break;
            }

            impact.enabled = true;
            impact.transform.SetPositionAndRotation(point, Quaternion.identity);
            FitSquare(impact, ImpactWorldSize * 0.75f);
            var elapsed = 0f;
            while (elapsed < ImpactSeconds)
            {
                elapsed += Time.deltaTime;
                var u = Mathf.Clamp01(elapsed / ImpactSeconds);
                FitSquare(impact, ImpactWorldSize * Mathf.Lerp(0.75f, 1.3f, u));
                SetAlpha(impact, 1f - u);
                yield return null;
            }
        }

        private static Vector3 Flatten(Vector3 v)
        {
            v.z = 0f;
            if (v.sqrMagnitude < 0.0001f)
            {
                return Vector3.right;
            }

            return v;
        }

        private static void FitSquare(SpriteRenderer sr, float worldSize)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var native = Mathf.Max(0.001f, sr.sprite.bounds.size.y);
            var scale = worldSize / native;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            sr.transform.localPosition = Vector3.zero;
        }

        private static void FitBeam(SpriteRenderer sr, float length)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var nativeW = Mathf.Max(0.001f, sr.sprite.bounds.size.x);
            var nativeH = Mathf.Max(0.001f, sr.sprite.bounds.size.y);
            sr.transform.localScale = new Vector3(length / nativeW, BeamHeight / nativeH, 1f);
            sr.transform.localPosition = Vector3.zero;
            sr.sortingOrder = SortingOrder;
        }

        private static void SetAlpha(SpriteRenderer sr, float alpha)
        {
            if (sr == null)
            {
                return;
            }

            var c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
        }
    }
}
