using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public static class SkillTimelineNoteResolver
    {
        private const string RenRoot = "UI/Combat/Timeline/Skills/Ren/";
        private const string CodaRoot = "UI/Combat/Timeline/Skills/Coda/";
        private const string CharlotteRoot = "UI/Combat/Timeline/Skills/Charlotte/";

        public static Sprite ResolveActive(SkillDefinitionSO skill)
        {
            if (skill == null)
            {
                return null;
            }

            if (skill.timelineActiveSprite != null)
            {
                return skill.timelineActiveSprite;
            }

            var path = ActiveResourcePath(skill.skillId);
            return string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
        }

        public static Sprite ResolveStanding(SkillDefinitionSO skill)
        {
            if (skill == null)
            {
                return null;
            }

            if (skill.timelineStandingSprite != null)
            {
                return skill.timelineStandingSprite;
            }

            var path = StandingResourcePath(skill.skillId);
            return string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
        }

        public static float ResolveActiveSize(SkillDefinitionSO skill, float fallbackDotSize)
        {
            var size = Mathf.Max(fallbackDotSize, TimelineLayoutLock.SkillNoteActiveSize);
            return ApplyCharacterScale(skill, size);
        }

        public static float ResolveStandingSize(SkillDefinitionSO skill, float fallbackDotSize)
        {
            var size = Mathf.Max(fallbackDotSize, TimelineLayoutLock.SkillNoteStandingSize);
            return ApplyCharacterScale(skill, size);
        }

        private static float ApplyCharacterScale(SkillDefinitionSO skill, float size)
        {
            if (skill == null)
            {
                return size;
            }

            if (IsCoda(skill.skillId) || IsCharlotte(skill.skillId))
            {
                return size * TimelineLayoutLock.CodaCharlotteNoteScale;
            }

            return size;
        }

        private static string ActiveResourcePath(string skillId)
        {
            return skillId switch
            {
                "ren_basic" or "ren_skill" => RenRoot + "ren_skill_note_main_v1",
                "ren_ult" => RenRoot + "ren_skill_note_ult_v1",
                "mage_basic" or "mage_skill" => CodaRoot + "coda_skill_note_main_v1",
                "mage_ult" => CodaRoot + "coda_skill_note_ult_v1",
                "Charlott_basic" or "tank_basic" or "tank_skill" => CharlotteRoot + "charlotte_skill_note_main_v1",
                "tank_ult" => CharlotteRoot + "charlotte_skill_note_ult_v1",
                _ => null
            };
        }

        private static string StandingResourcePath(string skillId)
        {
            if (IsRen(skillId))
            {
                return RenRoot + "ren_skill_note_wait_v1";
            }

            if (IsCoda(skillId))
            {
                return CodaRoot + "coda_skill_note_wait_v1";
            }

            if (IsCharlotte(skillId))
            {
                return CharlotteRoot + "charlotte_skill_note_wait_v1";
            }

            return null;
        }

        private static bool IsRen(string id) =>
            id is "ren_basic" or "ren_skill" or "ren_ult";

        private static bool IsCoda(string id) =>
            id is "mage_basic" or "mage_skill" or "mage_ult";

        private static bool IsCharlotte(string id) =>
            id is "Charlott_basic" or "tank_basic" or "tank_skill" or "tank_ult";
    }
}
