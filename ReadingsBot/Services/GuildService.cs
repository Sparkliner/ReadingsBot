using MongoDB.Driver;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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
                _database = client.GetDatabase("readingsbot");
                _guilds = _database.GetCollection<Data.GuildEntity>("guilds");
            }
            catch(MongoException e)
            {
                LogException(e);
                throw e;
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
                throw e;
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
                throw e;
            }
        }

        public async Task SetGuildPrefix(ulong guildId, string newPrefix)
        {
            var filter = Builders<Data.GuildEntity>.Filter.Eq("GuildID", guildId);
            var update = Builders<Data.GuildEntity>.Update.Set("Prefix", newPrefix);
            var options = new UpdateOptions { IsUpsert = true };
            await _guilds.UpdateOneAsync(filter, update, options);
        }

        private void LogException(MongoException e)
        {
            LogUtilities.WriteLog(Discord.LogSeverity.Error, e.ToString());
        }
    }
}
