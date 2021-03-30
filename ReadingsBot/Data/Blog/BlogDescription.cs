using System;
using System.Collections.Generic;

namespace ReadingsBot.Data
{
    public sealed class BlogDescription : IRssInfo, IEquatable<BlogDescription>
    {
        public BlogId BId { get; }
        public string BlogName => BId.BlogName;
        public string Author => BId.Author;
        public List<string> Aliases { get; }
        public string RssFeedUrl { get; }

        public BlogDescription(string blogName, string author, string rssurl, List<string> aliases)
        {
            BId = new BlogId(blogName, author);
            RssFeedUrl = rssurl;
            Aliases = aliases;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as BlogDescription);
        }
        public bool Equals(BlogDescription? other)
        {
            if (other is null)
                return false;
            return this.BlogName == other.BlogName && this.Author == other.Author && this.RssFeedUrl == other.RssFeedUrl;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine<string, string, string>(BlogName, Author, RssFeedUrl);
        }
    }
}
