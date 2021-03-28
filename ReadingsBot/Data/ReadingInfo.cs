using System;
using System.Collections.Generic;
using System.Linq;

namespace ReadingsBot.Data
{
    public interface IReadingInfo
    {
        string Description { get; }
    }

    public interface IRssInfo
    {
        string RssFeedUrl { get; }
    }

    public sealed class SaintsLivesReadingInfo : IReadingInfo, IEquatable<SaintsLivesReadingInfo>
    {
        public string Description { get; private set; }
        public SaintsLivesReadingInfo()
        {
            Description = "Lives of the Saints";
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as SaintsLivesReadingInfo);
        }
        public bool Equals(SaintsLivesReadingInfo? other)
        {
            if (other is null)
                return false;
            return true; //all SaintsLivesReadingInfo objects are equal for now
        }
        public override int GetHashCode()
        {
            return Description.GetHashCode();
        }
    }

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

    public sealed class ImageQuoteReadingInfo : IReadingInfo, IEquatable<ImageQuoteReadingInfo>
    {
        public string Description { get; }
        public string ImageLocation { get; }
        public ImageQuoteReadingInfo(string imageLocation)
        {
            Description = "Image Quote";
            ImageLocation = imageLocation;
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as ImageQuoteReadingInfo);
        }
        public bool Equals(ImageQuoteReadingInfo? other)
        {
            if (other is null)
                return false;
            return ImageLocation.Equals(other.ImageLocation);
        }
        public override int GetHashCode()
        {
            return ImageLocation.GetHashCode();
        }
    }
}
