namespace ReadingsBot.Data
{
    public class BlogSubscription
    {
        public BlogId BId { get; private set; }
        public PostId PId { get; set; }

        public BlogSubscription(BlogId bId, PostId pId)
        {
            BId = bId;
            PId = pId;
        }

    }
}
