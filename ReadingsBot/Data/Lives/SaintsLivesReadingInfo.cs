using System;

namespace ReadingsBot.Data
{
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
}
