using Discord;
using Microsoft.Extensions.Configuration;
using NodaTime;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public abstract class JsonCacheService<T>
    {
        protected IConfigurationRoot _config;
        protected string JsonCacheDirectory { get; }
        protected string JsonFileName { get; }

        protected JsonSerializerOptions JsonOptions { get; }

        protected LocalDateTime CacheLastUpdatedTime { get; set; }
        protected DateTimeZone CacheTimeZone { get; set; }

        protected ZonedClock CacheClock { get; }

        protected T LocalCache;

        protected JsonCacheService(IConfigurationRoot config, IClock clock, string cacheDir, string cacheFile, JsonSerializerOptions options, DateTimeZone timeZone)
        {
            _config = config;
            JsonCacheDirectory = string.Join("/", _config["data_directory"], cacheDir);
            JsonFileName = string.Join("/", JsonCacheDirectory, cacheFile);
            JsonOptions = options;
            JsonOptions.WriteIndented = true;
            CacheTimeZone = timeZone;
            CacheClock = new ZonedClock(clock, timeZone, CalendarSystem.Iso);
        }

        protected virtual async Task UpdateCacheAsync()
        {
            if (LocalCache is null)
            {
                //load from cached file first, if exists
                if (File.Exists(JsonFileName))
                {
                    LogUtilities.WriteLog(LogSeverity.Verbose, "Loading cache from disk");
                    LoadToCache(File.ReadAllText(JsonFileName));
                    UpdateCacheDate();
                }
                else
                {
                    await UpdateCacheWebAsync();
                    return;
                }
            }
            if (IsCacheOutOfDate())
            {
                LogUtilities.WriteLog(LogSeverity.Verbose, "Cache is out of date");
                await UpdateCacheWebAsync();
            }

            UpdateCacheDate();
        }

        protected abstract bool IsCacheOutOfDate();

        protected abstract void UpdateCacheDate();

        protected virtual void LoadToCache(string json_string)
        {
            LocalCache = JsonSerializer.Deserialize<T>(json_string, JsonOptions);
        }

        protected async Task WriteCacheToDiskAsync()
        {
            if (!Directory.Exists(JsonCacheDirectory))
                Directory.CreateDirectory(JsonCacheDirectory);
            await File.WriteAllTextAsync(
                JsonFileName,
                JsonSerializer.Serialize(
                    LocalCache,
                    JsonOptions));
        }

        protected abstract Task UpdateCacheWebAsync();
    }
}
