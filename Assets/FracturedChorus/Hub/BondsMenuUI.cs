using UnityEngine;

namespace FracturedChorus.Hub
{
    public sealed class BondsMenuUI : MonoBehaviour
    {
        public static int WrapRosterIndex(int index, int delta, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            var wrapped = (index + delta) % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }
    }
}
