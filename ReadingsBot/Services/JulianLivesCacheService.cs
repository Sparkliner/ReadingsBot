using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.Text;
using ReadingsBot.Data;
using ReadingsBot.Utilities;

namespace ReadingsBot
{
    public class JulianLivesCacheService : JsonCacheService<JulianLives>, ILivesCacheService
    {
        private readonly HttpClient _httpClient;

        private string ImageCacheDirectory { get; }

        public JulianLivesCacheService(IConfigurationRoot config, IClock clock, HttpClient httpClient)
    : base(config,
          clock,
          "cache/HTOC/",
          "lives.json",
          new JsonSerializerOptions() { PropertyNameCaseInsensitive = true },
          DateTimeZoneProviders.Tzdb["UTC"])
        {
            _httpClient = httpClient;
            JsonOptions.PropertyNameCaseInsensitive = true;
            ImageCacheDirectory = string.Join("/", _config["data_directory"], "cache/HTOC/images");
        }

        public List<EmbedWithImage> GetLives()
        {
            //test that cache exists and is up to date
            lock (CacheLock)
            {
                UpdateCacheAsync().Wait();
            }
            return LocalCache.GetEmbeds();
        }

        private async Task DownloadAndCacheImages()
        {
            LogUtilities.WriteLog(LogSeverity.Verbose, "Caching HTOC images to disk");
            ClearImageCache();
            if (!Directory.Exists(ImageCacheDirectory))
                Directory.CreateDirectory(ImageCacheDirectory);
            int i = 0;
            foreach (ISaintsLife life in LocalCache.Commemorations)
            {
                string url = life.ImageUrl;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    string imageFile = string.Join("/", ImageCacheDirectory, $"image_{i}.jpg");

                    using HttpResponseMessage response = await _httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    using var inStream = await response.Content.ReadAsStreamAsync();

                    using var fileStream = File.Create(imageFile);
                    inStream.Seek(0, SeekOrigin.Begin);
                    await inStream.CopyToAsync(fileStream);

                    //update this in our class structure as well
                    life.ImageUrl = imageFile;
                    ++i;
                }
            }
        }

        private void ClearImageCache()
        {
            if (Directory.Exists(ImageCacheDirectory))
            {
                System.IO.DirectoryInfo di = new(ImageCacheDirectory);
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
                .CreateWithInvariantCulture("G")
                .Parse(LocalCache.DateWithOffset).Value
                .LocalDateTime;
        }

        protected override async Task UpdateCacheWebAsync()
        {
            LogUtilities.WriteLog(LogSeverity.Verbose, "Updating HTOC Lives cache from the web");
            //fetch lives from web
            List<JulianLife> commemorations = await GetLivesWeb();
            //store in memory and update date
            OffsetDateTime now = CacheClock.GetCurrentOffsetDateTime();
            LocalCache = new JulianLives("", "", now.ToString("G", CultureInfo.InvariantCulture), commemorations);
            await DownloadAndCacheImages().ContinueWith(task => WriteCacheToDiskAsync()); 
        }
        private async Task<List<JulianLife>> GetLivesWeb()
        {
            HtmlDocument livesDocument = await GetHtmlDocument($"{_config["htoc_uri"]}?dt=0&header=0&lives=1&trp=0&scripture=1");

            var lives = livesDocument.DocumentNode.SelectSingleNode("//span").ChildNodes;

            List<JulianLife> commemorations = new();

            StringBuilder name = new();
            List<string> links = new();
            foreach(HtmlNode node in lives)
            {
                if (node.Name == "img" || node.PreviousSibling.Name == "br")
                {
                    continue;
                }
                else if (node.Name == "a")
                {
                    name.Append(node.InnerText);
                    links.Add(node.GetAttributeValue("href", ""));
                }
                else if (node.Name == "br")
                {
                    if (links.Any())
                    {
                        foreach(string url in links)
                        {
                            commemorations.Add(await GetLifeFromLink(url));
                        }
                    }
                    else
                    {
                        commemorations.Add(new JulianLife(name.ToString()));
                    }
                    name.Clear();
                    links.Clear();
                }
                else
                {
                    name.Append(node.InnerText);
                }          
            }
            return commemorations;
        }

        private async Task<JulianLife> GetLifeFromLink(string url)
        {
            HtmlDocument lifeDocument = await GetHtmlDocument(url);
            var node = lifeDocument.DocumentNode.FirstChild;

            string commemoration = node.Element("head").Element("title").InnerText;
            string imageRelative = node.Element("body").SelectSingleNode("//img")?.GetAttributeValue("src", "");
            Uri imageUri = new(new Uri(url, UriKind.Absolute), imageRelative);

            string content = node.Element("body").SelectNodes("//p[@class='body10']").ElementAt(1).InnerHtml;

            return new JulianLife(
                commemoration, 
                url, 
                (imageRelative is null) ? "" : imageUri.ToString(),
                content);
        }

        private async Task<HtmlDocument> GetHtmlDocument(string url)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var content = response.Content;
            string htmlSource = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());
            htmlSource = WebUtility.HtmlDecode(htmlSource);
            HtmlDocument document = new();
            document.LoadHtml(htmlSource);
            return document;
        }
    }
}
