using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public interface IReadingInfo
    { 
        string Description { get; }
    }

    public class SaintsLivesReadingInfo : IReadingInfo, IEquatable<SaintsLivesReadingInfo>
    {
        public string Description { get; }
        public SaintsLivesReadingInfo()
        {
            Description = "Lives of the Saints";
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

    public class ImageQuoteReadingInfo: IReadingInfo, IEquatable<ImageQuoteReadingInfo>
    {
        public string Description { get; }
        public string ImageLocation { get; }
        public ImageQuoteReadingInfo(string imageLocation)
        {
            Description = "Image Quote";
            ImageLocation = imageLocation;
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
