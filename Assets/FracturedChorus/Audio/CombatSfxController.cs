using System.Collections;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Audio
{
    public class CombatSfxController : MonoBehaviour
    {
        private const float ScheduleAheadEpsilonSec = 0.002f;
        private const string PerfectCounterClipPath = "Assets/FracturedChorus/Audio/SFX/Perfect sound Game.wav";
        private const string PerfectBlockClipPath = "Assets/FracturedChorus/Audio/SFX/Perfect sound SFX.wav";
        private const string PerfectBlockResourcePath = "Audio/SFX/Perfect sound SFX";
        private const string ClashHitClipPath = "Assets/FracturedChorus/Audio/SFX/Clash Hit.wav";
        private const string RenSkill1ClipPath = "Assets/FracturedChorus/Audio/SFX/Ren_Skill1.wav";
        private const string RenSkill2ClipPath = "Assets/FracturedChorus/Audio/SFX/Ren_Skill2.wav";
        private const string RenSkill3ClipPath = "Assets/FracturedChorus/Audio/SFX/Ren_Skill3.mp3";
        private const string RenSkill1ResourcePath = "Audio/SFX/Ren_Skill1";
        private const string RenSkill2ResourcePath = "Audio/SFX/Ren_Skill2";
        private const string RenSkill3ResourcePath = "Audio/SFX/Ren_Skill3";
        private const string CodaSkill1ClipPath = "Assets/FracturedChorus/Audio/SFX/Coda_Skill1.mp3";
        private const string CodaSkill1ResourcePath = "Audio/SFX/Coda_Skill1";
        private const string CodaSkill23ClipPath = "Assets/FracturedChorus/Audio/SFX/Coda_Skill23.wav";
        private const string CodaSkill23ResourcePath = "Audio/SFX/Coda_Skill23";

        [SerializeField] private AudioSource perfectCounterSource;
        [SerializeField] private AudioClip perfectCounterClip;
        [SerializeField] private float perfectCounterVolume = 1f;
        [SerializeField] private AudioSource perfectBlockSource;
        [SerializeField] private AudioClip perfectBlockClip;
        [SerializeField] private float perfectBlockVolume = 1f;
        [Tooltip("Seconds to schedule before target DSP so clip onset lands on beat (0 if WAV already trimmed).")]
        [SerializeField] private float sfxLeadSec;
        [SerializeField] private AudioSource clashHitSource;
        [SerializeField] private AudioClip clashHitClip;
        [SerializeField] private float clashHitVolume = 1f;
        [SerializeField] private AudioSource renSkillSource;
        [SerializeField] private AudioClip renSkill1Clip;
        [SerializeField] private AudioClip renSkill2Clip;
        [SerializeField] private AudioClip renSkill3Clip;
        [SerializeField] private AudioClip codaSkill1Clip;
        [SerializeField] private AudioClip codaSkill23Clip;
        [SerializeField] private float renSkillVolume = 1f;
        [Tooltip("0–1 of skill clip length when character skill SFX fires (lower = earlier).")]
        [SerializeField] [Range(0.05f, 1f)] private float renSkillCueNormalizedTime = 0.45f;
        [Tooltip("Extra seconds pulled earlier than the cue point.")]
        [SerializeField] private float renSkillCueLeadSec = 0.05f;

        private void Awake()
        {
            EnsurePerfectCounterSource();
            EnsurePerfectBlockSource();
            EnsureClashHitSource();
            EnsureRenSkillSource();
            TryAssignDefaultClips();
            PrimePerfectCounterSource();
            PrimePerfectBlockSource();
            PrimeClashHitSource();
            PrimeRenSkillSource();
        }

        public void PlayPerfectCounter(double targetDspTime = -1d)
        {
            EnsureClip(ref perfectCounterClip, null, PerfectCounterClipPath);
            PlayClip(
                ref perfectCounterSource,
                perfectCounterClip,
                perfectCounterVolume,
                "PerfectCounterSfx",
                "perfectCounterClip",
                targetDspTime);
        }

        public void PlayPerfectBlock(double targetDspTime = -1d)
        {
            EnsureClip(ref perfectBlockClip, PerfectBlockResourcePath, PerfectBlockClipPath);
            if (perfectBlockClip == null)
            {
                Debug.LogWarning("[CombatSfx] perfectBlockClip missing — fallback counter clip.");
                PlayPerfectCounter(-1d);
                return;
            }

            EnsurePerfectBlockSource();
            if (perfectBlockSource != null)
            {
                perfectBlockSource.spatialBlend = 0f;
                perfectBlockSource.mute = false;
                perfectBlockSource.volume = perfectBlockVolume;
                perfectBlockSource.PlayOneShot(perfectBlockClip, perfectBlockVolume);
                Debug.Log($"[CombatSfx] PlayPerfectBlock len={perfectBlockClip.length:F2}s vol={perfectBlockVolume}");
                return;
            }

            var listener = FindAnyObjectByType<AudioListener>();
            var pos = listener != null ? listener.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(perfectBlockClip, pos, perfectBlockVolume);
            Debug.Log($"[CombatSfx] PlayPerfectBlock via PlayClipAtPoint len={perfectBlockClip.length:F2}s");
            _ = targetDspTime;
        }

        public void PlayClashHit()
        {
            if (clashHitClip == null)
            {
                return;
            }

            EnsureClashHitSource();
            PrimeClashHitSource();
            PlayClashHitImmediate();
        }

        public void PlayRenSkillAtClipEnd(SkillDefinitionSO skill, float clipLengthSeconds)
        {
            PlaySkillSfxAtClipCue(skill, clipLengthSeconds);
        }

        public void PlaySkillSfxAtClipCue(SkillDefinitionSO skill, float clipLengthSeconds)
        {
            if (skill == null || string.IsNullOrEmpty(skill.skillId))
            {
                return;
            }

            var clip = ResolveCharacterSkillClip(skill);
            if (clip == null)
            {
                return;
            }

            var cueAt = Mathf.Max(0f, clipLengthSeconds) * Mathf.Clamp01(renSkillCueNormalizedTime);
            cueAt = Mathf.Max(0f, cueAt - Mathf.Max(0f, renSkillCueLeadSec));
            StartCoroutine(PlaySkillClipAfterDelay(clip, cueAt));
        }

        private IEnumerator PlaySkillClipAfterDelay(AudioClip clip, float delaySeconds)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            PlaySkillClipImmediate(clip);
        }

        private AudioClip ResolveCharacterSkillClip(SkillDefinitionSO skill)
        {
            var id = skill.skillId;
            if (id.StartsWith("ren_", System.StringComparison.OrdinalIgnoreCase))
            {
                EnsureRenSkillClips();
                return skill.slotKind switch
                {
                    SkillSlotKind.BasicAttack => renSkill1Clip,
                    SkillSlotKind.Skill => renSkill2Clip,
                    SkillSlotKind.Ultimate => renSkill3Clip,
                    _ => null
                };
            }

            if (id.StartsWith("mage_", System.StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("coda_", System.StringComparison.OrdinalIgnoreCase))
            {
                EnsureCodaSkillClips();
                return skill.slotKind switch
                {
                    SkillSlotKind.BasicAttack => codaSkill1Clip,
                    SkillSlotKind.Skill => codaSkill23Clip,
                    SkillSlotKind.Ultimate => codaSkill23Clip,
                    _ => null
                };
            }

            return null;
        }

        private void PlaySkillClipImmediate(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            EnsureRenSkillSource();
            renSkillSource.spatialBlend = 0f;
            renSkillSource.mute = false;
            renSkillSource.volume = renSkillVolume;
            renSkillSource.PlayOneShot(clip, renSkillVolume);
        }

        private void PlayClip(
            ref AudioSource source,
            AudioClip clip,
            float volume,
            string sourceName,
            string clipFieldName,
            double targetDspTime)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[CombatSfx] {clipFieldName} not assigned — run 'Fractured Chorus/Wire Combat Music'.");
                return;
            }

            if (source == null)
            {
                source = FindOrCreateSfxSource(sourceName);
            }

            source.spatialBlend = 0f;
            source.mute = false;
            source.volume = volume;

            var now = AudioSettings.dspTime;
            var playAt = targetDspTime >= 0d ? targetDspTime - sfxLeadSec : now;
            if (playAt > now + ScheduleAheadEpsilonSec)
            {
                source.clip = clip;
                source.Stop();
                source.PlayScheduled(playAt);
                return;
            }

            source.PlayOneShot(clip, volume);
        }

        private void PlayClashHitImmediate()
        {
            if (clashHitClip == null || clashHitSource == null)
            {
                return;
            }

            clashHitSource.PlayOneShot(clashHitClip, clashHitVolume);
        }

        private void PrimePerfectCounterSource()
        {
            if (perfectCounterSource == null || perfectCounterClip == null)
            {
                return;
            }

            perfectCounterSource.clip = perfectCounterClip;
            perfectCounterSource.volume = perfectCounterVolume;
        }

        private void PrimePerfectBlockSource()
        {
            if (perfectBlockSource == null || perfectBlockClip == null)
            {
                return;
            }

            perfectBlockSource.clip = perfectBlockClip;
            perfectBlockSource.volume = perfectBlockVolume;
        }

        private void PrimeClashHitSource()
        {
            if (clashHitSource == null || clashHitClip == null)
            {
                return;
            }

            clashHitSource.clip = clashHitClip;
            clashHitSource.volume = clashHitVolume;
        }

        private void PrimeRenSkillSource()
        {
            if (renSkillSource == null)
            {
                return;
            }

            renSkillSource.volume = renSkillVolume;
        }

        private void EnsurePerfectCounterSource()
        {
            if (perfectCounterSource != null)
            {
                return;
            }

            perfectCounterSource = FindOrCreateSfxSource("PerfectCounterSfx");
        }

        private void EnsurePerfectBlockSource()
        {
            if (perfectBlockSource != null)
            {
                return;
            }

            perfectBlockSource = FindOrCreateSfxSource("PerfectBlockSfx");
        }

        private void EnsureClashHitSource()
        {
            if (clashHitSource != null)
            {
                return;
            }

            clashHitSource = FindOrCreateSfxSource("ClashHitSfx");
        }

        private void EnsureRenSkillSource()
        {
            if (renSkillSource != null)
            {
                return;
            }

            renSkillSource = FindOrCreateSfxSource("RenSkillSfx");
        }

        private AudioSource FindOrCreateSfxSource(string name)
        {
            var existing = transform.Find(name);
            if (existing != null && existing.TryGetComponent<AudioSource>(out var existingSource))
            {
                return existingSource;
            }

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.bypassReverbZones = true;
            source.priority = 0;
            return source;
        }

        private static void EnsureClip(ref AudioClip clip, string resourcePath, string editorAssetPath)
        {
            if (clip != null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(resourcePath))
            {
                clip = Resources.Load<AudioClip>(resourcePath);
            }

#if UNITY_EDITOR
            if (clip == null && !string.IsNullOrEmpty(editorAssetPath))
            {
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(editorAssetPath);
            }
#endif
        }

        private void EnsureRenSkillClips()
        {
            EnsureClip(ref renSkill1Clip, RenSkill1ResourcePath, RenSkill1ClipPath);
            EnsureClip(ref renSkill2Clip, RenSkill2ResourcePath, RenSkill2ClipPath);
            EnsureClip(ref renSkill3Clip, RenSkill3ResourcePath, RenSkill3ClipPath);
        }

        private void EnsureCodaSkillClips()
        {
            EnsureClip(ref codaSkill1Clip, CodaSkill1ResourcePath, CodaSkill1ClipPath);
            EnsureClip(ref codaSkill23Clip, CodaSkill23ResourcePath, CodaSkill23ClipPath);
        }

        private void TryAssignDefaultClips()
        {
            EnsureClip(ref perfectBlockClip, PerfectBlockResourcePath, PerfectBlockClipPath);
            EnsureRenSkillClips();
            EnsureCodaSkillClips();
#if UNITY_EDITOR
            if (perfectCounterClip == null)
            {
                perfectCounterClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(PerfectCounterClipPath);
            }

            if (clashHitClip == null)
            {
                clashHitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(ClashHitClipPath);
            }
#endif
        }
    }
}
