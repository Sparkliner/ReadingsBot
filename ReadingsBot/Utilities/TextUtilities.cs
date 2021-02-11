using System;
using System.Web;
using System.Globalization;

namespace ReadingsBot.Utilities
{
    public static class TextUtilities
    {
        public static string ParseWebText(string input)
        {
            return HttpUtility.HtmlDecode(input.Replace("<p>",""));
        }

        public static TimeSpan ParseTimeSpanAsUtc(string input, out string timeZone)
        {
            string[] args = input.Split('-');
            if (args.Length != 2)
                throw new ArgumentException("Time format not recognized - check `help` command for correct format");
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(args[1].Trim());
            }
            catch (TimeZoneNotFoundException)
            {
                throw new ArgumentException("Time zone not recognized");
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentException("Time zone missing");
            }
            if (!DateTime.TryParseExact(args[0].Trim(), "hh:mm tt", CultureInfo.InvariantCulture,DateTimeStyles.None,out DateTime dt))
                throw new ArgumentException("Time format not recognized - check `help` command for correct format");

            timeZone = tz.Id;
            return TimeZoneInfo.ConvertTimeToUtc(dt, tz).TimeOfDay;
        }

        public static string FormatTimeLocallyAsString(TimeSpan time, string timeZone)
        {
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime utcTime = new DateTime(2000, 01, 01, time.Hours, time.Minutes, time.Seconds, DateTimeKind.Utc);
            DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
            return localTime.ToString("hh:mm tt", CultureInfo.InvariantCulture) + " " + timeZone;
        }
    }
}
