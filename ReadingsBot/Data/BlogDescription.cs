using System;
using System.Collections.Generic;
using System.Text;

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
            return HashCode.Combine<string,string,string>(BlogName, Author, RssFeedUrl);
        }
    }

    public readonly struct BlogId : IEquatable<BlogId>
    {
        public string BlogName { get; }
        public string Author { get; }

        public BlogId(string blogName, string author)
        {
            BlogName = blogName;
            Author = author;
        }

        public override bool Equals(object? obj)
        {
            if (obj is BlogId id)
            {
                return this.Equals(id);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(BlogId other)
        {
            return this.BlogName == other.BlogName && this.Author == other.Author;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine<string, string>(BlogName, Author);
        }

        public static bool operator ==(BlogId left, BlogId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlogId left, BlogId right)
        {
            return !(left == right);
        }
    }
}
