using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using NodaTime;
using System;

namespace ReadingsBot.Data
{
    //stores daily scheduled events
    public class ScheduledEvent
    {
        [BsonIgnoreIfDefault]
        public MongoDB.Bson.ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public IReadingInfo EventInfo { get; set; }

        //next time this event should occur
        public LocalTime EventLocalTime { get; set; }
        public DateTimeZone EventTimeZone { get; set; }
        //redundant maybe but this is actually parseable by mongodb as a date
        public Instant EventInstant { get; set; }
        //how often this event occurs
        public Period EventPeriod { get; set; }
        public bool IsRecurring { get; set; }

        //empty constructor lets you set anything
        public ScheduledEvent()
        { }

        public ScheduledEvent(ulong guildId, ulong channelId, IReadingInfo eventInfo, ZonedDateTime zonedDateTime, Period period, bool isRecurring)
        {
            GuildId = guildId;
            ChannelId = channelId;
            EventInfo = eventInfo;
            EventLocalTime = zonedDateTime.LocalDateTime.TimeOfDay;
            EventTimeZone = zonedDateTime.Zone;
            EventInstant = zonedDateTime.ToInstant();
            EventPeriod = period;
            IsRecurring = isRecurring;
        }
        public static ScheduledEvent CreateDailyEvent(ulong guildId, ulong channelId, IReadingInfo eventInfo, ZonedDateTime zonedDateTime)
        {
            return new ScheduledEvent(guildId, channelId, eventInfo, zonedDateTime, Period.FromDays(1), true);
        }

        public DateTimeZone GetTimeZone()
        {
            return EventTimeZone;
        }

        public LocalDateTime GetLocalDateTime()
        {
            return GetZonedDateTime().LocalDateTime;
        }

        public ZonedDateTime GetZonedDateTime()
        {
            return EventInstant.InZone(EventTimeZone);
        }

        public LocalTime GetTimeOfDay()
        {
            return EventLocalTime;
        }
    }
}
