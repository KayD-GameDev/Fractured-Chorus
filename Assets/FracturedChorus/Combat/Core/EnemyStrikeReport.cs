using FracturedChorus.Combat.Units;

namespace FracturedChorus.Combat.Core
{
    public readonly struct EnemyStrikeReport
    {
        public EnemyStrikeReport(CombatUnit attacker, CombatUnit target, bool wasCountered, int beatIndex)
        {
            Attacker = attacker;
            Target = target;
            WasCountered = wasCountered;
            BeatIndex = beatIndex;
        }

        public CombatUnit Attacker { get; }
        public CombatUnit Target { get; }
        public bool WasCountered { get; }
        public int BeatIndex { get; }

        public bool IsValid => Attacker != null && Target != null;
    }
}
