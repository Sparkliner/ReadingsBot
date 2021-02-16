using MongoDB.Driver;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using NodaTime;

namespace ReadingsBot
{
    public class GuildService
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Data.GuildEntity> _guilds;
        private readonly IConfigurationRoot _config;

        public GuildService(IMongoClient client, IConfigurationRoot config)
        {
            _client = client;
            _config = config;

            try
            {
                _database = _client.GetDatabase(_config["database_id"]);
                _guilds = _database.GetCollection<Data.GuildEntity>("guilds");
                LogUtilities.WriteLog(Discord.LogSeverity.Verbose, "Connected to guild database");
            }
            catch(MongoException e)
            {
                LogException(e);
                throw;
            }
        }

        public async Task<string> GetGuildPrefix(ulong guildId)
        {
            var filter = Builders<Data.GuildEntity>.Filter.Eq("GuildID", guildId);
            Data.GuildEntity guild;
            try
            {
                guild = await _guilds.Find(filter).FirstOrDefaultAsync();
            }
            catch(MongoException e)
            {
                LogException(e);
                throw;
            }
            

            if (guild is null)
            {
                await CreateGuildRecord(guildId);
                return _config["default_prefix"];
            }
            else
            {
                return guild.Prefix;
            }
        }

        private async Task CreateGuildRecord(ulong guildId)
        {
            Data.GuildEntity guild = new Data.GuildEntity { GuildID = guildId, Prefix = _config["default_prefix"] };
            try
            {
                await _guilds.InsertOneAsync(guild);
            }
            catch (MongoException e)
            {
                LogException(e);
                throw;
            }
        }

        public async Task SetGuildTimeZone(ulong guildId, string timeZone)
        {
            var filter = Builders<Data.GuildEntity>.Filter.Eq("GuildID", guildId);
            var update = Builders<Data.GuildEntity>.Update.Set("TimeZone", timeZone);

            await _guilds.UpdateOneAsync(filter, update);
        }

        public async Task<DateTimeZone> GetGuildTimeZone(ulong guildId)
        {
            var filter = Builders<Data.GuildEntity>.Filter.Eq("GuildID", guildId);
            Data.GuildEntity guild;
            try
            {
                guild = await _guilds.Find(filter).FirstOrDefaultAsync();
            }
            catch(MongoException e)
            {
                LogException(e);
                throw;
            }

            if (guild is null)
            {
                return null;
            }
            else
            {
                return guild.TimeZone;
            }
        }

        public async Task SetGuildPrefix(ulong guildId, string newPrefix)
        {
            var filter = Builders<Data.GuildEntity>.Filter.Eq("GuildID", guildId);
            var update = Builders<Data.GuildEntity>.Update.Set("Prefix", newPrefix);
            var options = new UpdateOptions { IsUpsert = true };
            await _guilds.UpdateOneAsync(filter, update, options);
        }

        private static void LogException(MongoException e)
        {
            LogUtilities.WriteLog(Discord.LogSeverity.Error, e.ToString());
        }
    }
}
