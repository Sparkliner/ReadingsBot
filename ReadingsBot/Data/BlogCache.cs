using NodaTime;
using System.Collections.Generic;

namespace ReadingsBot.Data
{
    public class BlogCache
    {
        public ZonedDateTime LastUpdated { get; set; }
        public Dictionary<string, List<BlogPost>> Cache { get; set; }
        public BlogCache() => Cache = new Dictionary<string, List<BlogPost>>();
    }
}
