namespace FracturedChorus.Combat.Actions
{
    public interface ICombatAction
    {
        int Delay { get; }
        bool CanExecute(CombatContext ctx);
        void Execute(CombatContext ctx);
    }
}
