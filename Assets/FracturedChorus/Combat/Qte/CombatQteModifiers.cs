namespace FracturedChorus.Combat.Qte
{
    /// <summary>Snapshot grade cho đúng một beat đang resolve. Clear sau encounter.</summary>
    public static class CombatQteModifiers
    {
        public static bool HasActive { get; private set; }
        public static CombatQteGrade Grade { get; private set; }
        public static bool CancelEnemy { get; private set; } = true;
        public static float OutgoingMult { get; private set; } = 1f;
        public static float IncomingEnemyMult { get; private set; } = 1f;

        public static bool AllowsCancel => !HasActive || CancelEnemy;

        public static void Apply(CombatQteGrade grade, CombatQteGradeRule rule)
        {
            HasActive = true;
            Grade = grade;
            CancelEnemy = rule.cancelEnemy;
            OutgoingMult = rule.outgoingMult > 0f ? rule.outgoingMult : 1f;
            IncomingEnemyMult = rule.incomingEnemyMult > 0f ? rule.incomingEnemyMult : 1f;
        }

        public static void Clear()
        {
            HasActive = false;
            Grade = CombatQteGrade.None;
            CancelEnemy = true;
            OutgoingMult = 1f;
            IncomingEnemyMult = 1f;
        }
    }
}
