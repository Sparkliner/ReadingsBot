using Discord;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.Text;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public class OcaLivesCacheService : JsonCacheService<Data.OcaLives>
    {
        private readonly HttpClient _httpClient;

        private string ImageCacheDirectory { get; }

        public OcaLivesCacheService(IConfigurationRoot config, IClock clock, HttpClient httpClient)
            : base(config,
                  clock,
                  "cache/OCA/",
                  "lives.json",
                  new JsonSerializerOptions() { PropertyNameCaseInsensitive = true },
                  DateTimeZoneProviders.Tzdb["America/Detroit"])
        {
            _httpClient = httpClient;
            JsonOptions.PropertyNameCaseInsensitive = true;
            ImageCacheDirectory = string.Join("/", _config["data_directory"], "cache/OCA/images");
        }

        public async Task<Data.OcaLives> GetLivesAsync()
        {
            //test that cache exists and is up to date
            await UpdateCacheAsync();
            return LocalCache;
        }

        protected override async Task UpdateCacheWebAsync()
        {
            LogUtilities.WriteLog(LogSeverity.Verbose, "Updating OCA Lives cache from the web");
            //fetch lives from web
            string json_string = await GetLivesWeb();
            //store in memory and update date
            LoadToCache(json_string);
            Task imageCacheTask = DownloadAndCacheImages();
            //store to disk
            Task writeDiskTask = WriteCacheToDiskAsync();
            await Task.WhenAll(imageCacheTask, writeDiskTask);
        }

        private async Task DownloadAndCacheImages()
        {
            LogUtilities.WriteLog(LogSeverity.Verbose, "Caching OCA images to disk");
            ClearImageCache();
            if (!Directory.Exists(ImageCacheDirectory))
                Directory.CreateDirectory(ImageCacheDirectory);
            int i = 0;
            foreach (Data.OcaLife life in LocalCache.commemorations)
            {
                string url = life.image;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    string imageFile = String.Join("/", ImageCacheDirectory, $"image_{i}.jpg");

                    using HttpResponseMessage response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    using var inStream = await response.Content.ReadAsStreamAsync();

                    using var fileStream = File.Create(imageFile);
                    inStream.Seek(0, SeekOrigin.Begin);
                    await inStream.CopyToAsync(fileStream);

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

        protected override bool IsCacheOutOfDate()
        {
            LocalDate today = CacheClock.GetCurrentDate();

            return today > CacheLastUpdatedTime.Date;
        }

        protected override void UpdateCacheDate()
        {
            CacheLastUpdatedTime = OffsetDateTimePattern
                .CreateWithInvariantCulture("ddd, dd MMM uuuu HH:mm:ss o<M>")
                .Parse(LocalCache.date_rfc).Value
                .LocalDateTime;
        }

        private async Task<string> GetLivesWeb()
        {
            LogUtilities.WriteLog(
                LogSeverity.Debug,
                $"Using HTTP user-agent: {_httpClient.DefaultRequestHeaders.UserAgent}");
            using HttpResponseMessage response = await _httpClient.GetAsync(_config["oca_uri"]);
            response.EnsureSuccessStatusCode();

            using var content = response.Content;
            return await response.Content.ReadAsStringAsync();
        }
    }
}
