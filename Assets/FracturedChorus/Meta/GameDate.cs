using System;

namespace FracturedChorus.Meta
{
    [Serializable]
    public struct GameDate : IEquatable<GameDate>, IComparable<GameDate>
    {
        public int Month;
        public int Day;

        public GameDate(int month, int day)
        {
            Month = month;
            Day = day;
        }

        public static GameDate Arc1Start => new GameDate(9, 1);

        public static GameDate Arc1End => new GameDate(9, 30);

        public static GameDate VaultDeadline => new GameDate(9, 20);

        public int CompareTo(GameDate other)
        {
            var monthCompare = Month.CompareTo(other.Month);
            return monthCompare != 0 ? monthCompare : Day.CompareTo(other.Day);
        }

        public bool Equals(GameDate other) => Month == other.Month && Day == other.Day;

        public override bool Equals(object obj) => obj is GameDate other && Equals(other);

        public override int GetHashCode() => (Month * 100) + Day;

        public GameDate AddDays(int days)
        {
            if (days <= 0)
            {
                return this;
            }

            var month = Month;
            var day = Day + days;

            while (day > DaysInMonth(month))
            {
                day -= DaysInMonth(month);
                month++;
            }

            return new GameDate(month, day);
        }

        public int DaysUntil(GameDate target)
        {
            if (target.CompareTo(this) <= 0)
            {
                return 0;
            }

            var cursor = this;
            var count = 0;

            while (cursor.CompareTo(target) < 0)
            {
                cursor = cursor.AddDays(1);
                count++;
            }

            return count;
        }

        public string ToDisplayString() => $"{Day:00}/{Month:00}";

        public static int GetDaysInMonth(int month) => DaysInMonth(month);

        public static bool operator ==(GameDate left, GameDate right) => left.Equals(right);

        public static bool operator !=(GameDate left, GameDate right) => !left.Equals(right);

        public static bool operator <(GameDate left, GameDate right) => left.CompareTo(right) < 0;

        public static bool operator >(GameDate left, GameDate right) => left.CompareTo(right) > 0;

        public static bool operator <=(GameDate left, GameDate right) => left.CompareTo(right) <= 0;

        public static bool operator >=(GameDate left, GameDate right) => left.CompareTo(right) >= 0;

        private static int DaysInMonth(int month) => month switch
        {
            2 => 28,
            4 or 6 or 9 or 11 => 30,
            _ => 31
        };
    }
}
