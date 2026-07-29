using System;

namespace FracturedChorus.Meta
{
    [Serializable]
    public sealed class WalletState
    {
        public int Notes;

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Notes += amount;
        }

        public bool CanAfford(int amount) => amount >= 0 && Notes >= amount;

        public bool Spend(int amount)
        {
            if (!CanAfford(amount))
            {
                return false;
            }

            Notes -= amount;
            return true;
        }
    }
}
