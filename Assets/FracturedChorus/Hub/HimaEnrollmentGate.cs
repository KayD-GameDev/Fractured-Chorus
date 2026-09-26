using FracturedChorus.Meta;
using FracturedChorus.Narrative.Vn;

namespace FracturedChorus.Hub
{
    public static class HimaEnrollmentGate
    {
        public const string LocationId = "hima";
        public const string ActivityId = "hima_enrollment";
        public const string PinLabel = "Enrollment";

        public static bool IsActive(GameMetaState state)
        {
            return state != null
                   && state.HasFlag(StoryFlagIds.RenEnRouteHima)
                   && !state.HasFlag(StoryFlagIds.HimaEnrollmentDone);
        }
    }

    public static class HimaEnrollmentLaunch
    {
        private static bool s_pending;

        public static bool IsPending => s_pending;

        public static void Arm()
        {
            s_pending = true;
        }

        public static void Cancel()
        {
            s_pending = false;
        }

        public static bool TryConsume(out VnScriptSO script)
        {
            if (!s_pending)
            {
                script = null;
                return false;
            }

            s_pending = false;
            script = HimaEnrollmentScriptBuilder.CreateRuntimeInstance();
            return script != null;
        }
    }
}
