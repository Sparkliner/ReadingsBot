using System;
using System.Collections.Generic;

namespace ReadingsBot.Data
{
    public sealed class BlogsReadingInfo : IReadingInfo, IEquatable<BlogsReadingInfo>
    {
        public string Description { get; private set; }
        public string Blogs => "\n\u2003" + string.Join("\n\u2003", Subscriptions.Select(sub => sub.BId.ToString()));
        public List<BlogSubscription> Subscriptions { get; set; }

        public BlogsReadingInfo()
        {
            Description = "Subscribed blogs";
            Subscriptions = new List<BlogSubscription>();
        }
        public override bool Equals(object? obj)
        {
            return this.Equals(obj as BlogsReadingInfo);
        }
        public bool Equals(BlogsReadingInfo? other)
        {
            if (other is null)
                return false;
            return this.Description == other.Description;
        }
        public override int GetHashCode()
        {
            return Description.GetHashCode();
        }
    }
}
