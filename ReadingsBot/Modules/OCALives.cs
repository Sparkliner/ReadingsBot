using Discord;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReadingsBot.Modules
{
    public static class OCALivesCache
    {
        private static string CacheDirectory { get; }

        private static string CacheFile => Path.Combine(CacheDirectory, "lives.json");

        private static OCALives LocalCache;

        private static DateTimeOffset CacheDate { get; set; }

        private static readonly CultureInfo provider = CultureInfo.InvariantCulture;
        private static readonly TimeSpan timeOffset = new TimeSpan(-5, 0, 0);

        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions();

        static OCALivesCache()
        {
            CacheDirectory = Path.Combine(AppContext.BaseDirectory, "cache/OCA");
            jsonOptions.PropertyNameCaseInsensitive = true;
        }

        private static async Task UpdateCache()
        {
            if (LocalCache is null)
            {
                //load from cached file first, if exists
                if (File.Exists(CacheFile))
                {
                    LogUtilities.WriteLog(LogSeverity.Verbose, "Loading OCA Lives from disk");
                    LoadToCache(File.ReadAllText(CacheFile));
                }
                else
                {
                    await UpdateCacheWeb();
                    return;
                }
            }
            //check that cached file is current
            if (CacheDate.Date != DateTimeOffset.UtcNow.ToOffset(OCALivesCache.timeOffset).Date)
            {
                LogUtilities.WriteLog(LogSeverity.Verbose, "OCA Lives cache is out of date");
                await UpdateCacheWeb();
            }
        }

        private static void LoadToCache(string json_string)
        {
            LocalCache = JsonSerializer.Deserialize<OCALives>(json_string, jsonOptions);
            UpdateCacheDate();
        }

        private static async Task UpdateCacheWeb()
        {
            LogUtilities.WriteLog(LogSeverity.Verbose, "Updating OCA Lives cache from the web");
            //fetch lives from web
            string json_string = await GetLivesWeb();
            //store in memory and update date
            LoadToCache(json_string);
            //store to disk
            if (!Directory.Exists(CacheDirectory))
                Directory.CreateDirectory(CacheDirectory);
            await File.WriteAllTextAsync(CacheFile, json_string);
        }

        private static void UpdateCacheDate()
        {
            CacheDate = DateTimeOffset.ParseExact(LocalCache.date_rfc, "ddd, dd MMM yyyy HH:mm:ss zz'00'", OCALivesCache.provider);
        }

        public static async Task<OCALives> GetLives()
        {
            //test that cache exists and is up to date
            await UpdateCache();
            return LocalCache;
        }

        private static async Task<string> GetLivesWeb()
        {
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://www.oca.org/saints/today.json");
            response.EnsureSuccessStatusCode();

            using var content = response.Content;
            return await response.Content.ReadAsStringAsync();
        }
    }

    public class OCALives
    {
        public string header { get; set; }

        public string link { get; set; }
        public string date { get; set; }
        public string date_full { get; set; }
        public string date_rfc { get; set; }
        public string copyright { get; set; }
        public OCALife[] commemorations { get; set; }
    }

    public class OCALife
    {
        public string title { get; set; }
        public string link { get; set; }
        public string image { get; set; }
        public string thumb { get; set; }
        public string text { get; set; }

        public EmbedBuilder ToEmbedBuilder(Color color)
        {
            string life_text = ParsingUtilities.ParseWebText(text);
            var builder = new EmbedBuilder()
            {
                Color = color,
                Title = title,
                Url = link,
            }
            .AddField(
                "\u200b",
                (life_text.Length <= 256 ? life_text : life_text.Substring(0, 256) + $"...[Read more]({link})"))
            .WithImageUrl(image)
            .WithFooter(footer => footer.Text = ParsingUtilities.ParseWebText("&copy; The Orthodox Church in America (OCA.org)."));
            //just going to hardcode the copyright because I can't be bothered right now

            return builder;
        }
    }
}
