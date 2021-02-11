using Discord;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ReadingsBot
{
    public class OCALivesCacheService
    {
        private readonly IConfigurationRoot _config;

        private string JsonCacheDirectory { get; }
        private string ImageCacheDirectory { get; }
        private string JsonFileName { get; }

        private OCALives LocalCache;

        private DateTimeOffset CacheDate { get; set; }

        private readonly CultureInfo provider = CultureInfo.InvariantCulture;
        private readonly TimeSpan timeOffset = new TimeSpan(-5, 0, 0);

        private readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions();

        public OCALivesCacheService(IConfigurationRoot config)
        {
            _config = config;
            jsonOptions.PropertyNameCaseInsensitive = true;
            JsonCacheDirectory = string.Join("/", _config["cache_directory"],"cache/OCA/");
            ImageCacheDirectory = string.Join("/", _config["cache_directory"], "cache/OCA/images");
            JsonFileName = string.Join("/", JsonCacheDirectory, "lives.json");
        }

        public async Task<OCALives> GetLives()
        {
            //test that cache exists and is up to date
            await UpdateCache();
            return LocalCache;
        }

        private async Task UpdateCache()
        {
            if (LocalCache is null)
            {
                //load from cached file first, if exists
                //also check that images are cached
                if (File.Exists(JsonFileName) 
                    && Directory.Exists(ImageCacheDirectory) 
                    && Directory.EnumerateFiles(ImageCacheDirectory).Count() != 0)
                {
                    LogUtilities.WriteLog(LogSeverity.Verbose, "Loading OCA Lives from disk");
                    LoadToCache(File.ReadAllText(JsonFileName));
                }
                else
                {
                    await UpdateCacheWeb();
                    return;
                }
            }
            //check that cached file is current
            if (CacheDate.Date != DateTimeOffset.UtcNow.ToOffset(timeOffset).Date)
            {
                LogUtilities.WriteLog(LogSeverity.Verbose, "OCA Lives cache is out of date");
                await UpdateCacheWeb();
            }
        }

        private void LoadToCache(string json_string)
        {
            LocalCache = JsonSerializer.Deserialize<OCALives>(json_string, jsonOptions);
            UpdateCacheDate();
        }

        private async Task UpdateCacheWeb()
        {
            LogUtilities.WriteLog(LogSeverity.Verbose, "Updating OCA Lives cache from the web");
            //fetch lives from web
            string json_string = await GetLivesWeb();
            //store in memory and update date
            LoadToCache(json_string);
            await DownloadAndCacheImages();
            //store to disk
            if (!Directory.Exists(JsonCacheDirectory))
                Directory.CreateDirectory(JsonCacheDirectory);
            await File.WriteAllTextAsync(
                JsonFileName, 
                JsonSerializer.Serialize(
                    LocalCache,
                    options: new JsonSerializerOptions { WriteIndented = true }
                    )
                );
        }

        private async Task DownloadAndCacheImages()
        {
            LogUtilities.WriteLog(LogSeverity.Verbose, "Caching OCA images to disk");
            ClearImageCache();
            if (!Directory.Exists(ImageCacheDirectory))
                Directory.CreateDirectory(ImageCacheDirectory);
            int i = 0;
            using WebClient client = new WebClient();
            foreach (OCALife life in LocalCache.commemorations)
            {
                string url = life.image;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    string imageFile = String.Join("/", ImageCacheDirectory, $"image_{i}.jpg");
                    await client.DownloadFileTaskAsync(
                        new Uri(url),
                        imageFile
                        );
                    //update this in our class structure as well
                    life.image = imageFile;
                    ++i;
                }
            }
        }

        private void ClearImageCache()
        {
            if (Directory.Exists(ImageCacheDirectory))
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(ImageCacheDirectory);
                foreach (FileInfo file in di.EnumerateFiles())
                {
                    file.Delete();
                }
            }
        }

        private void UpdateCacheDate()
        {
            CacheDate = DateTimeOffset.ParseExact(LocalCache.date_rfc, "ddd, dd MMM yyyy HH:mm:ss zz'00'", provider);
        }

        private async Task<string> GetLivesWeb()
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
        public List<OCALife> commemorations { get; set; }

        public List<EmbedWithImage> GetEmbeds()
        {
            return commemorations.Select(s => s.ToEmbed()).ToList();
        }
    }

    public class OCALife
    {
        public string title { get; set; }
        public string link { get; set; }
        public string image { get; set; }
        public string thumb { get; set; }
        public string text { get; set; }

        public EmbedWithImage ToEmbed()
        {
            EmbedWithImage embed = new EmbedWithImage();
            string life_text = Utilities.TextUtilities.ParseWebText(text);
            embed.ImageFile = image;
            embed.Builder = new EmbedBuilder()
            {
                Title = title,
                Url = link,
            }
            .AddField(
                "\u200b",
                (life_text.Length <= 256 ? life_text : life_text.Substring(0, 256) + $"...[Read more]({link})"))
            .WithFooter(footer => footer.Text = Utilities.TextUtilities.ParseWebText("&copy; The Orthodox Church in America (OCA.org)."));
            //just going to hardcode the copyright because I can't be bothered right now

            if (!string.IsNullOrWhiteSpace(image))
            {
                string ImageName = image.Substring(image.LastIndexOf("image"));
                embed.Builder = embed.Builder.WithImageUrl($"attachment://{ImageName}");
            }

            return embed;
        }
    }

    public class EmbedWithImage
    {
        public EmbedBuilder Builder;
        public string ImageFile;
    }
}
