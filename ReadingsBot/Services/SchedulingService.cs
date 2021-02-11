using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public class SchedulingService
    {
        private readonly IConfigurationRoot _config;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Data.ScheduledEvent> _events;

        public enum EventType
        {
            OCALives
        }

        private static string[] EventTypeDescription =
        {
            "Lives of the Saints"
        };

        public SchedulingService(IMongoClient client, IConfigurationRoot config)
        {
            _config = config;
            _client = client;

            try
            {
                _database = client.GetDatabase(_config["database_id"]);
                _events = _database.GetCollection<Data.ScheduledEvent>("events");
            }
            catch (MongoException e)
            {
                LogException(e);
                throw;
            }
        }

        public async Task<bool> ScheduleNewEvent(ulong guildId, ulong channelId, TimeSpan eventTime, string timeZone, EventType eventType)
        {
            //check that this event is not scheduled already
            var builder = Builders<Data.ScheduledEvent>.Filter;
            var filter = builder.And(
                builder.Eq("GuildId", guildId),
                builder.Eq("ChannelId", channelId),
                builder.Eq("EventType", eventType)
                );

            //upsert the record
            var options = new UpdateOptions { IsUpsert = true };
            var update = Builders<Data.ScheduledEvent>.Update
                .Set("EventTimeTicks", eventTime.Ticks)
                .Set("TimeZone", timeZone);
            var result = await _events.UpdateOneAsync(filter, update, options);

            bool rescheduled = false;
            if (result.IsAcknowledged)
            {
                rescheduled = result.MatchedCount > 0;
            }

            return rescheduled;
        }

        public async Task<bool> CancelScheduledEvent(ulong guildId, ulong channelId, EventType eventType)
        {
            var builder = Builders<Data.ScheduledEvent>.Filter;
            var filter = builder.And(
                builder.Eq("GuildId", guildId),
                builder.Eq("ChannelId", channelId),
                builder.Eq("EventType", eventType)
                );
            var result = await _events.DeleteOneAsync(filter);
            bool deleted = false;
            if (result.IsAcknowledged)
            {
                deleted = result.DeletedCount > 0;
            }
            return deleted;
        }

        public async Task<List<Data.ScheduledEvent>> GetGuildEvents(ulong guildId)
        {
            var builder = Builders<Data.ScheduledEvent>.Filter;
            var filter = builder.Eq("GuildId", guildId);
            List<Data.ScheduledEvent> events = await _events.Find(filter).ToListAsync();
            return events;
        }

        public async Task<List<Data.ScheduledEvent>> GetCurrentEvents(TimeSpan now)
        {
            //get events that need to be fired this minute
            long timeInTicks = new TimeSpan(now.Hours,now.Minutes,0).Ticks;
            var builder = Builders<Data.ScheduledEvent>.Filter;
            var filter = builder.Eq("EventTimeTicks", timeInTicks);
            return await _events.Find(filter).ToListAsync();
        }
        
        public static string EventTypeToDescription(EventType eventType)
        {
            return EventTypeDescription[(int)eventType];
        }

        private void LogException(MongoException e)
        {
            LogUtilities.WriteLog(Discord.LogSeverity.Error, e.ToString());
        }
    }

}
