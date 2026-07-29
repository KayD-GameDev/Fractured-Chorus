namespace FracturedChorus.Combat.Difficulty
{
    public static class DifficultyRuntime
    {
        public const int OnBeat = 0;
        public const int Cadence = 1;
        public const int OffBeat = 2;

        public readonly struct Multipliers
        {
            public Multipliers(
                float enemyHp,
                float enemyDamage,
                float pierceFrontBias,
                float notesEarn,
                int planningWindowBonus,
                float earlyLateBlockPenalty)
            {
                EnemyHp = enemyHp;
                EnemyDamage = enemyDamage;
                PierceFrontBias = pierceFrontBias;
                NotesEarn = notesEarn;
                PlanningWindowBonus = planningWindowBonus;
                EarlyLateBlockPenalty = earlyLateBlockPenalty;
            }

            public float EnemyHp { get; }
            public float EnemyDamage { get; }
            public float PierceFrontBias { get; }
            public float NotesEarn { get; }
            public int PlanningWindowBonus { get; }
            public float EarlyLateBlockPenalty { get; }
        }

        public static Multipliers Get(int difficulty)
        {
            switch (difficulty)
            {
                case OnBeat:
                    return new Multipliers(
                        enemyHp: 0.85f,
                        enemyDamage: 0.85f,
                        pierceFrontBias: 0.8f,
                        notesEarn: 1.1f,
                        planningWindowBonus: 1,
                        earlyLateBlockPenalty: 0f);
                case OffBeat:
                    return new Multipliers(
                        enemyHp: 1.15f,
                        enemyDamage: 1.2f,
                        pierceFrontBias: 1.15f,
                        notesEarn: 1f,
                        planningWindowBonus: 0,
                        earlyLateBlockPenalty: 0.1f);
                default:
                    return new Multipliers(
                        enemyHp: 1f,
                        enemyDamage: 1f,
                        pierceFrontBias: 1f,
                        notesEarn: 1f,
                        planningWindowBonus: 0,
                        earlyLateBlockPenalty: 0f);
            }
        }
    }
}
