using System;

namespace OpenDCP.Core.Core.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime RoundToMillisecondsUtc(this DateTime value)
        {
            const long ticksPerMillisecond = TimeSpan.TicksPerMillisecond;
            long utcTicks = value.ToUniversalTime().Ticks;

            // Round to nearest millisecond
            long roundedTicks = ((utcTicks + 5000) / ticksPerMillisecond) * ticksPerMillisecond;

            return new DateTime(roundedTicks, DateTimeKind.Utc);
        }
    }
}
