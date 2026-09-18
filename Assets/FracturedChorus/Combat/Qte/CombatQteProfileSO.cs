using System;
using UnityEngine;

namespace FracturedChorus.Combat.Qte
{
    public enum CombatQteGrade
    {
        None = 0,
        Perfect = 1,
        Good = 2,
        Miss = 3
    }

    [Serializable]
    public struct CombatQteGradeRule
    {
        [Tooltip("True = hủy nốt quái như counter đủ hit.")]
        public bool cancelEnemy;

        [Tooltip("Nhân sát thương player gây ra.")]
        public float outgoingMult;

        [Tooltip("Nhân sát thương quái gây ra (khi không hủy nốt).")]
        public float incomingEnemyMult;

        public Sprite gradeSprite;
        public AudioClip sfx;
    }

    [CreateAssetMenu(fileName = "CombatQteProfile", menuName = "Fractured Chorus/Combat QTE Profile")]
    public sealed class CombatQteProfileSO : ScriptableObject
    {
        [Header("Chance")]
        [Range(0f, 1f)] public float baseChance = 0.30f;
        [Min(1)] public int phasesPerStep = 2;
        [Range(0f, 1f)] public float chanceStep = 0.10f;
        [Range(0f, 1f)] public float chanceCap = 0.80f;

        [Header("Feel")]
        [Min(0.05f)] public float shrinkDuration = 0.75f;
        [Min(1f)] public float outerStartScale = 1.65f;
        [Min(0.01f)] public float perfectWindowSec = 0.06f;
        [Min(0.01f)] public float goodWindowSec = 0.14f;
        [Min(0.05f)] public float gradeHoldSeconds = 0.4f;

        [Header("Input")]
        public KeyCode confirmKey = KeyCode.Space;
        public bool acceptMouseClick = true;

        [Header("Miss — không hủy đòn, giảm dmg")]
        [Tooltip("Giảm dmg khi Miss ở phase 1–2 (0.25 = ít hơn 25%).")]
        [Range(0f, 1f)] public float missReductionBase = 0.25f;
        [Tooltip("Cộng thêm mỗi 2 phase.")]
        [Range(0f, 1f)] public float missReductionStep = 0.05f;
        [Tooltip("Trần giảm dmg khi Miss.")]
        [Range(0f, 1f)] public float missReductionCap = 0.35f;

        [Header("Grades")]
        public CombatQteGradeRule perfect = new CombatQteGradeRule
        {
            cancelEnemy = true,
            outgoingMult = 1.50f,
            incomingEnemyMult = 1f
        };

        public CombatQteGradeRule good = new CombatQteGradeRule
        {
            cancelEnemy = true,
            outgoingMult = 1.20f,
            incomingEnemyMult = 1f
        };

        public CombatQteGradeRule miss = new CombatQteGradeRule
        {
            cancelEnemy = false,
            outgoingMult = 0.75f,
            incomingEnemyMult = 0.75f
        };

        [Header("Art")]
        public Sprite innerRing;
        public Sprite outerRing;
        public Sprite prompt;
        [Tooltip("Khung vùng Perfect — bấm Space khi vòng ngoài khớp khung này.")]
        public Sprite perfectZone;

        public float GetChance(int phaseIndex) => GetChance(phaseIndex, baseChance);

        public float GetChance(int phaseIndex, float baseOverride)
        {
            var stepSize = Mathf.Max(1, phasesPerStep);
            var phase = Mathf.Max(0, phaseIndex);
            var steps = phase / stepSize;
            return Mathf.Clamp01(Mathf.Min(chanceCap, baseOverride + chanceStep * steps));
        }

        public CombatQteGrade Evaluate(float elapsedSec) => Evaluate(elapsedSec, 1f);

        public CombatQteGrade Evaluate(float elapsedSec, float windowMult)
        {
            var delta = elapsedSec - shrinkDuration;
            var abs = Mathf.Abs(delta);
            var safeMult = Mathf.Clamp(windowMult, 0.2f, 1f);
            if (abs <= perfectWindowSec * safeMult)
            {
                return CombatQteGrade.Perfect;
            }

            if (abs <= goodWindowSec * safeMult)
            {
                return CombatQteGrade.Good;
            }

            return CombatQteGrade.Miss;
        }

        public CombatQteGradeRule GetRule(CombatQteGrade grade) => GetRule(grade, 0, missReductionBase);

        public CombatQteGradeRule GetRule(CombatQteGrade grade, int phaseIndex, float missReductionBaseOverride)
        {
            CombatQteGradeRule rule;
            switch (grade)
            {
                case CombatQteGrade.Perfect:
                    rule = perfect;
                    break;
                case CombatQteGrade.Good:
                    rule = good;
                    break;
                default:
                    rule = miss;
                    var keep = 1f - GetMissReduction(phaseIndex, missReductionBaseOverride);
                    rule.cancelEnemy = false;
                    rule.outgoingMult = keep;
                    rule.incomingEnemyMult = keep;
                    break;
            }

            return rule;
        }

        public float GetMissReduction(int phaseIndex) => GetMissReduction(phaseIndex, missReductionBase);

        public float GetMissReduction(int phaseIndex, float baseOverride)
        {
            var stepSize = Mathf.Max(1, phasesPerStep);
            var steps = Mathf.Max(0, phaseIndex) / stepSize;
            var reduction = baseOverride + missReductionStep * steps;
            var cap = Mathf.Max(0f, missReductionCap);
            return Mathf.Clamp(reduction, 0f, cap);
        }
    }
}
