using System;

namespace FracturedChorus.Meta
{
    /// <summary>
    /// Chỗ người chơi đang đứng trong Campus Hub. Trước đây pin đang chọn chỉ nằm trong RAM của
    /// TownMapView nên load lại là mất; giờ nó theo file save.
    /// </summary>
    [Serializable]
    public sealed class HubLocationState
    {
        public string LastLocationId = string.Empty;
        public string LastSubLocationId = string.Empty;

        /// <summary>Hoạt động đã chọn nhưng chưa tiêu slot — giữ lại để load vào đúng bước đang dở.</summary>
        public string PendingActivityId = string.Empty;

        public bool HasLocation => !string.IsNullOrWhiteSpace(LastLocationId);

        public void SetLocation(string locationId, string subLocationId = null)
        {
            LastLocationId = locationId ?? string.Empty;
            LastSubLocationId = subLocationId ?? string.Empty;
        }

        public void SetPendingActivity(string activityId)
        {
            PendingActivityId = activityId ?? string.Empty;
        }

        public void ClearPendingActivity()
        {
            PendingActivityId = string.Empty;
        }

        public void Clear()
        {
            LastLocationId = string.Empty;
            LastSubLocationId = string.Empty;
            PendingActivityId = string.Empty;
        }
    }
}
