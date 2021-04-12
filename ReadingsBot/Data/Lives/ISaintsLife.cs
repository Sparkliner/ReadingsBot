namespace ReadingsBot.Data
{
    public interface ISaintsLife
    {
        string ImageUrl { get; set; }
        string Link { get; }
        string Content { get; }
        string ThumbnailUrl { get; }
        string Name { get; }

        EmbedWithImage ToEmbed();
    }
}