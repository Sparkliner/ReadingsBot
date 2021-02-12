using Discord;
using System;
using System.Globalization;
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

        private Data.OCALives LocalCache;

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

        public async Task<Data.OCALives> GetLives()
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
                    && Directory.EnumerateFiles(ImageCacheDirectory).Any())
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
            LocalCache = JsonSerializer.Deserialize<Data.OCALives>(json_string, jsonOptions);
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
            foreach (Data.OCALife life in LocalCache.commemorations)
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
            HttpResponseMessage response = await client.GetAsync(_config["oca_uri"]);
            response.EnsureSuccessStatusCode();

            using var content = response.Content;
            return await response.Content.ReadAsStringAsync();
        }
    }
}
