using NodaTime;
using System;

namespace ReadingsBot.Data
{
    public sealed class PostId : IEquatable<PostId>
    {
        public string PostTitle { get; }
        public Instant PostDateTime { get; }

        public PostId(string postTitle, Instant postDateTime)
        {
            PostTitle = postTitle;
            PostDateTime = postDateTime;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PostId id)
            {
                return this.Equals(id);
            }
            else
            {
                return false;
            }
        }

        public bool Equals(PostId other)
        {
            if (other is null)
                return false;
            return this.PostTitle == other.PostTitle && this.PostDateTime.Equals(other.PostDateTime);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine<string, Instant>(PostTitle, PostDateTime);
        }

        public static bool operator ==(PostId left, PostId right)
        {
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(PostId left, PostId right)
        {
            if (left is null || right is null)
                return false;
            return !(left == right);
        }
    }
}
