using System;
using System.Text.RegularExpressions;

namespace OpenDCP.Core.Core.Classes
{
    public static class TimeParser
    {
        public static DateTime Parse(string? input, DateTime? defaultValue = null)
        {
            var fallback = defaultValue ?? DateTime.UtcNow;

            if (string.IsNullOrEmpty(input))
                return fallback;

            if (input.StartsWith("*"))
            {
                var now = DateTime.UtcNow;
                var offset = input[1..];
                return ApplyOffset(now, offset);
            }

            return DateTimeOffset.TryParse(input, out var dt) ? dt.UtcDateTime : fallback;
        }

        public static DateTime AddInterval(DateTime date, (int Value, string Unit) interval)
        {
            return interval.Unit switch
            {
                "s" => date.AddSeconds(interval.Value),
                "m" => date.AddMinutes(interval.Value),
                "h" => date.AddHours(interval.Value),
                "d" => date.AddDays(interval.Value),
                "w" => date.AddDays(7 * interval.Value),
                "mo" => date.AddMonths(interval.Value),
                "q" => date.AddMonths(3 * interval.Value),
                "y" => date.AddYears(interval.Value),
                _ => date
            };
        }

        private static DateTime ApplyOffset(DateTime baseTime, string offset)
        {
            if(string.IsNullOrEmpty(offset))
                return baseTime;

            var match = Regex.Match(offset, @"^([+-])(\d+)(s|m|h|d|w|mo|q|y)$");
            if (!match.Success) return baseTime;

            var sign = match.Groups[1].Value;
            var value = int.Parse(match.Groups[2].Value);
            var unit = match.Groups[3].Value;

            var number = sign == "-" ? -value : value;

            return AddInterval(baseTime, (number, unit));
        }
    }
}
