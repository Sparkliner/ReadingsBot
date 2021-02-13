using System;

namespace ReadingsBot.Data
{
    //stores daily scheduled events
    public class ScheduledEvent
    {
        public MongoDB.Bson.ObjectId Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        //Time of the event as UTC
        public long EventTimeTicks { get; set; }
        public string TimeZone { get; set; }
        public IReadingInfo EventInfo { get; set; }

        public TimeSpan GetEventTime()
        {
            return new TimeSpan(EventTimeTicks);
        }
    }
}
