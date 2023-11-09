namespace K.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


    public static class DateTimeUtil
    {
        /// <summary>
        /// Converts the value of a DateTime object to Coordinated Universal Time (UTC).
        /// </summary>
        /// <remarks>
        /// If <paramref name="dateTime"/>'s Kind equals <see cref="DateTimeKind.Unspecified"/>, 
        /// this method assumes that it is Coordinated Universal Time (UTC).
        /// </remarks>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static DateTime ConvertTimeToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return dateTime;
            }

            if (dateTime.Kind == DateTimeKind.Local)
            {
                // ArgumentException: invalid local time
                return TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, TimeZoneInfo.Utc);
            }

            // DateTimeKind.Unspecified
            return new DateTime(dateTime.Ticks, DateTimeKind.Utc);
        }
    }
}
