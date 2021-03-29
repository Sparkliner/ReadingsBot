using System;

namespace ReadingsBot.Data
{
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
