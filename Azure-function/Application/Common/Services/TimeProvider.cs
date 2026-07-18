using Application.Common.Interfaces;
using System;

namespace Application.Common.Services
{
    public class TimeProvider : ITimeProvider
    {
        public DateTime GetLocalTime()
        {
            foreach (var tzId in new[] { "India Standard Time", "Asia/Kolkata" })
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                }
                catch (TimeZoneNotFoundException) { /* try next */ }
            }
            return DateTime.Now;
        }
    }
}
