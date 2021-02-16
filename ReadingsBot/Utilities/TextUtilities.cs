using NodaTime;
using NodaTime.Text;
using NodaTime.TimeZones;
using System;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Collections.Generic;
using TimeZoneNames;

namespace ReadingsBot.Utilities
{
    public static class TextUtilities
    {
        private readonly static CompositePatternBuilder<LocalTime> patternBuilder = new CompositePatternBuilder<LocalTime>();

        private readonly static IPattern<LocalTime> localTimePattern;

        private readonly static List<(string pattern,Func<LocalTime,Boolean> predicate)> patterns = new List<(string, Func<LocalTime, bool>)>
        {
            ( "HH':'mm", _ => true ),
            ( "hh':'mm tt", _ => true ),
            ( "hh':'mmtt", _ => false ),
            ( "hh':'mm t", _ => true ),
            ( "hh':'mmt", _ => false ),
            ( "H':'mm", _ => true ),
            ( "HH", _ => false ),
            ( "h':'mm tt", _ => true ),
            ( "h':'mmtt", _ => false ),
            ( "h':'mm t", _ => true ),
            ( "h':'mmt", _ => false ),
            ( "%H", _ => false ),
            ( "h tt", _ => false ),
            ( "htt", _ => false ),
            ( "h t", _ => false ),
            ( "ht", _ => false )
        };

        static TextUtilities()
        {
            foreach ((string pattern, Func<LocalTime, Boolean> predicate) in patterns)
            {
                patternBuilder.Add(LocalTimePattern.Create(
                    pattern, CultureInfo.InvariantCulture,
                    new LocalTime(0, 0)), predicate);
            }

            localTimePattern = patternBuilder.Build();
        }

        public static DateTimeZone ParseTimeZone(string input)
        {
            DateTimeZone tz = DateTimeZoneProviders.Tzdb.GetZoneOrNull(input.Trim());
            if (tz is null)
            {
                throw new ArgumentException("Time zone not recognized");
            }
            else
            {
                return tz;
            }
        }

        public static string ParseWebText(string input)
        {
            return HttpUtility.HtmlDecode(input.Replace("<p>",""));
        }

        //public static ZonedDateTime NodaParseTime(string input, out string timeZone)
        //{
        //    var pattern = ZonedDateTimePattern.Create("H:mm")
        //}
        public static LocalTime ParseLocalTime(string input, out DateTimeZone timeZone)
        {
            List<String> tokens = input.Trim().Split("-t").ToList();
            foreach (var token in tokens)
            {
                LogUtilities.WriteLog(Discord.LogSeverity.Debug, $"{token}");
            }

            if (tokens.Count > 2)
            {
                throw new ArgumentException("Time format not recognized - check `help` command for correct format");
            }
            else if (tokens.Count == 2)
            {
                //assume time zone was input, let's try to parse it
                timeZone = ParseTimeZone(tokens[1].Trim());
            }
            else
            {
                //use default time zone, for now pass null back
                timeZone = null;
            }

            ParseResult<LocalTime>  parseResult = localTimePattern.Parse(tokens[0].Trim());

            if (!parseResult.Success)
            {
                throw new ArgumentException("Time format not recognized - check `help` command for correct format");
            }
            else
            {
                return parseResult.Value;
            }
        }

        public static string FormatLocalTimeAndTimeZone(LocalTime time, DateTimeZone timeZone)
        {
            return $"{time.ToString("h:mm tt", CultureInfo.InvariantCulture)}" +
                $" {TZNames.GetNamesForTimeZone(timeZone.Id, "en-US").Generic}";
        }
    }
}
