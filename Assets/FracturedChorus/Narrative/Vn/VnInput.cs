using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    public static class VnInput
    {
        public static bool WasAdvancePressedThisFrame() => PrologueInput.WasAdvancePressedThisFrame();

        public static bool WasUpPressedThisFrame() => PrologueInput.WasUpPressedThisFrame();

        public static bool WasDownPressedThisFrame() => PrologueInput.WasDownPressedThisFrame();
    }
}
