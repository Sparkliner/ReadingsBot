using Discord;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.Text;
using ReadingsBot.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ReadingsBot
{
    public class BlogCacheService : JsonCacheService<Dictionary<string, BlogPost>>
    {
        private readonly HttpClient _httpClient;

        private readonly List<BlogDescription> _allBlogs;

        public BlogCacheService(IConfigurationRoot config, IClock clock, HttpClient httpClient)
            : base(config,
                  clock,
                  "/cache/Blogs/",
                  "latest_posts.json",
                  new JsonSerializerOptions(),
                  DateTimeZoneProviders.Tzdb["UTC"])
        {
            _httpClient = httpClient;
            _allBlogs = new List<BlogDescription>();

            ConfigureBlogList();
        }

        private void ConfigureBlogList()
        {
            string configFile = string.Join("/", _config["data_directory"], "config/blog_list.json");
            string jsonString = File.ReadAllText(configFile);
            using JsonDocument jsonDocument = JsonDocument.Parse(jsonString);

            JsonElement root = jsonDocument.RootElement;
            foreach (JsonElement blogInfo in root.EnumerateArray())
            {
                string blogName = blogInfo.GetProperty("Name").GetString();

                string blogAuthor = blogInfo.GetProperty("Author").GetString();

                List<string> blogAliases = new List<string>();
                foreach (JsonElement alias in blogInfo.GetProperty("Aliases").EnumerateArray())
                {
                    blogAliases.Add(alias.GetString());
                }

                string blogRssUrl = blogInfo.GetProperty("RssFeedUrl").GetString();

                _allBlogs.Add(new BlogDescription(blogName, blogAuthor, blogRssUrl, blogAliases));
            }
        }

        protected override bool IsCacheOutOfDate()
        {
            LocalDateTime now = CacheClock.GetCurrentLocalDateTime();
            return now >= CacheLastUpdatedTime.Plus(Period.FromHours(1));
        }

        protected override void UpdateCacheDate()
        {
            LocalDateTime now = CacheClock.GetCurrentLocalDateTime();
            CacheLastUpdatedTime = now.Date + new LocalTime(now.Hour, 0);
        }

        public List<BlogDescription> GetBlogList()
        {
            return _allBlogs;
        }

        public async Task<List<EmbedBuilder>> GetLatestBlogPostEmbedsAsync(List<BlogId> blogs = null)
        {
            if (blogs is null)
                blogs = _allBlogs.Select(b => b.BId).ToList();

            await UpdateCacheAsync();
            return LocalCache.Values
                .Join(blogs,
                    blogPost => blogPost.BId,
                    blogId => blogId,
                    (blogPost, blogId) => blogPost)
                .Select(blogPost => blogPost.ToEmbed())
                .ToList();
        }

        protected override async Task UpdateCacheWebAsync()
        {
            LocalCache = new Dictionary<string, BlogPost>();
            var groupedBlogs = _allBlogs.GroupBy<BlogDescription, string>(x => x.RssFeedUrl);

            foreach (var group in groupedBlogs)
            {
                LogUtilities.WriteLog(LogSeverity.Verbose, $"Looking at blogs from {group.Key}");
                using HttpResponseMessage response = await _httpClient.GetAsync(group.Key);
                response.EnsureSuccessStatusCode();
                using HttpContent content = response.Content;

                XDocument document = XDocument.Parse(await content.ReadAsStringAsync());

                var blogItems = document.Root.Elements("channel").First()
                    .Elements("item")
                    .Join(group,
                        item => item.Element("category").Value,
                        blogInfo => blogInfo.BlogName,
                        (item, blogInfo) =>
                            item);

                foreach (var blogItem in blogItems)
                {
                    string blogName = blogItem.Element("category").Value;
                    OffsetDateTime blogPostDateTime = OffsetDateTimePattern
                        .CreateWithInvariantCulture("ddd, dd MMM uuuu HH:mm:ss o<M>")
                        .Parse(blogItem.Element("pubDate").Value).Value;

                    //check that we don't already have a later post
                    if (LocalCache.ContainsKey(blogName) && OffsetDateTime.Comparer.Instant.Compare(LocalCache[blogName].PostDateTime, blogPostDateTime) >= 0)
                    {
                        continue;
                    }

                    string blogDescription = blogItem.Element("description").Value;
                    string blogLink = blogItem.Element("link").Value;
                    string blogImageUrl = blogItem.Element("image")?.Value;
                    if (blogImageUrl is null)
                    {
                        //try to get from meta tag
                        blogImageUrl = await TryGetImageFromPageAsync(blogLink);
                    }
                    BlogPost blogPost = new BlogPost( 
                        blogName: blogName,
                        author: blogItem.Element(blogItem.GetNamespaceOfPrefix("dc") + "creator").Value,
                        postTitle: blogItem.Element("title").Value,
                        postDateTime: blogPostDateTime,
                        authorImageUrl: blogItem.Element(blogItem.GetNamespaceOfPrefix("media") + "content").Attribute("url").Value,
                        postImageUrl: blogImageUrl,
                        link: blogLink,
                        description: Utilities.TextUtilities.ParseWebText(blogDescription));

                    LocalCache[blogName] = blogPost;
                }
            }
            await WriteCacheToDiskAsync();
        }

        private async Task<string> TryGetImageFromPageAsync(string blogLink)
        {
            using HttpResponseMessage blogResponse = await _httpClient.GetAsync(blogLink);
            blogResponse.EnsureSuccessStatusCode();
            using HttpContent blogContent = blogResponse.Content;
            //hardcoding this might be a bad idea some day
            string source = Encoding.UTF8.GetString(await blogContent.ReadAsByteArrayAsync());
            source = WebUtility.HtmlDecode(source);
            HtmlDocument blogDocument = new HtmlDocument();
            blogDocument.LoadHtml(source);
            return blogDocument
                .DocumentNode
                .Descendants()
                .First(x => x.Name == "meta" && x.Attributes["property"]?.Value == "og:image")
                .GetAttributeValue("content", "");
    }
    }
}
