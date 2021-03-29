using System;

namespace ReadingsBot.Data
{
    public sealed class BlogId : IEquatable<BlogId>
    {
        public string BlogName { get; }
        public string Author { get; }

        public BlogId(string blogName, string author)
        {
            BlogName = blogName;
            Author = author;
        }

        public override string ToString()
        {
            return $"{BlogName} by {Author}";
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
            if (other is null)
                return false;
            return this.BlogName == other.BlogName && this.Author == other.Author;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine<string, string>(BlogName, Author);
        }

        public static bool operator ==(BlogId left, BlogId right)
        {
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(BlogId left, BlogId right)
        {
            if (left is null || right is null)
                return false;
            return !(left == right);
        }
    }
}
