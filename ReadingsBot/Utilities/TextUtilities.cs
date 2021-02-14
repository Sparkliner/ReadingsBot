using NodaTime;
using NodaTime.Text;
using NodaTime.TimeZones;
using System;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Collections.Generic;

namespace ReadingsBot.Utilities
{
    public static class TextUtilities
    {
        //private static CompositePatternBuilder<ZonedDateTime> patternBuilder;

        //private static string[] patterns =
        //{
        //    "HH':'mm z",
        //    "H':'mm z",
        //    "HH z",
        //    "H z",
        //    "hh':'mm tt z",
        //    "hh':'mm t z",
        //    "h':'mm tt z",
        //    "h':'mm t z",
        //    "h tt z",
        //    "h t z"
        //};

        //static TextUtilities()
        //{
        //    foreach (string pattern in patterns)
        //    {
        //        patternBuilder.Add(ZonedDateTimePattern.Create(
        //            pattern, CultureInfo.InvariantCulture,
        //            Resolvers.LenientResolver, DateTimeZoneProviders.Tzdb, new ZonedDateTime()));
        //    }
        //}

        public static string ParseWebText(string input)
        {
            return HttpUtility.HtmlDecode(input.Replace("<p>",""));
        }

        //public static ZonedDateTime NodaParseTime(string input, out string timeZone)
        //{
        //    var pattern = ZonedDateTimePattern.Create("H:mm")
        //}
        public static TimeSpan ParseTimeSpanAsLocal(string input, out string timeZone)
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
            //dt assumes the current date according to the documentation
            timeZone = tz.Id;
            return dt.TimeOfDay;
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
