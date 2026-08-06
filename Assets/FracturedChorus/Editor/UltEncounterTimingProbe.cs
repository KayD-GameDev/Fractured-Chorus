using FracturedChorus.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class UltEncounterTimingProbe
    {
        private const string CombatScenePath = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";

        [MenuItem("Fractured Chorus/Combat/Probe Skill 3 Encounter Timing")]
        public static void Probe()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.EndsWith("CombatPrototype.unity"))
            {
                EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
            }

            var director = Object.FindAnyObjectByType<EncounterDirector>();
            var ren = Object.FindAnyObjectByType<PlayerSkillShotChoreographer>();
            var coda = Object.FindAnyObjectByType<CodaSkillChoreographer>();
            var charlotte = Object.FindAnyObjectByType<CharlotteSkillChoreographer>();
            var feel = Object.FindAnyObjectByType<CombatImpactFeel>();

            var soDirector = director != null ? new SerializedObject(director) : null;
            var soRen = ren != null ? new SerializedObject(ren) : null;
            var soCoda = coda != null ? new SerializedObject(coda) : null;
            var soCharlotte = charlotte != null ? new SerializedObject(charlotte) : null;
            var soFeel = feel != null ? new SerializedObject(feel) : null;

            var ultAnim = F(soDirector, "ultimateEncounterAnimSpeed", 0.42f);
            var aftermath = F(soDirector, "ultimateAftermathHoldSeconds", 0.55f);
            var spread = F(soDirector, "stageSpreadExtra", 0.85f);

            var renSample = Mathf.Max(1f, F(soRen, "skill3AnimSampleRate", 24f));
            var renCharge = Mathf.Max(
                F(soRen, "skill3MinChargeSeconds", 0.95f),
                I(soRen, "skill3ShotFrameA", 14) / renSample);
            var renSpan = (I(soRen, "skill3ShotFrameB", 22) - I(soRen, "skill3ShotFrameA", 14)) / renSample;
            var renHold = F(soRen, "skill3ImpactHoldSeconds", 0.16f)
                          + F(soRen, "skill3AftermathHoldSeconds", 0.35f);
            var renBullets = I(soRen, "skill3BulletCount", 3);
            var renCore = renCharge + renSpan + 0.2f + renHold;

            var codaCharge = F(soCoda, "skill3ChargeSeconds", 0.95f);
            var codaVolley = F(soCoda, "skill3FlightSeconds", 0.48f)
                             + F(soCoda, "skill3StaggerSeconds", 0.07f)
                             * Mathf.Max(0, I(soCoda, "skill3BoltCount", 5) - 1)
                             + F(soCoda, "skill3FinaleImpactSeconds", 0.42f)
                             + F(soCoda, "skill3AftermathHoldSeconds", 0.35f);
            var codaCore = codaCharge + codaVolley;

            var charlotteWindup = F(soCharlotte, "ultWindupSeconds", 0.48f);
            var charlotteImpactAt = I(soCharlotte, "ultImpactFrame", 7)
                                    / Mathf.Max(1f, F(soCharlotte, "ultAnimSampleRate", 20f));
            var charlotteTail = F(soCharlotte, "ultImpactHoldSeconds", 0.18f)
                                + F(soCharlotte, "ultBossKnockSeconds", 0.34f)
                                + F(soCharlotte, "ultAftermathHoldSeconds", 0.4f)
                                + 0.35f;
            var charlotteCore = charlotteWindup + charlotteImpactAt + charlotteTail;

            var hitStop = F(soFeel, "ultimateHitStopSeconds", 0.1f);
            var trauma = F(soFeel, "ultimateTrauma", 0.78f);

            Debug.Log(
                "[UltTiming]\n"
                + $"Encounter: ultAnim={ultAnim:F2} aftermath={aftermath:F2}s spreadExtra={spread:F2}\n"
                + $"Feel: ultimateTrauma={trauma:F2} hitStopRealtime={hitStop:F2}s\n"
                + $"Ren S3 ≈ {renCore + aftermath:F2}s (charge {renCharge:F2} + volley span {renSpan:F2} + holds {renHold:F2}, bullets={renBullets})\n"
                + $"Coda S3 ≈ {codaCore + aftermath:F2}s (charge {codaCharge:F2} + volley/finale {codaVolley:F2})\n"
                + $"Charlotte S3 ≈ {charlotteCore + aftermath:F2}s (windup {charlotteWindup:F2} + impact@{charlotteImpactAt:F2} + tail {charlotteTail:F2})\n"
                + "Note: hit-stop uses realtime; WaitForSeconds during freeze stretches wall-clock slightly.");

            if (!Application.isPlaying)
            {
                Debug.Log(
                    "[UltTiming] Enter Play Mode → cast Ultimate on Ren/Coda/Charlotte to verify gồng/nổ + shake/hit-stop. "
                    + "ContextMenu on CombatImpactFeel: Test Punch Ultimate.");
            }
            else
            {
                CombatImpactFeel.PunchUltimateNow();
                Debug.Log("[UltTiming] Fired PunchUltimateNow in Play Mode.");
            }
        }

        private static float F(SerializedObject so, string prop, float fallback)
        {
            if (so == null)
            {
                return fallback;
            }

            var p = so.FindProperty(prop);
            return p != null ? p.floatValue : fallback;
        }

        private static int I(SerializedObject so, string prop, int fallback)
        {
            if (so == null)
            {
                return fallback;
            }

            var p = so.FindProperty(prop);
            return p != null ? p.intValue : fallback;
        }
    }
}
