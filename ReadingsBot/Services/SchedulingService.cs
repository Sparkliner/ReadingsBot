using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NodaTime;
using ReadingsBot.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public class SchedulingService
    {
        private readonly IConfigurationRoot _config;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Data.ScheduledEvent> _events;

        private readonly IClock _clock;

        public SchedulingService(IMongoClient client, IConfigurationRoot config, IClock clock)
        {
            _config = config;
            _client = client;
            _clock = clock;

            try
            {
                _database = _client.GetDatabase(_config["database_id"]);
                _events = _database.GetCollection<Data.ScheduledEvent>("events");
                LogUtilities.WriteLog(Discord.LogSeverity.Verbose, "Connected to schedule database");
            }
            catch (MongoException e)
            {
                LogException(e);
                throw;
            }

            RegisterClasses();
            RegisterSerializers();
        }

        private static void RegisterClasses()
        {
            BsonClassMap.RegisterClassMap<SaintsLivesReadingInfo>();
            BsonClassMap.RegisterClassMap<BlogsReadingInfo>();
            BsonClassMap.RegisterClassMap<ImageQuoteReadingInfo>();

            BsonClassMap.RegisterClassMap<BlogDescription>();
        }

        private static void RegisterSerializers()
        {
            BsonSerializer.RegisterSerializer(ZonedDateTimeSerializer.Instance);
            BsonSerializer.RegisterSerializer(LocalTimeSerializer.Instance);
            BsonSerializer.RegisterSerializer(DateTimeZoneSerializer.Instance);
            BsonSerializer.RegisterSerializer(PeriodSerializer.Instance);
            BsonSerializer.RegisterSerializer(InstantSerializer.Instance);
        }

        public async Task<bool> ScheduleOrUpdateEventAsync(Data.ScheduledEvent scheduledEvent)
        {
            //check that this event is not scheduled already
            var builder = Builders<Data.ScheduledEvent>.Filter;
            var filter = builder.And(
                builder.Eq("GuildId", scheduledEvent.GuildId),
                builder.Eq("ChannelId", scheduledEvent.ChannelId),
                builder.Eq("EventInfo", scheduledEvent.EventInfo)
                );

            //upsert the record
            var options = new ReplaceOptions { IsUpsert = true };
            var result = await _events.ReplaceOneAsync(filter, scheduledEvent, options);

            bool rescheduled = false;
            if (result.IsAcknowledged)
            {
                rescheduled = result.MatchedCount > 0;
            }

            return rescheduled;
        }

        public async Task HandleEventRecurrenceAsync(Data.ScheduledEvent scheduledEvent)
        {
            if (!scheduledEvent.IsRecurring)
            {
                await DeleteScheduledEventAsync(scheduledEvent.GuildId, scheduledEvent.GuildId, scheduledEvent.EventInfo);
            }
            else
            {
                LocalDateTime nextLocalEventDateTime = scheduledEvent.GetLocalDateTime().Plus(scheduledEvent.EventPeriod);
                ZonedDateTime nextEventTime = nextLocalEventDateTime.InZoneStrictly(scheduledEvent.GetTimeZone());
                Instant nextEventInstant = nextEventTime.ToInstant();

                var builder = Builders<Data.ScheduledEvent>.Filter;
                var filter = builder.Eq("Id", scheduledEvent.Id);

                var update = Builders<Data.ScheduledEvent>.Update
                    .Set("EventInstant", nextEventInstant);

                await _events.UpdateOneAsync(filter, update);
            }
        }

        public async Task<bool> DeleteScheduledEventAsync(ulong guildId, ulong channelId, IReadingInfo eventInfo)
        {
            Data.ScheduledEvent existing = await GetMatchingEventAsync(guildId, channelId, eventInfo);

            if (existing is null)
            {
                return false;
            }
            else
            {
                var builder = Builders<Data.ScheduledEvent>.Filter;
                var filter = builder.Eq("_id", existing.Id);
                var result = await _events.DeleteOneAsync(filter);
                bool deleted = false;
                if (result.IsAcknowledged)
                {
                    deleted = result.DeletedCount > 0;
                }
                return deleted;
            }
        }

        private async Task<Data.ScheduledEvent> GetMatchingEventAsync(ulong guildId, ulong channelId, IReadingInfo eventInfo)
        {
            var builder = Builders<Data.ScheduledEvent>.Filter;
            var filter = builder.And(
                builder.Eq("GuildId", guildId),
                builder.Eq("ChannelId", channelId)
                );

            List<Data.ScheduledEvent> events = await _events.Find(filter).ToListAsync();

            return events.FirstOrDefault(evt => evt.EventInfo.Equals(eventInfo));
        }

        public async Task<List<Data.ScheduledEvent>> GetGuildEventsAsync(ulong guildId)
        {
            var builder = Builders<Data.ScheduledEvent>.Filter;
            var filter = builder.Eq("GuildId", guildId);
            List<Data.ScheduledEvent> events = await _events.Find(filter).ToListAsync();
            return events;
        }

        public async Task<List<Data.ScheduledEvent>> GetCurrentEventsAsync()
        {
            //get events where the scheduled event time is in the past
            Instant now = _clock.GetCurrentInstant();
            FilterDefinitionBuilder<Data.ScheduledEvent> builder = Builders<Data.ScheduledEvent>.Filter;
            var filter = builder.Lte("EventInstant", now);
            return await _events.Find(filter).ToListAsync();
        }

        private static void LogException(MongoException e)
        {
            LogUtilities.WriteLog(Discord.LogSeverity.Error, e.ToString());
        }
    }

}
