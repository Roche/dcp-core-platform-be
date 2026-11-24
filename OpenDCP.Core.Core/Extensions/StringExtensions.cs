using System;
using System.Globalization;

namespace OpenDCP.Core.Core.Extensions
{
    public static class StringExtensions
    {
        public static DateTime ParseISO8601(this string value)
        {
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).UtcDateTime;
        }
    }
}
