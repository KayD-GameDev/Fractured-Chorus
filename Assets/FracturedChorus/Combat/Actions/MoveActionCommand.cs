using FracturedChorus.Combat.Actions;

namespace FracturedChorus.Combat.Actions
{
    public class MoveActionCommand : ICombatAction
    {
        public int Delay => 2;

        public bool CanExecute(CombatContext ctx)
        {
            return ctx?.Source != null && ctx.Source.IsAlive;
        }

        public void Execute(CombatContext ctx)
        {
            // Shift movement deferred — stub for UC-06.
        }
    }
}
