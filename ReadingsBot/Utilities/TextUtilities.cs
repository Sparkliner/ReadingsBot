using System;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Collections.Generic;

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
            string timeString;
            string timeZoneString;
            List<String> tokens = input.Trim().Split().ToList();

            //assume AM/PM must be in one of first two tokens for 12hr time
            int indexOfAmPm = IndexOfAmPm(tokens.Take(2).ToList());
            if (indexOfAmPm > 0)
            {
                timeString = string.Join(" ", tokens.Take(indexOfAmPm + 1));
                timeZoneString = string.Join(" ", tokens.Skip(indexOfAmPm + 1));
            }
            else
            {
                //assume 24 hour time
                timeString = tokens[0];
                timeZoneString = string.Join(" ", tokens.Skip(1));
            }

            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneString);
            }
            catch (TimeZoneNotFoundException)
            {
                throw new ArgumentException("Time zone or time format not recognized");
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentException("Time zone missing");
            }
            if (!DateTime.TryParse(timeString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                throw new ArgumentException("Time format not recognized - check `help` command for correct format");

            timeZone = tz.Id;
            return TimeZoneInfo.ConvertTimeToUtc(dt, tz).TimeOfDay;
        }

        private static int IndexOfAmPm(List<String> tokens)
        {
            //tokens: a 2-member IEnumerable of strings
            //check first and second token for am/pm
            //if not found return -1
            return Math.Max(
                tokens.FindIndex(s => s.ToLower().Contains("am")),
                tokens.FindIndex(s => s.ToLower().Contains("pm"))
                );
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
