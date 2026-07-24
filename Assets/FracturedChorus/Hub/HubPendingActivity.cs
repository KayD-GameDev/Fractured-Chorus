namespace FracturedChorus.Hub
{
    public static class HubPendingActivity
    {
        public static string ActivityId { get; private set; }

        public static bool HasPending => !string.IsNullOrWhiteSpace(ActivityId);

        public static void Set(string activityId)
        {
            ActivityId = activityId;
        }

        public static string Consume()
        {
            var id = ActivityId;
            ActivityId = null;
            return id;
        }

        public static void Clear()
        {
            ActivityId = null;
        }
    }
}
