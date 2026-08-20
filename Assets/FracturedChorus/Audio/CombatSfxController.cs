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
        private const string MirrorBreakingClipPath = "Assets/FracturedChorus/Audio/SFX/Mirror_Breaking.wav";
        private const string MirrorBreakingResourcePath = "Audio/SFX/Mirror_Breaking";
        private const string CodaSkill1ClipPath = "Assets/FracturedChorus/Audio/SFX/Coda_Skill1.mp3";
        private const string CodaSkill1ResourcePath = "Audio/SFX/Coda_Skill1";
        private const string CodaSkill23ClipPath = "Assets/FracturedChorus/Audio/SFX/Coda_Skill23.wav";
        private const string CodaSkill23ResourcePath = "Audio/SFX/Coda_Skill23";
        private const string CharlotteSkill1ClipPath = "Assets/FracturedChorus/Audio/SFX/Charlotte_Skill1.wav";
        private const string CharlotteSkill1ResourcePath = "Audio/SFX/Charlotte_Skill1";
        private const string CharlotteSkill2ClipPath = "Assets/FracturedChorus/Audio/SFX/Charlotte_Skill2.wav";
        private const string CharlotteSkill2ResourcePath = "Audio/SFX/Charlotte_Skill2";
        private const string CharlotteSkill3ClipPath = "Assets/FracturedChorus/Audio/SFX/Charlotte_Skill3.wav";
        private const string CharlotteSkill3ResourcePath = "Audio/SFX/Charlotte_Skill3";
        private const string CharlotteSkill2DashClipPath = "Assets/FracturedChorus/Audio/SFX/Charlotte_Skill2_Dash.wav";
        private const string CharlotteSkill2DashResourcePath = "Audio/SFX/Charlotte_Skill2_Dash";
        private const string ClashHitResourcePath = "Audio/SFX/Clash Hit";
        private const string PlanningTransitionClipPath =
            "Assets/FracturedChorus/Audio/SFX/Combat_PlanningTransition.wav";
        private const string PlanningTransitionResourcePath = "Audio/SFX/Combat_PlanningTransition";
        private const string UiClickClipPath = "Assets/FracturedChorus/Audio/SFX/Combat_UiClick.wav";
        private const string UiClickResourcePath = "Audio/SFX/Combat_UiClick";
        private const string SkillPlaceClipPath = "Assets/FracturedChorus/Audio/SFX/Combat_SkillPlace.wav";
        private const string SkillPlaceResourcePath = "Audio/SFX/Combat_SkillPlace";

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
        [SerializeField] private AudioClip mirrorBreakingClip;
        [SerializeField] [Range(0.1f, 1.5f)] private float mirrorBreakingVolume = 1f;
        [SerializeField] private AudioClip codaSkill1Clip;
        [SerializeField] private AudioClip codaSkill23Clip;
        [SerializeField] private AudioClip charlotteSkill1Clip;
        [SerializeField] private AudioClip charlotteSkill2Clip;
        [SerializeField] private AudioClip charlotteSkill2DashClip;
        [SerializeField] private AudioClip charlotteSkill3Clip;
        [SerializeField] private float renSkillVolume = 1f;
        [Tooltip("0–1 of skill clip length when character skill SFX fires (lower = earlier).")]
        [SerializeField] [Range(0.05f, 1f)] private float renSkillCueNormalizedTime = 0.45f;
        [Tooltip("Extra seconds pulled earlier than the cue point.")]
        [SerializeField] private float renSkillCueLeadSec = 0.05f;
        [SerializeField] private AudioSource planningTransitionSource;
        [SerializeField] private AudioClip planningTransitionClip;
        [SerializeField] private float planningTransitionVolume = 0.85f;
        [SerializeField] private AudioSource uiClickSource;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private float uiClickVolume = 0.9f;
        [SerializeField] private AudioSource skillPlaceSource;
        [SerializeField] private AudioClip skillPlaceClip;
        [SerializeField] private float skillPlaceVolume = 0.9f;

        private void Awake()
        {
            EnsurePerfectCounterSource();
            EnsurePerfectBlockSource();
            EnsureClashHitSource();
            EnsureRenSkillSource();
            EnsurePlanningTransitionSource();
            EnsureUiClickSource();
            EnsureSkillPlaceSource();
            TryAssignDefaultClips();
            PrimePerfectCounterSource();
            PrimePerfectBlockSource();
            PrimeClashHitSource();
            PrimeRenSkillSource();
        }

        public void PlayPlanningTransition()
        {
            EnsureClip(ref planningTransitionClip, PlanningTransitionResourcePath, PlanningTransitionClipPath);
            if (planningTransitionClip == null)
            {
                return;
            }

            EnsurePlanningTransitionSource();
            if (planningTransitionSource == null)
            {
                return;
            }

            planningTransitionSource.spatialBlend = 0f;
            planningTransitionSource.mute = false;
            planningTransitionSource.volume = planningTransitionVolume;
            planningTransitionSource.PlayOneShot(planningTransitionClip, planningTransitionVolume);
        }

        public void PlayUiClick()
        {
            EnsureClip(ref uiClickClip, UiClickResourcePath, UiClickClipPath);
            if (uiClickClip == null)
            {
                return;
            }

            EnsureUiClickSource();
            if (uiClickSource == null)
            {
                return;
            }

            uiClickSource.spatialBlend = 0f;
            uiClickSource.mute = false;
            uiClickSource.volume = uiClickVolume;
            uiClickSource.PlayOneShot(uiClickClip, uiClickVolume);
        }

        public void PlaySkillPlace()
        {
            EnsureClip(ref skillPlaceClip, SkillPlaceResourcePath, SkillPlaceClipPath);
            if (skillPlaceClip == null)
            {
                return;
            }

            EnsureSkillPlaceSource();
            if (skillPlaceSource == null)
            {
                return;
            }

            skillPlaceSource.spatialBlend = 0f;
            skillPlaceSource.mute = false;
            skillPlaceSource.volume = skillPlaceVolume;
            skillPlaceSource.PlayOneShot(skillPlaceClip, skillPlaceVolume);
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
            EnsureClip(ref clashHitClip, ClashHitResourcePath, ClashHitClipPath);
            if (clashHitClip == null)
            {
                return;
            }

            EnsureClashHitSource();
            PrimeClashHitSource();
            PlayClashHitImmediate();
        }

        public void PlayCharlotteSkill2Dash()
        {
            EnsureClip(ref charlotteSkill2DashClip, CharlotteSkill2DashResourcePath, CharlotteSkill2DashClipPath);
            PlaySkillClipImmediate(charlotteSkill2DashClip);
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

            // Ren NorHit + Charlotte skills play SFX at impact from choreographers.
            if (UsesImpactSyncedSkillSfx(skill))
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

        private static bool UsesImpactSyncedSkillSfx(SkillDefinitionSO skill)
        {
            var id = skill.skillId;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            if (id.StartsWith("Charlott", System.StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("charlotte", System.StringComparison.OrdinalIgnoreCase)
                || id is "tank_basic" or "tank_skill" or "tank_ult")
            {
                return true;
            }

            if (id.StartsWith("mage_", System.StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("coda_", System.StringComparison.OrdinalIgnoreCase)
                || id.StartsWith("Coda_", System.StringComparison.OrdinalIgnoreCase))
            {
                return skill.slotKind is SkillSlotKind.BasicAttack
                       or SkillSlotKind.Skill
                       or SkillSlotKind.Ultimate;
            }

            if (!id.StartsWith("ren_", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return skill.slotKind is SkillSlotKind.BasicAttack
                   or SkillSlotKind.Skill
                   or SkillSlotKind.Ultimate;
        }

        public void PlayMirrorBreaking()
        {
            EnsureClip(ref mirrorBreakingClip, MirrorBreakingResourcePath, MirrorBreakingClipPath);
            if (mirrorBreakingClip == null)
            {
                return;
            }

            EnsureRenSkillSource();
            renSkillSource.spatialBlend = 0f;
            renSkillSource.mute = false;
            renSkillSource.volume = renSkillVolume;
            renSkillSource.PlayOneShot(mirrorBreakingClip, mirrorBreakingVolume);
        }

        public void PlayRenSkillSlotImmediate(SkillSlotKind slotKind)
        {
            PlayRenSkillSlot(slotKind, restart: false);
        }

        public void PlayRenSkillSlotRestarted(SkillSlotKind slotKind)
        {
            PlayRenSkillSlot(slotKind, restart: true);
        }

        private void PlayRenSkillSlot(SkillSlotKind slotKind, bool restart)
        {
            EnsureRenSkillClips();
            var clip = slotKind switch
            {
                SkillSlotKind.BasicAttack => renSkill1Clip,
                SkillSlotKind.Skill => renSkill2Clip,
                SkillSlotKind.Ultimate => renSkill3Clip,
                _ => null
            };
            if (restart)
            {
                PlaySkillClipRestarted(clip);
                return;
            }

            PlaySkillClipImmediate(clip);
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

            if (id.StartsWith("Charlott", System.StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("charlotte", System.StringComparison.OrdinalIgnoreCase) ||
                id is "tank_basic" or "tank_skill" or "tank_ult")
            {
                EnsureCharlotteSkillClips();
                return skill.slotKind switch
                {
                    SkillSlotKind.BasicAttack => charlotteSkill1Clip,
                    SkillSlotKind.Skill => charlotteSkill2Clip,
                    SkillSlotKind.Ultimate => charlotteSkill3Clip,
                    _ => null
                };
            }

            return null;
        }

        public void PlayDamageHitFallback()
        {
            EnsureRenSkillClips();
            PlaySkillClipImmediate(renSkill1Clip);
        }

        public void PlaySkillSfxImmediate(SkillDefinitionSO skill)
        {
            if (skill == null)
            {
                return;
            }

            var clip = ResolveCharacterSkillClip(skill);
            if (clip != null)
            {
                PlaySkillClipImmediate(clip);
                return;
            }

            PlayDamageHitFallback();
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

        private void PlaySkillClipRestarted(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            EnsureRenSkillSource();
            renSkillSource.spatialBlend = 0f;
            renSkillSource.mute = false;
            renSkillSource.volume = renSkillVolume;
            renSkillSource.Stop();
            renSkillSource.clip = clip;
            renSkillSource.loop = false;
            renSkillSource.Play();
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

        private void EnsurePlanningTransitionSource()
        {
            if (planningTransitionSource != null)
            {
                return;
            }

            planningTransitionSource = FindOrCreateSfxSource("PlanningTransitionSfx");
        }

        private void EnsureUiClickSource()
        {
            if (uiClickSource != null)
            {
                return;
            }

            uiClickSource = FindOrCreateSfxSource("UiClickSfx");
        }

        private void EnsureSkillPlaceSource()
        {
            if (skillPlaceSource != null)
            {
                return;
            }

            skillPlaceSource = FindOrCreateSfxSource("SkillPlaceSfx");
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

        private void EnsureCharlotteSkillClips()
        {
            EnsureClip(ref charlotteSkill1Clip, CharlotteSkill1ResourcePath, CharlotteSkill1ClipPath);
            EnsureClip(ref charlotteSkill2Clip, CharlotteSkill2ResourcePath, CharlotteSkill2ClipPath);
            EnsureClip(ref charlotteSkill2DashClip, CharlotteSkill2DashResourcePath, CharlotteSkill2DashClipPath);
            EnsureClip(ref charlotteSkill3Clip, CharlotteSkill3ResourcePath, CharlotteSkill3ClipPath);
        }

        private void TryAssignDefaultClips()
        {
            EnsureClip(ref perfectBlockClip, PerfectBlockResourcePath, PerfectBlockClipPath);
            EnsureClip(ref planningTransitionClip, PlanningTransitionResourcePath, PlanningTransitionClipPath);
            EnsureClip(ref uiClickClip, UiClickResourcePath, UiClickClipPath);
            EnsureClip(ref skillPlaceClip, SkillPlaceResourcePath, SkillPlaceClipPath);
            EnsureRenSkillClips();
            EnsureClip(ref mirrorBreakingClip, MirrorBreakingResourcePath, MirrorBreakingClipPath);
            EnsureCodaSkillClips();
            EnsureCharlotteSkillClips();
            EnsureClip(ref clashHitClip, ClashHitResourcePath, ClashHitClipPath);
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
