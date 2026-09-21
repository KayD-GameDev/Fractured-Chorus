using System;

namespace FracturedChorus.Meta
{
    public static class HubDateHudFormat
    {
        public static string ShortDay(DayOfWeek day) => day switch
        {
            DayOfWeek.Monday => "Mon",
            DayOfWeek.Tuesday => "Tue",
            DayOfWeek.Wednesday => "Wed",
            DayOfWeek.Thursday => "Thu",
            DayOfWeek.Friday => "Fri",
            DayOfWeek.Saturday => "Sat",
            DayOfWeek.Sunday => "Sun",
            _ => day.ToString().Substring(0, Math.Min(3, day.ToString().Length))
        };

        public static bool TryParseDisplayDate(string raw, out GameDate date)
        {
            date = default;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            var normalized = raw.Replace(" ", string.Empty);
            var parts = normalized.Split('/');
            if (parts.Length != 2)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out var day) || !int.TryParse(parts[1], out var month))
            {
                return false;
            }

            if (day < 1 || day > 31 || month < 1 || month > 12)
            {
                return false;
            }

            date = new GameDate(month, day);
            return true;
        }

        public static DayPhase ResolvePhaseFromLabel(string phaseLabel, bool eveningFallback)
        {
            if (string.IsNullOrWhiteSpace(phaseLabel))
            {
                return eveningFallback ? DayPhase.Evening : DayPhase.Day;
            }

            var key = phaseLabel.Trim().ToLowerInvariant();
            if (key.Contains("morning") || key.Contains("dawn"))
            {
                return DayPhase.Morning;
            }

            if (key.Contains("evening") || key.Contains("night") || key.Contains("late"))
            {
                return DayPhase.Evening;
            }

            if (key.Contains("noon") || key.Contains("school") || key.Contains("day"))
            {
                return DayPhase.Day;
            }

            return eveningFallback ? DayPhase.Evening : DayPhase.Day;
        }
    }
}
