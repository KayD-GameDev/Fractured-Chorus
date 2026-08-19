#if UNITY_EDITOR
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class PartyCombatVisualSetupEditor
    {
        private const string RenIdleClip = "Assets/FracturedChorus/Art/Characters/Ren/Animation/Ren - Idle/Ren Idle.anim";
        private const string RenNormalClip = "Assets/FracturedChorus/Art/Characters/Ren/Animation/Ren - Skill 1/Ren Skill 1.anim";
        private const string RenSkillClip = "Assets/FracturedChorus/Art/Characters/Ren/Animation/Ren - Skill 2/Ren - Skill 2.anim";
        private const string RenUltClip = "Assets/FracturedChorus/Art/Characters/Ren/Animation/Ren - Skill 3/Ren - Skill 3.anim";

        private const string CodaIdleClip = "Assets/FracturedChorus/Art/Characters/Coda/Animation/Coda - Idle/Coda - Idle.anim";
        private const string CodaNormalClip = "Assets/FracturedChorus/Art/Characters/Coda/Animation/Coda - Skill 1/Coda Skill 1.anim";
        private const string CodaSkillClip = "Assets/FracturedChorus/Art/Characters/Coda/Animation/Coda - Skill 1/Coda Skill 2.anim";
        private const string CodaUltClip = "Assets/FracturedChorus/Art/Characters/Coda/Animation/Coda - Skill 3/Coda Skill 3.anim";

        private const string CharlotteIdleClip = "Assets/FracturedChorus/Art/Characters/Charlotte/Animation/Charlott_Idle.anim";
        private const string CharlotteNormalClip = "Assets/FracturedChorus/Art/Characters/Charlotte/Animation/Charlott_NorHit.anim";
        private const string CharlotteSkillClip = "Assets/FracturedChorus/Art/Characters/Charlotte/Animation/Charlott_Skill.anim";
        private const string CharlotteUltClip = "Assets/FracturedChorus/Art/Characters/Charlotte/Animation/Charlott_Ultimate.anim";

        [MenuItem("Fractured Chorus/Wire Party Combat Visuals")]
        public static void WireOpenScene()
        {
            var views = Object.FindObjectsByType<UnitView>(FindObjectsInactive.Include);
            var count = 0;
            foreach (var view in views)
            {
                if (WireParty(view))
                {
                    count++;
                }
            }

            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog(
                "Fractured Chorus",
                count > 0
                    ? $"Wired Animator + simulator clips on {count} party unit(s)."
                    : "No party units (Ren / Coda / Charlotte) in the open scene.",
                "OK");
        }

        public static bool WireParty(UnitView view)
        {
            if (view == null || !TryResolveKit(view, out var kit))
            {
                return false;
            }

            Undo.RecordObject(view, "Wire Party Combat Visuals");
            var animator = view.GetComponent<Animator>();
            var so = new SerializedObject(view);
            if (animator != null)
            {
                so.FindProperty("animator").objectReferenceValue = animator;
            }

            so.FindProperty("idleStateName").stringValue = kit.Idle;
            so.FindProperty("counterStateName").stringValue = kit.Counter;
            SetIfExists(so, "beCounteredStateName", kit.Hurt);
            SetIfExists(so, "movingStateName", kit.Moving);
            SetIfExists(so, "deathStateName", kit.Death);
            SetIfExists(so, "normalHitStateName", kit.Normal);
            SetIfExists(so, "skillStateName", kit.Skill);
            SetIfExists(so, "ultStateName", kit.Ult);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            var sim = UnitSpriteSimulator.EnsureOn(view);
            if (sim == null)
            {
                return true;
            }

            Undo.RecordObject(sim, "Wire Party Simulator Clips");
            sim.EnsureIdleAnimationClip(LoadClip(kit.IdlePath));
            sim.EnsureCounterStillKind();
            sim.EnsureClipLinkedState(
                UnitCombatVisualState.NormalHit, "NormalHit", LoadClip(kit.NormalPath));
            sim.EnsureClipLinkedState(
                UnitCombatVisualState.SkillHit, "SkillHit", LoadClip(kit.SkillPath));
            sim.EnsureClipLinkedState(
                UnitCombatVisualState.UltHit, "UltHit", LoadClip(kit.UltPath));
            EditorUtility.SetDirty(sim);
            return true;
        }

        private static void SetIfExists(SerializedObject so, string field, string value)
        {
            var prop = so.FindProperty(field);
            if (prop != null && !string.IsNullOrEmpty(value))
            {
                prop.stringValue = value;
            }
        }

        private static AnimationClip LoadClip(string path)
        {
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static bool TryResolveKit(UnitView view, out PartyKit kit)
        {
            kit = default;
            var key = view.DemoUnitKey?.ToLowerInvariant() ?? string.Empty;
            if (key.Contains("ren"))
            {
                kit = new PartyKit
                {
                    Idle = "Ren Idle",
                    Counter = "Ren Counter",
                    Hurt = "Ren Hurt",
                    Moving = "Ren Moving",
                    Death = "Ren Death",
                    Normal = "Ren Skill 1",
                    Skill = "Ren - Skill 2",
                    Ult = "Ren - Skill 3",
                    IdlePath = RenIdleClip,
                    NormalPath = RenNormalClip,
                    SkillPath = RenSkillClip,
                    UltPath = RenUltClip
                };
                return true;
            }

            if (key.Contains("mage") || key.Contains("coda"))
            {
                kit = new PartyKit
                {
                    Idle = "Coda - Idle",
                    Counter = "Coda - Counter",
                    Hurt = "Coda - Hurt",
                    Moving = "Coda - Moving",
                    Death = "Coda - Death",
                    Normal = "Coda Skill 1",
                    Skill = "Coda Skill 2",
                    Ult = "Coda Skill 3",
                    IdlePath = CodaIdleClip,
                    NormalPath = CodaNormalClip,
                    SkillPath = CodaSkillClip,
                    UltPath = CodaUltClip
                };
                return true;
            }

            if (key.Contains("charlott") || key.Contains("charlotte") || key == "tank")
            {
                kit = new PartyKit
                {
                    Idle = "Charlott_Idle",
                    Counter = "Charlott_Counter",
                    Hurt = "Charlott_Hurt",
                    Moving = "Charlott_Moving",
                    Death = "Charlott_Death",
                    Normal = "Charlott_NorHit",
                    Skill = "Charlott_Skill",
                    Ult = "Charlott_Ultimate",
                    IdlePath = CharlotteIdleClip,
                    NormalPath = CharlotteNormalClip,
                    SkillPath = CharlotteSkillClip,
                    UltPath = CharlotteUltClip
                };
                return true;
            }

            return false;
        }

        private struct PartyKit
        {
            public string Idle;
            public string Counter;
            public string Hurt;
            public string Moving;
            public string Death;
            public string Normal;
            public string Skill;
            public string Ult;
            public string IdlePath;
            public string NormalPath;
            public string SkillPath;
            public string UltPath;
        }
    }
}
#endif
