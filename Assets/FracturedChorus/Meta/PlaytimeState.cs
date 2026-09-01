using System;

namespace FracturedChorus.Meta
{
    /// <summary>
    /// Tổng thời gian chơi thực tế của một file save, tính bằng giây.
    /// Dùng double vì cộng dồn delta time từng frame suốt nhiều giờ sẽ mất chính xác nếu dùng float.
    /// </summary>
    [Serializable]
    public sealed class PlaytimeState
    {
        public double TotalSeconds;

        public int TotalSecondsRounded => (int)Math.Min(int.MaxValue, Math.Max(0d, Math.Round(TotalSeconds)));

        public void Add(double seconds)
        {
            if (seconds <= 0d || double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                return;
            }

            TotalSeconds += seconds;
        }

        public void Reset()
        {
            TotalSeconds = 0d;
        }

        /// <summary>Định dạng H:MM cho danh sách slot.</summary>
        public string Format() => Format(TotalSecondsRounded);

        public static string Format(int totalSeconds)
        {
            var clamped = Math.Max(0, totalSeconds);
            return $"{clamped / 3600}:{clamped % 3600 / 60:00}";
        }
    }
}
